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
from ur10_commands import play_program, dashboard_command, set_payload, set_tcp, set_tool_digital_out_open
from ur10_commands import _send_urscript

DEFAULT_ROBOT_IP = "192.168.1.100"
MOVEIT_BRIDGE_URL = "http://127.0.0.1:5002"
EXTERNAL_CONTROL_NAILGUN = "external_control_n.urp"
EXTERNAL_CONTROL_GRIPPER = "external_control_g.urp"
SUPPORTED_MOVE_TYPES = ['movej', 'movel', 'movep', 'movec']


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
    """Send gripper open/close via direct URScript (tool digital outputs)."""
    try:
        data = request.json
        print(f"Received gripper request: {json.dumps(data, indent=2)}")
        start_time = time.time()
        robot_ip = data.get('robotIp') or DEFAULT_ROBOT_IP
        command_type = data.get('commandType', '').lower()

        if command_type == 'open_gripper':
            # TDO0 = True → open
            cmd = (
                "def gripper_open():\n"
                "  set_tool_digital_out(0, True)\n"
                "  sleep(0.8)\n"
                "end\n"
            )
            _send_urscript(robot_ip, cmd)
            time.sleep(0.8)  # wait for gripper to finish before returning to C#
            msg = "Gripper opened (TDO0=True)"
        elif command_type == 'close_gripper':
            # TDO0 = False, TDO1 = True → close
            cmd = (
                "def gripper_close():\n"
                "  set_tool_digital_out(0, False)\n"
                "  set_tool_digital_out(1, True)\n"
                "  sleep(0.8)\n"
                "end\n"
            )
            _send_urscript(robot_ip, cmd)
            time.sleep(0.8)  # wait for gripper to finish before returning to C#
            msg = "Gripper closed (TDO0=False, TDO1=True)"
        else:
            return jsonify({'success': False, 'error': f"Unknown gripper command: {command_type}", 'executionTimeSeconds': 0}), 400

        elapsed = time.time() - start_time
        print(f"Gripper command completed: {msg} ({elapsed:.2f}s)")
        return jsonify({'success': True, 'message': msg, 'executionTimeSeconds': elapsed})
    except Exception as e:
        print(f"Gripper command failed: {str(e)}")
        return jsonify({'success': False, 'error': str(e), 'executionTimeSeconds': 0}), 500


@app.route('/lift', methods=['POST'])
def robot_lift():
    """Lift the TCP straight up from the current pose."""
    try:
        data = request.json
        print(f"Received lift request: {json.dumps(data, indent=2)}")

        robot_ip = data.get('robotIp') or DEFAULT_ROBOT_IP
        height = data.get('height') or 0.1

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
            # Track the newly equipped tool so /move commands pick up the right TCP/payload
            robot_state.set_end_effector(end_effector_type)
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


def _poll_bridge_controller_active(timeout_sec=30.0):
    """Poll the bridge's /controller_active endpoint until the joint trajectory
    controller is active or timeout_sec elapses.

    robot_service.py runs on Windows and cannot call ROS2 services directly,
    so it delegates the check to the bridge running in WSL.
    Returns True when active, False on timeout.
    """
    deadline = time.time() + timeout_sec
    print(f"Waiting for joint controller to become active (timeout={timeout_sec:.0f}s)...")
    while time.time() < deadline:
        try:
            resp = http_requests.get(f"{MOVEIT_BRIDGE_URL}/controller_active", timeout=5)
            resp_json = resp.json()
            print(f"[poll] controller_active: {resp_json}")
            if resp_json.get('active'):
                print("Joint controller is active — proceeding")
                return True
        except Exception as e:
            print(f"_poll_bridge_controller_active: bridge unreachable ({e}), retrying...")
        time.sleep(1.0)
    print(f"Joint controller did not become active within {timeout_sec:.0f}s")
    return False


