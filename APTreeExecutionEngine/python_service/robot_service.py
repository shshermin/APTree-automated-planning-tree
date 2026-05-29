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
from ur10_commands import _run_urscript_with_done
from ur10_commands import PYTHON_HOST_IP, CALLBACK_PORT
from ur10_commands import execute_trajectory as _execute_trajectory
from ur10_commands import get_current_pose as _get_current_pose

DEFAULT_ROBOT_IP = "192.168.1.100"
MOVEIT_BRIDGE_URL = "http://127.0.0.1:5002"
EXTERNAL_CONTROL_NAILGUN = "external_control_n.urp"
EXTERNAL_CONTROL_GRIPPER = "external_control_g.urp"
SUPPORTED_MOVE_TYPES = ['movej', 'movel', 'movep', 'movec', 'planned', 'plannedj', 'plannedl']

# ── Speed governor for MoveIt/Pilz planned moves ────────────────────────────
# Pilz interprets these as max_velocity_scaling_factor / max_acceleration_scaling_factor
# (fraction of UR10 joint max). 1.0 = full UR10 speed, which is unsafe for this
# cell. All planned-move code paths (/move plannedj/plannedl and the internal
# _planned_lift helper used by stack_release/nail_and_retract) clamp through
# these two constants — change here to change global planned speed.
PLANNED_VEL_CAP = 0.45
PLANNED_ACC_CAP = 0.45
PLANNED_DEFAULT_VEL = 0.45
PLANNED_DEFAULT_ACC = 0.45


def _rotvec_to_quat_base_frame(rx, ry, rz):
    """Convert a URScript axis-angle rotation vector to a base_link quaternion.

    URScript pose orientation is (rx, ry, rz) where the *direction* is the
    rotation axis and the *magnitude* is the rotation angle (radians). The
    standard rotvec→quat formula is:

        angle = ||(rx,ry,rz)||
        axis  = (rx,ry,rz) / angle      (or (0,0,1) if angle≈0)
        q     = (axis * sin(angle/2), cos(angle/2))   # (x, y, z, w)

    Then to express the same rotation in MoveIt's base_link frame — which is
    rotated 180° about Z relative to the UR robot base — we conjugate by
    Rz(180°). For pure unit quaternions, that conjugation collapses to
    negating qx and qy (the same parity rule we already apply to x,y
    positions). qz, qw are unchanged.

    Returns (qx, qy, qz, qw).
    """
    rx = float(rx); ry = float(ry); rz = float(rz)
    angle = math.sqrt(rx*rx + ry*ry + rz*rz)
    if angle < 1e-9:
        qx, qy, qz, qw = 0.0, 0.0, 0.0, 1.0
    else:
        s = math.sin(angle / 2.0) / angle
        qx = rx * s
        qy = ry * s
        qz = rz * s
        qw = math.cos(angle / 2.0)
    # Rz(180°) conjugation: (qx, qy, qz, qw) → (-qx, -qy, qz, qw)
    out = (-qx, -qy, qz, qw)
    print(f"[ORI] rotvec=({rx:+.6f},{ry:+.6f},{rz:+.6f}) angle={angle:.4f}rad "
          f"-> quat(base_link)=(qx={out[0]:+.6f},qy={out[1]:+.6f},qz={out[2]:+.6f},qw={out[3]:+.6f})", flush=True)
    return out


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


