# -*- coding: utf-8 -*-
# ______________________________________________________________________
# UR10 gesture→gripper control (Robotiq) — V4 (PURE RTDE, no external helper, no URCap socket)
#
# What changed in this version
# • Uses ONLY ur-rtde (RTDEControlInterface) to send tiny URScript snippets
#   that call the Robotiq URCap functions: rq_activate/rq_open/rq_close,
#   rq_set_speed/rq_set_force. No helper files, no tcp/63352 socket.
# • Keeps Dashboard (29999) raw-socket for play/pause + bring-up (optional).
# • Preserves the app.py gesture mapping: 3/4/5/6 → play/pause/close/open.
# • Safe shutdown: open gripper, stop RTDE cleanly.
#
# How to run
# 1) conda activate LARA8_RealSense
# 2) Terminal A: python app.py --device 2
# 3) Terminal B: python this_file.py
#
# IMPORTANT
# • The Robotiq URCap must be installed on the controller so the URScript
#   functions (rq_activate/rq_open/...) exist. You do NOT need the Robotiq
#   socket server (63352) for this version.
# ______________________________________________________________________

import socket
import threading
import time
import logging
from contextlib import closing

# =========================
# ======= CONFIG ==========
# =========================
ROBOT_IP = "192.168.3.60" #QUT
#"169.254.130.206"   # UR10, RoboLab K1
DASHBOARD_PORT = 29999

# Robotiq tuning (percent 0..100)
RQB_SPEED = 100
RQB_FORCE = 50

# Optional: on shutdown, open gripper so the part isn't clamped
OPEN_ON_SHUTDOWN = True

# Gesture socket (from app.py)
GESTURE_HOST = "localhost"
GESTURE_PORT = 65432

# Timing
ACTIVATE_WAIT_S = 2.0   # wait after activation
MOVE_WAIT_S     = 0.6   # wait after open/close command

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s | %(levelname)s | %(message)s",
    datefmt="%H:%M:%S"
)

# =========================
# ===== Imports ===========
# =========================
from rtde_control import RTDEControlInterface as RTDEControl

# =========================
# === Dashboard helper ====
# =========================
class URDashboard:
    """Raw-socket dashboard helper (no ur_rtde.dashboard_client needed)."""
    def __init__(self, ip, port=DASHBOARD_PORT, timeout=2.0):
        self.ip = ip; self.port = port; self.timeout = timeout

    def _send(self, cmd, expect_reply=True):
        with closing(socket.socket(socket.AF_INET, socket.SOCK_STREAM)) as s:
            s.settimeout(self.timeout)
            s.connect((self.ip, self.port))
            _ = s.recv(1024)  # banner
            s.sendall((cmd + " ").encode("ascii"))
            return s.recv(1024).decode("utf-8", errors="ignore").strip() if expect_reply else ""

    def play(self):   return self._send("play")
    def pause(self):  return self._send("pause")
    def power_on(self):      return self._send("power on")
    def brake_release(self): return self._send("brake release")


# =========================
# ===== UR Robot ==========
# =========================
class URRobot:
    """Dashboard PAUSE/PLAY + bring-up via 29999."""
    def __init__(self, ip=ROBOT_IP, port=DASHBOARD_PORT):
        self.ip = ip
        self.port = port

    def _dashboard(self, cmd: str, expect_reply=True) -> str:
        with closing(socket.socket(socket.AF_INET, socket.SOCK_STREAM)) as s:
            s.settimeout(2.0)
            s.connect((self.ip, self.port))
            _ = s.recv(1024)
            s.sendall((cmd + "").encode("ascii"))
            if expect_reply:
                return s.recv(1024).decode("utf-8", errors="ignore").strip()
            return ""

    def play(self):
        try:
            resp = self._dashboard("play")
            logging.info(f"[dashboard] play -> {resp}")
        except Exception as e:
            logging.warning(f"[dashboard] play error: {e}")

    def pause(self):
        try:
            resp = self._dashboard("pause")
            logging.info(f"[dashboard] pause -> {resp}")
        except Exception as e:
            logging.warning(f"[dashboard] pause error: {e}")

    def bringup(self):
        """Power on + brake release (best-effort)."""
        try:
            mode = self._dashboard("robotmode")
            logging.info(f"[dashboard] robotmode -> {mode}")
            logging.info("[dashboard] powerOn ...")
            self._dashboard("power on")
            time.sleep(1.0)
            logging.info("[dashboard] brakeRelease ...")
            self._dashboard("brake release")
        except Exception as e:
            logging.warning(f"[dashboard] bringup warning: {e}")


