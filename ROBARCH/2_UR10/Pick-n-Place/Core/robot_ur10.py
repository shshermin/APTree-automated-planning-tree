# -*- coding: utf-8 -*-
# ______________________________________________________________________
# UR10 
#  UR10's IP: 169.254.130.206
# define IPv4 address 169.254.130.100 (subnet mask 255.255.255.0)
# ______________________________________________________________________
# To run this script:
# 1. adapt Ipv4 to the robot's IP: (RoboLab: 169.254.130.206)
# 2. Activate your environment (GR uses Develope powershell for VS)
# ______________________________________________________________________

from typing import List, Optional, Dict, Any

# Lazy import RTDE libs
try:
    from rtde_control import RTDEControlInterface as _RTDEControl
    from rtde_receive import RTDEReceiveInterface as _RTDEReceive
    try:
        from rtde_io import RTDEIOInterface as _RTDEIO
    except Exception:
        _RTDEIO = None
    _RTDE_OK = True
except Exception as e:
    _RTDE_OK = False
    _RTDEControl = _RTDEReceive = _RTDEIO = None

# Prefer a Robotiq factory if available; otherwise we can still run without it
try:
    from robotiq_gripper_v2 import create_gripper as _create_gripper
    _GRIPPER_OK = True
except Exception:
    _GRIPPER_OK = False
    _create_gripper = None

class UR10:
    def __init__(self, ip: str, urcap_port: int = 63352):
        self.ip = ip
        self.urcap_port = urcap_port
        self._c = None
        self._r = None
        self._io = None
        self._gripper = None
        self._connected = False

    # ----------------------- lifecycle -----------------------
    def connect(self) -> bool:
        if not _RTDE_OK:
            # simulation fallback
            self._connected = True
            print("[UR][SIM] RTDE unavailable; running in sim.")
            return True
        try:
            self._c = _RTDEControl(self.ip)
            self._r = _RTDEReceive(self.ip)
            self._io = _RTDEIO(self.ip) if _RTDEIO else None

            # Robotiq via URCap socket (preferred)
            if _GRIPPER_OK:
                g = _create_gripper()
                g.connect(self.ip, self.urcap_port)
                g.activate(wait=True)
                self._gripper = g
                print("[UR][Gripper] Robotiq connected & activated")
            self._connected = True
            print(f"[UR] Connected to {self.ip}")
            return True
        except Exception as e:
            print(f"[UR][ERROR] connect(): {e}")
            self._connected = False
            return False

    def disconnect(self) -> bool:
        try:
            if self._gripper:
                try: self._gripper.disconnect()
                except Exception: pass
                self._gripper = None
            if self._c:
                try: self._c.disconnect()
                except Exception: pass
            if self._r:
                try: self._r.disconnect()
                except Exception: pass
            if self._io:
                try: self._io.disconnect()
                except Exception: pass
            self._connected = False
            print("[UR] Disconnected")
            return True
        except Exception as e:
            print(f"[UR][ERROR] disconnect(): {e}")
            return False

    # ----------------------- status -----------------------
    def info(self) -> Dict[str, Any]:
        out = {
            "ip": self.ip,
            "connected": self._connected,
            "rtde_ok": _RTDE_OK,
            "gripper_ok": self._gripper is not None,
            "sim": not _RTDE_OK,
        }
        # add optional safety/mode reads if present in your rtde_receive version
        if _RTDE_OK and self._r:
            try: out["robot_mode"] = getattr(self._r, "getRobotMode", lambda: None)()
            except Exception: pass
            try: out["safety_mode"] = getattr(self._r, "getSafetyMode", lambda: None)()
            except Exception: pass
        return out

    def joints(self) -> Optional[List[float]]:
        if _RTDE_OK and self._r and self._connected:
            try: return self._r.getActualQ()
            except Exception as e:
                print(f"[UR][ERROR] joints(): {e}")
                return None
        return [0.0, -1.57, 1.57, 0.0, 0.0, 0.0]

    def tcp(self) -> Optional[List[float]]:
        if _RTDE_OK and self._r and self._connected:
            try: return self._r.getActualTCPPose()
            except Exception as e:
                print(f"[UR][ERROR] tcp(): {e}")
                return None
        return [0.0, 0.0, 0.5, 0.0, 3.14, 0.0]

    # ----------------------- motion -----------------------
    def move_j(self, q: List[float], speed: float, acc: float, sync: bool=False) -> bool:
        if _RTDE_OK and self._c and self._connected:
            try:
                self._c.moveJ(q, speed, acc, asynchronous=not sync)
                return True
            except Exception as e:
                print(f"[UR][ERROR] move_j(): {e}")
                return False
        print(f"[UR][SIM] moveJ {q} @ {speed}/{acc}")
        return True

    def move_l(self, p: List[float], speed: float, acc: float, sync: bool=False) -> bool:
        if _RTDE_OK and self._c and self._connected:
            try:
                self._c.moveL(p, speed, acc, asynchronous=not sync)
                return True
            except Exception as e:
                print(f"[UR][ERROR] move_l(): {e}")
                return False
        print(f"[UR][SIM] moveL {p} @ {speed}/{acc}")
        return True

    def move_j_path(self, waypoints: List[List[float]], speed: float, acc: float) -> bool:
        for q in waypoints:
            if not self.move_j(q, speed, acc, sync=True): return False
        return True

    def move_l_path(self, poses: List[List[float]], speed: float, acc: float) -> bool:
        for p in poses:
            if not self.move_l(p, speed, acc, sync=True): return False
        return True

    def stop(self) -> bool:
        if _RTDE_OK and self._c and self._connected:
            try:
                self._c.stopL(); self._c.stopJ(); self._c.stopScript()
                return True
            except Exception as e:
                print(f"[UR][ERROR] stop(): {e}")
                return False
        print("[UR][SIM] stop()")
        return True

    # ----------------------- gripper -----------------------
    def gripper_open(self) -> bool:
        if self._gripper:
            try:
                self._gripper.open()
                return True
            except Exception as e:
                print(f"[UR][ERROR] gripper_open(): {e}")
        # DO fallback (if wired), else simulate
        if _RTDE_OK and self._io and self._connected:
            try:
                self._io.setStandardDigitalOut(0, True)
                return True
            except Exception as e:
                print(f"[UR][ERROR] DO open(): {e}")
                return False
        print("[UR][SIM] gripper open")
        return True

    def gripper_close(self) -> bool:
        if self._gripper:
            try:
                self._gripper.close()
                return True
            except Exception as e:
                print(f"[UR][ERROR] gripper_close(): {e}")
        if _RTDE_OK and self._io and self._connected:
            try:
                self._io.setStandardDigitalOut(0, False)
                return True
            except Exception as e:
                print(f"[UR][ERROR] DO close(): {e}")
                return False
        print("[UR][SIM] gripper close")
        return True
