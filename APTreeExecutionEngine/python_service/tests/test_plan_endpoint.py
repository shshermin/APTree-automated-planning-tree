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
    Contract check against ServicePlanning.IsCommunicationError (C#): it only
    retries errors containing "Failed to communicate with planning service",
    "Planning request timed out" or "An error occurred while sending the
    request". This service's own timeout text is "ENHSP planning timed out"
    (wrapped as "ENHSP failed to find a plan: ENHSP planning timed out"), which
    matches none of them - so a planner-side timeout is a permanent failure in
    the engine, not a retry. Pinned here so a wording change on either side is
    noticed.
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
    Finding, pinned: `error` is a plain string when ENHSP fails (HTTP 200) but
    an object {code, message, details} for validation failures (HTTP 4xx/5xx).
    The C# side copes only because it treats every non-2xx body as opaque text
    (RestPlannerCommunicator); a client that parsed `error` uniformly would break.
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


# ── Security findings, pinned rather than fixed ──────────────────────────────

def test_finding_inline_content_can_be_written_to_any_absolute_path(client, fake_run, tmp_path):
    """
    /plan writes `domainFileContent` / `problemFileContent` to whatever
    `domainFile` / `problemFile` says. Only paths starting with "Plannerinputs/"
    are anchored to the service directory; any other path - including an
    absolute one anywhere on disk - is used as-is, and parent directories are
    created. So any client that can reach the service can create or overwrite
    arbitrary files the service user can write to (content fully controlled),
    e.g. ~/.bashrc or an authorized_keys file. Demonstrated here inside tmp_path only.
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


def test_finding_service_is_started_on_all_interfaces_with_the_debugger_enabled():
    """
    The __main__ block runs `app.run(host='0.0.0.0', port=5000, debug=True)`:
    reachable from the whole network, no authentication, with Flask's
    interactive Werkzeug debugger on (which allows remote code execution on
    the host, PIN-protected but PIN-guessable in some setups). Together with
    the file-write above this should not be exposed beyond localhost / an SSH
    tunnel as it is documented in the README. Pinned as a source check because
    the block only runs when the file is executed directly.
    """
    import re
    from pathlib import Path

    source = (Path(svc.__file__)).read_text()
    match = re.search(r"app\.run\((?P<args>[^)]*)\)", source)

    assert match, "app.run(...) call not found"
    assert "host='0.0.0.0'" in match.group("args")
    assert "debug=True" in match.group("args")
