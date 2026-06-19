"""
UR10 Dashboard Client — Communicate with the UR10 via Dashboard Server (port 29999).

No external libraries required — uses Python's built-in socket module.
"""

import json
import math
import os
import socket
import struct
import time

DASHBOARD_PORT = 29999
REALTIME_PORT = 30003
POSITIONS_FILE = os.path.join(os.path.dirname(__file__), "positions.json")

# URScript callback: the robot connects back to this host/port after each move.
PYTHON_HOST_IP = "192.168.1.2"   # Windows host IP on the robot LAN (Ethernet adapter)
# Must be OUTSIDE Windows excluded TCP range (check: netsh int ipv4 show excludedportrange protocol=tcp).
# 50000-50059 is reserved by Hyper-V/WSL2 on this host, so we use 40001.
CALLBACK_PORT = 35001             # Port Python listens on for the "done" signal

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

# ── URScript move defaults ─────────────────────────────────────────
# Single place to tune speeds for direct URScript moves (movej / movel / movep /
# movec / lift_z). Units differ by move type — keep them separate:
#   movej : v in rad/s (joint speed),  a in rad/s²
#   movel : v in m/s   (tool speed),    a in m/s²
# Planned (MoveIt/Pilz) moves live in robot_service.py and are governed by
# PLANNED_VEL_CAP there, not by these constants.
MOVEJ_DEFAULT_VEL = 1.0    # rad/s
MOVEJ_DEFAULT_ACC = 1.0    # rad/s²
MOVEL_DEFAULT_VEL = 0.25   # m/s
MOVEL_DEFAULT_ACC = 1.2    # m/s²
MOVEP_BLEND_RADIUS = 0.05  # m
LIFT_Z_DEFAULT_VEL = 0.1   # m/s
LIFT_Z_DEFAULT_ACC = 0.3   # m/s²

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


def play_program(robot_ip: str, program_name: str, speed: int = 30, max_retries: int = 1, retry_delay: float = 3.0) -> str:
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


def _open_done_listener():
    """Bind and listen on CALLBACK_PORT BEFORE the URScript is sent.

    Returns a server socket ready for accept(). The caller MUST send the
    URScript only AFTER this returns, otherwise the controller can run
    socket_open() before Python is listening (connection refused, no "done"
    callback ever arrives, and the next command can run while the robot is
    still moving — or while the previous "done" satisfies the new wait).
    """
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind(("0.0.0.0", CALLBACK_PORT))
    server.listen(1)
    server.settimeout(2.0)  # wake up every 2 s to check safety mode
    return server


def _wait_for_controller_ready(robot_ip: str, max_wait: float = 5.0,
                               vel_threshold: float = 0.01) -> bool:
    """Poll the real-time interface until all joint velocities are near zero.

    The UR secondary port (30002) silently drops a new URScript if the
    controller is still executing the previous one. Near-zero velocities
    indicate the robot is at rest and the interpreter is likely idle.
    Retries every 0.5 s up to max_wait seconds.

    Returns True when ready, False if max_wait elapses without all |q̇| < threshold.
    """
    t_start = time.time()
    print(f"[READY] checking controller idle (threshold={vel_threshold} rad/s, max_wait={max_wait:.1f}s)", flush=True)
    deadline = t_start + max_wait
    attempt = 0
    while time.time() < deadline:
        attempt += 1
        try:
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.settimeout(2.0)
            sock.connect((robot_ip, REALTIME_PORT))
            data = b""
            while len(data) < 348:
                chunk = sock.recv(4096)
                if not chunk:
                    break
                data += chunk
            sock.close()
            if len(data) >= 348:
                velocities = struct.unpack("!6d", data[300:348])
                max_vel = max(abs(v) for v in velocities)
                if all(abs(v) < vel_threshold for v in velocities):
                    waited = time.time() - t_start
                    if attempt > 1:
                        print(f"[READY] controller idle after {waited:.2f}s ({attempt} polls)", flush=True)
                    return True
                print(f"[READY] attempt {attempt}: controller busy max|q\u0307|={max_vel:.4f} rad/s — waiting 0.5s", flush=True)
            else:
                print(f"[READY] attempt {attempt}: short RT packet ({len(data)} bytes) — waiting 0.5s", flush=True)
        except Exception as e:
            print(f"[READY] attempt {attempt}: RT read error: {e} — waiting 0.5s", flush=True)
        time.sleep(0.5)
    print(f"[READY] controller not idle after {max_wait:.1f}s — sending URScript anyway", flush=True)
    return False


