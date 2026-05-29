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
from controller_manager_msgs.srv import ListControllers

from move_to_task import MoveToTask
from dynamic_scene_example import DynamicSceneManager

app = Flask(__name__)

# ── Global ROS state (initialised once in main) ──────────────────────────────
move_to_task = None      # MoveToTask node  (persistent)
scene = None             # DynamicSceneManager node (persistent)
io_client = None         # SetIO service client (persistent)
ctrl_list_client = None  # ListControllers service client (persistent)
stop_pub = None          # Publisher to clear MoveGroup's trajectory execution state
tf_buffer = None         # TF2 buffer for reading actual TCP pose
_last_disabled_ee = [None]  # tracks which EE was last disabled to avoid redundant ACM updates
_last_payload_ee = [None]   # tracks which EE payload was last set to avoid redundant updates
_init_lock = threading.Lock()

_JOINT_CTRL = 'scaled_joint_trajectory_controller'  # controller whose active state we check


def init_ros():
    """Initialise rclpy and create persistent nodes.

    We do NOT spin in the background — the existing
    rclpy.spin_until_future_complete() calls inside MoveToTask already
    handle spinning when waiting for action/service results.
    """
    global move_to_task, scene, io_client, ctrl_list_client, stop_pub, tf_buffer

    with _init_lock:
        if move_to_task is not None:
            return  # already initialised

        rclpy.init()

        scene = DynamicSceneManager()
        move_to_task = MoveToTask(end_effector_type='gripper')  # default; EE link updated per request
        io_client = move_to_task.create_client(SetIO, '/io_and_status_controller/set_io')
        ctrl_list_client = move_to_task.create_client(
            ListControllers, '/controller_manager/list_controllers'
        )
        stop_pub = move_to_task.create_publisher(StringMsg, '/trajectory_execution_event', 1)
        tf_buffer = tf2_ros.Buffer()
        tf2_ros.TransformListener(tf_buffer, move_to_task)

        print("ROS 2 nodes initialised (persistent, no background spin).")


# ── Helpers ───────────────────────────────────────────────────────────────────

def _wait_for_controller_active(timeout_sec=30.0):
    """Spin-poll /controller_manager/list_controllers until scaled_joint_trajectory_controller
    reports state == 'active'.  Returns True when active, False on timeout.

    Called between retry attempts so that CONTROL_FAILED retries only fire
    after the RT reverse interface has reconnected and the controller is ready,
    rather than after a fixed sleep that may be too short.
    """
    deadline = time.time() + timeout_sec
    move_to_task.get_logger().info(
        f'Waiting for {_JOINT_CTRL} to become active (timeout={timeout_sec:.0f}s)...'
    )
    while time.time() < deadline:
        if not ctrl_list_client.wait_for_service(timeout_sec=0.5):
            rclpy.spin_once(move_to_task, timeout_sec=0.1)
            continue
        fut = ctrl_list_client.call_async(ListControllers.Request())
        rclpy.spin_until_future_complete(move_to_task, fut, timeout_sec=2.0)
        if fut.result() is not None:
            for ctrl in fut.result().controller:
                if ctrl.name == _JOINT_CTRL and ctrl.state == 'active':
                    move_to_task.get_logger().info(f'{_JOINT_CTRL} is active — proceeding')
                    return True
        rclpy.spin_once(move_to_task, timeout_sec=0.5)
    move_to_task.get_logger().warn(
        f'{_JOINT_CTRL} did not become active within {timeout_sec:.0f}s'
    )
    return False


EE_LINK_MAP = {
    'none': 'tool0',
    'gripper': 'gripper_tip',
    'nailgun': 'nailgun_tip',
}


def _build_pose(x, y, z, yaw_deg):
    """Build a geometry_msgs/Pose: tool facing down with the given yaw.

    Caller negates x/y to compensate for the 180° Z-rotation between the
    robot base frame and ROS base_link. The yaw value passed in here is
    already expressed in the same negated convention (derived by C# from
    the URScript pose axis-angle), so no further yaw offset is applied.
    """
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