@app.route('/execute_trajectory', methods=['POST'])
def robot_execute_trajectory():
    """Execute a pre-planned joint trajectory (from MoveIt) on the UR10 via
    chained URScript movej commands. The robot is driven directly through
    port 30002; no External Control / URCap involvement.

    Expected JSON body:
        joint_names  (list[str]) — joint names in the order matching each
                                   points[i].positions entry.
        points       (list)      — [{positions:[6 floats], time_from_start:sec}, ...]
        robotIp      (str)       — robot IP (optional, default 192.168.1.100)

    The first point is treated as the start state and skipped (the robot is
    assumed to already be at points[0]).
    """
    try:
        data = request.json or {}
        joint_names = data.get('joint_names')
        points = data.get('points')
        if not joint_names or not isinstance(points, list):
            return jsonify({
                'success': False,
                'error': 'joint_names (list[str]) and points (list[dict]) required'
            }), 400

        robot_ip = data.get('robotIp') or DEFAULT_ROBOT_IP
        tcp_for_move, payload_for_move, payload_cog_for_move = robot_state.snapshot()
        if tcp_for_move:
            print(f"execute_trajectory: re-applying stored TCP: {tcp_for_move}")
        if payload_for_move:
            print(f"execute_trajectory: re-applying stored payload: {payload_for_move} kg")

        nominal_duration = points[-1].get('time_from_start', 0.0) if points else 0.0
        print(
            f"/execute_trajectory: received {len(points)} points, "
            f"nominal_duration={nominal_duration:.2f}s, robot={robot_ip}"
        )

        start_time = time.time()
        msg = _execute_trajectory(
            robot_ip=robot_ip,
            joint_names=joint_names,
            points=points,
            tcp=tcp_for_move,
            payload=payload_for_move,
            payload_cog=payload_cog_for_move,
        )
        elapsed = time.time() - start_time
        overhead = elapsed - nominal_duration
        print(
            f"/execute_trajectory: done in {elapsed:.2f}s "
            f"(nominal {nominal_duration:.2f}s, overhead {overhead:+.2f}s) — {msg}"
        )
        return jsonify({
            'success': True,
            'message': msg,
            'pointCount': len(points),
            'nominalDurationSeconds': nominal_duration,
            'executionTimeSeconds': elapsed,
            'overheadSeconds': overhead,
        })
    except Exception as e:
        import traceback; traceback.print_exc()
        print(f"execute_trajectory failed: {e}")
        return jsonify({'success': False, 'error': str(e), 'executionTimeSeconds': 0}), 500


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
                f'  if socket_open("{PYTHON_HOST_IP}", {CALLBACK_PORT}, "cb"):\n'
                '    socket_send_string("done", "cb")\n'
                '    socket_close("cb")\n'
                '  end\n'
                "end\n"
                "gripper_open()\n"
            )
            _run_urscript_with_done(robot_ip, cmd)
            msg = "Gripper opened (TDO0=True)"
        elif command_type == 'close_gripper':
            # TDO0 = False, TDO1 = True → close
            cmd = (
                "def gripper_close():\n"
                "  set_tool_digital_out(0, False)\n"
                "  set_tool_digital_out(1, True)\n"
                "  sleep(0.8)\n"
                f'  if socket_open("{PYTHON_HOST_IP}", {CALLBACK_PORT}, "cb"):\n'
                '    socket_send_string("done", "cb")\n'
                '    socket_close("cb")\n'
                '  end\n'
                "end\n"
                "gripper_close()\n"
            )
            _run_urscript_with_done(robot_ip, cmd)
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

        elif command_type in ('planned', 'plannedj', 'plannedl'):
            # MoveIt plans, URScript executes. Two-step:
            #   1) POST /plan_only on the bridge → joint trajectory
            #   2) Run trajectory via chained movej over port 30002
            #
            # plannedj  → Pilz PTP (joint-space; mimics URScript movej semantics)
            # plannedl  → Pilz LIN (straight-line Cartesian; mimics URScript movel semantics)
            # planned   → legacy alias; behaves like plannedj if joints provided, else PTP cartesian
            is_linear = (command_type == 'plannedl')
            ee_type = data.get('endEffectorType') or robot_state.get_end_effector()
            # Both EEs are physically mounted on this workspace; tell the bridge
            # to disable the inactive one's collisions so Pilz LIN/PTP can plan
            # without false nailgun_base ↔ table contacts.
            both_loaded = bool(data.get('bothLoaded', True))
            no_object = bool(data.get('noObject', True))
            # Speed comes from the BT model via the C# request; clamped here through
            # the module-level PLANNED_VEL_CAP / PLANNED_ACC_CAP (top of file).
            vel_val = min(velocity if velocity is not None else PLANNED_DEFAULT_VEL, PLANNED_VEL_CAP)
            acc_val = min(acceleration if acceleration is not None else PLANNED_DEFAULT_ACC, PLANNED_ACC_CAP)

            plan_body = {
                'end_effector_type': ee_type,
                'velocity': vel_val,
                'acceleration': acc_val,
                'both_loaded': both_loaded,
                'no_object': no_object,
            }

            if is_linear:
                # LIN requires a Cartesian target; joints are ignored.
                if not inline_pose or len(inline_pose) < 6:
                    return jsonify({'success': False, 'error': 'plannedl requires pose [x,y,z,rx,ry,rz]'}), 400
                qx, qy, qz, qw = _rotvec_to_quat_base_frame(
                    inline_pose[3], inline_pose[4], inline_pose[5]
                )
                plan_body.update({
                    'x': float(inline_pose[0]),
                    'y': float(inline_pose[1]),
                    'z': float(inline_pose[2]),
                    'orientation_quat': [qx, qy, qz, qw],
                    'pipeline_id': 'pilz_industrial_motion_planner',
                    'planner_id': 'LIN',
                })
            elif inline_joints and len(inline_joints) >= 6:
                # PTP joint-space (matches movej intent).
                plan_body['joints'] = list(inline_joints)
                plan_body['use_pilz_ptp'] = True
            elif inline_pose and len(inline_pose) >= 6:
                # PTP via Cartesian target (still joint-interpolated under the hood).
                qx, qy, qz, qw = _rotvec_to_quat_base_frame(
                    inline_pose[3], inline_pose[4], inline_pose[5]
                )
                plan_body.update({
                    'x': float(inline_pose[0]),
                    'y': float(inline_pose[1]),
                    'z': float(inline_pose[2]),
                    'orientation_quat': [qx, qy, qz, qw],
                    'pipeline_id': 'pilz_industrial_motion_planner',
                    'planner_id': 'PTP',
                })
            else:
                return jsonify({'success': False, 'error': f'{command_type} move requires joints[6] or pose[6]'}), 400

            plan_start = time.time()
            print(f"[PLAN_BODY] command_type={command_type} keys={sorted(plan_body.keys())} "
                  f"pos=({plan_body.get('x')},{plan_body.get('y')},{plan_body.get('z')}) "
                  f"quat={plan_body.get('orientation_quat')} "
                  f"joints={plan_body.get('joints')} planner={plan_body.get('planner_id')}", flush=True)
            try:
                plan_resp = http_requests.post(
                    f"{MOVEIT_BRIDGE_URL}/plan_only",
                    json=plan_body,
                    timeout=60,
                ).json()
            except Exception as plan_ex:
                print(f"planned move: bridge /plan_only unreachable: {plan_ex}")
                return jsonify({'success': False, 'error': f'bridge unreachable: {plan_ex}'}), 502
            plan_rtt = time.time() - plan_start

            if not plan_resp.get('success'):
                err = plan_resp.get('error', 'planning failed')
                print(f"planned move: planning failed: {err}")
                return jsonify({
                    'success': False,
                    'error': err,
                    'planningTimeSeconds': plan_resp.get('planningTimeSeconds', 0.0),
                    'executionTimeSeconds': 0.0,
                }), 500

            plan_time = float(plan_resp.get('planningTimeSeconds', 0.0))
            nominal = float(plan_resp.get('nominalDurationSeconds', 0.0))
            n_points = int(plan_resp.get('pointCount', len(plan_resp.get('points', []))))
            print(
                f"planned move: bridge plan ok in {plan_rtt:.3f}s "
                f"(planning={plan_time:.3f}s, points={n_points}, nominal={nominal:.2f}s)"
            )

            pts = plan_resp.get('points', [])
            if pts:
                q0 = pts[0].get('positions')
                qN = pts[-1].get('positions')
                tN = pts[-1].get('time_from_start')
                print(f"[PLAN_OUT] points={len(pts)} nominal={nominal:.2f}s q_start={q0} q_end={qN} t_end={tN}", flush=True)
            exec_start = time.time()
            print(f"[EXEC] dispatching trajectory to {robot_ip} ...", flush=True)
            result_msg = _execute_trajectory(
                robot_ip=robot_ip,
                joint_names=plan_resp['joint_names'],
                points=plan_resp['points'],
                tcp=tcp_for_move,
                payload=payload_for_move,
                payload_cog=payload_cog_for_move,
            )
            exec_elapsed = time.time() - exec_start
            print(f"[EXEC] done in {exec_elapsed:.2f}s (nominal {nominal:.2f}s)", flush=True)
            total_elapsed = time.time() - start_time
            print(
                f"planned move: done | plan_rtt={plan_rtt:.3f}s exec={exec_elapsed:.2f}s "
                f"(nominal {nominal:.2f}s, overhead {exec_elapsed - nominal:+.2f}s) total={total_elapsed:.2f}s"
            )
            return jsonify({
                'success': True,
                'message': result_msg,
                'planningTimeSeconds': plan_time,
                'executionTimeSeconds': exec_elapsed,
                'pointCount': n_points,
                'nominalDurationSeconds': nominal,
                'totalTimeSeconds': total_elapsed,
            })

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


