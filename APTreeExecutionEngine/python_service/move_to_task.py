#!/usr/bin/env python3
"""
Simple moveTo task: Move robot end-effector to a specified Cartesian pose.
"""
import rclpy
from rclpy.node import Node
from rclpy.action import ActionClient
from moveit_msgs.action import MoveGroup
from moveit_msgs.msg import (
    MotionPlanRequest,
    Constraints,
    JointConstraint,
    PositionConstraint,
    OrientationConstraint,
    BoundingVolume,
    PlanningScene,
    AllowedCollisionMatrix,
    AllowedCollisionEntry
)
from geometry_msgs.msg import Pose, PoseStamped, Vector3
from shape_msgs.msg import SolidPrimitive
from moveit_msgs.srv import GetPlanningScene
from ur_msgs.srv import SetPayload, SetIO
import time
import argparse
import math
import rclpy.time
import tf2_ros


# Map end-effector types to their tip link names
EE_LINK_MAP = {
    'none': 'tool0',
    'gripper': 'gripper_tip',
    'nailgun': 'nailgun_tip',
}

# Links belonging to each end-effector
EE_LINKS = {
    'gripper': ['gripper_base', 'gripper_left_finger', 'gripper_right_finger', 'gripper_tip'],
    'nailgun': ['nailgun_base', 'nailgun_tip'],
}

# Payload configuration per end-effector type
# mass in kg, cog [x, y, z] in meters relative to tool0
EE_PAYLOAD = {
    'none':    {'mass': 0.0,  'cog': [0.0, 0.0, 0.0]},
    'gripper': {'mass': 0.95, 'cog': [-0.001, 0.015, 0.028]},
    'nailgun': {'mass': 8.25, 'cog': [-0.013, 0.001, 0.151]},
}

# Joint names in order for the ur_manipulator planning group
UR_JOINT_NAMES = [
    'shoulder_pan_joint',
    'shoulder_lift_joint',
    'elbow_joint',
    'wrist_1_joint',
    'wrist_2_joint',
    'wrist_3_joint',
]