@app.route('/plan_only', methods=['POST'])
def plan_only():
    """Plan a motion with MoveIt but do NOT execute. Returns the planned
    joint trajectory as JSON, intended for execution by robot_service.py
    via direct URScript movej.

    Expected JSON body (cartesian):
        x, y, z              (float) — target position (robot frame, will be negated to ROS frame)
        yaw                  (float) — target yaw in degrees (cartesian only)
        end_effector_type    (str)   — "gripper" or "nailgun" (default: "gripper")
        velocity             (float) — velocity scaling 0-1 (default: 0.15)
        acceleration         (float) — acceleration scaling 0-1 (default: 0.15)
        planning_time        (float) — max planning seconds (default: 5.0)
        pipeline_id, planner_id (str) — MoveIt planner selection
        both_loaded          (bool)  — disable inactive EE collisions

    Or joint-space (overrides cartesian):
        joints               (list[6]) — joint angles in radians
        use_pilz_ptp         (bool)    — use Pilz PTP instead of OMPL

    Returns:
        {success, joint_names: [...], points: [{positions, time_from_start}, ...]}
    """
    try:
        data = request.json or {}
        end_effector_type = data.get('end_effector_type', 'gripper')
        velocity = float(data.get('velocity', 0.15))
        acceleration = float(data.get('acceleration', 0.15))
        planning_time = float(data.get('planning_time', 5.0))
        both_loaded = data.get('both_loaded', False)
        no_object = data.get('no_object', True)

        req_start = time.time()
        move_to_task.get_logger().info(
            f'[/plan_only] request: ee={end_effector_type} vel={velocity} acc={acceleration} '
            f'plan_time={planning_time}s both_loaded={both_loaded} no_object={no_object}'
        )

        move_to_task.end_effector_link = EE_LINK_MAP.get(end_effector_type, 'tool0')

        setup_start = time.time()
        if both_loaded and end_effector_type in ('gripper', 'nailgun'):
            if _last_disabled_ee[0] != end_effector_type:
                move_to_task.disable_inactive_ee(end_effector_type)
                _last_disabled_ee[0] = end_effector_type
                time.sleep(2.0)

        # Nailgun always plans without an attached object
        if end_effector_type == 'nailgun':
            no_object = True

        # Attach carried object to the planning scene so collisions include it
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
                touch_links=touch_links,
            )
            time.sleep(5.0)
        setup_elapsed = time.time() - setup_start

        plan_start = time.time()
        joints = data.get('joints')
        if joints:
            move_to_task.get_logger().info(
                f'[/plan_only] planning joint-space, target={[round(v,3) for v in joints]}'
            )
            traj = move_to_task.plan_to_joints(
                joint_values=joints,
                velocity_scaling=velocity,
                acceleration_scaling=acceleration,
                planning_time=planning_time,
                use_pilz_ptp=bool(data.get('use_pilz_ptp', True)),
            )
        else:
            x, y, z = data.get('x'), data.get('y'), data.get('z')
            if x is None or y is None or z is None:
                return jsonify({'success': False, 'error': 'x, y, z (or joints) required'}), 400
            orientation_quat = data.get('orientation_quat')
            keep_orientation = bool(data.get('keep_orientation', False))
            yaw = data.get('yaw')
            if end_effector_type == 'nailgun':
                yaw = 90.0
                keep_orientation = False  # nailgun always pins canonical orientation
                orientation_quat = None
            if orientation_quat is not None:
                if len(orientation_quat) != 4:
                    return jsonify({'success': False,
                                    'error': 'orientation_quat must be [qx,qy,qz,qw]'}), 400
                target_pose = Pose()
                target_pose.position.x = -float(x)
                target_pose.position.y = -float(y)
                target_pose.position.z = float(z)
                qx, qy, qz, qw = (float(v) for v in orientation_quat)
                # Normalise defensively — MoveIt rejects non-unit quaternions.
                n = math.sqrt(qx*qx + qy*qy + qz*qz + qw*qw) or 1.0
                target_pose.orientation.x = qx / n
                target_pose.orientation.y = qy / n
                target_pose.orientation.z = qz / n
                target_pose.orientation.w = qw / n
                yaw_log = (f'QUAT({target_pose.orientation.x:.3f},'
                           f'{target_pose.orientation.y:.3f},'
                           f'{target_pose.orientation.z:.3f},'
                           f'{target_pose.orientation.w:.3f})')
            elif keep_orientation:
                # Read live EE orientation from TF and reuse it verbatim, so a
                # straight-z lift cannot rotate the wrist. Position is still
                # built from x,y,z (with the usual base_link negation).
                ee_link = EE_LINK_MAP.get(end_effector_type, 'tool0')
                # Pump TF callbacks before lookup — bridge has no background spin,
                # so the buffer is empty unless we flush. Mirror the nailgun
                # press-down path below which also spins before lookup_transform.
                tf_spin_deadline = time.time() + 1.5
                tr = None
                last_tf_ex = None
                while time.time() < tf_spin_deadline:
                    rclpy.spin_once(move_to_task, timeout_sec=0.1)
                    try:
                        tr = tf_buffer.lookup_transform('base_link', ee_link, rclpy.time.Time())
                        break
                    except Exception as tf_ex:
                        last_tf_ex = tf_ex
                        continue
                if tr is None:
                    move_to_task.get_logger().error(
                        f'[/plan_only] keep_orientation: TF lookup base_link<-{ee_link} failed: {last_tf_ex}'
                    )
                    return jsonify({'success': False,
                                    'error': f'keep_orientation TF lookup failed: {last_tf_ex}'}), 500
                target_pose = Pose()
                target_pose.position.x = -float(x)
                target_pose.position.y = -float(y)
                target_pose.position.z = float(z)
                target_pose.orientation = tr.transform.rotation
                move_to_task.get_logger().info(
                    f'[/plan_only] keep_orientation: using live TF orientation '
                    f'q=({tr.transform.rotation.x:.3f},{tr.transform.rotation.y:.3f},'
                    f'{tr.transform.rotation.z:.3f},{tr.transform.rotation.w:.3f})'
                )
                yaw_log = 'KEEP'
            else:
                if yaw is None:
                    return jsonify({'success': False,
                                    'error': 'orientation_quat, keep_orientation, or yaw required for cartesian plan'}), 400
                target_pose = _build_pose(-float(x), -float(y), float(z), float(yaw))
                yaw_log = f'{yaw}'
            pipeline = data.get('pipeline_id', 'pilz_industrial_motion_planner')
            planner = data.get('planner_id', 'PTP')
            move_to_task.get_logger().info(
                f'[/plan_only] planning cartesian, target=({x},{y},{z},yaw={yaw_log}) '
                f'pipeline={pipeline} planner={planner}'
            )
            traj = move_to_task.plan_to(
                target_pose,
                velocity_scaling=velocity,
                acceleration_scaling=acceleration,
                planning_time=planning_time,
                pipeline_id=pipeline,
                planner_id=planner,
                wrist3_lock=data.get('wrist3_lock'),
            )
        plan_elapsed = time.time() - plan_start

        if traj is None:
            total_elapsed = time.time() - req_start
            move_to_task.get_logger().error(
                f'[/plan_only] PLANNING FAILED after {plan_elapsed:.3f}s '
                f'(setup={setup_elapsed:.3f}s, total={total_elapsed:.3f}s)'
            )
            return jsonify({
                'success': False,
                'error': 'Planning failed',
                'setupTimeSeconds': setup_elapsed,
                'planningTimeSeconds': plan_elapsed,
            }), 500

        # Detach the carried object so subsequent /plan_only calls start clean.
        # The trajectory was already computed with the object included.
        if not no_object:
            try:
                scene.detach_object('target_object', link_name='gripper_base')
            except Exception as det_ex:
                move_to_task.get_logger().warn(f'[/plan_only] detach warning: {det_ex}')

        serialize_start = time.time()
        jt = traj.joint_trajectory
        points = []
        for p in jt.points:
            t = p.time_from_start.sec + p.time_from_start.nanosec * 1e-9
            points.append({
                'positions': list(p.positions),
                'time_from_start': t,
            })
        serialize_elapsed = time.time() - serialize_start
        nominal_duration = points[-1]['time_from_start'] if points else 0.0
        total_elapsed = time.time() - req_start
        move_to_task.get_logger().info(
            f'[/plan_only] OK: points={len(points)} '
            f'nominal_duration={nominal_duration:.2f}s | '
            f'setup={setup_elapsed:.3f}s plan={plan_elapsed:.3f}s '
            f'serialize={serialize_elapsed:.3f}s total={total_elapsed:.3f}s'
        )
        return jsonify({
            'success': True,
            'joint_names': list(jt.joint_names),
            'points': points,
            'pointCount': len(points),
            'nominalDurationSeconds': nominal_duration,
            'setupTimeSeconds': setup_elapsed,
            'planningTimeSeconds': plan_elapsed,
            'serializeTimeSeconds': serialize_elapsed,
            'totalTimeSeconds': total_elapsed,
        })
    except Exception as e:
        import traceback; traceback.print_exc()
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/health', methods=['GET'])
def health():
    return jsonify({'status': 'ok', 'service': 'moveit_bridge'})


