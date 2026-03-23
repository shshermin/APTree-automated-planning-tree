"""
Test connection to a UR10 robot via the Dashboard Server (port 29999).

No external libraries required — uses Python's built-in socket module.

Usage:
    python test_connection.py --ip 192.168.1.100
"""

import argparse
import socket
import sys

DASHBOARD_PORT = 29999


def test_connection(robot_ip: str) -> bool:
    """Connect to the Dashboard Server and query robot status."""
    print(f"Connecting to {robot_ip}:{DASHBOARD_PORT} ...")
    try:
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.settimeout(5)
        sock.connect((robot_ip, DASHBOARD_PORT))
        banner = sock.recv(1024).decode("utf-8", errors="replace").strip()
        print(f"  Banner: {banner}")
    except Exception as e:
        print(f"  Connection FAILED: {e}")
        return False

    def dashboard_cmd(cmd: str) -> str:
        sock.sendall((cmd + "\n").encode("utf-8"))
        return sock.recv(4096).decode("utf-8", errors="replace").strip()

    try:
        print(f"  Robot mode   : {dashboard_cmd('robotmode')}")
        print(f"  Safety mode  : {dashboard_cmd('safetymode')}")
        print(f"  Program state: {dashboard_cmd('programState')}")
    finally:
        sock.close()

    print("\nConnection OK.")
    return True


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Test connection to UR10")
    parser.add_argument("--ip", required=True, help="Robot IP address")
    args = parser.parse_args()

    success = test_connection(args.ip)
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()
