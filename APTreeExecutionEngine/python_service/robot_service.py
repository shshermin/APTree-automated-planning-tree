#!/usr/bin/env python3
"""
Robot Execution Service
REST API for sending move and gripper commands to the UR10 robot.
Runs locally on the PC that has network access to the robot.

Usage:
    python robot_service.py
    → Starts on http://localhost:5001
"""

from flask import Flask, request, jsonify
import os
import sys
import json
import time
import math
import threading
import requests as http_requests

app = Flask(__name__)

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(SCRIPT_DIR, "ur10_control"))
from ur10_commands import move_to_pose, move_to_pose_l, move_to_pose_p, move_to_pose_c, set_digital_out_sequence
from ur10_commands import play_program, dashboard_command, set_payload, set_tcp

DEFAULT_ROBOT_IP = "192.168.1.100"
MOVEIT_BRIDGE_URL = "http://127.0.0.1:5002"
EXTERNAL_CONTROL_NAILGUN = "external_control_n.urp"
EXTERNAL_CONTROL_GRIPPER = "external_control_g.urp"
SUPPORTED_MOVE_TYPES = ['movej', 'movel', 'movep', 'movec', 'planned']


class RobotState:
    """Thread-safe container for active tool state.

    Set by /play_program (equip/deequip), re-read by /move before every command.
    All access is serialised through an internal lock so concurrent Flask
    requests (unlikely with a single BT executor, but possible) cannot race.
    """

    def __init__(self):
        self._lock = threading.Lock()
        self._tcp = None            # list [x, y, z, rx, ry, rz] or None
        self._payload = None        # float (kg) or None
        self._payload_cog = None    # list [cx, cy, cz] or None
        self._end_effector = 'gripper'  # currently equipped tool

    def update(self, tcp=None, payload=None, payload_cog=None):
        """Atomically update all tool settings at once."""
        with self._lock:
            self._tcp = tcp
            self._payload = payload
            self._payload_cog = payload_cog

    def set_end_effector(self, ee: str):
        """Store the currently equipped end effector."""
        with self._lock:
            self._end_effector = ee

    def get_end_effector(self) -> str:
        """Return the currently equipped end effector."""
        with self._lock:
            return self._end_effector

    def snapshot(self):
        """Return a consistent (tcp, payload, payload_cog) tuple."""
        with self._lock:
            return self._tcp, self._payload, self._payload_cog

    def reapply(self, robot_ip: str):
        """Re-send the stored payload and TCP to the robot.

        Call this AFTER any .urp program starts (because loading a .urp resets
        the controller to pendant/installation defaults), and BEFORE any raw
        URScript command that moves the robot.
        """
        tcp, payload, payload_cog = self.snapshot()
        if payload is not None:
            msg = set_payload(robot_ip, payload, cog=payload_cog)
            print(f"Reapplied payload: {payload} kg, COG: {payload_cog} -> {msg}")
        if tcp is not None:
            msg = set_tcp(robot_ip, tcp)
            print(f"Reapplied TCP: {tcp} -> {msg}")


robot_state = RobotState()


@app.route('/health', methods=['GET'])
def health():
    return jsonify({'status': 'ok', 'service': 'robot_execution', 'robotIp': DEFAULT_ROBOT_IP})


@app.route('/gripper', methods=['POST'])
def robot_gripper():
    """Forward gripper command to the MoveIt bridge (ROS IO service).

    URScript via port 30002 is ignored while External Control holds the
    fieldbus lock, so commands must go through the ROS driver instead.
    """
    try:
        data = request.json
        print(f"Received gripper request: {json.dumps(data, indent=2)}")
        start_time = time.time()
        resp = http_requests.post(f"{MOVEIT_BRIDGE_URL}/gripper", json=data, timeout=15)
        resp_data = resp.json()
        elapsed = time.time() - start_time
        print(f"Gripper command completed: {resp_data.get('message', '')} ({elapsed:.2f}s)")
        if resp_data.get('success'):
            return jsonify({'success': True, 'message': resp_data.get('message', ''), 'executionTimeSeconds': elapsed})
        return jsonify({'success': False, 'error': resp_data.get('error', 'Gripper command failed'), 'executionTimeSeconds': elapsed}), 500
    except Exception as e:
        print(f"Gripper command failed: {str(e)}")
        return jsonify({'success': False, 'error': str(e), 'executionTimeSeconds': 0}), 500


