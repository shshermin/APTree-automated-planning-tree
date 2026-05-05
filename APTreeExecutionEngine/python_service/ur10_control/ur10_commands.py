"""
UR10 Dashboard Client — Communicate with the UR10 via Dashboard Server (port 29999).

No external libraries required — uses Python's built-in socket module.
"""

import json
import os
import socket
import struct
import time

DASHBOARD_PORT = 29999
REALTIME_PORT = 30003
POSITIONS_FILE = os.path.join(os.path.dirname(__file__), "positions.json")

# Safety mode constants — UR real-time interface, byte offset 812 (CB3 firmware 3.10+)
SAFETY_MODE_NORMAL = 1
SAFETY_MODE_REDUCED = 2
SAFETY_MODE_PROTECTIVE_STOP = 3
SAFETY_MODE_RECOVERY = 4
SAFETY_MODE_SAFEGUARD_STOP = 5
SAFETY_MODE_SYSTEM_EMERGENCY_STOP = 6
SAFETY_MODE_ROBOT_EMERGENCY_STOP = 7
SAFETY_MODE_VIOLATION = 8
SAFETY_MODE_FAULT = 9

SAFETY_MODE_NAMES = {
    1: "NORMAL", 2: "REDUCED", 3: "PROTECTIVE_STOP", 4: "RECOVERY",
    5: "SAFEGUARD_STOP", 6: "SYSTEM_EMERGENCY_STOP", 7: "ROBOT_EMERGENCY_STOP",
    8: "VIOLATION", 9: "FAULT",
}


class RobotSafetyError(Exception):
    """Raised when the robot enters a safety stop state (protective stop, e-stop, etc.)."""
    pass


def check_safety_mode(robot_ip: str):
    """Read the current safety mode from the robot's real-time interface.

    Returns: (is_safe: bool, message: str)
        is_safe is True when the robot is in NORMAL or REDUCED mode.
    """
    try:
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.settimeout(5)
        sock.connect((robot_ip, REALTIME_PORT))
        data = b""
        while len(data) < 1116:
            chunk = sock.recv(4096)
            if not chunk:
                break
            data += chunk
        sock.close()
        if len(data) >= 820:
            safety_mode = int(round(struct.unpack("!d", data[812:820])[0]))
            if safety_mode not in (SAFETY_MODE_NORMAL, SAFETY_MODE_REDUCED):
                mode_name = SAFETY_MODE_NAMES.get(safety_mode, str(safety_mode))
                return False, f"Robot in {mode_name}"
        return True, "OK"
    except Exception as e:
        return False, f"Could not read safety mode: {e}"


# TCP offsets (flange → tool tip) for each tool, in meters and radians.
# Format: [x, y, z, rx, ry, rz]
TCP_OFFSETS = {
    "gripper": [0.00723, 0.00095, 0.148, 0, 0, 0],
    "nailgun": [-0.09515, -0.00026, 0.3165, 0, 0, 0],
}

# Note: The Dashboard Server is a simple TCP server that accepts text commands and returns responses.
def dashboard_command(robot_ip: str, command: str) -> str:
    """Send a single dashboard command to the robot and return its text response.

    Input:  robot_ip (str) — IP address of the UR10.
            command  (str) — Dashboard command to send (e.g. "play", "stop").
    Output: str — The robot's text response.
    """
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(5)
    sock.connect((robot_ip, DASHBOARD_PORT))
    sock.recv(1024)  # discard welcome banner
    sock.sendall((command + "\n").encode("utf-8"))
    response = sock.recv(4096).decode("utf-8", errors="replace").strip()
    sock.close()
    return response

# Example command: "load myprogram.urp"
def load_program(robot_ip: str, program_name: str) -> str:
    """Load a .urp program file on the UR10 controller.

    Input:  robot_ip     (str) — IP address of the UR10.
            program_name (str) — Filename of the program (e.g. "myprogram.urp").
    Output: str — Dashboard response, e.g.
            "Loading program: /programs/myprogram.urp" on success,
            "File not found: myprogram.urp" on failure.
    """
    return dashboard_command(robot_ip, f"load {program_name}")