def _wait_for_motion_complete(robot_ip: str, timeout: float = 60.0, server=None, **_kwargs):
    """Block until the robot signals motion complete via URScript socket callback.

    The URScript program calls socket_open / socket_send_string / socket_close
    after the move finishes, sending "done" to PYTHON_HOST_IP:CALLBACK_PORT.
    This function listens for that signal instead of polling joint velocities.

    Safety mode is re-checked every 2 s while waiting so protective stops and
    e-stops are still caught promptly.

    Raises RobotSafetyError if a protective stop, e-stop, or fault is detected.

    Input:  robot_ip  (str)   — IP address of the UR10 (used for safety checks).
            timeout   (float) — Max wait time in seconds (default: 60).
            server    (sock)  — Pre-bound listener from _open_done_listener().
                                If None, one is created here (legacy/unsafe — only
                                use for backwards compatibility; race-prone).
    """
    owns_server = server is None
    if owns_server:
        server = _open_done_listener()
    start = time.time()
    print(f"[DONE] waiting for robot callback on {PYTHON_HOST_IP}:{CALLBACK_PORT} (timeout={timeout:.0f}s)", flush=True)
    try:
        while time.time() - start < timeout:
            try:
                conn, addr = server.accept()
                conn.recv(64)   # read "done" (contents not checked — arrival is enough)
                conn.close()
                elapsed = time.time() - start
                print(f"[DONE] callback received from {addr[0]} after {elapsed:.3f}s", flush=True)
                return          # motion complete
            except socket.timeout:
                elapsed = time.time() - start
                print(f"[DONE] still waiting... {elapsed:.0f}s elapsed", flush=True)
                # No callback yet — check safety mode before waiting again
                is_safe, msg = check_safety_mode(robot_ip)
                if not is_safe:
                    print(f"[DONE] safety stop detected: {msg}", flush=True)
                    raise RobotSafetyError(msg)
    finally:
        if owns_server:
            server.close()
        else:
            server.close()

    print(f"[DONE] WARNING: timed out after {timeout:.0f}s — no 'done' callback received. URScript may have been dropped.", flush=True)


def _run_urscript_with_done(robot_ip: str, cmd: str, timeout: float = 60.0):
    """Bind listener, send URScript, wait for "done" callback.

    Order matters: the listener MUST be bound before _send_urscript so the
    controller's socket_open call can connect. Use this for every URScript
    that includes the done-callback handshake (moves, gripper, IO, lift).
    """
    print(f"[URSCRIPT] preparing to send {len(cmd.encode())} bytes to {robot_ip}:{SECONDARY_PORT}", flush=True)
    server = _open_done_listener()
    try:
        _wait_for_controller_ready(robot_ip)
        t_send = time.time()
        _send_urscript(robot_ip, cmd)
        print(f"[URSCRIPT] script sent in {(time.time()-t_send)*1000:.1f}ms — waiting for done callback", flush=True)
        t_wait = time.time()
        _wait_for_motion_complete(robot_ip, timeout=timeout, server=server)
        print(f"[URSCRIPT] done in {time.time()-t_wait:.3f}s — settling 0.3s", flush=True)
    finally:
        try:
            server.close()
        except Exception:
            pass
    # The controller sends "done" just before socket_close/end/script-exit.
    # Without this settle, the next URScript can arrive on port 30002 while the
    # interpreter is still in its cleanup tail and silently drop the script.
    time.sleep(0.3)
    print(f"[URSCRIPT] settle complete — controller ready for next command", flush=True)


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
    """Wrap a move command in a URScript function with TCP/payload setup and done-callback.

    The generated script defines and immediately calls move_with_tcp(), which:
      1. Optionally sets TCP offset and payload.
      2. Executes the move command.
      3. Opens a socket back to PYTHON_HOST_IP:CALLBACK_PORT and sends "done",
         so Python knows the motion has truly finished (not just that it started).
    """
    tcp = _resolve_tcp(tcp)
    lines = ["def move_with_tcp():"]
    if tcp is not None:
        lines.append(f"  set_tcp(p{tcp})")
    if payload is not None:
        if payload_cog:
            lines.append(f"  set_payload({payload}, [{payload_cog[0]}, {payload_cog[1]}, {payload_cog[2]}])")
        else:
            lines.append(f"  set_payload({payload})")
    lines.append(f"  {move_cmd.strip()}")
    # Guard socket_open: on failure URScript would otherwise silently skip the
    # send/close and the robot would finish without Python ever getting "done".
    lines.append(f'  if socket_open("{PYTHON_HOST_IP}", {CALLBACK_PORT}, "cb"):')
    lines.append(f'    socket_send_string("done", "cb")')
    lines.append(f'    socket_close("cb")')
    lines.append(f'  end')
    lines.append("end")
    lines.append("move_with_tcp()")
    return "\n".join(lines) + "\n"


