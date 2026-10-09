"""
The /plan endpoint of pddl_planning_service.

ENHSP itself is never run: subprocess.run is replaced with a stub, and all
files live under pytest's tmp_path.
"""
import subprocess

import pytest

import pddl_planning_service as svc
from pddl_planning_service import app


@pytest.fixture
def client():
    app.config["TESTING"] = True
    with app.test_client() as c:
        yield c


@pytest.fixture
def pddl_files(tmp_path):
    domain = tmp_path / "domain.pddl"
    problem = tmp_path / "problem.pddl"
    jar = tmp_path / "enhsp.jar"
    domain.write_text("(define (domain d))")
    problem.write_text("(define (problem p) (:domain d))")
    jar.write_text("not a real jar")
    return {"domainFile": str(domain), "problemFile": str(problem), "plannerPath": str(jar)}


@pytest.fixture
def fake_run(monkeypatch):
    """Replace subprocess.run; tests set .result / .raises and read .calls."""
    class Stub:
        calls = []
        result = subprocess.CompletedProcess([], 0, stdout="0.0: (pickuphl e1 p1 r1 g1)\n", stderr="")
        raises = None

        def __call__(self, cmd, **kwargs):
            self.calls.append((cmd, kwargs))
            if self.raises:
                raise self.raises
            return self.result

    stub = Stub()
    stub.calls = []
    monkeypatch.setattr(svc.subprocess, "run", stub)
    return stub


def test_health_reports_supported_planners_and_paths(client):
    body = client.get("/health").get_json()

    assert body["status"] == "healthy"
    assert set(body["supported_planners"]) == {"ENHSP", "FF", "LAMA-FIRST"}
    assert body["default_planner"] == "ENHSP"
    for key in ("enhsp_available", "domain_file_available", "problem_file_available"):
        assert isinstance(body[key], bool)


def test_valid_domain_and_problem_return_the_raw_planner_output(client, pddl_files, fake_run):
    response = client.post("/plan", json={**pddl_files, "plannerName": "Enhsp"})

    assert response.status_code == 200
    body = response.get_json()
    assert body["success"] is True
    assert body["plan"] == "0.0: (pickuphl e1 p1 r1 g1)\n"
    assert body["plannerUsed"] == "ENHSP"
    assert body["planningTimeSeconds"] >= 0


def test_enhsp_is_invoked_with_the_given_files_and_default_search_config(client, pddl_files, fake_run):
    client.post("/plan", json=pddl_files)

    cmd, kwargs = fake_run.calls[0]
    assert cmd[:3] == ["java", "-jar", pddl_files["plannerPath"]]
    assert cmd[cmd.index("-o") + 1] == pddl_files["domainFile"]
    assert cmd[cmd.index("-f") + 1] == pddl_files["problemFile"]
    assert cmd[cmd.index("-planner") + 1] == "pt-blind"
    assert kwargs["timeout"] == svc.DEFAULT_TIMEOUT_SECONDS


def test_enhsp_config_and_timeout_from_the_request_are_passed_through(client, pddl_files, fake_run):
    client.post("/plan", json={**pddl_files, "enhspConfig": "opt-hmax", "timeoutSeconds": 7})

    cmd, kwargs = fake_run.calls[0]
    assert cmd[cmd.index("-planner") + 1] == "opt-hmax"
    assert kwargs["timeout"] == 7


def test_planner_rejection_is_a_200_with_success_false_and_the_planner_stderr(client, pddl_files, fake_run):
    fake_run.result = subprocess.CompletedProcess([], 1, stdout="", stderr="Problem unsolvable")

    response = client.post("/plan", json=pddl_files)

    assert response.status_code == 200
    body = response.get_json()
    assert body["success"] is False
    assert "Problem unsolvable" in body["error"]
    assert isinstance(body["error"], str)  # C# PlanningResult.Error is a string


def test_malformed_pddl_surfaces_the_parser_error_without_a_stack_trace(client, pddl_files, fake_run):
    fake_run.result = subprocess.CompletedProcess([], 255, stdout="", stderr="Parsing error: unexpected token ')' at line 1")

    body = client.post("/plan", json=pddl_files).get_json()

    assert body["success"] is False
    assert "Parsing error" in body["error"]
    assert "Traceback" not in body["error"]


def test_enhsp_timeout_is_reported_as_a_failed_plan_not_a_server_error(client, pddl_files, fake_run):
    fake_run.raises = subprocess.TimeoutExpired(cmd="java", timeout=1)

    response = client.post("/plan", json=pddl_files)

    assert response.status_code == 200
    body = response.get_json()
    assert body["success"] is False
    assert "timed out" in body["error"]