@app.route('/lift', methods=['POST'])
def robot_lift():
    """Lift the TCP straight up from the current pose."""
    try:
        data = request.json
        print(f"Received lift request: {json.dumps(data, indent=2)}")

        robot_ip = data.get('robotIp', DEFAULT_ROBOT_IP)
        height = data.get('height', 0.1)

        from ur10_control.ur10_commands import lift_z
        start_time = time.time()
        result_msg = lift_z(robot_ip, height=height)
        elapsed = time.time() - start_time

        print(f"Lift completed: {result_msg} ({elapsed:.2f}s)")
        return jsonify({
            'success': True,
            'message': result_msg,
            'executionTimeSeconds': elapsed
        })
    except Exception as e:
        print(f"Lift command failed: {str(e)}")
        return jsonify({
            'success': False,
            'error': str(e),
            'executionTimeSeconds': 0
        }), 500


@app.route('/play_program', methods=['POST'])
def robot_play_program():
    """Execute a .urp program on the UR10 robot.

    Expected JSON body:
        programName  (str)   — name of the program (e.g. "equipdemo.urp")
        robotIp      (str)   — robot IP (optional, default 192.168.1.100)
        speed        (int)   — speed slider percentage (optional, default 30)
        payload      (float) — payload mass in kg to set after program finishes (optional)
    """
    try:
        data = request.json
        print(f"Received play_program request: {json.dumps(data, indent=2)}")

        program_name = data.get('programName')
        if not program_name:
            return jsonify({'success': False, 'error': 'programName is required'}), 400

        robot_ip = data.get('robotIp', DEFAULT_ROBOT_IP)
        speed = data.get('speed', 30)
        payload_mass = data.get('payload')
        payload_cog = data.get('payloadCog')

        start_time = time.time()
        # Equip/deequip programs may be rejected while the robot is still settling —
        # retry up to 5 times.  External Control and other persistent programs must
        # NOT be retried: the retry loop re-sends setUserRole/setSpeedSlider and
        # the wait-for-completion poll would hang because EC never stops itself.
        is_deequip_program = 'deequip' in program_name.lower()
        is_equip_program = 'equip' in program_name.lower() and not is_deequip_program

        # Equip/deequip programs cannot run while EC holds the fieldbus lock —
        # stop EC first so the dashboard can load and play the new program.
        if is_equip_program or is_deequip_program:
            state = dashboard_command(robot_ip, "programState")
            if "PLAYING" in state.upper():
                print(f"Stopping EC before running '{program_name}'")
                dashboard_command(robot_ip, "stop")
                time.sleep(1.0)  # let the controller release the fieldbus

        result_msg = play_program(robot_ip, program_name, speed=speed,
                                  max_retries=5 if (is_equip_program or is_deequip_program) else 1)

        # Check if the program actually failed to play
        if result_msg and ("File not found" in result_msg or "Failed" in result_msg):
            elapsed = time.time() - start_time
            print(f"Program '{program_name}' FAILED: {result_msg} ({elapsed:.2f}s)")
            return jsonify({
                'success': False,
                'error': result_msg,
                'programName': program_name,
                'executionTimeSeconds': elapsed
            }), 500

        # Set TCP and payload after program completes, and remember them
        tcp_values = data.get('tcp')
        tcp_msg = None
        if tcp_values is not None:
            tcp_msg = set_tcp(robot_ip, tcp_values)
            print(f"TCP set and stored: {tcp_msg}")

        payload_msg = None
        payload_val = float(payload_mass) if payload_mass is not None else None
        if payload_val is not None:
            payload_msg = set_payload(robot_ip, payload_val, cog=payload_cog)
            print(f"Payload set and stored: {payload_msg}")

        # Atomically update stored state for subsequent /move commands
        robot_state.update(
            tcp=tcp_values,
            payload=payload_val,
            payload_cog=payload_cog
        )

        # Auto-start the correct EC program after equip/deequip so the 4s settle
        # is paid here rather than on the first move, and so that EC loading does
        # not clobber the payload we just set above.
        end_effector_type = data.get('endEffectorType')
        if is_equip_program and end_effector_type:
            # Track the newly equipped tool so /move commands pick up the right EC
            robot_state.set_end_effector(end_effector_type)
            # Give the controller time to fully settle after the equip program stops,
            # then start EC matching the newly equipped tool.
            print(f"Auto-starting EC '{end_effector_type}' after equip program '{program_name}'")
            time.sleep(0.5)
            _ensure_ec_running(robot_ip, end_effector_type)
        elif is_deequip_program:
            # Track that the tool has been removed (back to gripper)
            robot_state.set_end_effector('gripper')
            # Do NOT start EC here — the equip that follows will start the right one.
            print(f"Deequip complete: end effector reset to 'gripper', EC will start on next equip/move")

        elapsed = time.time() - start_time

        print(f"Program '{program_name}' completed: {result_msg} ({elapsed:.2f}s)")
        response = {
            'success': True,
            'message': result_msg,
            'programName': program_name,
            'executionTimeSeconds': elapsed
        }
        if tcp_msg:
            response['tcp'] = tcp_msg
        if payload_msg:
            response['payload'] = payload_msg
        return jsonify(response)
    except Exception as e:
        print(f"Play program failed: {str(e)}")
        return jsonify({
            'success': False,
            'error': str(e),
            'executionTimeSeconds': 0
        }), 500