def _ensure_ec_running(robot_ip, end_effector='gripper'):
    """Ensure the correct External Control URCap program is running.

    Reuses it if already PLAYING with the correct program (no restart overhead).
    Stops and reloads if the wrong EC program is running (tool switch scenario).
    Loads and plays fresh if nothing is running.
    The 4s settle is only paid once per tool session, not per request.
    """
    ext_program = EXTERNAL_CONTROL_NAILGUN if end_effector == 'nailgun' else EXTERNAL_CONTROL_GRIPPER

    state = dashboard_command(robot_ip, "programState")
    print(f"[EC check] programState: {state!r}")
    if "PLAYING" in state.upper():
        # Check which program is actually loaded — guard against wrong EC being active
        loaded = dashboard_command(robot_ip, "get loaded program")
        print(f"[EC check] loadedProgram: {loaded!r}")
        # Response is e.g. "Loaded program: /programs/external_control_g.urp"
        if ext_program.lower() in loaded.lower():
            print(f"External Control '{ext_program}' already running — skipping load/play")
            # Dashboard says PLAYING but the RT reverse interface may still be
            # reconnecting after an EC drop (UR watchdog reset).  Poll the bridge
            # until the joint controller is actually active before returning.
            if _poll_bridge_controller_active(timeout_sec=30.0):
                return
            # Dashboard says PLAYING but the RT interface is dead — force EC restart.
            # This happens when the UR watchdog terminates the URScript connection
            # while the URCap process itself is still listed as PLAYING.
            print(f"[WARN] Controller not active after 30s despite EC PLAYING — forcing EC restart")
            dashboard_command(robot_ip, "stop")
            time.sleep(1.0)
            # fall through to load+play below
        else:
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
    # Poll the bridge until scaled_joint_trajectory_controller is active.
    # Dashboard PLAYING only means the URCap started; the RT reverse interface
    # (and therefore the controller) takes 5-15 s more to connect.  A fixed
    # 1.5 s sleep is far too short and causes CONTROL_FAILED on the first move.
    _poll_bridge_controller_active(timeout_sec=30.0)
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
            # Joint-space move via URScript movej — no EC required
            # Accepts joints OR pose (movej supports Cartesian targets via IK in URScript)
            joints = inline_joints
            pose = inline_pose
            if (not joints or len(joints) < 6) and (not pose or len(pose) < 6):
                return jsonify({'success': False, 'error': 'joints [j1..j6] or pose [x,y,z,rx,ry,rz] required for movej'}), 400
            vel_val = velocity if velocity is not None else 1.0
            acc_val = acceleration if acceleration is not None else 1.0
            if joints and len(joints) >= 6:
                position_arg = {'joints': joints}
            else:
                position_arg = {'pose': pose}
            result_msg = move_to_pose(
                robot_ip=robot_ip, name=final_position,
                position=position_arg,
                velocity=vel_val, acceleration=acc_val,
                tcp=tcp_for_move, payload=payload_for_move, payload_cog=payload_cog_for_move,
            )

        elif command_type == 'movel':
            # Cartesian linear move via URScript movel — no EC required
            pose = inline_pose
            if not pose or len(pose) < 6:
                return jsonify({'success': False, 'error': 'pose [x,y,z,rx,ry,rz] required for movel'}), 400
            vel_val = velocity if velocity is not None else 0.5
            acc_val = acceleration if acceleration is not None else 0.8
            result_msg = move_to_pose_l(
                robot_ip=robot_ip, name=final_position,
                position={'pose': pose},
                velocity=vel_val, acceleration=acc_val,
                tcp=tcp_for_move, payload=payload_for_move, payload_cog=payload_cog_for_move,
            )

        elif command_type == 'movep':
            result_msg = move_to_pose_p(
                robot_ip=robot_ip, name=final_position, position=position_data,
                velocity=velocity if velocity is not None else 0.5,
                acceleration=acceleration if acceleration is not None else 1.2,
                tcp=tcp_for_move, payload=payload_for_move, payload_cog=payload_cog_for_move,
            )
        elif command_type == 'movec':
            initial_position = data.get('initialPosition')
            if not initial_position:
                return jsonify({'success': False, 'error': 'initialPosition (via-point) is required for movec'}), 400
            result_msg = move_to_pose_c(
                robot_ip=robot_ip, via_name=initial_position, end_name=final_position,
                velocity=velocity if velocity is not None else 0.5,
                acceleration=acceleration if acceleration is not None else 1.2,
                tcp=tcp_for_move, payload=payload_for_move, payload_cog=payload_cog_for_move,
            )


        elapsed = time.time() - start_time

        print(f"Move completed: {result_msg} ({elapsed:.2f}s)")
        response_data = {
            'success': True,
            'message': result_msg,
            'executionTimeSeconds': elapsed
        }
        return jsonify(response_data)

    except Exception as e:
        print(f"Move failed: {str(e)}")
        return jsonify({
            'success': False,
            'error': str(e),
            'executionTimeSeconds': 0
        }), 500


