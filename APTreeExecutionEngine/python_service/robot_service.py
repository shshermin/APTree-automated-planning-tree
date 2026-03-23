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

app = Flask(__name__)

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(SCRIPT_DIR, "ur10_control"))
from ur10_commands import move_to_pose, move_to_pose_l, move_to_pose_p, move_to_pose_c, set_digital_out_sequence

DEFAULT_ROBOT_IP = "192.168.1.100"
SUPPORTED_MOVE_TYPES = ['movej', 'movel', 'movep', 'movec']


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

        if command_type == 'movej':
            result_msg = move_to_pose(
                robot_ip=robot_ip, name=final_position, position=position_data,
                velocity=velocity if velocity is not None else 0.5,
                acceleration=acceleration if acceleration is not None else 1.0,
            )
        elif command_type == 'movel':
            result_msg = move_to_pose_l(
                robot_ip=robot_ip, name=final_position, position=position_data,
                velocity=velocity if velocity is not None else 0.25,
                acceleration=acceleration if acceleration is not None else 1.2,
            )
        elif command_type == 'movep':
            result_msg = move_to_pose_p(
                robot_ip=robot_ip, name=final_position, position=position_data,
                velocity=velocity if velocity is not None else 0.25,
                acceleration=acceleration if acceleration is not None else 1.2,
            )
        elif command_type == 'movec':
            initial_position = data.get('initialPosition')
            if not initial_position:
                return jsonify({'success': False, 'error': 'initialPosition (via-point) is required for movec'}), 400
            result_msg = move_to_pose_c(
                robot_ip=robot_ip, via_name=initial_position, end_name=final_position,
                velocity=velocity if velocity is not None else 0.25,
                acceleration=acceleration if acceleration is not None else 1.2,
            )

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
    app.run(host='0.0.0.0', port=5001, debug=True)
