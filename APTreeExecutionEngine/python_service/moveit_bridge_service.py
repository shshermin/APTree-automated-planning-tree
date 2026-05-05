#!/usr/bin/env python3
"""
MoveIt Bridge Service
Runs inside WSL. Receives pose + yaw from robot_service.py (Windows)
and calls the MoveIt motion planner to plan and execute the trajectory.

ROS 2 nodes (MoveToTask, DynamicSceneManager) are created ONCE at startup
and reused across all HTTP requests, avoiding the overhead of rclpy.init()
and action-server discovery on every call.

Usage (inside WSL):
    cd /home/shermin/ws_moveit
    source install/setup.bash
    python3 /mnt/c/Users/sherk/Documents/BehaviorTreeMainProject/APTreeExecutionEngine/python_service/moveit_bridge_service.py

Starts on http://127.0.0.1:5002
"""

from flask import Flask, request, jsonify
import json
import math
import sys
import time
import threading

# Ensure the scripts directory is importable
sys.path.insert(0, '/home/shermin/ws_moveit/src/hello_moveit/scripts')

import rclpy
import rclpy.time
import tf2_ros
from geometry_msgs.msg import Pose
from moveit_msgs.msg import Constraints, OrientationConstraint
from std_msgs.msg import String as StringMsg
from ur_msgs.srv import SetIO

from move_to_task import MoveToTask
from dynamic_scene_example import DynamicSceneManager

app = Flask(__name__)

# ── Global ROS state (initialised once in main) ──────────────────────────────
move_to_task = None      # MoveToTask node  (persistent)
scene = None             # DynamicSceneManager node (persistent)
io_client = None         # SetIO service client (persistent)
stop_pub = None          # Publisher to clear MoveGroup's trajectory execution state
tf_buffer = None         # TF2 buffer for reading actual TCP pose
_last_disabled_ee = [None]  # tracks which EE was last disabled to avoid redundant ACM updates
_last_payload_ee = [None]   # tracks which EE payload was last set to avoid redundant updates
_init_lock = threading.Lock()


def init_ros():
    """Initialise rclpy and create persistent nodes.

    We do NOT spin in the background — the existing
    rclpy.spin_until_future_complete() calls inside MoveToTask already
    handle spinning when waiting for action/service results.
    """
    global move_to_task, scene, io_client, stop_pub, tf_buffer

    with _init_lock:
        if move_to_task is not None:
            return  # already initialised

        rclpy.init()

        scene = DynamicSceneManager()
        move_to_task = MoveToTask(end_effector_type='gripper')  # default; EE link updated per request
        io_client = move_to_task.create_client(SetIO, '/io_and_status_controller/set_io')
        stop_pub = move_to_task.create_publisher(StringMsg, '/trajectory_execution_event', 1)
        tf_buffer = tf2_ros.Buffer()
        tf2_ros.TransformListener(tf_buffer, move_to_task)

        print("ROS 2 nodes initialised (persistent, no background spin).")


# ── Helpers ───────────────────────────────────────────────────────────────────

EE_LINK_MAP = {
    'none': 'tool0',
    'gripper': 'gripper_tip',
    'nailgun': 'nailgun_tip',
}


def _build_pose(x, y, z, yaw_deg):
    """Build a geometry_msgs/Pose: tool facing down with the given yaw."""
    pose = Pose()
    pose.position.x = float(x)
    pose.position.y = float(y)
    pose.position.z = float(z)

    yaw_rad = math.radians(yaw_deg)
    cy = math.cos(yaw_rad / 2)
    sy = math.sin(yaw_rad / 2)
    pose.orientation.x = cy
    pose.orientation.y = sy
    pose.orientation.z = 0.0
    pose.orientation.w = 0.0
    return pose


def _build_nailgun_path_constraints(target_pose):
    """Return path constraints that keep the nailgun facing down."""
    pc = Constraints()
    pc.name = 'nailgun_facing_down'
    oc = OrientationConstraint()
    oc.header.frame_id = 'base_link'
    oc.link_name = 'tool0'
    oc.orientation = target_pose.orientation
    oc.absolute_x_axis_tolerance = 0.1
    oc.absolute_y_axis_tolerance = 0.1
    oc.absolute_z_axis_tolerance = 3.14
    oc.weight = 1.0
    pc.orientation_constraints.append(oc)
    return pc


