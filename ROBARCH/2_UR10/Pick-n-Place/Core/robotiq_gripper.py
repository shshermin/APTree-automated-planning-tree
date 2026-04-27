# -*- coding: utf-8 -*-
# ________________________________________________________________
# Robotiq 2F / Hand-E via MODBUS RTU (COMx) — helper
#    - Minimal API (connect/activate/open/close/disconnect)
#    - Emits BOTH logging.info() AND print() for each action
# ________________________________________________________________

import time
import serial
import binascii
import logging
from typing import Optional

try:
    from helpers.logging_utils import notify as _emit
except Exception:
    # fallback if helper isn’t available
    def _emit(msg: str, level: int = 20):
        print(msg, flush=True)


def _crc16_modbus(b: bytes) -> bytes:
    crc = 0xFFFF
    for ch in b:
        crc ^= ch
        for _ in range(8):
            if crc & 1:
                crc = (crc >> 1) ^ 0xA001
            else:
                crc >>= 1
    return crc.to_bytes(2, "little")


def _tx_modbus(ser: serial.Serial, payload_wo_crc: bytes, rx_len: int = 0, pause: float = 0.03) -> bytes:
    frame = payload_wo_crc + _crc16_modbus(payload_wo_crc)
    ser.write(frame)
    time.sleep(pause)
    data = ser.read(rx_len) if rx_len > 0 else ser.read(64)
    if data:
        hexstr = binascii.hexlify(data).decode()
        _emit(f"[GRIPPER][RX] {hexstr}")
    return data


# FC16 header: slave 0x09, func 0x10, start 0x03E8, count 0x0003, 6 data bytes
_FC16_HEADER = b"\x09\x10\x03\xE8\x00\x03\x06"

# Prebuilt frames (byte-for-byte as in your working handshake)
_FRAME_ACTIVATE = _FC16_HEADER + bytes([0x01, 0x01, 0x00, 0x00, 0xFF, 0x80])  # rACT=1, rGTO=1
_FRAME_CLOSE    = _FC16_HEADER + bytes([0x09, 0x00, 0x00, 0xFF, 0xFF, 0xFF])  # close fully
_FRAME_OPEN_50  = _FC16_HEADER + bytes([0x09, 0x00, 0x00, 0x40, 0xFF, 0xFF])  # open to 50%
_FRAME_READ_3REG = b"\x09\x04\x07\xD3\x00\x03"  # gSTA/gOBJ/gFLT


class RobotiqModbusRTUGripper:
    """
    Minimal MODBUS RTU client for Robotiq 2F/Hand-E.

    Usage:
        g = RobotiqModbusRTUGripper("COM4", baud=115200, timeout=0.3, dwell=0.8)
        g.connect()
        g.activate()
        g.open()
        g.close()
        g.disconnect()
    """

    def __init__(self, port: str, baud: int = 115200, timeout: float = 0.3, dwell: float = 0.8):
        self.port: str = port
        self.baud: int = baud
        self.timeout: float = timeout
        self.dwell: float = dwell
        self.ser: Optional[serial.Serial] = None

    # --- lifecycle ---
    def connect(self):
        if self.ser and self.ser.is_open:
            return
        self.ser = serial.Serial(
            self.port, self.baud, timeout=self.timeout,
            parity=serial.PARITY_NONE,
            stopbits=serial.STOPBITS_ONE,
            bytesize=serial.EIGHTBITS,
        )
        _emit(f"[GRIPPER] Opened {self.port} @ {self.baud}")

    def disconnect(self):
        if self.ser:
            try:
                self.ser.close()
            finally:
                _emit(f"[GRIPPER] Closed {self.port}")
                self.ser = None

    # --- commands ---
    def activate(self):
        assert self.ser and self.ser.is_open, "Call connect() first"
        _emit("[GRIPPER] ACTIVATE (rACT=1, rGTO=1)")
        _tx_modbus(self.ser, _FRAME_ACTIVATE)
        time.sleep(0.2)
        _emit("[GRIPPER][STATUS] Activated")

    def open(self):
        assert self.ser and self.ser.is_open, "Call connect() first"
        _emit("[GRIPPER] OPEN (50%)")
        _tx_modbus(self.ser, _FRAME_OPEN_50)
        time.sleep(self.dwell)
        _emit("[GRIPPER][STATUS] Opened")

    def close(self):
        assert self.ser and self.ser.is_open, "Call connect() first"
        _emit("[GRIPPER] CLOSE")
        _tx_modbus(self.ser, _FRAME_CLOSE)
        time.sleep(self.dwell)
        _emit("[GRIPPER][STATUS] Closed")
    
    def read_status(self):
        assert self.ser and self.ser.is_open, "Call connect() first"
        _emit("[GRIPPER] Reading status (gSTA/gOBJ/gFLT)...")
        data = _tx_modbus(self.ser, _FRAME_READ_3REG)  # emits RX hex
        return data  # optionally parse/return fields
