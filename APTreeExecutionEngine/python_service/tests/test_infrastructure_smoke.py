"""Checks the pytest harness can import the Flask app and hit an endpoint."""
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