# =========================
# === Robotiq via RTDE ====
# =========================
class RobotiqRTDE:
    """Send tiny URScript snippets through RTDEControl to control Robotiq.
       Requires Robotiq URCap installed (for rq_* URScript funcs).
    """
    def __init__(self, robot_ip: str, speed_pct: int, force_pct: int):
        self.robot_ip = robot_ip
        self.speed_pct = int(max(0, min(100, speed_pct)))
        self.force_pct = int(max(0, min(100, force_pct)))
        self.rtde_c = None

    def connect(self):
        self.rtde_c = RTDEControl(self.robot_ip)
        logging.info("[robotiq/rtde] Connected to RTDEControl")
        self._script(f"rq_set_speed({self.speed_pct})\n"
                     f"rq_set_force({self.force_pct})\n"
                     f"rq_activate()")
        time.sleep(ACTIVATE_WAIT_S)
        logging.info("[robotiq/rtde] Activated")

    def set_speed(self, pct: int):
        self.speed_pct = int(max(0, min(100, pct)))
        self._script(f"rq_set_speed({self.speed_pct})")

    def set_force(self, pct: int):
        self.force_pct = int(max(0, min(100, pct)))
        self._script(f"rq_set_force({self.force_pct})")

    def open(self):
        self._script("rq_open()")
        time.sleep(MOVE_WAIT_S)
        logging.info("[robotiq] OPEN")

    def close(self):
        self._script("rq_close()")
        time.sleep(MOVE_WAIT_S)
        logging.info("[robotiq] CLOSE")

    def shutdown(self):
        try:
            if OPEN_ON_SHUTDOWN:
                self.open()
        except Exception:
            pass
        try:
            if self.rtde_c is not None:
                self.rtde_c.stopRobot()
        except Exception:
            pass
        logging.info("[robotiq/rtde] Shutdown complete")

    # ---- low-level helper ----
    def _script(self, body: str):
        if self.rtde_c is None:
            raise RuntimeError("RTDE not connected")
        # Send as a tiny program (executes immediately on the controller)
        prog = "def rq_prog():" + "".join("  " + ln for ln in body.splitlines()) + "end"
        try:
            # newer ur_rtde versions provide sendCustomScript; fallback to moveJ + dashboard if needed
            self.rtde_c.sendCustomScript(prog)  # type: ignore[attr-defined]
        except AttributeError:
            # Fallback: use dashboard 'play' of a loaded program is not ideal; better to require sendCustomScript
            raise RuntimeError("Your ur-rtde version lacks sendCustomScript(). Please update ur-rtde.")


# =========================
# === Gesture Listener ====
# =========================
class GestureListener(threading.Thread):
    """Connects to app.py (localhost:65432) and triggers actions.
       4 -> pause, 3 -> play, 5 -> close gripper, 6 -> open gripper
    """
    def __init__(self, robot: URRobot, gripper: RobotiqRTDE,
                 host=GESTURE_HOST, port=GESTURE_PORT):
        super().__init__(daemon=True)
        self.robot = robot
        self.gripper = gripper
        self.host = host
        self.port = port
        self._stop = threading.Event()
        self._last_id = None

    def stop(self):
        self._stop.set()

    def run(self):
        with closing(socket.socket(socket.AF_INET, socket.SOCK_STREAM)) as c:
            logging.info(f"[gesture] Connecting to {self.host}:{self.port} ...")
            c.connect((self.host, self.port))
            logging.info("[gesture] Connected")
            c.settimeout(0.2)

            buf = b""
            while not self._stop.is_set():
                try:
                    data = c.recv(1024)
                    if not data:
                        time.sleep(0.05)
                        continue
                    buf += data
                    while buf:
                        ch = buf[0:1]; buf = buf[1:]
                        if not ch.isdigit():
                            continue
                        gid = int(ch)
                        if gid == self._last_id:
                            continue
                        self._last_id = gid
                        logging.info(f"[gesture] Hand Sign ID: {gid}")
                        if   gid == 4:
                            self.robot.pause()
                        elif gid == 3:
                            self.robot.play()
                        elif gid == 5:
                            self.gripper.close()
                        elif gid == 6:
                            self.gripper.open()
                except socket.timeout:
                    continue
                except Exception as e:
                    logging.warning(f"[gesture] {e}")
                    time.sleep(0.2)


# =========================
# ========= Main ==========
# =========================

def main():
    robot = URRobot(ROBOT_IP)
    robot.bringup()  # best-effort power/brakes (safe if already on)

    g = RobotiqRTDE(robot_ip=ROBOT_IP, speed_pct=RQB_SPEED, force_pct=RQB_FORCE)
    g.connect()

    listener = GestureListener(robot=robot, gripper=g)
    listener.start()

    logging.info("Running. Press Ctrl+C to exit.")
    try:
        while True:
            time.sleep(0.5)
    except KeyboardInterrupt:
        logging.info("Shutting down...")
    finally:
        listener.stop(); listener.join(timeout=2.0)
        try:
            g.shutdown()
        except Exception:
            pass
        logging.info("Done.")


if __name__ == "__main__":
    main()