def move_to_pose(robot_ip: str, name: str, position: dict = None, velocity: float = None, acceleration: float = None, tcp=None, payload=None, payload_cog=None) -> str:
    """Move the robot to a pose using movej (joint-space interpolation)."""
    if position is None:
        position = get_position(name)
    v = MOVEJ_DEFAULT_VEL if velocity is None else velocity
    a = MOVEJ_DEFAULT_ACC if acceleration is None else acceleration
    if "joints" in position:
        joints = position["joints"]
        cmd = _wrap_with_tcp(f"movej({joints}, a={a}, v={v})\n", tcp, payload, payload_cog)
        _run_urscript_with_done(robot_ip, cmd)
        return f"movej to '{name}' with joints={joints}"
    elif "pose" in position:
        pose = position["pose"]
        cmd = _wrap_with_tcp(f"movej(p{pose}, a={a}, v={v})\n", tcp, payload, payload_cog)
        _run_urscript_with_done(robot_ip, cmd)
        return f"movej to '{name}' with pose={pose}"
    else:
        raise ValueError(f"Position '{name}' has neither 'joints' nor 'pose'")


def move_to_pose_l(robot_ip: str, name: str, position: dict = None, velocity: float = None, acceleration: float = None, tcp=None, payload=None, payload_cog=None) -> str:
    """Move the robot to a pose using movel (linear TCP interpolation)."""
    if position is None:
        position = get_position(name)
    v = MOVEL_DEFAULT_VEL if velocity is None else velocity
    a = MOVEL_DEFAULT_ACC if acceleration is None else acceleration
    pose = position["pose"]
    cmd = _wrap_with_tcp(f"movel(p{pose}, a={a}, v={v})\n", tcp, payload, payload_cog)
    _run_urscript_with_done(robot_ip, cmd)
    return f"movel to '{name}' with pose={pose}"


def move_to_pose_p(robot_ip: str, name: str, position: dict = None, velocity: float = None, acceleration: float = None, blend_radius: float = None, tcp=None, payload=None, payload_cog=None) -> str:
    """Move the robot to a pose using movep (process / blend move)."""
    if position is None:
        position = get_position(name)
    v = MOVEL_DEFAULT_VEL if velocity is None else velocity
    a = MOVEL_DEFAULT_ACC if acceleration is None else acceleration
    r = MOVEP_BLEND_RADIUS if blend_radius is None else blend_radius
    pose = position["pose"]
    cmd = _wrap_with_tcp(f"movep(p{pose}, a={a}, v={v}, r={r})\n", tcp, payload, payload_cog)
    _run_urscript_with_done(robot_ip, cmd)
    return f"movep to '{name}' with pose={pose}"


