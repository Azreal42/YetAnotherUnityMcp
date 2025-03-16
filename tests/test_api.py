"""
Tests for the MCP API
"""

import pytest
from fastapi.testclient import TestClient
from server.main import app


client = TestClient(app)


def test_root_endpoint():
    """Test the root endpoint returns correct status"""
    response = client.get("/")
    assert response.status_code == 200
    assert response.json() == {"status": "running", "server": "Unity MCP"}


def test_get_unity_infos():
    """Test the get_unity_infos endpoint"""
    response = client.get("/get_unity_infos")
    assert response.status_code == 200
    data = response.json()
    assert "unity_version" in data
    assert "platform" in data
    assert "project_name" in data


def test_get_logs():
    """Test the get_logs endpoint"""
    response = client.get("/get_logs")
    assert response.status_code == 200
    data = response.json()
    assert isinstance(data, list)
    if data:  # If there are any logs
        assert "timestamp" in data[0]
        assert "level" in data[0]
        assert "message" in data[0]


def test_get_editor_state():
    """Test the get_editor_state endpoint"""
    response = client.get("/api/get_editor_state")
    assert response.status_code == 200
    data = response.json()
    assert "scene_name" in data
    assert "selected_objects" in data
    assert "play_mode_active" in data


def test_execute_code():
    """Test the execute_code endpoint"""
    response = client.post(
        "/api/execute_code", json={"code": 'Debug.Log("Hello World");'}
    )
    assert response.status_code == 200
    data = response.json()
    assert "success" in data
    assert data["success"] is True


def test_screen_shot_editor():
    """Test the screen_shot_editor endpoint"""
    response = client.post(
        "/api/screen_shot_editor", json={"output_path": "/tmp/screenshot.png"}
    )
    assert response.status_code == 200
    data = response.json()
    assert "result" in data