def _ensure_ec_running(robot_ip, end_effector='gripper'):
    """Ensure the correct External Control URCap program is running.

    Reuses it if already PLAYING with the correct program (no restart overhead).
    Stops and reloads if the wrong EC program is running (tool switch scenario).
    Loads and plays fresh if nothing is running.
    The 4s settle is only paid once per tool session, not per request.
    """
    ext_program = EXTERNAL_CONTROL_NAILGUN if end_effector == 'nailgun' else EXTERNAL_CONTROL_GRIPPER

    state = dashboard_command(robot_ip, "programState")
    if "PLAYING" in state.upper():
        # Check which program is actually loaded — guard against wrong EC being active
        loaded = dashboard_command(robot_ip, "get loaded program")
        # Response is e.g. "Loaded program: /programs/external_control_g.urp"
        if ext_program.lower() in loaded.lower():
            print(f"External Control '{ext_program}' already running — skipping load/play")
            return
        # Wrong EC program is playing (tool was switched) — stop and reload
        print(f"Wrong EC program running (expected '{ext_program}', got '{loaded}') — reloading")
        dashboard_command(robot_ip, "stop")
        time.sleep(0.5)

    print(f"Loading External Control program: {ext_program}")
    load_resp = dashboard_command(robot_ip, f"load {ext_program}")
    if "File not found" in load_resp:
        raise RuntimeError(f"External Control program not found: {ext_program}")
    for attempt in range(1, 4):
        play_resp = dashboard_command(robot_ip, "play")
        if "Failed" not in play_resp and "Rejected" not in play_resp:
            break
        print(f"EC play attempt {attempt} rejected ({play_resp!r}), retrying in 3s")
        time.sleep(3.0)
    waited = 0.0
    while waited < 15.0:
        time.sleep(0.5)
        waited += 0.5
        state = dashboard_command(robot_ip, "programState")
        if "PLAYING" in state.upper():
            print(f"External Control program running after {waited:.1f}s")
            break
    time.sleep(1.5)  # settle: URCap <-> ROS2 driver handshake (paid once per tool session)
    # NOTE: do NOT call robot_state.reapply() here. set_payload/set_tcp use URScript
    # (port 30002) which preempts and kills the EC program on e-Series robots.


