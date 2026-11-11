"""
Every kind of move is clamped to one velocity/acceleration cap
(ur10_commands.MOVE_VEL_CAP / MOVE_ACC_CAP), whatever the caller sends.

SAFETY: the URScript sender is replaced with a recorder, so nothing here
reaches a robot.
"""
import re

import pytest

import robot_service as rs  # puts ur10_control on sys.path
import ur10_commands
import ur10_control.ur10_commands as ur10_control_commands

CAP_V = ur10_commands.MOVE_VEL_CAP
CAP_A = ur10_commands.MOVE_ACC_CAP
POSE = {"pose": [0.4, 0.5, 0.2, 0.0, 3.14, 0.0]}


@pytest.fixture
def sent(monkeypatch):
    """Record the URScript each move would send instead of sending it."""
    scripts = []
    for module in (ur10_commands, ur10_control_commands):
        monkeypatch.setattr(module, "_run_urscript_with_done", lambda ip, cmd, timeout=60.0: scripts.append(cmd))
    return scripts


def speeds(script):
    """(a, v) of the single move in a URScript snippet."""
    match = re.search(r"a=([-\d.e]+), v=([-\d.e]+)", script)
    assert match, script
    return float(match.group(1)), float(match.group(2))


MOVES = {
    "movej": lambda v, a: ur10_commands.move_to_pose("ip", "p", position={"joints": [0] * 6}, velocity=v, acceleration=a),
    "movel": lambda v, a: ur10_commands.move_to_pose_l("ip", "p", position=POSE, velocity=v, acceleration=a),
    "movep": lambda v, a: ur10_commands.move_to_pose_p("ip", "p", position=POSE, velocity=v, acceleration=a),
    "movec": lambda v, a: ur10_commands.move_to_pose_c("ip", "via", "end", via_position=POSE, end_position=POSE, velocity=v, acceleration=a),
    "lift": lambda v, a: ur10_control_commands.lift_z("ip", 0.1, velocity=v, acceleration=a),
}


@pytest.mark.parametrize("move", MOVES)
def test_a_too_fast_request_is_clamped_to_the_cap(sent, move):
    MOVES[move](5.0, 5.0)

    assert speeds(sent[-1]) == (CAP_A, CAP_V)


@pytest.mark.parametrize("move", MOVES)
def test_defaults_above_the_cap_are_clamped_too(sent, move):
    MOVES[move](None, None)

    a, v = speeds(sent[-1])
    assert a <= CAP_A and v <= CAP_V


@pytest.mark.parametrize("move", MOVES)
def test_slower_requests_are_kept(sent, move):
    MOVES[move](0.1, 0.2)

    assert speeds(sent[-1]) == (0.2, 0.1)


def test_planned_moves_use_the_same_cap():
    assert (rs.PLANNED_VEL_CAP, rs.PLANNED_ACC_CAP) == (CAP_V, CAP_A)