def test_planner_timeout_text_is_not_treated_as_a_communication_error_by_the_engine(client, pddl_files, fake_run):
    """
    The engine's ServicePlanning.IsCommunicationError only retries specific
    messages; this service's own timeout text must not match them, so a
    planner timeout stays a permanent failure.
    """
    fake_run.raises = subprocess.TimeoutExpired(cmd="java", timeout=1)
    error = client.post("/plan", json=pddl_files).get_json()["error"]

    for retried_marker in ("Failed to communicate with planning service",
                           "Planning request timed out",
                           "An error occurred while sending the request"):
        assert retried_marker not in error


@pytest.mark.parametrize("missing, code", [("domainFile", "DOMAIN_FILE_NOT_FOUND"), ("problemFile", "PROBLEM_FILE_NOT_FOUND")])
def test_missing_input_files_give_a_500_with_a_structured_error(client, pddl_files, fake_run, tmp_path, missing, code):
    pddl_files[missing] = str(tmp_path / "does-not-exist.pddl")

    response = client.post("/plan", json=pddl_files)

    assert response.status_code == 500
    assert response.get_json()["error"]["code"] == code
    assert fake_run.calls == []


def test_error_shape_differs_between_failure_kinds(client, pddl_files, fake_run, tmp_path):
    """
    Known issue: `error` is a string for ENHSP failures (HTTP 200) but an object
    for validation failures (4xx/5xx). The C# client only copes because it
    treats non-2xx bodies as opaque text.
    """
    fake_run.result = subprocess.CompletedProcess([], 1, stdout="", stderr="boom")
    planner_failure = client.post("/plan", json=pddl_files).get_json()
    pddl_files["domainFile"] = str(tmp_path / "nope.pddl")
    validation_failure = client.post("/plan", json=pddl_files).get_json()

    assert isinstance(planner_failure["error"], str)
    assert isinstance(validation_failure["error"], dict)


def test_missing_enhsp_jar_is_reported_before_any_subprocess_runs(client, pddl_files, fake_run, tmp_path):
    pddl_files["plannerPath"] = str(tmp_path / "missing.jar")

    response = client.post("/plan", json=pddl_files)

    assert response.status_code == 500
    assert response.get_json()["error"]["code"] == "ENHSP_NOT_FOUND"
    assert fake_run.calls == []


def test_unsupported_planner_is_a_400(client, pddl_files, fake_run):
    response = client.post("/plan", json={**pddl_files, "plannerName": "MadeUpPlanner"})

    assert response.status_code == 400
    assert response.get_json()["error"]["code"] == "UNSUPPORTED_PLANNER"


def test_unsupported_planning_type_is_a_400(client, pddl_files, fake_run):
    response = client.post("/plan", json={**pddl_files, "planningType": "HTN"})

    assert response.status_code == 400
    assert response.get_json()["error"]["code"] == "UNSUPPORTED_PLANNING_TYPE"


def test_inline_file_content_overrides_the_file_on_disk(client, pddl_files, fake_run, tmp_path):
    response = client.post("/plan", json={**pddl_files, "problemFileContent": "(define (problem from-request))"})

    assert response.status_code == 200
    assert (tmp_path / "problem.pddl").read_text() == "(define (problem from-request))"


def test_non_json_body_is_answered_with_a_json_error(client):
    response = client.post("/plan", data="not json", content_type="text/plain")

    assert response.get_json() is not None
    assert response.get_json()["success"] is False


# ── Known security issues ────────────────────────────────────────────────────

def test_inline_content_can_be_written_to_any_absolute_path(client, fake_run, tmp_path):
    """
    /plan writes the inline file contents to whatever domainFile/problemFile
    path it is given; only "Plannerinputs/..." paths are anchored. Any client
    can therefore write arbitrary files as the service user.
    """
    target = tmp_path / "some" / "other" / "place" / "evil.txt"
    jar = tmp_path / "enhsp.jar"
    jar.write_text("x")

    client.post("/plan", json={
        "domainFile": str(target),
        "domainFileContent": "attacker-controlled content",
        "problemFile": str(target),
        "plannerPath": str(jar),
    })

    assert target.read_text() == "attacker-controlled content"


def test_service_is_started_on_all_interfaces_with_the_debugger_enabled():
    """
    The service runs with host='0.0.0.0' and debug=True: unauthenticated and
    network-reachable, with the Werkzeug debugger (remote code execution) on.
    Checked in the source because __main__ only runs when executed directly.
    """
    import re
    from pathlib import Path

    source = (Path(svc.__file__)).read_text()
    match = re.search(r"app\.run\((?P<args>[^)]*)\)", source)

    assert match, "app.run(...) call not found"
    assert "host='0.0.0.0'" in match.group("args")
    assert "debug=True" in match.group("args")
