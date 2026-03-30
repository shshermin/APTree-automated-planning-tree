#!/usr/bin/env python3
"""
MoveIt Bridge Service
Runs inside WSL. Receives pose + yaw from robot_service.py (Windows)
and calls the MoveIt motion planner to plan and execute the trajectory.

Usage (inside WSL):
    cd /home/shermin/ws_moveit
    source install/setup.bash
    python3 /mnt/c/Users/sherk/Documents/BehaviorTreeMainProject/APTreeExecutionEngine/python_service/moveit_bridge_service.py

Or copy this file into WSL and run from there.
Starts on http://0.0.0.0:5002
"""

from flask import Flask, request, jsonify
import subprocess
import json
import time

app = Flask(__name__)

MOVEIT_WS = "/home/shermin/ws_moveit"
MOVE_SCRIPT = "src/hello_moveit/scripts/move_to_task.py"


@app.route('/health', methods=['GET'])
def health():
    return jsonify({'status': 'ok', 'service': 'moveit_bridge'})


@app.route('/plan_and_execute', methods=['POST'])
def plan_and_execute():
    """
    Plan and execute a motion to the given pose using MoveIt.

    Expected JSON body:
        x               (float) — target X position
        y               (float) — target Y position
        z               (float) — target Z position
        yaw             (float) — target yaw in degrees (for gripper only)
        end_effector_type (str) — "gripper" or "nailgun" (default: "gripper")
        no_object       (bool)  — if true, pass --no_object flag (default: true)
        both_loaded     (bool)  — if true, pass --both_loaded flag (for nailgun)
    """
    try:
        data = request.json
        print(f"Received MoveIt request: {json.dumps(data, indent=2)}")

        x = data.get('x')
        y = data.get('y')
        z = data.get('z')
        yaw = data.get('yaw')
        end_effector_type = data.get('end_effector_type', 'gripper')
        no_object = data.get('no_object', True)
        both_loaded = data.get('both_loaded', False)

        if x is None or y is None or z is None:
            return jsonify({'success': False, 'error': 'x, y, and z are required'}), 400

        if end_effector_type != 'nailgun' and yaw is None:
            return jsonify({'success': False, 'error': 'yaw is required for non-nailgun moves'}), 400

        # Build the command
        cmd = [
            "python3", MOVE_SCRIPT,
            "--x", str(x),
            "--y", str(y),
            "--z", str(z),
            "--end_effector_type", end_effector_type,
        ]
        if yaw is not None:
            cmd.extend(["--yaw", str(yaw)])
        if no_object:
            cmd.append("--no_object")
        if both_loaded:
            cmd.append("--both_loaded")

        print(f"Running MoveIt command: {' '.join(cmd)}")
        print(f"Working directory: {MOVEIT_WS}")

        start_time = time.time()

        result = subprocess.run(
            cmd,
            cwd=MOVEIT_WS,
            capture_output=True,
            text=True,
            timeout=120
        )

        elapsed = time.time() - start_time

        print(f"MoveIt stdout:\n{result.stdout}")
        if result.stderr:
            print(f"MoveIt stderr:\n{result.stderr}")

        if result.returncode == 0:
            # Check stderr for motion failure even with exit code 0
            if "[ERROR]" in result.stderr and "Failed" in result.stderr:
                print(f"MoveIt motion failed (exit code 0 but errors in output)")
                return jsonify({
                    'success': False,
                    'error': f"MoveIt motion failed: {result.stderr}",
                    'stdout': result.stdout,
                    'executionTimeSeconds': elapsed
                }), 500
            print(f"MoveIt execution completed successfully ({elapsed:.2f}s)")
            return jsonify({
                'success': True,
                'message': f"MoveIt planned and executed to ({x}, {y}, {z}, yaw={yaw}°)",
                'stdout': result.stdout,
                'executionTimeSeconds': elapsed
            })
        else:
            print(f"MoveIt execution failed (exit code {result.returncode})")
            return jsonify({
                'success': False,
                'error': f"MoveIt failed (exit code {result.returncode}): {result.stderr}",
                'stdout': result.stdout,
                'executionTimeSeconds': elapsed
            }), 500

    except subprocess.TimeoutExpired:
        print("MoveIt command timed out")
        return jsonify({
            'success': False,
            'error': 'MoveIt command timed out (120s)',
            'executionTimeSeconds': 120
        }), 500
    except Exception as e:
        print(f"MoveIt bridge error: {str(e)}")
        return jsonify({
            'success': False,
            'error': str(e),
            'executionTimeSeconds': 0
        }), 500


if __name__ == '__main__':
    print("Starting MoveIt Bridge Service...")
    print(f"MoveIt workspace: {MOVEIT_WS}")
    print(f"Move script: {MOVE_SCRIPT}")
    print("Listening on http://127.0.0.1:5002")
    app.run(host='127.0.0.1', port=5002, debug=True)
