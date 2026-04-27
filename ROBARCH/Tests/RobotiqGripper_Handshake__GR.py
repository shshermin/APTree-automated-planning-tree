# Minimal Robotiq/Hand-E serial loop on COM4 (UR10 side doesn't matter here)
# Requires: pip install pyserial

import time, serial, binascii

COM_PORT = "COM4"
BAUD = 115200
READ_TIMEOUT = 0.3
DWELL = 1.0  # seconds between motions

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
    frame = payload_wo_crc + crc16_modbus(payload_wo_crc)
    ser.write(frame)
    time.sleep(pause)
    data = ser.read(rx_len) if rx_len > 0 else ser.read(64)
    if data:
        print("RX:", binascii.hexlify(data))
    return data

# ---------------- Frames (without CRC) ----------------
# All write-multiple-registers (FC16) to 0x03E8..0x03EA (3 regs, 6 data bytes)
# Data bytes layout (works with your device):
#   [0]=rACT (bit0), [1]=rGTO (bit0), [2]=reserved/ATR, [3]=POS, [4]=SPE, [5]=FOR

FC16_HEADER = b"\x09\x10\x03\xE8\x00\x03\x06"

# Robust activation: ACT=1, GTO=1, POS=0x00, SPE=0xFF, FOR=0x80
FRAME_ACTIVATE = FC16_HEADER + bytes([0x01, 0x01, 0x00, 0x00, 0xFF, 0x80])

# CLOSE: use your proven bytes (POS=0xFF, SPE=0xFF, FOR=0xFF)
FRAME_CLOSE    = FC16_HEADER + bytes([0x09, 0x00, 0x00, 0xFF, 0xFF, 0xFF])

# 50% OPEN: POS=0x80, SPE=0xFF, FOR=0xFF (keeps same style as your working frame)
FRAME_OPEN_50  = FC16_HEADER + bytes([0x09, 0x00, 0x00, 0x80, 0xFF, 0xFF])

# Optional read (FC04) of status gSTA/gOBJ/gFLT starting at 0x07D3 (3 regs)
FRAME_READ_3REG = b"\x09\x04\x07\xD3\x00\x03"  # CRC added at send

def activate(ser):
    print("TX: ACTIVATE (rACT=1, rGTO=1, SPE,FOR set)")
    tx(ser, FRAME_ACTIVATE)

def loop_open_close(ser):
    while True:
        print("CLOSE")
        tx(ser, FRAME_CLOSE)
        time.sleep(DWELL)

        print("OPEN 50%")
        tx(ser, FRAME_OPEN_50)
        time.sleep(DWELL)

def main():
    with serial.Serial(COM_PORT, BAUD, timeout=READ_TIMEOUT,
                       parity=serial.PARITY_NONE,
                       stopbits=serial.STOPBITS_ONE,
                       bytesize=serial.EIGHTBITS) as ser:
        print(f"Opened {COM_PORT} @ {BAUD}")
        activate(ser)
        loop_open_close(ser)

if __name__ == "__main__":
    main()
