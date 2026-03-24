"""
UR10 Dashboard Client — Communicate with the UR10 via Dashboard Server (port 29999).

No external libraries required — uses Python's built-in socket module.
"""

import json
import os
import socket
import struct

DASHBOARD_PORT = 29999
REALTIME_PORT = 30003
POSITIONS_FILE = os.path.join(os.path.dirname(__file__), "positions.json")

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


def play_program(robot_ip: str, program_name: str, speed: int = 30) -> str:
    """Load a program, set the speed slider, and start execution.

    Input:  robot_ip     (str) — IP address of the UR10.
            program_name (str) — Filename of the program (e.g. "myprogram.urp").
            speed        (int) — Speed slider value 1-100% (default: 30).
    Output: str — Dashboard response to the play command, or error if file not found.
    """
    load_response = load_program(robot_ip, program_name)
    if "File not found" in load_response:
        return load_response
    dashboard_command(robot_ip, f"setUserRole locked")
    dashboard_command(robot_ip, f"setSpeedSlider {speed}")
    play_response = dashboard_command(robot_ip, "play")

    # Wait for the program to finish, then stop
    import time
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


def _send_urscript(robot_ip: str, cmd: str):
    """Send a raw URScript command to the robot via the secondary port."""
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(10)
    sock.connect((robot_ip, SECONDARY_PORT))
    sock.sendall(cmd.encode("utf-8"))
    sock.close()


def move_to_pose(robot_ip: str, name: str, position: dict = None, velocity: float = 0.5, acceleration: float = 1.0) -> str:
    """Move the robot to a pose using movej (joint-space interpolation).

    Input:  robot_ip     (str)   — IP address of the UR10.
            name         (str)   — Name of the position.
            position     (dict)  — Position dict with "joints" key. If None, looks up from positions.json.
            velocity     (float) — Joint velocity in rad/s (default: 0.5).
            acceleration (float) — Joint acceleration in rad/s² (default: 1.0).
    Output: str — Confirmation message.
    """
    if position is None:
        position = get_position(name)
    if "joints" in position:
        joints = position["joints"]
        cmd = f"movej({joints}, a={acceleration}, v={velocity})\n"
        _send_urscript(robot_ip, cmd)
        return f"movej to '{name}' with joints={joints}"
    elif "pose" in position:
        pose = position["pose"]
        cmd = f"movej(p{pose}, a={acceleration}, v={velocity})\n"
        _send_urscript(robot_ip, cmd)
        return f"movej to '{name}' with pose={pose}"
    else:
        raise ValueError(f"Position '{name}' has neither 'joints' nor 'pose'")


def move_to_pose_l(robot_ip: str, name: str, position: dict = None, velocity: float = 0.25, acceleration: float = 1.2) -> str:
    """Move the robot to a pose using movel (linear TCP interpolation).

    Input:  robot_ip     (str)   — IP address of the UR10.
            name         (str)   — Name of the position.
            position     (dict)  — Position dict with "pose" key (x,y,z,rx,ry,rz). If None, looks up from positions.json.
            velocity     (float) — TCP velocity in m/s (default: 0.25).
            acceleration (float) — TCP acceleration in m/s² (default: 1.2).
    Output: str — Confirmation message.
    """
    if position is None:
        position = get_position(name)
    pose = position["pose"]
    cmd = f"movel(p{pose}, a={acceleration}, v={velocity})\n"
    _send_urscript(robot_ip, cmd)
    return f"movel to '{name}' with pose={pose}"


def move_to_pose_p(robot_ip: str, name: str, position: dict = None, velocity: float = 0.25, acceleration: float = 1.2, blend_radius: float = 0.05) -> str:
    """Move the robot to a pose using movep (process / blend move).

    Input:  robot_ip     (str)   — IP address of the UR10.
            name         (str)   — Name of the position.
            position     (dict)  — Position dict with "pose" key. If None, looks up from positions.json.
            velocity     (float) — TCP velocity in m/s (default: 0.25).
            acceleration (float) — TCP acceleration in m/s² (default: 1.2).
            blend_radius (float) — Blend radius in m (default: 0.05).
    Output: str — Confirmation message.
    """
    if position is None:
        position = get_position(name)
    pose = position["pose"]
    cmd = f"movep(p{pose}, a={acceleration}, v={velocity}, r={blend_radius})\n"
    _send_urscript(robot_ip, cmd)
    return f"movep to '{name}' with pose={pose}"


def move_to_pose_c(robot_ip: str, via_name: str, end_name: str, via_position: dict = None, end_position: dict = None, velocity: float = 0.25, acceleration: float = 1.2) -> str:
    """Move the robot along a circular arc using movec (via-point → end-point).

    Input:  robot_ip     (str)   — IP address of the UR10.
            via_name     (str)   — Name of the via (intermediate) position.
            end_name     (str)   — Name of the end position.
            via_position (dict)  — Via position dict with "pose" key. If None, looks up from positions.json.
            end_position (dict)  — End position dict with "pose" key. If None, looks up from positions.json.
            velocity     (float) — TCP velocity in m/s (default: 0.25).
            acceleration (float) — TCP acceleration in m/s² (default: 1.2).
    Output: str — Confirmation message.
    """
    if via_position is None:
        via_position = get_position(via_name)
    if end_position is None:
        end_position = get_position(end_name)
    via_pose = via_position["pose"]
    end_pose = end_position["pose"]
    cmd = f"movec(p{via_pose}, p{end_pose}, a={acceleration}, v={velocity})\n"
    _send_urscript(robot_ip, cmd)
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
        "  sleep(2)\n"
        "  set_tool_digital_out(0, False)\n"
        "end\n"
    )
    _send_urscript(robot_ip, cmd)
    return "Tool digital out sequence: TDO1=True, wait 2s, TDO0=False"


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
    return f"Lift: moved TCP up {height}m from current pose"


def set_tool_digital_out_open(robot_ip: str) -> str:
    """Open the gripper by setting tool digital output 0 to True.

    Input:  robot_ip (str) — IP address of the UR10.
    Output: str — Confirmation message.
    """
    cmd = (
        "def gripper_open():\n"
        "  set_tool_digital_out(0, True)\n"
        "end\n"
    )
    _send_urscript(robot_ip, cmd)
    return "Tool digital out: TDO0=True (open)"


if __name__ == "__main__":
    import sys
    if len(sys.argv) != 3:
        print("Usage: python ur10_commands.py <robot_ip> <program_name>")
        sys.exit(1)
    result = load_program(sys.argv[1], sys.argv[2])
    print(result)
