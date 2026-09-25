"""
Contract between the C# engine (RobotCommandRequest /
ExeAction subclasses) and robot_service.py, exercised through Flask's test
client with every hardware-touching function replaced.

SAFETY: robot_service talks to a real UR10 over the network. The autouse
fixture below replaces every hardware function it imports with one that FAILS
the test if called, so a test can never reach a robot by accident; individual
tests opt in to a recording fake for exactly the function they expect.
"""
import pytest

import robot_service as rs

# Values the C# side sends as `commandType` (ExeAction subclasses in
# src/Actions/LLActions/*.cs; /move uses MoveType.ToString().ToLower()).
CSHARP_MOVE_COMMAND_TYPES = ["movej", "movel", "movep", "movec", "planned", "plannedj", "plannedl"]
CSHARP_GRIPPER_COMMAND_TYPES = ["open_gripper", "close_gripper"]

HARDWARE_FUNCTIONS = [
    "move_to_pose", "move_to_pose_l", "move_to_pose_p", "move_to_pose_c",
    "set_digital_out_sequence", "play_program", "dashboard_command", "set_payload",
    "set_tcp", "set_tool_digital_out_open", "_send_urscript", "_run_urscript_with_done",
    "_execute_trajectory", "_get_current_pose", "_ensure_ec_running",
]


@pytest.fixture(autouse=True)
def no_hardware(monkeypatch):
    def forbidden(name):
        def _fail(*args, **kwargs):
            raise AssertionError(f"robot_service.{name} would have touched the robot / network")
        return _fail
    for name in HARDWARE_FUNCTIONS:
        monkeypatch.setattr(rs, name, forbidden(name))


@pytest.fixture
def recorder(monkeypatch):
    """Install a recording fake for one hardware function; returns the call list."""
    def install(name, return_value="ok"):
        calls = []
        def _fake(*args, **kwargs):
            calls.append((args, kwargs))
            return return_value
        monkeypatch.setattr(rs, name, _fake)
        return calls
    return install


@pytest.fixture
def client():
    rs.app.config["TESTING"] = True
    with rs.app.test_client() as c:
        yield c


def csharp_style_request(**overrides):
    """Shape RobotCommandRequest serializes to (camelCase, unset fields as null)."""
    body = {
        "endpoint": "/move", "commandType": "movej", "initialPosition": None,
        "finalPosition": "pickpos", "robotIp": "192.168.1.100", "velocity": 0.5,
        "acceleration": 1.0, "joints": None, "pose": None, "programName": None,
        "speed": 30, "payload": None, "payloadCog": None, "tcp": None,
        "endEffectorType": None, "height": None, "moveType": None,
    }
    body.update(overrides)
    return body


def test_health_identifies_the_service(client):
    body = client.get("/health").get_json()

    assert body["status"] == "ok"
    assert body["service"] == "robot_execution"


def test_every_move_command_type_the_engine_can_send_is_accepted_by_the_service():
    assert sorted(rs.SUPPORTED_MOVE_TYPES) == sorted(CSHARP_MOVE_COMMAND_TYPES)


def test_move_with_an_unsupported_command_type_is_a_400(client):
    response = client.post("/move", json=csharp_style_request(commandType="teleport"))

    assert response.status_code == 400
    assert "Unsupported commandType" in response.get_json()["error"]


def test_move_requires_a_final_position(client):
    response = client.post("/move", json=csharp_style_request(finalPosition=None, joints=[0] * 6))

    assert response.status_code == 400
    assert "finalPosition" in response.get_json()["error"]


def test_movej_requires_joints_or_pose(client):
    response = client.post("/move", json=csharp_style_request())

    assert response.status_code == 400


def test_movel_requires_a_pose(client):
    response = client.post("/move", json=csharp_style_request(commandType="movel", joints=[0] * 6))

    assert response.status_code == 400
    assert "pose" in response.get_json()["error"]


def test_movec_requires_a_via_point(client):
    response = client.post("/move", json=csharp_style_request(commandType="movec"))

    assert response.status_code == 400
    assert "initialPosition" in response.get_json()["error"]


def test_movej_with_joints_from_a_csharp_shaped_request_reaches_move_to_pose(client, recorder):
    calls = recorder("move_to_pose", return_value="moved")

    response = client.post("/move", json=csharp_style_request(joints=[0.1, 0.2, 0.3, 0.4, 0.5, 0.6]))

    assert response.status_code == 200
    assert response.get_json()["success"] is True
    (_, kwargs), = calls
    assert kwargs["robot_ip"] == "192.168.1.100"
    assert kwargs["name"] == "pickpos"
    assert kwargs["position"] == {"joints": [0.1, 0.2, 0.3, 0.4, 0.5, 0.6]}
    assert kwargs["velocity"] == 0.5


@pytest.mark.parametrize("command_type, expected", [("open_gripper", "TDO0=True"), ("close_gripper", "TDO0=False, TDO1=True")])
def test_gripper_commands_from_the_engine_are_executed(client, recorder, command_type, expected):
    calls = recorder("_run_urscript_with_done")

    response = client.post("/gripper", json=csharp_style_request(endpoint="/gripper", commandType=command_type))

    assert response.status_code == 200
    assert expected in response.get_json()["message"]
    (args, _), = calls
    assert args[0] == "192.168.1.100"
    assert "set_tool_digital_out" in args[1]


def test_gripper_command_type_is_case_insensitive(client, recorder):
    recorder("_run_urscript_with_done")

    assert client.post("/gripper", json={"commandType": "OPEN_GRIPPER"}).status_code == 200


def test_gripper_rejects_unknown_commands_without_touching_the_robot(client):
    response = client.post("/gripper", json={"commandType": "wiggle"})

    assert response.status_code == 400
    assert response.get_json()["success"] is False


def test_gripper_without_a_command_type_is_a_400(client):
    assert client.post("/gripper", json={}).status_code == 400


def test_gripper_falls_back_to_the_default_robot_ip_for_null_and_empty(client, recorder):
    calls = recorder("_run_urscript_with_done")

    client.post("/gripper", json={"commandType": "open_gripper", "robotIp": None})
    client.post("/gripper", json={"commandType": "open_gripper", "robotIp": ""})

    assert [args[0] for args, _ in calls] == [rs.DEFAULT_ROBOT_IP, rs.DEFAULT_ROBOT_IP]