class MoveToTask(Node):
    """Simple task to move robot to a Cartesian pose."""
    
    def __init__(self, end_effector_type='none'):
        super().__init__('move_to_task')
        
        # Action client for MoveGroup
        self.move_group_client = ActionClient(self, MoveGroup, '/move_action')
        
        # Configuration
        self.planning_group = 'ur_manipulator'
        self.end_effector_link = EE_LINK_MAP.get(end_effector_type, 'tool0')
        self.reference_frame = 'base_link'
        self.get_logger().info(f'End-effector link: {self.end_effector_link}')
        
        # Wait for action server
        self.get_logger().info('Waiting for move_group action server...')
        if not self.move_group_client.wait_for_server(timeout_sec=10.0):
            self.get_logger().error('MoveGroup action server not available!')
            raise RuntimeError('MoveGroup action server timeout')
        
        self.get_logger().info('Connected to move_group')

    def set_robot_payload(self, end_effector_type):
        """Set the robot payload via the ur_robot_driver's ROS2 service.

        This is safe to call while external_control.urp is running because it
        goes through the driver (not URScript port 30002).
        """
        cfg = EE_PAYLOAD.get(end_effector_type, EE_PAYLOAD['none'])

        client = self.create_client(SetPayload, '/io_and_status_controller/set_payload')
        if not client.wait_for_service(timeout_sec=5.0):
            self.get_logger().warn('set_payload service not available — skipping')
            return False

        req = SetPayload.Request()
        req.mass = float(cfg['mass'])
        req.center_of_gravity = Vector3(
            x=cfg['cog'][0], y=cfg['cog'][1], z=cfg['cog'][2]
        )

        future = client.call_async(req)
        rclpy.spin_until_future_complete(self, future)
        result = future.result()
        if result and result.success:
            self.get_logger().info(
                f'Payload set: {cfg["mass"]} kg, COG {cfg["cog"]}'
            )
            return True
        else:
            self.get_logger().warn(f'set_payload call returned success=False')
            return False

    def disable_inactive_ee(self, active_ee):
        """
        When launched with 'both' end-effectors, disable ALL collisions for the
        inactive one by publishing an ACM update via /planning_scene.
        This includes collisions with robot links AND scene objects (e.g. table).
        """
        inactive = 'nailgun' if active_ee == 'gripper' else 'gripper'
        inactive_links = EE_LINKS.get(inactive, [])
        if not inactive_links:
            return
        
        # Get current planning scene (ACM + world objects)
        client = self.create_client(GetPlanningScene, '/get_planning_scene')
        if not client.wait_for_service(timeout_sec=5.0):
            self.get_logger().warn('GetPlanningScene service not available, skipping ACM update')
            return
        
        req = GetPlanningScene.Request()
        req.components.components = (
            req.components.ALLOWED_COLLISION_MATRIX | req.components.WORLD_OBJECT_NAMES
        )
        future = client.call_async(req)
        rclpy.spin_until_future_complete(self, future)
        result = future.result().scene
        current_acm = result.allowed_collision_matrix
        
        # Gather all scene object names (table, etc.)
        scene_object_names = [obj.id for obj in result.world.collision_objects]
        
        # Build updated ACM: allow collisions between inactive links and everything
        entry_names = list(current_acm.entry_names)
        entry_values = [list(e.enabled) for e in current_acm.entry_values]
        
        # Also ensure scene objects are in the ACM
        for obj_name in scene_object_names:
            if obj_name not in entry_names:
                idx = len(entry_names)
                entry_names.append(obj_name)
                for row in entry_values:
                    row.append(False)
                entry_values.append([False] * len(entry_names))
        
        # Now set all entries for inactive links to True (allow all collisions)
        for link in inactive_links:
            if link not in entry_names:
                idx = len(entry_names)
                entry_names.append(link)
                for row in entry_values:
                    row.append(True)
                entry_values.append([True] * len(entry_names))
            else:
                idx = entry_names.index(link)
                for i in range(len(entry_names)):
                    entry_values[idx][i] = True
                    entry_values[i][idx] = True
        
        # Publish updated ACM
        new_acm = AllowedCollisionMatrix()
        new_acm.entry_names = entry_names
        for row in entry_values:
            entry = AllowedCollisionEntry()
            entry.enabled = row
            new_acm.entry_values.append(entry)
        
        scene_pub = self.create_publisher(PlanningScene, '/planning_scene', 10)
        scene_msg = PlanningScene()
        scene_msg.is_diff = True
        scene_msg.allowed_collision_matrix = new_acm
        
        # Publish a few times to make sure it's received
        for _ in range(5):
            scene_pub.publish(scene_msg)
            time.sleep(0.1)
        
        self.get_logger().info(f'Disabled collisions for inactive end-effector: {inactive}')
    
    def move_to_joints(self, joint_values, velocity_scaling=0.3, acceleration_scaling=0.3,
                       planning_time=5.0, tolerance=0.01, use_pilz_ptp=False):
        """Move to a joint-space configuration using OMPL RRTConnect.

        Args:
            joint_values: list of 6 floats [j1..j6] in radians
            velocity_scaling: fraction of max velocity (0.0-1.0)
            acceleration_scaling: fraction of max acceleration (0.0-1.0)
            planning_time: max planning time in seconds
            tolerance: per-joint position tolerance in radians

        Returns:
            bool: True if motion succeeded, False otherwise
        """
        self.get_logger().info(
            f'Joint-space move to: {[round(v, 3) for v in joint_values]}'
        )

        goal = MoveGroup.Goal()
        goal.request.group_name = self.planning_group
        goal.request.num_planning_attempts = 3
        goal.request.allowed_planning_time = planning_time
        goal.request.max_velocity_scaling_factor = velocity_scaling
        goal.request.max_acceleration_scaling_factor = acceleration_scaling
        goal.request.workspace_parameters.header.frame_id = self.reference_frame
        if use_pilz_ptp:
            goal.request.pipeline_id = 'pilz_industrial_motion_planner'
            goal.request.planner_id = 'PTP'
            goal.request.allowed_planning_time = 1.0  # Pilz PTP plans in <5ms
        else:
            goal.request.pipeline_id = 'ompl'
            goal.request.planner_id = 'RRTConnect'

        constraints = Constraints()
        constraints.name = 'joint_target'
        for name, value in zip(UR_JOINT_NAMES, joint_values):
            jc = JointConstraint()
            jc.joint_name = name
            jc.position = float(value)
            jc.tolerance_above = tolerance
            jc.tolerance_below = tolerance
            jc.weight = 1.0
            constraints.joint_constraints.append(jc)
        goal.request.goal_constraints.append(constraints)

        goal.planning_options.plan_only = True
        goal.planning_options.planning_scene_diff.is_diff = True
        goal.planning_options.planning_scene_diff.robot_state.is_diff = True
        goal.planning_options.replan = False
        goal.planning_options.replan_attempts = 0
        goal.planning_options.replan_delay = 0.0

        send_goal_future = self.move_group_client.send_goal_async(goal)
        rclpy.spin_until_future_complete(self, send_goal_future)

        goal_handle = send_goal_future.result()
        if not goal_handle.accepted:
            self.get_logger().error('Joint-space goal rejected by move_group')
            return False

        self.get_logger().info('Joint goal accepted, planning...')
        result_future = goal_handle.get_result_async()
        rclpy.spin_until_future_complete(self, result_future)
        rclpy.spin_once(self, timeout_sec=0.5)

        result = result_future.result().result
        if result.error_code.val == 1:
            self.get_logger().info('Joint-space move succeeded!')
            return True

        _codes = {
            -1: 'PLANNING_FAILED', -4: 'CONTROL_FAILED', -6: 'TIMED_OUT',
            -10: 'START_STATE_IN_COLLISION', -12: 'GOAL_IN_COLLISION',
            -31: 'NO_IK_SOLUTION',
        }
        self.get_logger().error(
            f'Joint-space move failed: '
            f'{_codes.get(result.error_code.val, "UNKNOWN")} ({result.error_code.val})'
        )
        return False

    def move_to(self, target_pose, velocity_scaling=0.1, acceleration_scaling=0.1,
                planning_time=5.0, tolerance_position=0.001, tolerance_orientation=0.01,
                path_constraints=None, pipeline_id='ompl', planner_id='RRTConnect'):
        """
        Move end-effector to target Cartesian pose.
        
        Args:
            target_pose: geometry_msgs/Pose - target position and orientation
            velocity_scaling: Max velocity as fraction of maximum (0.0-1.0)
            acceleration_scaling: Max acceleration as fraction of maximum (0.0-1.0)
            planning_time: Maximum time for planning in seconds
            tolerance_position: Position tolerance in meters
            tolerance_orientation: Orientation tolerance in radians
            
        Returns:
            bool: True if motion succeeded, False otherwise
        """
        self.get_logger().info(f'Planning motion to pose: '
                              f'pos=({target_pose.position.x:.3f}, '
                              f'{target_pose.position.y:.3f}, '
                              f'{target_pose.position.z:.3f})')
        
        # Create goal
        goal = MoveGroup.Goal()
        
        # Set planning parameters
        goal.request.group_name = self.planning_group
        goal.request.num_planning_attempts = 3
        goal.request.allowed_planning_time = planning_time
        goal.request.max_velocity_scaling_factor = velocity_scaling
        goal.request.max_acceleration_scaling_factor = acceleration_scaling
        goal.request.workspace_parameters.header.frame_id = self.reference_frame
        goal.request.pipeline_id = pipeline_id
        goal.request.planner_id = planner_id
        
        # Set target pose as goal constraint
        constraints = Constraints()
        constraints.name = 'target_pose'
        
        # Position constraint
        position_constraint = PositionConstraint()
        position_constraint.header.frame_id = self.reference_frame
        position_constraint.link_name = self.end_effector_link
        position_constraint.target_point_offset.x = 0.0
        position_constraint.target_point_offset.y = 0.0
        position_constraint.target_point_offset.z = 0.0
        
        # Define constraint region as small box around target
        constraint_region = BoundingVolume()
        box = SolidPrimitive()
        box.type = SolidPrimitive.BOX
        box.dimensions = [tolerance_position * 2, tolerance_position * 2, tolerance_position * 2]
        constraint_region.primitives.append(box)
        
        # Box pose at target position
        box_pose = Pose()
        box_pose.position = target_pose.position
        box_pose.orientation.w = 1.0
        constraint_region.primitive_poses.append(box_pose)
        
        position_constraint.constraint_region = constraint_region
        position_constraint.weight = 1.0
        
        # Orientation constraint
        orientation_constraint = OrientationConstraint()
        orientation_constraint.header.frame_id = self.reference_frame
        orientation_constraint.link_name = self.end_effector_link
        orientation_constraint.orientation = target_pose.orientation
        orientation_constraint.absolute_x_axis_tolerance = tolerance_orientation
        orientation_constraint.absolute_y_axis_tolerance = tolerance_orientation
        orientation_constraint.absolute_z_axis_tolerance = tolerance_orientation
        orientation_constraint.weight = 1.0
        
        # Add constraints to goal
        constraints.position_constraints.append(position_constraint)
        constraints.orientation_constraints.append(orientation_constraint)
        goal.request.goal_constraints.append(constraints)
        
        # Path constraints (applied throughout the entire trajectory)
        if path_constraints is not None:
            goal.request.path_constraints = path_constraints
            self.get_logger().info('Path constraints applied to trajectory')
        
        # Planning options
        goal.planning_options.plan_only = True  # Plan only — do NOT execute
        goal.planning_options.planning_scene_diff.is_diff = True
        goal.planning_options.planning_scene_diff.robot_state.is_diff = True
        goal.planning_options.replan = False
        goal.planning_options.replan_attempts = 0
        goal.planning_options.replan_delay = 0.0
        
        # Send goal
        self.get_logger().info('Sending planning request to move_group...')
        send_goal_future = self.move_group_client.send_goal_async(goal)
        rclpy.spin_until_future_complete(self, send_goal_future)
        
        goal_handle = send_goal_future.result()
        if not goal_handle.accepted:
            self.get_logger().error('Goal rejected by move_group')
            return False
        
        self.get_logger().info('Goal accepted, executing motion...')
        
        # Wait for result
        result_future = goal_handle.get_result_async()
        rclpy.spin_until_future_complete(self, result_future)

        # Flush any pending execution/feedback callbacks before returning.
        # This ensures the goal handle is fully retired in MoveGroup's internal state.
        rclpy.spin_once(self, timeout_sec=0.5)

        result = result_future.result().result
        
        # Check result
        if result.error_code.val == 1:  # SUCCESS
            self.get_logger().info('Motion completed successfully!')
            return True
        else:
            error_codes = {
                -1: 'PLANNING_FAILED',
                -2: 'INVALID_MOTION_PLAN',
                -3: 'MOTION_PLAN_INVALIDATED_BY_ENVIRONMENT_CHANGE',
                -4: 'CONTROL_FAILED',
                -5: 'UNABLE_TO_AQUIRE_SENSOR_DATA',
                -6: 'TIMED_OUT',
                -7: 'PREEMPTED',
                -10: 'START_STATE_IN_COLLISION',
                -11: 'START_STATE_VIOLATES_PATH_CONSTRAINTS',
                -12: 'GOAL_IN_COLLISION',
                -13: 'GOAL_VIOLATES_PATH_CONSTRAINTS',
                -14: 'GOAL_CONSTRAINTS_VIOLATED',
                -15: 'INVALID_GROUP_NAME',
                -16: 'INVALID_GOAL_CONSTRAINTS',
                -17: 'INVALID_ROBOT_STATE',
                -18: 'INVALID_LINK_NAME',
                -26: 'START_STATE_INVALID',
                -27: 'GOAL_STATE_INVALID',
                -28: 'UNRECOGNIZED_GOAL_TYPE',
                -31: 'NO_IK_SOLUTION',
                99999: 'FAILURE',
            }
            error_name = error_codes.get(result.error_code.val, 'UNKNOWN_ERROR')
            self.get_logger().error(f'Motion failed: {error_name} (code: {result.error_code.val})')
            return False


    # ----------------------------------------------------------------------
    # Plan-only variants: build the same MoveGroup goal as move_to /
    # move_to_joints but return the planned RobotTrajectory instead of a
    # bool.  Caller is responsible for executing the trajectory elsewhere
    # (e.g. by streaming it as URScript movej from robot_service.py).
    # ----------------------------------------------------------------------
    def _send_plan_goal(self, goal):
        '''Send a MoveGroup.Goal (with plan_only=True) and return the planned
        moveit_msgs/RobotTrajectory, or None on failure.'''
        send_goal_future = self.move_group_client.send_goal_async(goal)
        rclpy.spin_until_future_complete(self, send_goal_future)
        goal_handle = send_goal_future.result()
        if goal_handle is None or not goal_handle.accepted:
            self.get_logger().error('plan goal rejected by move_group')
            return None
        result_future = goal_handle.get_result_async()
        rclpy.spin_until_future_complete(self, result_future)
        rclpy.spin_once(self, timeout_sec=0.5)
        result = result_future.result().result
        if result.error_code.val != 1:
            self.get_logger().error(
                f'plan failed: error_code={result.error_code.val}'
            )
            return None
        traj = result.planned_trajectory
        if not traj.joint_trajectory.points:
            self.get_logger().error('plan returned empty trajectory')
            return None
        return traj

    def plan_to(self, target_pose, velocity_scaling=0.1, acceleration_scaling=0.1,
                planning_time=5.0, tolerance_position=0.001, tolerance_orientation=0.01,
                path_constraints=None, pipeline_id='ompl', planner_id='RRTConnect',
                wrist3_lock=None, wrist3_tolerance=0.05):
        '''Plan a Cartesian-pose move; return planned RobotTrajectory or None.

        wrist3_lock: if set, adds a JointConstraint pinning wrist_3_joint to
        this value (±wrist3_tolerance rad) on the GOAL constraints. Use to
        force the no-flip IK branch when the target pose is at a horizontal-
        axis π singularity (two IK solutions differ by π on wrist3).
        '''
        self.get_logger().info(
            f'plan_to pose=({target_pose.position.x:.3f}, '
            f'{target_pose.position.y:.3f}, {target_pose.position.z:.3f}) '
            f'pipeline={pipeline_id} planner={planner_id}'
            + (f' wrist3_lock={wrist3_lock:.4f}' if wrist3_lock is not None else '')
        )
        goal = MoveGroup.Goal()
        goal.request.group_name = self.planning_group
        goal.request.num_planning_attempts = 3
        goal.request.allowed_planning_time = planning_time
        goal.request.max_velocity_scaling_factor = velocity_scaling
        goal.request.max_acceleration_scaling_factor = acceleration_scaling
        goal.request.workspace_parameters.header.frame_id = self.reference_frame
        goal.request.pipeline_id = pipeline_id
        goal.request.planner_id = planner_id

        constraints = Constraints()
        constraints.name = 'target_pose'
        position_constraint = PositionConstraint()
        position_constraint.header.frame_id = self.reference_frame
        position_constraint.link_name = self.end_effector_link
        constraint_region = BoundingVolume()
        box = SolidPrimitive()
        box.type = SolidPrimitive.BOX
        box.dimensions = [tolerance_position * 2] * 3
        constraint_region.primitives.append(box)
        box_pose = Pose()
        box_pose.position = target_pose.position
        box_pose.orientation.w = 1.0
        constraint_region.primitive_poses.append(box_pose)
        position_constraint.constraint_region = constraint_region
        position_constraint.weight = 1.0

        orientation_constraint = OrientationConstraint()
        orientation_constraint.header.frame_id = self.reference_frame
        orientation_constraint.link_name = self.end_effector_link
        orientation_constraint.orientation = target_pose.orientation
        orientation_constraint.absolute_x_axis_tolerance = tolerance_orientation
        orientation_constraint.absolute_y_axis_tolerance = tolerance_orientation
        orientation_constraint.absolute_z_axis_tolerance = tolerance_orientation
        orientation_constraint.weight = 1.0

        constraints.position_constraints.append(position_constraint)
        constraints.orientation_constraints.append(orientation_constraint)
        if wrist3_lock is not None:
            jc = JointConstraint()
            jc.joint_name = 'wrist_3_joint'
            jc.position = float(wrist3_lock)
            jc.tolerance_above = float(wrist3_tolerance)
            jc.tolerance_below = float(wrist3_tolerance)
            jc.weight = 1.0
            constraints.joint_constraints.append(jc)
        goal.request.goal_constraints.append(constraints)

        if path_constraints is not None:
            goal.request.path_constraints = path_constraints

        goal.planning_options.plan_only = True
        goal.planning_options.planning_scene_diff.is_diff = True
        goal.planning_options.planning_scene_diff.robot_state.is_diff = True
        goal.planning_options.replan = False
        return self._send_plan_goal(goal)

    def plan_to_joints(self, joint_values, velocity_scaling=0.3, acceleration_scaling=0.3,
                       planning_time=5.0, tolerance=0.01, use_pilz_ptp=False):
        '''Plan a joint-space move; return planned RobotTrajectory or None.'''
        self.get_logger().info(
            f'plan_to_joints {[round(v, 3) for v in joint_values]} '
            f'pilz={use_pilz_ptp}'
        )
        goal = MoveGroup.Goal()
        goal.request.group_name = self.planning_group
        goal.request.num_planning_attempts = 3
        goal.request.allowed_planning_time = planning_time
        goal.request.max_velocity_scaling_factor = velocity_scaling
        goal.request.max_acceleration_scaling_factor = acceleration_scaling
        goal.request.workspace_parameters.header.frame_id = self.reference_frame
        if use_pilz_ptp:
            goal.request.pipeline_id = 'pilz_industrial_motion_planner'
            goal.request.planner_id = 'PTP'
            goal.request.allowed_planning_time = 1.0
        else:
            goal.request.pipeline_id = 'ompl'
            goal.request.planner_id = 'RRTConnect'

        constraints = Constraints()
        constraints.name = 'joint_target'
        for name, value in zip(UR_JOINT_NAMES, joint_values):
            jc = JointConstraint()
            jc.joint_name = name
            jc.position = float(value)
            jc.tolerance_above = tolerance
            jc.tolerance_below = tolerance
            jc.weight = 1.0
            constraints.joint_constraints.append(jc)
        goal.request.goal_constraints.append(constraints)

        goal.planning_options.plan_only = True
        goal.planning_options.planning_scene_diff.is_diff = True
        goal.planning_options.planning_scene_diff.robot_state.is_diff = True
        goal.planning_options.replan = False
        return self._send_plan_goal(goal)





