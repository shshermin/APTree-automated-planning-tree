#________________________________________________________________
# MoveL between 3 TCP poses + Robotiq Modbus-RTU (serial) open/close
#________________________________________________________________
# Requirements:
#   pip install ur-rtde pyserial
#
# Notes:
#   - Targets remain EXACTLY as in your working script (no unit changes).
#   - Gripper uses Modbus RTU bytes (your FC16 frames) over COM4.
#   - Opens BEFORE pick_1_1, closes AFTER pick_2_1.
#________________________________________________________________

from rtde_control import RTDEControlInterface as RTDEControl
from rtde_receive import RTDEReceiveInterface as RTDEReceive
import time, binascii
import math 

try:
    import serial
except Exception:
    serial = None

ROBOT_IP = "192.168.3.60" #QUT
#"169.254.130.206"   # UR10, RoboLab K1

# ---------------- Motion parameters (kept as in your file) ----------------
SPEED = 0.6   # MoveL speed (kept as-is)
ACC   = 0.8   # MoveL acceleration (kept as-is)
BLEND = 0.0   # not used in this RTDE call (kept for consistency)

# ---------------- Tool params (optional) ----------------------------------
TCP_OFFSET   = [0.0, 0.0, 0.0, 0.0, 0.0, 0.0]  # [m, m, m, rad, rad, rad] wrt flange
PAYLOAD_KG   = 1.0
COG_AT_TCP   = [0.0, 0.0, 0.05]                # [m, m, m]

# ---------------- Targets  -------------
# ptJ_1-2 (x,y,z,rx,ry,rz) in deg
# ptJ_1 (Joint position) = [-97.54, -99.85, 135.96, -126.3, -89.17, -188.56]
# ptJ_2 (Joint position) = [-4.4, 494.83, 132.61, 0.035, -3.126, -0.007]

#_2_two targets, described by Joint Position (in deg), translated into radians (roboLab_GR_26.10.25)
ptJ_1 = [-97.54, -99.85, 135.96, -126.3, -89.17, -188.56]
P1 = [math.radians(d) for d in ptJ_1]

ptJ_2= [-4.4, 494.83, 132.61, 0.035, -3.126, -0.007] 
P2 = [math.radians(d) for d in ptJ_2] 



# ---------------- Robotiq Serial (Modbus RTU) client ----------------------

COM_PORT = "COM4"
BAUD = 115200
READ_TIMEOUT = 0.3
DWELL = 0.2   # small dwell between serial actions

def crc16_modbus(b: bytes) -> bytes:
    crc = 0xFFFF
    for ch in b:
        crc ^= ch
        for _ in range(8):
            if crc & 1:
                crc = (crc >> 1) ^ 0xA001
            else:
                crc >>= 1
    return crc.to_bytes(2, "little")

def tx(ser: serial.Serial, payload_wo_crc: bytes, rx_len: int = 0, pause: float = 0.03) -> bytes:
    """Send payload (no CRC) -> appends CRC16, reads reply (best-effort)."""
    frame = payload_wo_crc + crc16_modbus(payload_wo_crc)
    ser.write(frame)
    time.sleep(pause)
    data = ser.read(rx_len) if rx_len > 0 else ser.read(64)
    if data:
        print("RX:", binascii.hexlify(data))
    return data

# All write-multiple-registers (FC16) to 0x03E8..0x03EA (3 regs, 6 data bytes)
# Data bytes layout (works with your device):
#   [0]=rACT(bit0), [1]=rGTO(bit0), [2]=reserved/ATR, [3]=POS, [4]=SPE, [5]=FOR
FC16_HEADER = b"\x09\x10\x03\xE8\x00\x03\x06"

# Robust activation: ACT=1, GTO=1, POS=0x00, SPE=0xFF, FOR=0x80
FRAME_ACTIVATE = FC16_HEADER + bytes([0x01, 0x01, 0x00, 0x00, 0xFF, 0x80])

# CLOSE (your proven style): POS=0xFF, SPE=0xFF, FOR=0xFF
FRAME_CLOSE    = FC16_HEADER + bytes([0x09, 0x00, 0x00, 0xFF, 0xFF, 0xFF])

# OPEN 50% (keeps same pattern): POS=0x80, SPE=0xFF, FOR=0xFF
FRAME_OPEN_50  = FC16_HEADER + bytes([0x09, 0x00, 0x00, 0x80, 0xFF, 0xFF])

class RobotiqSerial:
    def __init__(self, port=COM_PORT, baud=BAUD, timeout=READ_TIMEOUT):
        self.port = port
        self.baud = baud
        self.timeout = timeout
        self.ser = None

    def __enter__(self):
        if serial is None:
            raise RuntimeError("pyserial not available. pip install pyserial")
        self.ser = serial.Serial(self.port, self.baud, timeout=self.timeout,
                                 parity=serial.PARITY_NONE,
                                 stopbits=serial.STOPBITS_ONE,
                                 bytesize=serial.EIGHTBITS)
        print(f"[Gripper] Opened {self.port} @ {self.baud}")
        time.sleep(DWELL)
        return self

    def __exit__(self, exc_type, exc, tb):
        if self.ser and self.ser.is_open:
            try:
                self.ser.close()
                print("[Gripper] Serial closed")
            except Exception:
                pass
        self.ser = None

    def activate(self):
        print("[Gripper] ACTIVATE")
        tx(self.ser, FRAME_ACTIVATE)
        time.sleep(0.3)

    def open50(self):
        print("[Gripper] OPEN 50%")
        tx(self.ser, FRAME_OPEN_50)
        time.sleep(0.3)

    def close(self):
        print("[Gripper] CLOSE")
        tx(self.ser, FRAME_CLOSE)
        time.sleep(0.3)

# ---------------- Main sequence -------------------------------------------

def main():
    rtde_c = RTDEControl(ROBOT_IP)
    rtde_r = RTDEReceive(ROBOT_IP)

    print("[Cmd] P1 (m,rad):", P1)
    rtde_c.moveJ(P1, SPEED, ACC)
    print("[Cmd] P2 (m,rad):", P2)
    rtde_c.moveJ(P2, SPEED, ACC)
    print("[Cmd] P3 (m,rad):", P1)
    rtde_c.moveJ(P1, SPEED, ACC) 

    try:
        # Tool context (optional if set in Polyscope)
        rtde_c.setTcp(TCP_OFFSET)
        rtde_c.setPayload(PAYLOAD_KG, COG_AT_TCP)

        # Pre-move sanity (best-effort)
        try:
            print("[State] ActualQ (rad):", rtde_r.getActualQ())
            print("[State] Actual TCP pose [m,rad]:", rtde_r.getActualTCPPose())
        except Exception:
            pass

        # --- Gripper (Modbus RTU over COM4) ---
        with RobotiqSerial() as g:
            g.activate()

            # === Your requested timing ===
            # 1) BEFORE arriving to pick_1_1: OPEN
            g.open50()

            # 2) Move to pick_1_1
            print("moveJ -> P1")
            rtde_c.moveJ(P1, SPEED, ACC)

            # 3) Move to pick_2_1
            print("moveJ -> P2")
            rtde_c.moveJ(P2, SPEED, ACC)

            # 4) AFTER arriving to pick_2_1: CLOSE
            g.close()

            # 5) Optional third pose
            print("moveJ -> P1")
            rtde_c.moveJ(P1, SPEED, ACC)


    finally:
        # Clean stop for linear motion & disconnects
        try:
            rtde_c.stopL(ACC)
        except Exception:
            pass
        rtde_c.disconnect()
        rtde_r.disconnect()

if __name__ == "__main__":
    main()