def move_to_pose_c(robot_ip: str, via_name: str, end_name: str, via_position: dict = None, end_position: dict = None, velocity: float = None, acceleration: float = None, tcp=None, payload=None, payload_cog=None) -> str:
    """Move the robot along a circular arc using movec (via-point -> end-point)."""
    if via_position is None:
        via_position = get_position(via_name)
    if end_position is None:
        end_position = get_position(end_name)
    v = MOVEL_DEFAULT_VEL if velocity is None else velocity
    a = MOVEL_DEFAULT_ACC if acceleration is None else acceleration
    via_pose = via_position["pose"]
    end_pose = end_position["pose"]
    cmd = _wrap_with_tcp(f"movec(p{via_pose}, p{end_pose}, a={a}, v={v})\n", tcp, payload, payload_cog)
    _run_urscript_with_done(robot_ip, cmd)
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
        f'  if socket_open("{PYTHON_HOST_IP}", {CALLBACK_PORT}, "cb"):\n'
        '    socket_send_string("done", "cb")\n'
        '    socket_close("cb")\n'
        '  end\n'
        "end\n"
        "gripper_seq()\n"
    )
    _run_urscript_with_done(robot_ip, cmd)
    return "Tool digital out sequence: TDO1=True, wait 0.5s, TDO0=False"


def lift_z(robot_ip: str, height: float = 0.1, velocity: float = None, acceleration: float = None) -> str:
    """Move the TCP straight up by `height` meters from the current pose.

    Reads the actual TCP pose on the robot controller and does a movel
    to the same x,y with z + height. Defaults come from LIFT_Z_DEFAULT_VEL /
    LIFT_Z_DEFAULT_ACC at the top of this file.
    """
    height = float(height) if height is not None else 0.1
    velocity = LIFT_Z_DEFAULT_VEL if velocity is None else velocity
    acceleration = LIFT_Z_DEFAULT_ACC if acceleration is None else acceleration
    cmd = (
        "def lift_up():\n"
        "  local curr = get_actual_tcp_pose()\n"
        f"  local target_z = curr[2] + {height}\n"
        "  local cx = curr[0]\n"
        "  local cy = curr[1]\n"
        "  local crx = curr[3]\n"
        "  local cry = curr[4]\n"
        "  local crz = curr[5]\n"
        "  movel(p[cx, cy, target_z, crx, cry, crz], "
        f"a={acceleration}, v={velocity})\n"
        f'  if socket_open("{PYTHON_HOST_IP}", {CALLBACK_PORT}, "cb"):\n'
        '    socket_send_string("done", "cb")\n'
        '    socket_close("cb")\n'
        '  end\n'
        "end\n"
        "lift_up()\n"
    )
    _run_urscript_with_done(robot_ip, cmd)
    return f"Lift: moved TCP up {height}m from current pose"


# Joint order URScript expects for movej(q).
UR_JOINT_NAMES = [
    'shoulder_pan_joint',
    'shoulder_lift_joint',
    'elbow_joint',
    'wrist_1_joint',
    'wrist_2_joint',
    'wrist_3_joint',
]