def main():
    """Example: Add object to scene and move to grasp position above it."""
    parser = argparse.ArgumentParser(description='Move robot to a target pose')
    parser.add_argument('--x', type=float, default=-0.5, help='Target X coordinate (default: -0.5)')
    parser.add_argument('--y', type=float, default=-0.5, help='Target Y coordinate (default: -0.5)')
    parser.add_argument('--z', type=float, default=0.001, help='Target Z coordinate (default: 0.001)')
    parser.add_argument('--yaw', type=float, default=180.0, help='Tool0 yaw in degrees (rotation around Z while facing down, default: 180)')
    parser.add_argument('--end_effector_type', type=str, default='none',
                        choices=['none', 'gripper', 'nailgun'],
                        help='End-effector type to plan with (default: none = flange)')
    parser.add_argument('--no_object', action='store_true',
                        help='Skip attaching object to gripper')
    parser.add_argument('--both_loaded', action='store_true',
                        help='Set when launched with end_effector_type:=both to disable inactive EE collisions')
    args = parser.parse_args()

    # Nailgun always uses fixed orientation (facing down, yaw=90 to align with X)
    if args.end_effector_type == 'nailgun':
        args.yaw = 90.0
        args.no_object = True

    rclpy.init()
    
    # Import scene manager
    from dynamic_scene_example import DynamicSceneManager
    
    # Create scene manager and motion planner
    scene = DynamicSceneManager()
    move_to_task = MoveToTask(end_effector_type=args.end_effector_type)
    tf_buffer = tf2_ros.Buffer()
    tf2_ros.TransformListener(tf_buffer, move_to_task)
    
    # When both EEs are loaded, disable collisions for the inactive one
    if args.both_loaded and args.end_effector_type in ('gripper', 'nailgun'):
        move_to_task.disable_inactive_ee(args.end_effector_type)

    # Set correct payload on the real robot controller via the UR driver service
    move_to_task.set_robot_payload(args.end_effector_type)

    if not args.no_object:
        # Step 1: Attach object to gripper so it moves with the robot
        # Object mesh: X(-0.01 to 0.01), Y(-0.1874 to 0.1876), Z(-0.02 to 0.0)
        # gripper_base axes now match tool0: Z+ outward, X+ left, Y+ up
        # gripper_tip is at (0.01, 0.0, 0.148) in gripper_base frame
        # Place object at fingertips along Z (outward), centered on X/Y
        move_to_task.get_logger().info('Step 1: Attaching object to gripper')

        touch_links = ['gripper_base', 'gripper_left_finger', 'gripper_right_finger', 'tool0']
        if args.both_loaded:
            touch_links.extend(['nailgun_base', 'nailgun_tip'])

        scene.attach_mesh_object(
            object_id='target_object',
            mesh_path='object',
            link_name='gripper_base',
            pos=(0.0, 0.0, 0.1225),
            scale=(1.0, 1.0, 1.0),
            touch_links=touch_links
        )
        
        # Wait for scene to fully update before planning
        time.sleep(5.0)
    
    # Step 2: Define target pose
    move_to_task.get_logger().info('Step 2: Moving to target position')
    
    target_pose = Pose()
    
    # Position: 15cm above the object
    target_pose.position.x = args.x
    target_pose.position.y = args.y
    target_pose.position.z = args.z 

    # Orientation: tool0 facing straight down + yaw around Z
    # RPY = (pi, 0, yaw) → quaternion = (cos(yaw/2), sin(yaw/2), 0, 0)
    yaw_rad = math.radians(args.yaw)
    cy = math.cos(yaw_rad / 2)
    sy = math.sin(yaw_rad / 2)
    target_pose.orientation.x = cy
    target_pose.orientation.y = sy
    target_pose.orientation.z = 0.0
    target_pose.orientation.w = 0.0
    move_to_task.get_logger().info(f'Orientation: facing down, tool0 yaw={args.yaw} deg')
    
    # Execute motion — nailgun uses Pilz PTP (deterministic, no path constraints needed)
    # Gripper uses OMPL RRTConnect (default)
    is_nailgun = args.end_effector_type == 'nailgun'
    success = move_to_task.move_to(
        target_pose,
        velocity_scaling=0.15,
        acceleration_scaling=0.15,
        planning_time=10.0,
        pipeline_id='pilz_industrial_motion_planner' if is_nailgun else 'ompl',
        planner_id='PTP' if is_nailgun else 'RRTConnect',
    )
    
    if success:
        move_to_task.get_logger().info('Successfully reached target position!')
        # Detach object from gripper, leaving it in the scene at its current pose
        if not args.no_object:
            scene.detach_object('target_object', link_name='gripper_base')
            move_to_task.get_logger().info('Object detached and left in scene')

        # Nailgun: press 2mm down using actual TCP pose from TF
        if args.end_effector_type == 'nailgun':
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

        # Open gripper after reaching target (tool digital output 0 = ON)
        if args.end_effector_type == 'gripper':
            move_to_task.get_logger().info('Opening gripper via SetIO...')
            io_client = move_to_task.create_client(SetIO, '/io_and_status_controller/set_io')
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
                time.sleep(0.5)  # Allow gripper to physically open
            else:
                move_to_task.get_logger().error('SetIO service not available')

        # Lift 5 cm straight up from current position (Cartesian Z+)
        lift_pose = Pose()
        lift_pose.position.x = args.x
        lift_pose.position.y = args.y
        lift_pose.position.z = args.z + 0.05
        lift_pose.orientation = target_pose.orientation
        move_to_task.get_logger().info('Lifting 5 cm straight up via MoveIt...')
        lift_success = move_to_task.move_to(
            lift_pose,
            velocity_scaling=0.15,
            acceleration_scaling=0.15,
            planning_time=5.0,
            pipeline_id='pilz_industrial_motion_planner',
            planner_id='LIN',
        )
        # flush Pilz execution handle
        rclpy.spin_once(move_to_task, timeout_sec=1.0)
        if lift_success:
            move_to_task.get_logger().info('Lift completed')
        else:
            move_to_task.get_logger().error('Lift failed')
    else:
        move_to_task.get_logger().error('Failed to reach target position')
    
    # Cleanup
    scene.destroy_node()
    move_to_task.destroy_node()
    rclpy.shutdown()


if __name__ == '__main__':
    main()