def play_program(robot_ip: str, program_name: str, speed: int = 30, max_retries: int = 5, retry_delay: float = 3.0) -> str:
    """Load a program, set the speed slider, and start execution.

    Input:  robot_ip     (str)   — IP address of the UR10.
            program_name (str)   — Filename of the program (e.g. "myprogram.urp").
            speed        (int)   — Speed slider value 1-100% (default: 30).
            max_retries  (int)   — Max attempts if play is rejected (default: 5).
            retry_delay  (float) — Seconds to wait between retries (default: 3.0).
    Output: str — Dashboard response to the play command, or error if file not found.

    Retries automatically if the play command is rejected by the controller (e.g. it
    is still settling after a previous stop). Only polls for completion and sends stop
    after a successful play — not after a rejection.
    """
    load_response = load_program(robot_ip, program_name)
    if "File not found" in load_response:
        return load_response

    play_response = None
    for attempt in range(1, max_retries + 1):
        dashboard_command(robot_ip, f"setUserRole locked")
        dashboard_command(robot_ip, f"setSpeedSlider {speed}")
        play_response = dashboard_command(robot_ip, "play")

        if "Failed" not in play_response:
            break  # play accepted — proceed to wait for completion

        print(f"play_program: attempt {attempt}/{max_retries} — play rejected: {play_response!r}. "
              f"Waiting {retry_delay}s before retry.")
        if attempt < max_retries:
            time.sleep(retry_delay)
    else:
        # All retries exhausted — return the last failure response without polling
        return play_response

    # Wait for the program to finish running, then stop
    time.sleep(1)  # give the program a moment to start
    while True:
        state = dashboard_command(robot_ip, "running")
        if "true" not in state.lower():
            break
        time.sleep(0.5)
    dashboard_command(robot_ip, "stop")
    return play_response


def play_program_with_pause(robot_ip: str, program_name: str, speed: int = 30):
    """Load and play a program with keyboard control in the terminal.

    Input:  robot_ip     (str) — IP address of the UR10.
            program_name (str) — Filename of the program (e.g. "myprogram.urp").
            speed        (int) — Speed slider value 1-100% (default: 30).
    Output: None — Prints status to console. Press SPACE to pause/resume, Q to quit.
    """
    import msvcrt

    result = play_program(robot_ip, program_name, speed)
    print(result)
    print("Program running. Press SPACE to pause/resume, Q to quit.")

    paused = False
    while True:
        if msvcrt.kbhit():
            key = msvcrt.getch()
            if key == b" ":
                if paused:
                    print(dashboard_command(robot_ip, "play"))
                    paused = False
                else:
                    print(dashboard_command(robot_ip, "pause"))
                    paused = True
            elif key in (b"q", b"Q"):
                print(dashboard_command(robot_ip, "stop"))
                print("Stopped.")
                break


def save_position(robot_ip: str, name: str) -> dict:
    """Read the robot's current joint angles and TCP pose, and save them with a name.

    Input:  robot_ip (str) — IP address of the UR10.
            name     (str) — Name to give this position (e.g. "home", "pick_up").
    Output: dict — The saved position with keys: "name", "joints" (6 floats in rad),
            "tcp_pose" (6 floats: x,y,z in meters, rx,ry,rz in rad).
            Also writes to positions.json.
    """
    # Read current state from real-time interface
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(5)
    sock.connect((robot_ip, REALTIME_PORT))
    data = b""
    while len(data) < 1116:
        chunk = sock.recv(4096)
        if not chunk:
            break
        data += chunk
    sock.close()

    if len(data) < 1116:
        raise ConnectionError(f"Could not read robot state (got {len(data)} bytes)")

    joints = list(struct.unpack("!6d", data[252:300]))
    tcp_pose = list(struct.unpack("!6d", data[444:492]))

    position = {
        "name": name,
        "joints": [round(v, 6) for v in joints],
        "tcp_pose": [round(v, 6) for v in tcp_pose],
    }

    # Load existing positions, add/update, save
    positions = {}
    if os.path.exists(POSITIONS_FILE):
        with open(POSITIONS_FILE, "r") as f:
            positions = json.load(f)
    positions[name] = position
    with open(POSITIONS_FILE, "w") as f:
        json.dump(positions, f, indent=2)

    print(f"Saved position '{name}': joints={position['joints']}")
    return position