@app.route('/move', methods=['POST'])
def robot_move():
    """Send a move command to the UR10 robot.

    Expected JSON body:
        commandType     (str)   — "movej", "movel", "movep", or "movec"
        finalPosition   (str)   — named target position
        robotIp         (str)   — robot IP (optional, default 192.168.1.100)
        velocity        (float) — velocity (optional)
        acceleration    (float) — acceleration (optional)
        joints          (list)  — joint angles [j1..j6] (optional, bypasses positions.json)
        pose            (list)  — TCP pose [x,y,z,rx,ry,rz] (optional, bypasses positions.json)
    """
    try:
        data = request.json
        print(f"Received robot move request: {json.dumps(data, indent=2)}")

        command_type = data.get('commandType', 'movej').lower()
        if command_type not in SUPPORTED_MOVE_TYPES:
            return jsonify({'success': False, 'error': f"Unsupported commandType '{command_type}'. Supported: {SUPPORTED_MOVE_TYPES}"}), 400

        final_position = data.get('finalPosition')
        if not final_position:
            return jsonify({'success': False, 'error': 'finalPosition is required'}), 400

        robot_ip = data.get('robotIp', DEFAULT_ROBOT_IP)
        velocity = data.get('velocity')
        acceleration = data.get('acceleration')

        inline_joints = data.get('joints')
        inline_pose = data.get('pose')
        position_data = None
        if inline_joints or inline_pose:
            position_data = {}
            if inline_joints:
                position_data['joints'] = inline_joints
            if inline_pose:
                position_data['pose'] = inline_pose
            print(f"Using inline position data: {position_data}")

        start_time = time.time()

        # Re-apply stored TCP/payload with every move command (thread-safe snapshot)
        tcp_for_move, payload_for_move, payload_cog_for_move = robot_state.snapshot()
        if tcp_for_move:
            print(f"Re-applying stored TCP: {tcp_for_move}")
        if payload_for_move:
            print(f"Re-applying stored payload: {payload_for_move} kg, COG: {payload_cog_for_move}")

        if command_type == 'movej':
            # Joint-space move via MoveIt RRTConnect — EC must be running
            joints = inline_joints
            if not joints or len(joints) < 6:
                return jsonify({'success': False, 'error': 'joints [j1..j6] required for movej'}), 400
            end_effector = data.get('endEffectorType') or robot_state.get_end_effector()
            _ensure_ec_running(robot_ip, end_effector)
            vel_val = velocity if velocity is not None else 0.3
            acc_val = acceleration if acceleration is not None else 0.3
            moveit_payload = {
                'joints': joints,
                'end_effector_type': end_effector,
                'velocity': vel_val,
                'acceleration': acc_val,
            }
            print(f"Forwarding movej to /move_joints: {moveit_payload}")
            resp = http_requests.post(f"{MOVEIT_BRIDGE_URL}/move_joints", json=moveit_payload, timeout=60)
            resp_data = resp.json()
            if resp_data.get('success'):
                result_msg = f"Joint-space move succeeded ({resp_data.get('executionTimeSeconds', 0):.2f}s)"
            else:
                return jsonify({'success': False, 'error': resp_data.get('error', 'movej failed')}), 500

        elif command_type == 'movel':
            # Cartesian linear move via Pilz LIN — EC must be running
            pose = inline_pose
            if not pose or len(pose) < 6:
                return jsonify({'success': False, 'error': 'pose [x,y,z,rx,ry,rz] required for movel'}), 400
            end_effector = data.get('endEffectorType') or robot_state.get_end_effector()
            _ensure_ec_running(robot_ip, end_effector)
            vel_val = velocity if velocity is not None else 0.15
            acc_val = acceleration if acceleration is not None else 0.15
            moveit_payload = {
                'pose': pose,
                'end_effector_type': end_effector,
                'velocity': vel_val,
                'acceleration': acc_val,
            }
            print(f"Forwarding movel to /move_lin: {moveit_payload}")
            resp = http_requests.post(f"{MOVEIT_BRIDGE_URL}/move_lin", json=moveit_payload, timeout=60)
            resp_data = resp.json()
            if resp_data.get('success'):
                result_msg = f"Pilz LIN move succeeded ({resp_data.get('executionTimeSeconds', 0):.2f}s)"
            else:
                return jsonify({'success': False, 'error': resp_data.get('error', 'movel failed')}), 500

        elif command_type == 'movep':
            result_msg = move_to_pose_p(
                robot_ip=robot_ip, name=final_position, position=position_data,
                velocity=velocity if velocity is not None else 0.25,
                acceleration=acceleration if acceleration is not None else 1.2,
                tcp=tcp_for_move, payload=payload_for_move, payload_cog=payload_cog_for_move,
            )
        elif command_type == 'movec':
            initial_position = data.get('initialPosition')
            if not initial_position:
                return jsonify({'success': False, 'error': 'initialPosition (via-point) is required for movec'}), 400
            result_msg = move_to_pose_c(
                robot_ip=robot_ip, via_name=initial_position, end_name=final_position,
                velocity=velocity if velocity is not None else 0.25,
                acceleration=acceleration if acceleration is not None else 1.2,
                tcp=tcp_for_move, payload=payload_for_move, payload_cog=payload_cog_for_move,
            )
        elif command_type == 'planned':
            # Forward to MoveIt bridge service running in WSL
            pose = inline_pose
            if not pose or len(pose) < 6:
                return jsonify({'success': False, 'error': 'pose [x,y,z,rx,ry,rz] is required for planned moves'}), 400

            # Determine which external_control program to use based on end effector
            end_effector = data.get('endEffectorType') or robot_state.get_end_effector()

            # Ensure external_control is running (reuse if already PLAYING)
            _ensure_ec_running(robot_ip, end_effector)

            # Step 2: Convert rotation vector (rx, ry) to yaw in degrees
            rx, ry = pose[3], pose[4]
            half_theta = math.atan2(ry, rx)
            yaw_deg = 2.0 * half_theta * (180.0 / math.pi)

            moveit_payload = {
                'x': pose[0],
                'y': pose[1],
                'z': pose[2],
                'end_effector_type': end_effector,
                'no_object': True,
                'both_loaded': True,
            }

            if end_effector != 'nailgun':
                moveit_payload['yaw'] = round(yaw_deg, 2)
            print(f"Forwarding to MoveIt bridge: {json.dumps(moveit_payload, indent=2)}")
            resp = http_requests.post(f"{MOVEIT_BRIDGE_URL}/plan_and_execute", json=moveit_payload, timeout=130)
            resp_data = resp.json()
            print(f"MoveIt response: {json.dumps(resp_data, indent=2)}")

            # external_control.urp stays running — the lift is now handled
            # inside move_to_task.py via MoveIt, so no need to stop/reload.

            if resp_data.get('success'):
                # Verify robot isn't in protective stop after MoveIt execution
                from ur10_control.ur10_commands import check_safety_mode
                is_safe, safety_msg = check_safety_mode(robot_ip)
                if not is_safe:
                    print(f"Robot safety issue after MoveIt execution: {safety_msg}")
                    return jsonify({'success': False, 'error': f"Robot safety issue after MoveIt execution: {safety_msg}"}), 500
                result_msg = resp_data.get('message', 'MoveIt execution completed')
                # Forward MoveIt timing breakdown
                moveit_motion_time = resp_data.get('motionTimeSeconds', 0)
                moveit_setup_time = resp_data.get('setupTimeSeconds', 0)
                moveit_lift_time = resp_data.get('liftTimeSeconds', 0)
            else:
                error_msg = resp_data.get('error', 'MoveIt execution failed')
                print(f"MoveIt FAILED: {error_msg}")
                return jsonify({'success': False, 'error': error_msg}), 500

        elapsed = time.time() - start_time

        print(f"Move completed: {result_msg} ({elapsed:.2f}s)")
        response_data = {
            'success': True,
            'message': result_msg,
            'executionTimeSeconds': elapsed
        }
        # Include MoveIt timing breakdown for planned moves
        if command_type == 'planned':
            response_data['planningTimeSeconds'] = moveit_motion_time
            response_data['setupTimeSeconds'] = moveit_setup_time
            response_data['liftTimeSeconds'] = moveit_lift_time
        return jsonify(response_data)

    except Exception as e:
        print(f"Move failed: {str(e)}")
        return jsonify({
            'success': False,
            'error': str(e),
            'executionTimeSeconds': 0
        }), 500


if __name__ == '__main__':
    print("Starting Robot Execution Service...")
    print(f"Robot IP: {DEFAULT_ROBOT_IP}")
    print(f"Supported move types: {', '.join(SUPPORTED_MOVE_TYPES)}")
    app.run(host='127.0.0.1', port=5001, debug=True, use_reloader=False)