# ── Flask routes ──────────────────────────────────────────────────────────────

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
        no_object       (bool)  — if true, skip attaching object (default: true)
        both_loaded     (bool)  — if true, disable inactive EE collisions
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

        # Nailgun overrides
        if end_effector_type == 'nailgun':
            yaw = 90.0
            no_object = True

        if end_effector_type != 'nailgun' and yaw is None:
            return jsonify({'success': False, 'error': 'yaw is required for non-nailgun moves'}), 400

        # Update end-effector link on the persistent node
        move_to_task.end_effector_link = EE_LINK_MAP.get(end_effector_type, 'tool0')

        start_time = time.time()

        # Disable collisions for inactive EE when both are loaded (only once per EE type)
        if both_loaded and end_effector_type in ('gripper', 'nailgun'):
            if _last_disabled_ee[0] != end_effector_type:
                move_to_task.disable_inactive_ee(end_effector_type)
                _last_disabled_ee[0] = end_effector_type
                time.sleep(2.0)  # let MoveIt digest the ACM update

        # Set correct payload on the real robot controller (only once per EE type)
        if _last_payload_ee[0] != end_effector_type:
            move_to_task.set_robot_payload(end_effector_type)
            _last_payload_ee[0] = end_effector_type
            time.sleep(2.0)  # let move_group absorb the robot state change

        # Attach object if needed
        if not no_object:
            touch_links = ['gripper_base', 'gripper_left_finger', 'gripper_right_finger', 'tool0']
            if both_loaded:
                touch_links.extend(['nailgun_base', 'nailgun_tip'])
            scene.attach_mesh_object(
                object_id='target_object',
                mesh_path='object',
                link_name='gripper_base',
                pos=(0.0, 0.0, 0.1225),
                scale=(1.0, 1.0, 1.0),
                touch_links=touch_links
            )
            time.sleep(5.0)

        # Build target pose
        target_pose = _build_pose(x, y, z, yaw)

        # Path constraints for nailgun
        path_constraints = None
        if end_effector_type == 'nailgun':
            path_constraints = _build_nailgun_path_constraints(target_pose)

        # Tell MoveGroup to stop any ongoing trajectory execution and clear its
        # internal state. Without this, the trajectory_execution_manager may try
        # to send a "continuation" of the previous (now stale) trajectory instead
        # of a fresh one, causing the controller to reject the goal.
        stop_msg = StringMsg()
        stop_msg.data = 'stop'
        stop_pub.publish(stop_msg)
        time.sleep(0.5)  # let MoveGroup process the stop before we send a new goal
        move_to_task.get_logger().info('Published stop to clear stale trajectory state')

        # Execute motion with retry as safety net.
        MAX_RETRIES = 3
        success = False
        for attempt in range(1, MAX_RETRIES + 1):
            success = move_to_task.move_to(
                target_pose,
                velocity_scaling=0.15,
                acceleration_scaling=0.15,
                planning_time=10.0,
                path_constraints=path_constraints
            )
            if success:
                break
            move_to_task.get_logger().warn(
                f'Motion attempt {attempt}/{MAX_RETRIES} failed, retrying...'
            )
            time.sleep(2.0)

        if success:
            # Detach object if it was attached
            if not no_object:
                scene.detach_object('target_object', link_name='gripper_base')

            # Open gripper after reaching target
            if end_effector_type == 'gripper':
                if io_client.wait_for_service(timeout_sec=5.0):
                    io_req = SetIO.Request()
                    io_req.fun = 1       # FUN_SET_DIGITAL_OUT
                    io_req.pin = 16      # PIN_TOOL_DOUT0
                    io_req.state = 1.0   # STATE_ON
                    io_future = io_client.call_async(io_req)
                    rclpy.spin_until_future_complete(move_to_task, io_future, timeout_sec=5.0)
                    if io_future.result() is not None and io_future.result().success:
                        move_to_task.get_logger().info('Gripper opened')
                    else:
                        move_to_task.get_logger().error('Failed to open gripper')
                    time.sleep(0.5)

            # Press 2mm down (Pilz LIN) for nailgun — use actual TCP pose from TF
            # so Pilz LIN start state exactly matches the robot's current state.
            if end_effector_type == 'nailgun':
                time.sleep(0.3)  # let joint state settle after approach
                try:
                    rclpy.spin_once(move_to_task, timeout_sec=0.5)  # flush TF callbacks
                    t = tf_buffer.lookup_transform('base_link', 'nailgun_tip', rclpy.time.Time())
                    tr = t.transform.translation
                    rot = t.transform.rotation
                    press_pose = Pose()
                    press_pose.position.x = tr.x
                    press_pose.position.y = tr.y
                    press_pose.position.z = tr.z - 0.002
                    press_pose.orientation.x = rot.x
                    press_pose.orientation.y = rot.y
                    press_pose.orientation.z = rot.z
                    press_pose.orientation.w = rot.w
                    move_to_task.get_logger().info(
                        f'Nailgun: pressing 2mm down from actual pose '
                        f'({tr.x:.4f}, {tr.y:.4f}, {tr.z:.4f})')
                    press_success = move_to_task.move_to(
                        press_pose,
                        velocity_scaling=0.15,
                        acceleration_scaling=0.15,
                        planning_time=5.0,
                        pipeline_id='pilz_industrial_motion_planner',
                        planner_id='LIN',
                    )
                    if not press_success:
                        move_to_task.get_logger().error('Nailgun: 2mm press failed')
                except Exception as tf_ex:
                    move_to_task.get_logger().error(f'Nailgun: TF lookup failed: {tf_ex}')

            time.sleep(0.5)  # settle after press before lift

            # Lift 5 cm straight up (Pilz LIN to stay on same IK branch)
            lift_pose = _build_pose(x, y, z + 0.05, yaw)
            move_to_task.move_to(
                lift_pose,
                velocity_scaling=0.15,
                acceleration_scaling=0.15,
                planning_time=5.0,
                pipeline_id='pilz_industrial_motion_planner',
                planner_id='LIN',
            )

        elapsed = time.time() - start_time

        if success:
            print(f"MoveIt execution completed successfully ({elapsed:.2f}s)")
            return jsonify({
                'success': True,
                'message': f"MoveIt planned and executed to ({x}, {y}, {z}, yaw={yaw}°)",
                'executionTimeSeconds': elapsed
            })
        else:
            print(f"MoveIt motion planning/execution failed ({elapsed:.2f}s)")
            return jsonify({
                'success': False,
                'error': 'MoveIt motion planning or execution failed',
                'executionTimeSeconds': elapsed
            }), 500

    except Exception as e:
        print(f"MoveIt bridge error: {str(e)}")
        import traceback; traceback.print_exc()
        return jsonify({
            'success': False,
            'error': str(e),
            'executionTimeSeconds': 0
        }), 500


if __name__ == '__main__':
    print("Starting MoveIt Bridge Service...")
    init_ros()
    print("Listening on http://127.0.0.1:5002")
    app.run(host='127.0.0.1', port=5002, debug=False)