def get_current_pose(robot_ip: str) -> dict:
    """Read the robot's current joint angles and TCP pose without saving."""
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(5)
    sock.connect((robot_ip, REALTIME_PORT))
    data = b""
    while len(data) < 1116:
        chunk = sock.recv(4096)
        if not chunk:
            break
        data += chunk
    sock.close()

    if len(data) < 1116:
        raise ConnectionError(f"Could not read robot state (got {len(data)} bytes)")

    joints = list(struct.unpack("!6d", data[252:300]))
    tcp_pose = list(struct.unpack("!6d", data[444:492]))

    return {
        "joints": [round(v, 6) for v in joints],
        "tcp_pose": [round(v, 6) for v in tcp_pose],
    }


def get_position(name: str) -> dict:
    """Retrieve a previously saved position by name.

    Input:  name (str) — Name of the saved position (e.g. "home").
    Output: dict — Position with keys: "name", "joints", "tcp_pose".
    Raises: FileNotFoundError if no positions saved, KeyError if name not found.
    """
    if not os.path.exists(POSITIONS_FILE):
        raise FileNotFoundError("No positions saved yet.")
    with open(POSITIONS_FILE, "r") as f:
        positions = json.load(f)
    if name not in positions:
        raise KeyError(f"Position '{name}' not found. Available: {list(positions.keys())}")
    return positions[name]


SECONDARY_PORT = 30002  # URScript commands port


def set_tcp(robot_ip: str, tcp: list) -> str:
    """Set the active TCP offset via URScript (persists until changed again).

    Input:  robot_ip (str)  — IP address of the UR10.
            tcp      (list) — TCP offset [x, y, z, rx, ry, rz] in meters/radians.
    Output: str — Confirmation message.
    """
    cmd = f"set_tcp(p[{tcp[0]}, {tcp[1]}, {tcp[2]}, {tcp[3]}, {tcp[4]}, {tcp[5]}])\n"
    _send_urscript(robot_ip, cmd)
    return f"TCP set to {tcp}"


def set_payload(robot_ip: str, mass: float, cog: list = None) -> str:
    """Set the robot payload via URScript (persists until changed again).

    Input:  robot_ip (str)   — IP address of the UR10.
            mass     (float) — Payload mass in kg.
            cog      (list)  — Optional center of gravity [cx, cy, cz] in meters.
    Output: str — Confirmation message.
    """
    if cog:
        cmd = f"set_payload({mass}, [{cog[0]}, {cog[1]}, {cog[2]}])\n"
    else:
        cmd = f"set_payload({mass})\n"
    _send_urscript(robot_ip, cmd)
    return f"Payload set to {mass} kg"


def _send_urscript(robot_ip: str, cmd: str):
    """Send a raw URScript command to the robot via the secondary port."""
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(10)
    sock.connect((robot_ip, SECONDARY_PORT))
    sock.sendall(cmd.encode("utf-8"))
    sock.close()