def execute_trajectory(robot_ip: str, joint_names: list, points: list,
                       tcp=None, payload: float = None, payload_cog: list = None,
                       min_segment_time: float = 0.05,
                       servo_cycle: float = 0.008,
                       servo_lookahead: float = 0.1,
                       servo_gain: int = 300) -> str:
    """Execute a joint-space trajectory (from MoveIt) smoothly via URScript servoj.

    The trajectory's (q, time_from_start) samples are linearly interpolated onto
    a uniform `servo_cycle` grid (default 8 ms = UR CB3 controller rate). Each
    grid sample becomes one `servoj(q, t=cycle, lookahead_time=..., gain=...)`
    call, which the UR servo loop blends continuously — so a dense Pilz LIN/PTP
    plan replays as a single smooth motion (no per-point decelerations).

    Input:
        robot_ip      (str)   — IP of the UR10.
        joint_names   (list)  — joint names matching points[i].positions; reordered
                                into UR_JOINT_NAMES.
        points        (list)  — [{'positions': [6 floats], 'time_from_start': sec}, ...]
        tcp/payload/payload_cog — optional, applied once before the trajectory.
        min_segment_time — unused (kept for backward compat with older callers).
        servo_cycle   (float) — controller cycle, 0.008 s for CB3 / 0.002 s for e-Series.
        servo_lookahead, servo_gain — servoj tuning (UR defaults: 0.1 s, 300).
    Output: str — Confirmation message.
    """
    if not points:
        return "execute_trajectory: empty trajectory, nothing to do"

    # Reorder columns so points[i].positions[idx] == UR_JOINT_NAMES order.
    try:
        idx = [joint_names.index(n) for n in UR_JOINT_NAMES]
    except ValueError as e:
        raise ValueError(
            f"Trajectory missing required UR joint name. joint_names={joint_names}"
        ) from e

    # Materialise (t, q) samples in UR joint order.
    samples = []
    for p in points:
        t = float(p['time_from_start'])
        q = [float(p['positions'][i]) for i in idx]
        samples.append((t, q))
    samples.sort(key=lambda s: s[0])
    nominal = samples[-1][0]
    if nominal <= 0.0 or len(samples) < 2:
        return "execute_trajectory: trajectory too short, nothing to do"

    # Resample onto uniform servo grid via linear interpolation between samples.
    grid = []
    n_steps = max(1, int(math.ceil(nominal / servo_cycle)))
    j = 0  # index of the lower bracket sample
    for k in range(1, n_steps + 1):
        t = min(k * servo_cycle, nominal)
        while j + 1 < len(samples) - 1 and samples[j + 1][0] < t:
            j += 1
        t0, q0 = samples[j]
        t1, q1 = samples[j + 1]
        span = t1 - t0
        a = 0.0 if span <= 1e-9 else max(0.0, min(1.0, (t - t0) / span))
        q = [q0[i] + a * (q1[i] - q0[i]) for i in range(6)]
        grid.append(q)

    tcp = _resolve_tcp(tcp)
    lines = ["def run_traj():"]
    if tcp is not None:
        lines.append(f"  set_tcp(p{tcp})")
    if payload is not None:
        if payload_cog:
            lines.append(
                f"  set_payload({payload}, "
                f"[{payload_cog[0]}, {payload_cog[1]}, {payload_cog[2]}])"
            )
        else:
            lines.append(f"  set_payload({payload})")

    # Stream the trajectory as one servoj per controller cycle. servoj blocks
    # for `t` seconds and the script loop runs inside the robot's real-time
    # context, so the cadence is intrinsically locked to the controller clock.
    for q in grid:
        q_str = "[" + ", ".join(f"{v:.6f}" for v in q) + "]"
        lines.append(
            f"  servoj({q_str}, t={servo_cycle:.4f}, "
            f"lookahead_time={servo_lookahead}, gain={servo_gain})"
        )
    # Drain the servoj lookahead buffer by repeating the final setpoint for at
    # least lookahead_time/servo_cycle cycles (~13 at defaults). This lets the
    # controller fully converge to the target before stopj halts the stream,
    # avoiding both the short-stop and the jerk from switching to movej.
    q_final_str = "[" + ", ".join(f"{v:.6f}" for v in grid[-1]) + "]"
    drain_steps = max(1, int(math.ceil(servo_lookahead / servo_cycle))) + 5
    for _ in range(drain_steps):
        lines.append(
            f"  servoj({q_final_str}, t={servo_cycle:.4f}, "
            f"lookahead_time={servo_lookahead}, gain={servo_gain})"
        )
    lines.append("  stopj(2.0)")
    lines.append(f'  if socket_open("{PYTHON_HOST_IP}", {CALLBACK_PORT}, "cb"):')
    lines.append(f'    socket_send_string("done", "cb")')
    lines.append(f'    socket_close("cb")')
    lines.append(f'  end')
    lines.append("end")
    lines.append("run_traj()")
    cmd = "\n".join(lines) + "\n"
    script_bytes = len(cmd.encode('utf-8'))

    # Bind listener BEFORE sending the script so the controller's socket_open
    # call cannot beat Python's accept() — a real risk for short trajectories.
    print(
        f"[TRAJ] sending trajectory: {len(grid)} servoj setpoints, "
        f"nominal={nominal:.2f}s, script={script_bytes} bytes → {robot_ip}:{SECONDARY_PORT}",
        flush=True
    )
    server = _open_done_listener()
    try:
        _wait_for_controller_ready(robot_ip)
        send_start = time.time()
        _send_urscript(robot_ip, cmd)
        send_elapsed = time.time() - send_start
        print(f"[TRAJ] script sent in {send_elapsed*1000:.1f}ms — waiting for done callback (timeout={max(60.0, nominal+15.0):.0f}s)", flush=True)

        # Allow nominal duration + 15 s safety margin (network + stopj settle).
        wait_start = time.time()
        _wait_for_motion_complete(robot_ip, timeout=max(60.0, nominal + 15.0), server=server)
        wait_elapsed = time.time() - wait_start
        print(f"[TRAJ] done in {wait_elapsed:.3f}s (nominal {nominal:.2f}s, overhead {wait_elapsed-nominal:+.2f}s) — settling 0.3s", flush=True)
        # Settle: controller sends "done" just before socket_close/end/script-exit.
        # Without this, the next URScript arrives while the interpreter is still
        # cleaning up and gets silently dropped.
        time.sleep(0.3)
        print(f"[TRAJ] settle complete — controller ready for next command", flush=True)
    finally:
        try:
            server.close()
        except Exception:
            pass

    overhead = wait_elapsed - nominal
    print(
        f"execute_trajectory: servoj setpoints={len(grid)} cycle={servo_cycle*1000:.1f}ms "
        f"nominal={nominal:.2f}s actual={wait_elapsed:.2f}s overhead={overhead:+.2f}s | "
        f"script_bytes={script_bytes} send={send_elapsed*1000:.1f}ms"
    )
    return (
        f"Executed trajectory: {len(grid)} servoj setpoints, "
        f"nominal {nominal:.2f}s, actual {wait_elapsed:.2f}s"
    )


