# ______________________________________
# Test: gesture → gripper integration
# ______________________________________

# to run this script: 
# 1) Activate your environment (Developer powershell for VS: "Conda activate LARA8_RealSense")
# 2) Activate the gripper using Robotiq's RUI
# 3) Run app.py to start the gesture client. Type in the Termianl:

# ______________________________________

import os, sys, time, subprocess, socket
from pathlib import Path
from serial.tools import list_ports
import socket
import threading

# ---- path bootstrap (works when run from Tests/ or root) ----
HERE = Path(__file__).resolve()
PROJ = HERE.parents[1]                 # ...\3_UR_pick-n-place
HELPERS = PROJ / "helpers"
CORE = PROJ / "core"
GESTURE = PROJ / "gesture"
for p in (PROJ, HELPERS, CORE, GESTURE):
    sp = str(p)
    if sp not in sys.path:
        sys.path.insert(0, sp)

# imports from your project
from helpers.gesture_client import GestureClient            # client that reads hand_sign_id
from core.robotiq_gripper import RobotiqModbusRTUGripper    # minimal Modbus RTU driver

# ---- config ----
HOST, PORT = "127.0.0.1", 65432

# Camera index assignments
# CAM_INDEX = 0 #int(os.getenv("CAM_INDEX", 0))         # laptop webcam (double check between 0, 1, 2)
CAM_INDEX_GESTURE = 0       # built-in laptop camera (used by gesture/app.py)
#CAM_INDEX_REALSENSE = 2     # Intel RealSense camera for perception



PREFERRED_COMS = ("COM4") #chnaged from COM3
DWELL = 0.8             # allow motion to finish before next command

# map gesture IDs to actions (supports both schemes you’ve used)
GESTURE_ACTIONS = {3: "open", 4: "close", 6: "open", 5: "close"}

def log(msg: str):
    print(msg, flush=True)

def autodetect_gripper_port(preferred=PREFERRED_COMS) -> str | None:
    ports = [p.device for p in list_ports.comports()]
    for cand in preferred:
        if cand in ports:
            return cand
    return ports[0] if ports else None

def start_gesture_server():
    """
    Launch the gesture recognition server (gesture/app.py) on camera 0
    in a background subprocess. Keeps stdout/stderr attached so you can
    see MediaPipe and socket prints in the same terminal.
    """
    app = GESTURE / "app.py"
    if not app.exists():
        raise SystemExit(f"[ERROR] Cannot find gesture server: {app}")
    env = os.environ.copy()
    proc = subprocess.Popen(
        [sys.executable, str(app), "--device", str(CAM_INDEX_GESTURE)],
        cwd=str(GESTURE),
        env=env,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
    )
    log(f"[GESTURE] Server starting from {app} (device {CAM_INDEX_GESTURE}) ...") # check cam index!!!
    return proc

def wait_for_server(timeout=8.0):
    t0 = time.time()
    while time.time() - t0 < timeout:
        try:
            with socket.create_connection((HOST, PORT), timeout=0.5):
                log("[GESTURE] server reachable")
                return True
        except Exception:
            time.sleep(0.25)
    return False

def main():
    # 1) start app.py (camera 0)
    proc = start_gesture_server()
    try:
        if not wait_for_server():
            if proc.stdout:
                for _ in range(12):
                    line = proc.stdout.readline().rstrip()
                    if not line:
                        break
                    log("[app.py] " + line)
            raise SystemExit("[ERROR] gesture server not reachable on 127.0.0.1:65432")

        # 2) start client
        client = GestureClient(HOST, PORT)
        client.start()
        time.sleep(0.2)
        log(f"[CLIENT] connected; first id = {client.latest_id()}")

        # 3) start gripper (connect → activate → status)
        port = autodetect_gripper_port()
        if not port:
            log("[GRIPPER][ERROR] No serial ports. Is the USB/RS485 adapter connected?")
            return
        log(f"[GRIPPER] Using port {port}")
        g = RobotiqModbusRTUGripper(port, baud=115200, timeout=0.4, dwell=DWELL)
        g.connect()
        g.activate()
        g.read_status()
        log("[GRIPPER] ready (open/close will follow gestures)")

        # 4) react to gesture changes
        last_id = None
        log("---- Show hand gestures (Open/Close). Press Ctrl+C to stop. ----")
        while True:
            gid = client.latest_id()
            action = GESTURE_ACTIONS.get(gid, "idle")

            # Only send valid actions (skip idle)
            if action != "idle":
                log(f"[GESTURE] id={gid} -> {action}")
                if action == "open":
                    g.open()
                    g.read_status()
                elif action == "close":
                    g.close()
                    g.read_status()

            # small delay to avoid flooding the serial connection
            time.sleep(0.3)

    except KeyboardInterrupt:
        log("[STOP] interrupted by user")
    finally:
        # cleanup gripper
        try:
            g.open(); g.disconnect()
        except Exception:
            pass
        # stop app.py subprocess
        try:
            if proc and proc.poll() is None:
                proc.terminate()
                try:
                    proc.wait(timeout=2.0)
                except Exception:
                    proc.kill()
        except Exception:
            pass
        log("[CLEANUP] done")

if __name__ == "__main__":
    main()
