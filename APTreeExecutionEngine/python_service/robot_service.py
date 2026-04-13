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

# Active tool state — set by /play_program (equip/deequip), re-applied on every move
_active_tcp = None          # list [x, y, z, rx, ry, rz] or None
_active_payload = None      # float (kg) or None
_active_payload_cog = None  # list [cx, cy, cz] or None


def _reapply_robot_settings(robot_ip: str):
    """Re-send the stored payload and TCP to the robot.

    Call this AFTER any .urp program starts (because loading a .urp resets
    the controller to pendant/installation defaults), and BEFORE any raw
    URScript command that moves the robot.
    """
    if _active_payload is not None:
        msg = set_payload(robot_ip, _active_payload, cog=_active_payload_cog)
        print(f"Reapplied payload: {_active_payload} kg, COG: {_active_payload_cog} -> {msg}")
    if _active_tcp is not None:
        msg = set_tcp(robot_ip, _active_tcp)
        print(f"Reapplied TCP: {_active_tcp} -> {msg}")


@app.route('/health', methods=['GET'])
def health():
    return jsonify({'status': 'ok', 'service': 'robot_execution', 'robotIp': DEFAULT_ROBOT_IP})


@app.route('/gripper', methods=['POST'])
def robot_gripper():
    """Send a gripper command to the UR10 robot."""
    try:
        data = request.json
        print(f"Received gripper request: {json.dumps(data, indent=2)}")

        robot_ip = data.get('robotIp', DEFAULT_ROBOT_IP)
        command_type = data.get('commandType', 'close_gripper')

        start_time = time.time()

        if command_type == 'close_gripper':
            result_msg = set_digital_out_sequence(robot_ip)
        elif command_type == 'open_gripper':
            from ur10_control.ur10_commands import set_tool_digital_out_open
            result_msg = set_tool_digital_out_open(robot_ip)
        else:
            return jsonify({'success': False, 'error': f"Unsupported gripper commandType '{command_type}'"}), 400

        elapsed = time.time() - start_time
        print(f"Gripper command completed: {result_msg} ({elapsed:.2f}s)")
        return jsonify({
            'success': True,
            'message': result_msg,
            'executionTimeSeconds': elapsed
        })

    except Exception as e:
        print(f"Gripper command failed: {str(e)}")
        return jsonify({
            'success': False,
            'error': str(e),
            'executionTimeSeconds': 0
        }), 500


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
        result_msg = play_program(robot_ip, program_name, speed=speed)

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

        # Set TCP after program completes if specified, and remember it
        global _active_tcp, _active_payload, _active_payload_cog
        tcp_values = data.get('tcp')
        tcp_msg = None
        if tcp_values is not None:
            _active_tcp = tcp_values
            tcp_msg = set_tcp(robot_ip, tcp_values)
            print(f"TCP set and stored: {tcp_msg}")
        else:
            _active_tcp = None

        # Set payload after program completes if specified, and remember it
        payload_msg = None
        if payload_mass is not None:
            _active_payload = float(payload_mass)
            _active_payload_cog = payload_cog
            payload_msg = set_payload(robot_ip, float(payload_mass), cog=payload_cog)
            print(f"Payload set and stored: {payload_msg}")
        else:
            _active_payload = None
            _active_payload_cog = None

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

        # Re-apply stored TCP/payload with every move command
        tcp_for_move = _active_tcp
        payload_for_move = _active_payload
        payload_cog_for_move = _active_payload_cog
        if tcp_for_move:
            print(f"Re-applying stored TCP: {tcp_for_move}")
        if payload_for_move:
            print(f"Re-applying stored payload: {payload_for_move} kg, COG: {payload_cog_for_move}")

        # Stop external_control.urp before URScript moves to avoid fieldbus disconnect
        if command_type in ('movej', 'movel', 'movep', 'movec'):
            state = dashboard_command(robot_ip, "programState")
            if "PLAYING" in state.upper():
                print("Stopping external_control before URScript move")
                dashboard_command(robot_ip, "stop")
                time.sleep(0.5)

        if command_type == 'movej':
            result_msg = move_to_pose(
                robot_ip=robot_ip, name=final_position, position=position_data,
                velocity=velocity if velocity is not None else 0.5,
                acceleration=acceleration if acceleration is not None else 1.0,
                tcp=tcp_for_move, payload=payload_for_move, payload_cog=payload_cog_for_move,
            )
        elif command_type == 'movel':
            result_msg = move_to_pose_l(
                robot_ip=robot_ip, name=final_position, position=position_data,
                velocity=velocity if velocity is not None else 0.25,
                acceleration=acceleration if acceleration is not None else 1.2,
                tcp=tcp_for_move, payload=payload_for_move, payload_cog=payload_cog_for_move,
            )
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
            end_effector = data.get('endEffectorType') or 'gripper'
            ext_program = EXTERNAL_CONTROL_NAILGUN if end_effector == 'nailgun' else EXTERNAL_CONTROL_GRIPPER

            # Step 1: Reuse external_control if already running, otherwise load+play
            state = dashboard_command(robot_ip, "programState")
            if "PLAYING" in state.upper():
                print(f"External Control already running — skipping load/play")
            else:
                print(f"Loading External Control program: {ext_program}")
                load_resp = dashboard_command(robot_ip, f"load {ext_program}")
                print(f"Load response: {load_resp}")
                if "File not found" in load_resp:
                    return jsonify({'success': False, 'error': f"External Control program not found: {ext_program}"}), 500
                dashboard_command(robot_ip, "play")

                # Wait for External Control program to reach PLAYING state,
                # then allow extra time for the URCap ↔ ROS2 driver handshake.
                waited = 0.0
                playing = False
                while waited < 15.0:
                    time.sleep(0.5)
                    waited += 0.5
                    state = dashboard_command(robot_ip, "programState")
                    if "PLAYING" in state.upper():
                        playing = True
                        print(f"External Control program running after {waited:.1f}s")
                        break
                if not playing:
                    print(f"WARNING: program did not reach PLAYING after {waited:.1f}s, proceeding anyway")
                # Extra settle time for the driver connection to fully establish
                time.sleep(2)

            # Step 2: Convert rotation vector (rx, ry) to yaw in degrees
            rx, ry = pose[3], pose[4]
            half_theta = math.atan2(ry, rx)
            yaw_deg = 2.0 * half_theta * (180.0 / math.pi)

            moveit_payload = {
                'x': -pose[0],
                'y': -pose[1],
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
            else:
                error_msg = resp_data.get('error', 'MoveIt execution failed')
                print(f"MoveIt FAILED: {error_msg}")
                return jsonify({'success': False, 'error': error_msg}), 500

        elapsed = time.time() - start_time

        print(f"Move completed: {result_msg} ({elapsed:.2f}s)")
        return jsonify({
            'success': True,
            'message': result_msg,
            'executionTimeSeconds': elapsed
        })

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
    app.run(host='127.0.0.1', port=5001, debug=True)