@app.route('/controller_active', methods=['GET'])
def controller_active():
    """Return whether scaled_joint_trajectory_controller is currently active.

    Used by robot_service.py (_ensure_ec_running) to wait for the RT reverse
    interface to reconnect after an EC restart, instead of a fixed sleep.
    Returns immediately — does NOT block for recovery.
    """
    try:
        if not ctrl_list_client.wait_for_service(timeout_sec=1.0):
            return jsonify({'active': False, 'error': 'controller_manager service unavailable'})
        fut = ctrl_list_client.call_async(ListControllers.Request())
        rclpy.spin_until_future_complete(move_to_task, fut, timeout_sec=2.0)
        if fut.result() is None:
            return jsonify({'active': False, 'error': 'ListControllers timed out'})
        for ctrl in fut.result().controller:
            if ctrl.name == _JOINT_CTRL and ctrl.state == 'active':
                return jsonify({'active': True})
        return jsonify({'active': False})
    except Exception as e:
        import traceback; traceback.print_exc()
        return jsonify({'active': False, 'error': str(e)})


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

        # Build target pose — negate x and y to convert from robot frame to ROS frame
        target_pose = _build_pose(-x, -y, z, yaw)

        # Nailgun uses Pilz PTP (no path constraints needed — Pilz ignores/rejects them)
        path_constraints = None

        stop_msg = StringMsg()
        stop_msg.data = 'stop'

        # Confirm the controller is active right now — setup (ACM + payload sleeps)
        # above can take 4 s, during which the controller may have dropped.
        # 10s is sufficient: robot_service already confirmed active before sending
        # this request, so this only catches drops during the setup phase.
        if not _wait_for_controller_active(timeout_sec=10.0):
            return jsonify({'success': False, 'error': 'scaled_joint_trajectory_controller not active — EC disconnected'}), 503

        # Execute motion with retry as safety net.
        MAX_RETRIES = 3
        success = False
        for attempt in range(1, MAX_RETRIES + 1):
            if attempt > 1:
                # Only publish stop before retries — NOT before attempt 1.
                # MoveGroup processes topic callbacks on its own thread only when
                # idle. If we publish stop then immediately send a goal, MoveGroup
                # queues the stop while busy planning/dispatching, then fires it
                # AFTER the controller starts executing → robot moves briefly then
                # gets cut off (CONTROL_FAILED).
                # Before attempt 1 the state is clean: either the end-of-previous-
                # request stop+spin cleared the Pilz handle, or this is the first
                # call and there is nothing to stop.
                stop_pub.publish(stop_msg)
                rclpy.spin_once(move_to_task, timeout_sec=0.1)  # flush pending callbacks
                time.sleep(1.5)  # give MoveGroup time to process stop before new goal
            move_to_task.get_logger().info(
                f'Starting attempt {attempt}/{MAX_RETRIES}'
            )
            if end_effector_type == 'nailgun':
                success = move_to_task.move_to(
                    target_pose,
                    velocity_scaling=0.15,
                    acceleration_scaling=0.15,
                    planning_time=10.0,
                    pipeline_id='pilz_industrial_motion_planner',
                    planner_id='PTP',
                )
            else:
                success = move_to_task.move_to(
                    target_pose,
                    velocity_scaling=0.15,
                    acceleration_scaling=0.15,
                    planning_time=10.0,
                    pipeline_id='pilz_industrial_motion_planner',
                    planner_id='PTP',
                )
            if success:
                break
            move_to_task.get_logger().warn(
                f'Motion attempt {attempt}/{MAX_RETRIES} failed, retrying...'
            )
            # If the controller is already inactive (EC dropped mid-move), return
            # 503 immediately so robot_service restarts EC and retries the whole
            # request, rather than burning 30 s waiting for a recovery that only
            # robot_service can trigger.
            if not _wait_for_controller_active(timeout_sec=2.0):
                return jsonify({'success': False, 'error': 'scaled_joint_trajectory_controller inactive after move failure — EC dropped'}), 503

        if success:
            # Drain TEM deferred deactivate callback from the approach move.
            # ALL planners (OMPL, Pilz PTP, Pilz LIN) queue a TEM stop ~700ms
            # after trajectory completion. Without draining here, it fires during
            # the lift move_to() mid-execution → CONTROL_FAILED.
            _drain_end = time.time() + 1.5
            while time.time() < _drain_end:
                rclpy.spin_once(move_to_task, timeout_sec=0.1)

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
                    # Drain deferred deactivate callback from press LIN before lift LIN.
                    # Loop for the full 1.5s window so the TEM callback (~700ms) is
                    # guaranteed to be processed even if TF/clock callbacks compete first.
                    _drain_end = time.time() + 1.5
                    while time.time() < _drain_end:
                        rclpy.spin_once(move_to_task, timeout_sec=0.1)
                except Exception as tf_ex:
                    move_to_task.get_logger().error(f'Nailgun: TF lookup failed: {tf_ex}')

            time.sleep(0.5)  # settle after press before lift

            # Lift 5 cm straight up (Pilz LIN to stay on same IK branch)
            lift_pose = _build_pose(-x, -y, z + 0.05, yaw)
            lift_success = move_to_task.move_to(
                lift_pose,
                velocity_scaling=0.15,
                acceleration_scaling=0.15,
                planning_time=5.0,
                pipeline_id='pilz_industrial_motion_planner',
                planner_id='LIN',
            )
            # Drain the deferred controller deactivate callback before returning.
            # Without this, the callback fires ~700ms later during the next
            # move_to() spin — mid-execution — causing CONTROL_FAILED, or
            # the RT watchdog drops EC while the callback is unprocessed.
            # Loop for the full 1.5s window so the TEM callback (~700ms) is
            # guaranteed to be processed even if TF/clock callbacks compete first.
            _drain_end = time.time() + 1.5
            while time.time() < _drain_end:
                rclpy.spin_once(move_to_task, timeout_sec=0.1)

            if not lift_success:
                move_to_task.get_logger().error('Lift move failed')
                if not _wait_for_controller_active(timeout_sec=2.0):
                    return jsonify({'success': False, 'error': 'Lift failed — EC dropped during lift'}), 503
                return jsonify({'success': False, 'error': 'Lift move failed'}), 500

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