@app.route('/nail_and_retract', methods=['POST'])
def robot_nail_and_retract():
    """Move to nail position (movej), push down 2 mm, then lift — all in one call.

    Expected JSON body:
        finalPosition   (str)   — named target position (for logging)
        robotIp         (str)   — robot IP (optional, default 192.168.1.100)
        velocity        (float) — movej velocity (optional, default 0.3)
        acceleration    (float) — movej acceleration (optional, default 0.3)
        joints          (list)  — joint angles [j1..j6] (optional)
        pose            (list)  — TCP pose [x,y,z,rx,ry,rz] (optional)
    """
    try:
        data = request.json
        print(f"Received nail_and_retract request: {json.dumps(data, indent=2)}")

        final_position = data.get('finalPosition', 'nailpos')
        robot_ip = data.get('robotIp', DEFAULT_ROBOT_IP)
        velocity = data.get('velocity', 0.3)
        acceleration = data.get('acceleration', 0.3)
        inline_joints = data.get('joints')
        inline_pose = data.get('pose')

        if (not inline_joints or len(inline_joints) < 6) and (not inline_pose or len(inline_pose) < 6):
            return jsonify({'success': False, 'error': 'joints [j1..j6] or pose [x,y,z,rx,ry,rz] required'}), 400

        tcp_for_move, payload_for_move, payload_cog_for_move = robot_state.snapshot()

        start_time = time.time()

        # Step 1: movej to nail position
        if inline_joints and len(inline_joints) >= 6:
            position_arg = {'joints': inline_joints}
        else:
            position_arg = {'pose': inline_pose}

        print(f"nail_and_retract: step 1 — movej to {final_position}")
        move_to_pose(
            robot_ip=robot_ip, name=final_position,
            position=position_arg,
            velocity=velocity, acceleration=acceleration,
            tcp=tcp_for_move, payload=payload_for_move, payload_cog=payload_cog_for_move,
        )

        # Step 2: push down 2 mm
        print("nail_and_retract: step 2 — push down 2 mm")
        from ur10_control.ur10_commands import lift_z
        lift_z(robot_ip, height=-0.002)

        # Step 3: lift back up
        print("nail_and_retract: step 3 — lift")
        lift_z(robot_ip)

        elapsed = time.time() - start_time
        print(f"nail_and_retract completed in {elapsed:.2f}s")
        return jsonify({'success': True, 'message': f'nail_and_retract at {final_position} done', 'executionTimeSeconds': elapsed})

    except Exception as e:
        print(f"nail_and_retract failed: {str(e)}")
        return jsonify({'success': False, 'error': str(e), 'executionTimeSeconds': 0}), 500


@app.route('/stack_release', methods=['POST'])
def robot_stack_release():
    """Approach a stack position (movej), open gripper, then lift — all in one call.

    Expected JSON body:
        finalPosition   (str)   — named target position (for logging)
        robotIp         (str)   — robot IP (optional, default 192.168.1.100)
        velocity        (float) — movej velocity (optional, default 1.0)
        acceleration    (float) — movej acceleration (optional, default 1.0)
        joints          (list)  — joint angles [j1..j6] (optional)
        pose            (list)  — TCP pose [x,y,z,rx,ry,rz] (optional)
    """
    try:
        data = request.json
        print(f"Received stack_release request: {json.dumps(data, indent=2)}")

        final_position = data.get('finalPosition', 'stackpos')
        robot_ip = data.get('robotIp', DEFAULT_ROBOT_IP)
        velocity = data.get('velocity', 1.0)
        acceleration = data.get('acceleration', 1.0)
        inline_joints = data.get('joints')
        inline_pose = data.get('pose')

        if (not inline_joints or len(inline_joints) < 6) and (not inline_pose or len(inline_pose) < 6):
            return jsonify({'success': False, 'error': 'joints [j1..j6] or pose [x,y,z,rx,ry,rz] required'}), 400

        tcp_for_move, payload_for_move, payload_cog_for_move = robot_state.snapshot()

        start_time = time.time()

        # Step 1: movej to stack position
        if inline_joints and len(inline_joints) >= 6:
            position_arg = {'joints': inline_joints}
        else:
            position_arg = {'pose': inline_pose}

        print(f"stack_release: step 1 — movej to {final_position}")
        move_to_pose(
            robot_ip=robot_ip, name=final_position,
            position=position_arg,
            velocity=velocity, acceleration=acceleration,
            tcp=tcp_for_move, payload=payload_for_move, payload_cog=payload_cog_for_move,
        )

        # Step 2: open gripper
        print("stack_release: step 2 — open gripper")
        open_cmd = (
            "def gripper_open():\n"
            "  set_tool_digital_out(0, True)\n"
            "  sleep(0.8)\n"
            "end\n"
        )
        _send_urscript(robot_ip, open_cmd)
        time.sleep(0.8)

        # Step 3: lift
        print("stack_release: step 3 — lift")
        from ur10_control.ur10_commands import lift_z
        lift_z(robot_ip)

        elapsed = time.time() - start_time
        print(f"stack_release completed in {elapsed:.2f}s")
        return jsonify({'success': True, 'message': f'stack_release at {final_position} done', 'executionTimeSeconds': elapsed})

    except Exception as e:
        print(f"stack_release failed: {str(e)}")
        return jsonify({'success': False, 'error': str(e), 'executionTimeSeconds': 0}), 500


if __name__ == '__main__':
    print("Starting Robot Execution Service...")
    print(f"Robot IP: {DEFAULT_ROBOT_IP}")
    print(f"Supported move types: {', '.join(SUPPORTED_MOVE_TYPES)}")
    app.run(host='127.0.0.1', port=5001, debug=True, use_reloader=False)