def _wait_for_motion_complete(robot_ip: str, timeout: float = 60.0, settle_time: float = 0.3, velocity_threshold: float = 0.001):
    """Block until the robot has finished moving by polling joint velocities.

    Also monitors safety mode from the real-time data packet (offset 812).
    Raises RobotSafetyError if a protective stop, e-stop, or fault is detected.

    Input:  robot_ip           (str)   — IP address of the UR10.
            timeout            (float) — Max wait time in seconds (default: 60).
            settle_time        (float) — How long velocities must stay below threshold (default: 0.3s).
            velocity_threshold (float) — Max joint velocity (rad/s) to consider "stopped" (default: 0.001).
    """
    import time

    time.sleep(0.5)  # give the motion time to start

    start = time.time()
    settled_since = None

    while time.time() - start < timeout:
        try:
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.settimeout(5)
            sock.connect((robot_ip, REALTIME_PORT))
            data = b""
            while len(data) < 1116:
                chunk = sock.recv(4096)
                if not chunk:
                    break
                data += chunk
            sock.close()

            # Check safety mode first (byte offset 812, 8-byte double)
            if len(data) >= 820:
                safety_mode = int(round(struct.unpack("!d", data[812:820])[0]))
                if safety_mode == SAFETY_MODE_PROTECTIVE_STOP:
                    mode_name = SAFETY_MODE_NAMES.get(safety_mode, str(safety_mode))
                    print(f"ERROR: Robot entered {mode_name} during motion!")
                    raise RobotSafetyError(f"Protective stop detected during motion")
                elif safety_mode in (SAFETY_MODE_SYSTEM_EMERGENCY_STOP, SAFETY_MODE_ROBOT_EMERGENCY_STOP):
                    raise RobotSafetyError(f"Emergency stop detected during motion")
                elif safety_mode == SAFETY_MODE_FAULT:
                    raise RobotSafetyError(f"Safety fault detected during motion")
                elif safety_mode == SAFETY_MODE_VIOLATION:
                    raise RobotSafetyError(f"Safety violation detected during motion")

            if len(data) >= 348:
                # Joint velocities (qd_actual) are at bytes 300:348 — 6 doubles, big-endian
                velocities = struct.unpack("!6d", data[300:348])
                max_vel = max(abs(v) for v in velocities)

                if max_vel < velocity_threshold:
                    if settled_since is None:
                        settled_since = time.time()
                    elif time.time() - settled_since >= settle_time:
                        return  # motion complete
                else:
                    settled_since = None
        except RobotSafetyError:
            raise  # don't swallow safety errors
        except (socket.error, ConnectionError):
            pass  # brief connection hiccup, retry

        time.sleep(0.1)

    print(f"Warning: _wait_for_motion_complete timed out after {timeout}s")


def _resolve_tcp(tcp):
    """Resolve a tcp argument: None, a list, or a tool name from TCP_OFFSETS."""
    if tcp is None:
        return None
    if isinstance(tcp, str):
        if tcp not in TCP_OFFSETS:
            raise KeyError(f"Unknown tool '{tcp}'. Available: {list(TCP_OFFSETS.keys())}")
        return TCP_OFFSETS[tcp]
    return list(tcp)


def _wrap_with_tcp(move_cmd: str, tcp, payload=None, payload_cog=None) -> str:
    """Wrap a move command in a program that sets TCP and payload first."""
    tcp = _resolve_tcp(tcp)
    if tcp is None and payload is None:
        return move_cmd
    lines = ["def move_with_tcp():"]
    if tcp is not None:
        lines.append(f"  set_tcp(p{tcp})")
    if payload is not None:
        if payload_cog:
            lines.append(f"  set_payload({payload}, [{payload_cog[0]}, {payload_cog[1]}, {payload_cog[2]}])")
        else:
            lines.append(f"  set_payload({payload})")
    lines.append(f"  {move_cmd.strip()}")
    lines.append("end")
    return "\n".join(lines) + "\n"


def move_to_pose(robot_ip: str, name: str, position: dict = None, velocity: float = 0.5, acceleration: float = 1.0, tcp=None, payload=None, payload_cog=None) -> str:
    """Move the robot to a pose using movej (joint-space interpolation)."""
    if position is None:
        position = get_position(name)
    if "joints" in position:
        joints = position["joints"]
        cmd = _wrap_with_tcp(f"movej({joints}, a={acceleration}, v={velocity})\n", tcp, payload, payload_cog)
        _send_urscript(robot_ip, cmd)
        _wait_for_motion_complete(robot_ip)
        return f"movej to '{name}' with joints={joints}"
    elif "pose" in position:
        pose = position["pose"]
        cmd = _wrap_with_tcp(f"movej(p{pose}, a={acceleration}, v={velocity})\n", tcp, payload, payload_cog)
        _send_urscript(robot_ip, cmd)
        _wait_for_motion_complete(robot_ip)
        return f"movej to '{name}' with pose={pose}"
    else:
        raise ValueError(f"Position '{name}' has neither 'joints' nor 'pose'")


def move_to_pose_l(robot_ip: str, name: str, position: dict = None, velocity: float = 0.25, acceleration: float = 1.2, tcp=None, payload=None, payload_cog=None) -> str:
    """Move the robot to a pose using movel (linear TCP interpolation)."""
    if position is None:
        position = get_position(name)
    pose = position["pose"]
    cmd = _wrap_with_tcp(f"movel(p{pose}, a={acceleration}, v={velocity})\n", tcp, payload, payload_cog)
    _send_urscript(robot_ip, cmd)
    _wait_for_motion_complete(robot_ip)
    return f"movel to '{name}' with pose={pose}"


