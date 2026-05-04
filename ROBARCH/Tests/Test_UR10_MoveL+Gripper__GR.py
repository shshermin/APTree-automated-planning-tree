#________________________________________________________________
# MoveL between 3 TCP poses + Robotiq open/close at waypoints
#________________________________________________________________
# Requirements:
#   pip install ur-rtde
# Notes:
#   - Targets remain exactly as in your working script.
#   - Adds a minimal Robotiq URCap-socket client (same idea as your helper).
#   - Opens before arriving at pick_1_1, closes after arriving at pick_2_1.
#________________________________________________________________


#_Imports
from rtde_control import RTDEControlInterface as RTDEControl
from rtde_receive import RTDEReceiveInterface as RTDEReceive
import socket, time
import time
import binascii
import socket
from contextlib import contextmanager

try:
    import serial
except Exception:
    serial = None

ROBOT_IP = "192.168.3.60" #QUT
#"169.254.130.206"   # UR10, RoboLab K1


ROBOTIQ_PORT = 63352           # Robotiq URCap socket
RQB_SPEED = 128                # 0..255
RQB_FORCE = 64                 # 0..255
RQB_OPEN  = 0                  # 0=fully open
RQB_CLOSE = 255                # 255=fully closed

#_1_Motion parameters (keep as in your file)
SPEED = 0.6   # MoveL speed (your working value)
ACC   = 0.8   # MoveL acceleration (your working value)
BLEND = 0.0   # not used in this RTDE call (kept for consistency)

#_2_Tool params (optional)
TCP_OFFSET   = [0.0, 0.0, 0.0, 0.0, 0.0, 0.0]  # [m, m, m, rad, rad, rad] wrt flange
PAYLOAD_KG   = 1.0
COG_AT_TCP   = [0.0, 0.0, 0.05]                # [m, m, m]

#_3_Targets (keep EXACTLY as in your working script)
pick_1_1 = [-143.03, 607.6, 725.2, 0.532, -3.154, 0.14]
pick_2_1 = [-143.03, 607.6, 922.15, 0.532, -3.154, 0.14]
pick_3   = [-143.03, 607.6, 725.2, 0.532, -3.154, 0.14]

# ---------------- Robotiq (URCap socket) mini-client ----------------
# Based on your helper’s inline client approach; trimmed for open/close. :contentReference[oaicite:1]{index=1}
class RobotiqURCapClient:
    def __init__(self, host, port=ROBOTIQ_PORT, timeout=2.0):
        self.host = host
        self.port = port
        self.timeout = timeout
        self.sock = None

    def connect(self):
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        s.settimeout(self.timeout)
        s.connect((self.host, self.port))
        self.sock = s

    def close_conn(self):
        if self.sock:
            try:
                self.sock.close()
            finally:
                self.sock = None

    def _send_line(self, line):
        if not self.sock:
            raise RuntimeError("Robotiq client not connected")
        if not line.endswith("\n"):
            line += "\n"
        self.sock.sendall(line.encode("ascii"))

    def _read_line(self):
        if not self.sock:
            raise RuntimeError("Robotiq client not connected")
        data = b""
        while not data.endswith(b"\n"):
            chunk = self.sock.recv(4096)
            if not chunk:
                break
            data += chunk
        return data.decode("ascii").strip()

    def set_var(self, name, value):
        self._send_line(f"SET {name} {int(value)}")
        return self._read_line()

    def get_var(self, name):
        self._send_line(f"GET {name}")
        resp = self._read_line()
        try:
            return int(resp.split()[-1])
        except Exception:
            return -1

    def activate(self, speed=RQB_SPEED, force=RQB_FORCE, wait=True, timeout=8.0):
        # ACT=1, GTO=1, optionally set default speed/force
        self.set_var("ACT", 1)
        self.set_var("GTO", 1)
        self.set_var("SPE", speed)
        self.set_var("FOR", force)
        if not wait:
            return
        t0 = time.time()
        while time.time() - t0 < timeout:
            flt = self.get_var("FLT")  # fault
            sta = self.get_var("STA")  # status (3 = active)
            if flt != 0:
                raise RuntimeError(f"Robotiq fault FLT={flt}")
            if sta == 3:
                return
            time.sleep(0.1)
        raise TimeoutError("Robotiq activate timeout")

    def move_and_wait(self, pos, speed=RQB_SPEED, force=RQB_FORCE, timeout=10.0):
        self.set_var("SPE", speed)
        self.set_var("FOR", force)
        self.set_var("POS", max(0, min(255, int(pos))))
        self.set_var("GTO", 1)
        t0 = time.time()
        while time.time() - t0 < timeout:
            pre = self.get_var("PRE")  # position request echoed
            obj = self.get_var("OBJ")  # object status
            sta = self.get_var("STA")  # status
            flt = self.get_var("FLT")  # fault
            if flt != 0:
                raise RuntimeError(f"Robotiq fault FLT={flt}")
            if pre == int(pos) and sta == 3 and obj in (1, 2, 3):
                return obj
            time.sleep(0.05)
        raise TimeoutError("Robotiq move timeout")

    def open(self):  return self.move_and_wait(RQB_OPEN)
    def close(self): return self.move_and_wait(RQB_CLOSE)

# ---------------- Main sequence ----------------
def main():
    rtde_c = RTDEControl(ROBOT_IP)
    rtde_r = RTDEReceive(ROBOT_IP)
    g = RobotiqURCapClient(ROBOT_IP, ROBOTIQ_PORT)

    try:
        # Tool context (optional if set in Polyscope)
        rtde_c.setTcp(TCP_OFFSET)
        rtde_c.setPayload(PAYLOAD_KG, COG_AT_TCP)

        # Pre-move sanity
        try:
            print("[State] ActualQ (rad):", rtde_r.getActualQ())
            print("[State] Actual TCP pose [m,rad]:", rtde_r.getActualTCPPose())
        except Exception:
            pass

        # --- Gripper connect + activate ---
        print("[Gripper] Connecting @ %s:%d" % (ROBOT_IP, ROBOTIQ_PORT))
        g.connect()
        print("[Gripper] Activate")
        g.activate()

        # === Your requested timing ===
        # 1) Before arriving to pick_1_1: OPEN
        print("[Gripper] OPEN before pick_1_1")
        try:
            g.open()
        except Exception as e:
            print("[Gripper] Open warning:", e)

        # 2) Move to pick_1_1
        print("moveL -> pick_1_1")
        rtde_c.moveL(pick_1_1, SPEED, ACC)

        # 3) Move to pick_2_1
        print("moveL -> pick_2_1")
        rtde_c.moveL(pick_2_1, SPEED, ACC)

        # 4) After arriving to pick_2_1: CLOSE
        print("[Gripper] CLOSE at pick_2_1")
        try:
            obj = g.close()
            print("[Gripper] OBJ:", obj)
        except Exception as e:
            print("[Gripper] Close warning:", e)

        # 5) Optional third pose
        print("moveL -> pick_3")
        rtde_c.moveL(pick_3, SPEED, ACC)

        # 6) Optional return
        print("moveL -> pick_1_1 (return)")
        rtde_c.moveL(pick_1_1, SPEED, ACC)

    finally:
        # Clean stops & disconnects
        try:
            rtde_c.stopL(ACC)
        except Exception:
            pass
        try:
            g.close_conn()
        except Exception:
            pass
        rtde_c.disconnect()
        rtde_r.disconnect()

if __name__ == "__main__":
    main()