@app.route('/gripper', methods=['POST'])
def gripper():
    """Control the gripper via the ROS IO service (works while EC is running).

    URScript sent via port 30002 is ignored when External Control holds the
    fieldbus lock — so gripper commands must go through the ROS driver instead.

    Expected JSON body:
        commandType  (str) — "close_gripper" or "open_gripper"
    """
    try:
        data = request.json
        command_type = data.get('commandType', 'close_gripper')

        if not io_client.wait_for_service(timeout_sec=5.0):
            return jsonify({'success': False, 'error': 'IO service not available'}), 500

        def _set_io(pin, state):
            req = SetIO.Request()
            req.fun = 1        # FUN_SET_DIGITAL_OUT
            req.pin = pin
            req.state = float(state)
            fut = io_client.call_async(req)
            rclpy.spin_until_future_complete(move_to_task, fut, timeout_sec=5.0)
            return fut.result() is not None and fut.result().success

        if command_type == 'close_gripper':
            # TDO1=True (close), wait, TDO0=False — mirrors URScript sequence
            ok1 = _set_io(17, 1.0)
            time.sleep(0.5)
            ok2 = _set_io(16, 0.0)
            if ok1 and ok2:
                return jsonify({'success': True, 'message': 'Gripper closed'})
            return jsonify({'success': False, 'error': 'IO set_io call failed'}), 500

        elif command_type == 'open_gripper':
            # TDO0=True (open)
            ok = _set_io(16, 1.0)
            time.sleep(0.5)
            if ok:
                return jsonify({'success': True, 'message': 'Gripper opened'})
            return jsonify({'success': False, 'error': 'IO set_io call failed'}), 500

        else:
            return jsonify({'success': False, 'error': f'Unknown commandType: {command_type}'}), 400

    except Exception as e:
        import traceback; traceback.print_exc()
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/move_lin', methods=['POST'])
def move_lin():
    """Execute a Cartesian linear move using Pilz LIN planner.

    Used for movel moves: straight-line TCP path to a Cartesian pose.

    Expected JSON body:
        pose            (list[6]) — [x, y, z, rx, ry, rz] target pose
        end_effector_type (str)   — "gripper" or "nailgun" (default: "gripper")
        velocity        (float)   — velocity scaling 0-1 (default: 0.15)
        acceleration    (float)   — acceleration scaling 0-1 (default: 0.15)
    """
    try:
        data = request.json
        print(f"Received /move_lin request: {data}")

        pose = data.get('pose')
        if not pose or len(pose) < 6:
            return jsonify({'success': False, 'error': 'pose [x,y,z,rx,ry,rz] required'}), 400

        end_effector_type = data.get('end_effector_type', 'gripper')
        vel = float(data.get('velocity', 0.15))
        acc = float(data.get('acceleration', 0.15))

        move_to_task.end_effector_link = EE_LINK_MAP.get(end_effector_type, 'tool0')

        # Disable collisions for the inactive EE (only once per EE type)
        if end_effector_type in ('gripper', 'nailgun'):
            if _last_disabled_ee[0] != end_effector_type:
                move_to_task.disable_inactive_ee(end_effector_type)
                _last_disabled_ee[0] = end_effector_type
                time.sleep(2.0)

        # Convert rotation vector (rx, ry) to yaw for _build_pose
        rx, ry = pose[3], pose[4]
        half_theta = math.atan2(ry, rx)
        yaw_deg = 2.0 * half_theta * (180.0 / math.pi)

        target_pose = _build_pose(-pose[0], -pose[1], pose[2], yaw_deg)

        stop_msg = StringMsg()
        stop_msg.data = 'stop'

        # Confirm the controller is active right now — setup sleeps above can
        # take up to 4 s, during which the controller may have dropped.
        # 10s is sufficient: robot_service already confirmed active before sending
        # this request, so this only catches drops during the setup phase.
        if not _wait_for_controller_active(timeout_sec=2.0):
            return jsonify({'success': False, 'error': 'scaled_joint_trajectory_controller not active — EC disconnected'}), 503

        start = time.time()
        MAX_RETRIES = 3
        success = False
        for attempt in range(1, MAX_RETRIES + 1):
            if attempt > 1:
                # Publish stop only before retries (same reasoning as plan_and_execute:
                # publishing stop then immediately sending a goal races with MoveGroup).
                stop_pub.publish(stop_msg)
                rclpy.spin_once(move_to_task, timeout_sec=0.1)
                if not _wait_for_controller_active(timeout_sec=2.0):
                    return jsonify({'success': False, 'error': 'scaled_joint_trajectory_controller inactive before retry — EC dropped'}), 503
            move_to_task.get_logger().info(f'move_lin attempt {attempt}/{MAX_RETRIES}')
            success = move_to_task.move_to(
                target_pose,
                velocity_scaling=vel,
                acceleration_scaling=acc,
                planning_time=10.0,
                pipeline_id='pilz_industrial_motion_planner',
                planner_id='LIN',
            )
            if success:
                break
            move_to_task.get_logger().warn(
                f'move_lin attempt {attempt}/{MAX_RETRIES} failed, retrying...'
            )
            if not _wait_for_controller_active(timeout_sec=2.0):
                return jsonify({'success': False, 'error': 'scaled_joint_trajectory_controller inactive after move failure — EC dropped'}), 503
        elapsed = time.time() - start

        if success:
            # Drain MoveGroup's deferred handle-retirement callback before returning.
            # The TrajectoryExecutionManager queues an internal stop ~700ms after a
            # completed Pilz LIN.  If we return immediately that callback fires later
            # during spin_until_future_complete inside the *next* move_to() call —
            # while the controller is mid-execution — causing CONTROL_FAILED.
            # Spinning here lets the deactivate/reactivate cycle complete while the
            # robot is idle, so the controller is clean for the next request.
            # Loop for the full 1.5s window so the TEM callback (~700ms) is
            # guaranteed to be processed even if TF/clock callbacks compete first.
            _drain_end = time.time() + 1.5
            while time.time() < _drain_end:
                rclpy.spin_once(move_to_task, timeout_sec=0.1)
            return jsonify({'success': True, 'executionTimeSeconds': elapsed})
        else:
            return jsonify({'success': False, 'error': 'Pilz LIN move failed',
                            'executionTimeSeconds': elapsed}), 500
    except Exception as e:
        import traceback; traceback.print_exc()
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/move_joints', methods=['POST'])
def move_joints():
    """Execute a joint-space move using Pilz PTP.

    Used for movej moves: joint-interpolated motion to a joint configuration
    or a Cartesian pose (resolved via IK inside MoveIt).

    Expected JSON body:
        joints          (list[6]) — joint angles [j1..j6] in radians
        end_effector_type (str)   — "gripper" or "nailgun" (default: "gripper")
        velocity        (float)   — velocity scaling 0-1 (default: 0.3)
        acceleration    (float)   — acceleration scaling 0-1 (default: 0.3)
    """
    try:
        data = request.json
        print(f"Received /move_joints request: {data}")

        joints = data.get('joints')
        if not joints or len(joints) < 6:
            return jsonify({'success': False, 'error': 'joints [j1..j6] required'}), 400

        end_effector_type = data.get('end_effector_type', 'gripper')
        vel = float(data.get('velocity', 0.3))
        acc = float(data.get('acceleration', 0.3))

        move_to_task.end_effector_link = EE_LINK_MAP.get(end_effector_type, 'tool0')

        # Disable collisions for the inactive EE (only once per EE type)
        if end_effector_type in ('gripper', 'nailgun'):
            if _last_disabled_ee[0] != end_effector_type:
                move_to_task.disable_inactive_ee(end_effector_type)
                _last_disabled_ee[0] = end_effector_type
                time.sleep(2.0)

        stop_msg = StringMsg()
        stop_msg.data = 'stop'

        # Confirm the controller is active right now — setup sleeps above can
        # take up to 4 s, during which the controller may have dropped.
        # 10s is sufficient: robot_service already confirmed active before sending
        # this request, so this only catches drops during the setup phase.
        if not _wait_for_controller_active(timeout_sec=10.0):
            return jsonify({'success': False, 'error': 'scaled_joint_trajectory_controller not active — EC disconnected'}), 503

        start = time.time()
        MAX_RETRIES = 3
        success = False
        for attempt in range(1, MAX_RETRIES + 1):
            if attempt > 1:
                stop_pub.publish(stop_msg)
                rclpy.spin_once(move_to_task, timeout_sec=0.1)
                if not _wait_for_controller_active(timeout_sec=2.0):
                    return jsonify({'success': False, 'error': 'scaled_joint_trajectory_controller inactive before retry — EC dropped'}), 503
            move_to_task.get_logger().info(f'move_joints attempt {attempt}/{MAX_RETRIES}')
            success = move_to_task.move_to_joints(
                joint_values=joints,
                velocity_scaling=vel,
                acceleration_scaling=acc,
                planning_time=10.0,
                use_pilz_ptp=True,
            )
            if success:
                break
            move_to_task.get_logger().warn(
                f'move_joints attempt {attempt}/{MAX_RETRIES} failed, retrying...'
            )
            if not _wait_for_controller_active(timeout_sec=2.0):
                return jsonify({'success': False, 'error': 'scaled_joint_trajectory_controller inactive after move failure — EC dropped'}), 503
        elapsed = time.time() - start

        if success:
            # Drain MoveGroup's deferred TEM callback (~700ms after trajectory end)
            # before returning, so it doesn't fire mid-execution of the next move.
            _drain_end = time.time() + 1.5
            while time.time() < _drain_end:
                rclpy.spin_once(move_to_task, timeout_sec=0.1)
            return jsonify({'success': True, 'executionTimeSeconds': elapsed})
        else:
            return jsonify({'success': False, 'error': 'Joint-space move failed',
                            'executionTimeSeconds': elapsed}), 500
    except Exception as e:
        import traceback; traceback.print_exc()
        return jsonify({'success': False, 'error': str(e)}), 500


if __name__ == '__main__':
    print("Starting MoveIt Bridge Service...")
    init_ros()
    print("Listening on http://127.0.0.1:5002")
    # threaded=False is required: rclpy only allows one executor to spin at a time.
    # Concurrent Flask threads (the default) would race on spin_once / spin_until_future_complete
    # and raise "Executor is already spinning". The BT engine is sequential anyway, so
    # serialising requests here has no practical cost.
    app.run(host='127.0.0.1', port=5002, debug=False, threaded=False)
