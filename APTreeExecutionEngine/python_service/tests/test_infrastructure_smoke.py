"""
Phase 0 smoke test: proves the pytest harness is wired up correctly
(imports the Flask app, runs under `pytest`, hits a real endpoint).
Real coverage per test-plan section 6 lands in later phases.
"""
import pytest

from pddl_planning_service import app


@pytest.fixture
def client():
    app.config["TESTING"] = True
    with app.test_client() as client:
        yield client


def test_health_endpoint_returns_200(client):
    response = client.get("/health")

    assert response.status_code == 200
    body = response.get_json()
    assert body["status"] == "healthy"
    assert "ENHSP" in body["supported_planners"]