def set_tool_digital_out_open(robot_ip: str) -> str:
    """Open the gripper by setting tool digital output 0 to True.

    Input:  robot_ip (str) — IP address of the UR10.
    Output: str — Confirmation message.
    """
    cmd = (
        "def gripper_open():\n"
        "  set_tool_digital_out(0, True)\n"
        "  sleep(0.5)\n"
        f'  if socket_open("{PYTHON_HOST_IP}", {CALLBACK_PORT}, "cb"):\n'
        '    socket_send_string("done", "cb")\n'
        '    socket_close("cb")\n'
        '  end\n'
        "end\n"
        "gripper_open()\n"
    )
    _run_urscript_with_done(robot_ip, cmd)
    return "Tool digital out: TDO0=True (open), wait 0.5s"


def fire_nailgun(robot_ip: str) -> str:
    """Fire the nailgun by pulsing digital output 0: True then False."""
    cmd = (
        "def fire_nailgun():\n"
        "  set_digital_out(0, True)\n"
        "  sleep(0.5)\n"
        "  set_digital_out(0, False)\n"
        f'  if socket_open("{PYTHON_HOST_IP}", {CALLBACK_PORT}, "cb"):\n'
        '    socket_send_string("done", "cb")\n'
        '    socket_close("cb")\n'
        '  end\n'
        "end\n"
        "fire_nailgun()\n"
    )
    _run_urscript_with_done(robot_ip, cmd)
    return "Nailgun fired: DO0=True, sleep 0.5s, DO0=False"


if __name__ == "__main__":
    import sys
    if len(sys.argv) != 3:
        print("Usage: python ur10_commands.py <robot_ip> <program_name>")
        sys.exit(1)
    result = load_program(sys.argv[1], sys.argv[2])
    print(result)
