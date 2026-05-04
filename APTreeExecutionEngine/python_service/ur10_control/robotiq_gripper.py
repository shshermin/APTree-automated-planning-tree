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

# Prebuilt frames — data bytes layout (6 bytes after FC16 header):
#   [rACT/rGTO, reserved, rPR(position), rSP(speed), rFR(force), reserved]
#   rPR: 0x00 = fully open, 0xFF = fully closed
_FRAME_ACTIVATE = _FC16_HEADER + bytes([0x01, 0x00, 0x00, 0x00, 0x00, 0x00])  # rACT=1 only
_FRAME_CLOSE    = _FC16_HEADER + bytes([0x09, 0x00, 0xFF, 0xFF, 0xFF, 0x00])  # rACT+rGTO, pos=0xFF (closed)
_FRAME_OPEN     = _FC16_HEADER + bytes([0x09, 0x00, 0x00, 0xFF, 0xFF, 0x00])  # rACT+rGTO, pos=0x00 (open)
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
        _emit("[GRIPPER] OPEN")
        _tx_modbus(self.ser, _FRAME_OPEN)
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


# ---------------------------------------------------------------------------
# TCP variant — Robotiq URCaps exposes Modbus RTU-over-TCP on port 63352
# Same frames as RTU, just sent over a plain socket instead of serial.
# ---------------------------------------------------------------------------

import socket as _socket

URCAPS_MODBUS_PORT = 63352  # Robotiq URCaps Modbus TCP listener on the robot
_tcp_tid = 0  # Modbus TCP transaction ID counter


def _tx_modbus_tcp(sock: _socket.socket, payload_wo_crc: bytes, pause: float = 0.03) -> bytes:
    """Send a Modbus TCP ADU (MBAP header + PDU, no CRC) and return the response."""
    global _tcp_tid
    _tcp_tid = (_tcp_tid + 1) & 0xFFFF
    # MBAP header: transaction_id(2) + protocol_id=0x0000(2) + length(2)
    # length = number of remaining bytes = entire payload_wo_crc (includes unit id)
    mbap = _tcp_tid.to_bytes(2, "big") + b"\x00\x00" + len(payload_wo_crc).to_bytes(2, "big")
    frame = mbap + payload_wo_crc  # no CRC for Modbus TCP
    sock.sendall(frame)
    time.sleep(pause)
    try:
        data = sock.recv(256)
    except _socket.timeout:
        data = b""
    if data:
        hexstr = binascii.hexlify(data).decode()
        _emit(f"[GRIPPER-TCP][RX] {hexstr}")
    return data


class RobotiqModbusTCPGripper:
    """
    Robotiq 2F/Hand-E via URCaps Modbus TCP (robot_ip:63352).

    The Robotiq URCaps driver on the UR controller exposes the same
    MODBUS RTU register map over a plain TCP socket on port 63352.
    No pymodbus required — we reuse the same RTU frames.

    Usage:
        g = RobotiqModbusTCPGripper("192.168.1.100")
        g.connect()
        g.activate()
        g.open()
        g.close()
        g.disconnect()
    """

    def __init__(self, robot_ip: str, tcp_port: int = URCAPS_MODBUS_PORT, timeout: float = 2.0, dwell: float = 0.8):
        self.robot_ip: str = robot_ip
        self.tcp_port: int = tcp_port
        self.timeout: float = timeout
        self.dwell: float = dwell
        self._sock: Optional[_socket.socket] = None

    @property
    def _connected(self) -> bool:
        return self._sock is not None

    def connect(self):
        if self._sock is not None:
            return
        s = _socket.socket(_socket.AF_INET, _socket.SOCK_STREAM)
        s.settimeout(self.timeout)
        s.connect((self.robot_ip, self.tcp_port))
        self._sock = s
        _emit(f"[GRIPPER-TCP] Connected to {self.robot_ip}:{self.tcp_port}")

    def disconnect(self):
        if self._sock is not None:
            try:
                self._sock.close()
            finally:
                _emit(f"[GRIPPER-TCP] Disconnected from {self.robot_ip}:{self.tcp_port}")
                self._sock = None

    def activate(self):
        assert self._sock, "Call connect() first"
        _emit("[GRIPPER-TCP] ACTIVATE")
        _tx_modbus_tcp(self._sock, _FRAME_ACTIVATE)
        time.sleep(0.2)
        _emit("[GRIPPER-TCP][STATUS] Activated")

    def open(self):
        assert self._sock, "Call connect() first"
        _emit("[GRIPPER-TCP] OPEN")
        _tx_modbus_tcp(self._sock, _FRAME_OPEN)
        time.sleep(self.dwell)
        _emit("[GRIPPER-TCP][STATUS] Opened")

    def close(self):
        assert self._sock, "Call connect() first"
        _emit("[GRIPPER-TCP] CLOSE")
        _tx_modbus_tcp(self._sock, _FRAME_CLOSE)
        time.sleep(self.dwell)
        _emit("[GRIPPER-TCP][STATUS] Closed")

    def read_status(self):
        assert self._sock, "Call connect() first"
        _emit("[GRIPPER-TCP] Reading status...")
        data = _tx_modbus_tcp(self._sock, _FRAME_READ_3REG)
        return data