def _planned_approach(robot_ip: str, command_type: str,
                      inline_joints, inline_pose,
                      velocity: float = None, acceleration: float = None,
                      ee_type: str = None,
                      tcp=None, payload=None, payload_cog=None):
    """Plan+execute a PTP/LIN approach via the MoveIt bridge.

    Mirrors the planned branch of /move so composite endpoints
    (/stack_release, /nail_and_retract) can honour moveType=plannedj/plannedl
    instead of falling back to a full-speed URScript movej.

    Returns (planning_sec, point_count, nominal_sec).
    """
    is_linear = (command_type == 'plannedl')
    vel_val = min(velocity if velocity is not None else PLANNED_DEFAULT_VEL, PLANNED_VEL_CAP)
    acc_val = min(acceleration if acceleration is not None else PLANNED_DEFAULT_ACC, PLANNED_ACC_CAP)

    plan_body = {
        'end_effector_type': ee_type or robot_state.get_end_effector(),
        'velocity': vel_val,
        'acceleration': acc_val,
        'both_loaded': True,
        'no_object': True,
    }

    if is_linear:
        if not inline_pose or len(inline_pose) < 6:
            raise ValueError('plannedl requires pose [x,y,z,rx,ry,rz]')
        qx, qy, qz, qw = _rotvec_to_quat_base_frame(inline_pose[3], inline_pose[4], inline_pose[5])
        plan_body.update({
            'x': float(inline_pose[0]), 'y': float(inline_pose[1]), 'z': float(inline_pose[2]),
            'orientation_quat': [qx, qy, qz, qw],
            'pipeline_id': 'pilz_industrial_motion_planner', 'planner_id': 'LIN',
        })
    elif inline_joints and len(inline_joints) >= 6:
        plan_body['joints'] = list(inline_joints)
        plan_body['use_pilz_ptp'] = True
    elif inline_pose and len(inline_pose) >= 6:
        qx, qy, qz, qw = _rotvec_to_quat_base_frame(inline_pose[3], inline_pose[4], inline_pose[5])
        plan_body.update({
            'x': float(inline_pose[0]), 'y': float(inline_pose[1]), 'z': float(inline_pose[2]),
            'orientation_quat': [qx, qy, qz, qw],
            'pipeline_id': 'pilz_industrial_motion_planner', 'planner_id': 'PTP',
        })
    else:
        raise ValueError(f'{command_type} requires joints[6] or pose[6]')

    plan_resp = http_requests.post(f"{MOVEIT_BRIDGE_URL}/plan_only", json=plan_body, timeout=60).json()
    if not plan_resp.get('success'):
        raise RuntimeError(plan_resp.get('error', 'planning failed'))

    plan_time = float(plan_resp.get('planningTimeSeconds', 0.0))
    nominal = float(plan_resp.get('nominalDurationSeconds', 0.0))
    n_points = int(plan_resp.get('pointCount', len(plan_resp.get('points', []))))
    _execute_trajectory(
        robot_ip=robot_ip,
        joint_names=plan_resp['joint_names'], points=plan_resp['points'],
        tcp=tcp, payload=payload, payload_cog=payload_cog,
    )
    return plan_time, n_points, nominal