def move_to_pose_p(robot_ip: str, name: str, position: dict = None, velocity: float = 0.25, acceleration: float = 1.2, blend_radius: float = 0.05, tcp=None, payload=None, payload_cog=None) -> str:
    """Move the robot to a pose using movep (process / blend move)."""
    if position is None:
        position = get_position(name)
    pose = position["pose"]
    cmd = _wrap_with_tcp(f"movep(p{pose}, a={acceleration}, v={velocity}, r={blend_radius})\n", tcp, payload, payload_cog)
    _send_urscript(robot_ip, cmd)
    _wait_for_motion_complete(robot_ip)
    return f"movep to '{name}' with pose={pose}"


def move_to_pose_c(robot_ip: str, via_name: str, end_name: str, via_position: dict = None, end_position: dict = None, velocity: float = 0.25, acceleration: float = 1.2, tcp=None, payload=None, payload_cog=None) -> str:
    """Move the robot along a circular arc using movec (via-point -> end-point)."""
    if via_position is None:
        via_position = get_position(via_name)
    if end_position is None:
        end_position = get_position(end_name)
    via_pose = via_position["pose"]
    end_pose = end_position["pose"]
    cmd = _wrap_with_tcp(f"movec(p{via_pose}, p{end_pose}, a={acceleration}, v={velocity})\n", tcp, payload, payload_cog)
    _send_urscript(robot_ip, cmd)
    _wait_for_motion_complete(robot_ip)
    return f"movec via '{via_name}' to '{end_name}'"


def set_digital_out_sequence(robot_ip: str) -> str:
    """Set digital output 1 to True, wait 2 seconds, then set digital output 0 to True.

    Uses a single URScript program sent to the robot so the timing is handled
    on the controller side (not subject to network latency).

    Input:  robot_ip (str) — IP address of the UR10.
    Output: str — Confirmation message.
    """
    cmd = (
        "def gripper_seq():\n"
        "  set_tool_digital_out(1, True)\n"
        "  sleep(0.5)\n"
        "  set_tool_digital_out(0, False)\n"
        "end\n"
    )
    _send_urscript(robot_ip, cmd)
    return "Tool digital out sequence: TDO1=True, wait 0.5s, TDO0=False"


def lift_z(robot_ip: str, height: float = 0.1, velocity: float = 0.1, acceleration: float = 0.3) -> str:
    """Move the TCP straight up by `height` meters from the current pose.

    Reads the actual TCP pose on the robot controller and does a movel
    to the same x,y with z + height.

    Input:  robot_ip     (str)   — IP address of the UR10.
            height       (float) — Distance to lift in meters (default: 0.1 = 10 cm).
            velocity     (float) — TCP velocity in m/s (default: 0.1).
            acceleration (float) — TCP acceleration in m/s² (default: 0.3).
    Output: str — Confirmation message.
    """
    cmd = (
        "def lift_up():\n"
        "  local curr = get_actual_tcp_pose()\n"
        f"  local target = p[curr[0], curr[1], curr[2]+{height}, curr[3], curr[4], curr[5]]\n"
        f"  movel(target, a={acceleration}, v={velocity})\n"
        "end\n"
    )
    _send_urscript(robot_ip, cmd)
    _wait_for_motion_complete(robot_ip)
    return f"Lift: moved TCP up {height}m from current pose"


def set_tool_digital_out_open(robot_ip: str) -> str:
    """Open the gripper by setting tool digital output 0 to True.

    Input:  robot_ip (str) — IP address of the UR10.
    Output: str — Confirmation message.
    """
    cmd = (
        "def gripper_open():\n"
        "  set_tool_digital_out(0, True)\n"
        "  sleep(0.5)\n"
        "end\n"
    )
    _send_urscript(robot_ip, cmd)
    return "Tool digital out: TDO0=True (open), wait 0.5s"


if __name__ == "__main__":
    import sys
    if len(sys.argv) != 3:
        print("Usage: python ur10_commands.py <robot_ip> <program_name>")
        sys.exit(1)
    result = load_program(sys.argv[1], sys.argv[2])
    print(result)