def _planned_lift(robot_ip: str, height: float, ee_type: str = None,
                  tcp=None, payload=None, payload_cog=None,
                  velocity: float = None, acceleration: float = None,
                  return_stats: bool = False):
    """Lift TCP straight up by `height` m via the MoveIt bridge (Pilz LIN).

    Reads current TCP pose from the controller, asks the bridge to plan a
    straight-line move to (x, y, z + height) at the same orientation, then
    executes via the shared servoj streamer. Collision-aware against the
    MoveIt scene; speed is bounded by the same PLANNED_VEL_CAP as /move.

    If return_stats is True, returns a dict with planning/point/nominal stats
    instead of the result string.
    """
    pose_info = _get_current_pose(robot_ip)
    cur = pose_info['tcp_pose']  # [x, y, z, rx, ry, rz]
    cur_joints = pose_info.get('joints')  # [j1..j6] in radians, j6 = wrist_3

    vel_val = min(velocity if velocity is not None else PLANNED_DEFAULT_VEL, PLANNED_VEL_CAP)
    acc_val = min(acceleration if acceleration is not None else PLANNED_DEFAULT_ACC, PLANNED_ACC_CAP)
    # Convert the live URScript rotvec orientation to a base_link quaternion and
    # send it as orientation_quat — the bridge will use it verbatim, so a
    # straight-z lift cannot rotate the wrist. (The legacy yaw path went through
    # a broken 2·atan2(ry,rx) approximation that silently rotated tilted poses.)
    qx, qy, qz, qw = _rotvec_to_quat_base_frame(cur[3], cur[4], cur[5])
    plan_body = {
        'end_effector_type': ee_type or robot_state.get_end_effector(),
        'velocity': vel_val,
        'acceleration': acc_val,
        'both_loaded': True,
        'no_object': True,
        'x': float(cur[0]),
        'y': float(cur[1]),
        'z': float(cur[2]) + float(height),
        'orientation_quat': [qx, qy, qz, qw],
        'pipeline_id': 'pilz_industrial_motion_planner',
        'planner_id': 'LIN',
    }
    # Pin wrist3 to the current value. At tool-down + horizontal-axis π
    # orientations the IK has two solutions that differ by π on wrist3; without
    # this lock Pilz LIN can pick the flipped branch and the lift visibly
    # rotates the wrist 180° versus the pose we just released from.
    if cur_joints and len(cur_joints) >= 6:
        plan_body['wrist3_lock'] = float(cur_joints[5])

    plan_start = time.time()
    plan_resp = http_requests.post(
        f"{MOVEIT_BRIDGE_URL}/plan_only", json=plan_body, timeout=60
    ).json()
    plan_rtt = time.time() - plan_start

    if not plan_resp.get('success'):
        err = plan_resp.get('error', 'planning failed')
        print(f"planned lift: planning failed ({err}); falling back to direct movel")
        from ur10_control.ur10_commands import lift_z
        msg = lift_z(robot_ip, height=height)
        if return_stats:
            return {'message': msg, 'planningSec': 0.0, 'pointCount': 0, 'nominalSec': 0.0, 'fallback': True}
        return msg

    nominal = float(plan_resp.get('nominalDurationSeconds', 0.0))
    plan_time = float(plan_resp.get('planningTimeSeconds', plan_rtt))
    n_points = int(plan_resp.get('pointCount', len(plan_resp.get('points', []))))
    print(
        f"planned lift: bridge plan ok in {plan_rtt:.3f}s "
        f"(points={n_points}, nominal={nominal:.2f}s, dz={height:+.3f}m)"
    )
    msg = _execute_trajectory(
        robot_ip=robot_ip,
        joint_names=plan_resp['joint_names'],
        points=plan_resp['points'],
        tcp=tcp, payload=payload, payload_cog=payload_cog,
    )
    if return_stats:
        return {'message': msg, 'planningSec': plan_time, 'pointCount': n_points, 'nominalSec': nominal, 'fallback': False}
    return msg


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
        steps = []

        # Step 1: movej to nail position
        if inline_joints and len(inline_joints) >= 6:
            position_arg = {'joints': inline_joints}
        else:
            position_arg = {'pose': inline_pose}

        print(f"nail_and_retract: step 1 — movej to {final_position}")
        t0 = time.time()
        move_to_pose(
            robot_ip=robot_ip, name=final_position,
            position=position_arg,
            velocity=velocity, acceleration=acceleration,
            tcp=tcp_for_move, payload=payload_for_move, payload_cog=payload_cog_for_move,
        )
        steps.append({'name': 'movej_to_nail', 'durationSec': time.time() - t0,
                      'planningSec': 0.0, 'pointCount': 0, 'nominalSec': 0.0})

        # Step 2: push down 2 mm — kept as direct movel because the move is
        # below MoveIt's planning resolution and intentionally contacts the nail.
        print("nail_and_retract: step 2 — push down 2 mm")
        from ur10_control.ur10_commands import lift_z
        t0 = time.time()
        lift_z(robot_ip, height=-0.002)
        steps.append({'name': 'push_down_2mm', 'durationSec': time.time() - t0,
                      'planningSec': 0.0, 'pointCount': 0, 'nominalSec': 0.0})

        # Step 3: lift back up — direct movel via lift_z
        print("nail_and_retract: step 3 — lift")
        from ur10_control.ur10_commands import lift_z
        t0 = time.time()
        lift_z(robot_ip, height=0.1)
        steps.append({'name': 'lift', 'durationSec': time.time() - t0,
                      'planningSec': 0.0, 'pointCount': 0, 'nominalSec': 0.0})

        elapsed = time.time() - start_time
        print(f"nail_and_retract completed in {elapsed:.2f}s")
        return jsonify({'success': True, 'message': f'nail_and_retract at {final_position} done',
                        'executionTimeSeconds': elapsed, 'steps': steps})

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
        move_type = (data.get('moveType') or 'movej').lower()

        if (not inline_joints or len(inline_joints) < 6) and (not inline_pose or len(inline_pose) < 6):
            return jsonify({'success': False, 'error': 'joints [j1..j6] or pose [x,y,z,rx,ry,rz] required'}), 400

        tcp_for_move, payload_for_move, payload_cog_for_move = robot_state.snapshot()

        start_time = time.time()
        steps = []

        # Step 1: approach the stack pose. Honour moveType: plannedj/plannedl go
        # through MoveIt (collision-aware, speed-capped by PLANNED_VEL_CAP);
        # anything else (default movej) runs as straight URScript at the
        # requested velocity.
        print(f"stack_release: step 1 — {move_type} to {final_position}")
        t0 = time.time()
        if move_type in ('planned', 'plannedj', 'plannedl'):
            plan_sec, n_pts, nominal_sec = _planned_approach(
                robot_ip=robot_ip, command_type=move_type,
                inline_joints=inline_joints, inline_pose=inline_pose,
                velocity=velocity, acceleration=acceleration,
                ee_type=data.get('endEffectorType'),
                tcp=tcp_for_move, payload=payload_for_move, payload_cog=payload_cog_for_move,
            )
            steps.append({'name': f'{move_type}_to_stack', 'durationSec': time.time() - t0,
                          'planningSec': plan_sec, 'pointCount': n_pts, 'nominalSec': nominal_sec})
        else:
            if inline_joints and len(inline_joints) >= 6:
                position_arg = {'joints': inline_joints}
            else:
                position_arg = {'pose': inline_pose}
            move_to_pose(
                robot_ip=robot_ip, name=final_position,
                position=position_arg,
                velocity=velocity, acceleration=acceleration,
                tcp=tcp_for_move, payload=payload_for_move, payload_cog=payload_cog_for_move,
            )
            steps.append({'name': 'movej_to_stack', 'durationSec': time.time() - t0,
                          'planningSec': 0.0, 'pointCount': 0, 'nominalSec': 0.0})

        # Step 2: open gripper
        print("stack_release: step 2 — open gripper")
        t0 = time.time()
        open_cmd = (
            "def gripper_open():\n"
            "  set_tool_digital_out(0, True)\n"
            "  sleep(0.8)\n"
            f'  if socket_open("{PYTHON_HOST_IP}", {CALLBACK_PORT}, "cb"):\n'
            '    socket_send_string("done", "cb")\n'
            '    socket_close("cb")\n'
            '  end\n'
            "end\n"
            "gripper_open()\n"
        )
        _run_urscript_with_done(robot_ip, open_cmd)
        steps.append({'name': 'open_gripper', 'durationSec': time.time() - t0,
                      'planningSec': 0.0, 'pointCount': 0, 'nominalSec': 0.0})

        # Step 3: lift — direct movel via lift_z
        print("stack_release: step 3 — lift")
        from ur10_control.ur10_commands import lift_z
        t0 = time.time()
        lift_z(robot_ip, height=0.1)
        steps.append({'name': 'lift', 'durationSec': time.time() - t0,
                      'planningSec': 0.0, 'pointCount': 0, 'nominalSec': 0.0})

        elapsed = time.time() - start_time
        print(f"stack_release completed in {elapsed:.2f}s")
        return jsonify({'success': True, 'message': f'stack_release at {final_position} done',
                        'executionTimeSeconds': elapsed, 'steps': steps})

    except Exception as e:
        print(f"stack_release failed: {str(e)}")
        return jsonify({'success': False, 'error': str(e), 'executionTimeSeconds': 0}), 500


if __name__ == '__main__':
    print("Starting Robot Execution Service...")
    print(f"Robot IP: {DEFAULT_ROBOT_IP}")
    print(f"Supported move types: {', '.join(SUPPORTED_MOVE_TYPES)}")
    app.run(host='127.0.0.1', port=5001, debug=True, use_reloader=False)
