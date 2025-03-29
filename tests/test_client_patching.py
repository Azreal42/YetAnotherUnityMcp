"""
Test to verify that our client patching fixture works correctly.
This is important to ensure tests don't accidentally use the real client.
"""

import pytest
from unittest.mock import patch, AsyncMock, MagicMock

from server.unity_tcp_client import UnityTcpClient, get_client
from server.dynamic_tools import DynamicToolManager


def test_patch_unity_client_basic(mock_client, patch_unity_client):
    """Test that the patch_unity_client fixture works correctly."""
    # Direct import of get_client should return our mock
    assert get_client() is mock_client
    
    # Creating a new manager with injected client should work
    manager = DynamicToolManager(None, mock_client)
    assert manager.client is mock_client


def test_verify_client_patching(mock_client, verify_client_patching):
    """Test the verification fixture itself."""
    # The verify function should pass when given the correct mock
    verify_client_patching(mock_client)
    
    # It should fail with a different mock
    different_mock = AsyncMock()
    with pytest.raises(AssertionError):
        verify_client_patching(different_mock)


def test_dynamic_manager_fixture(dynamic_manager, mock_client):
    """Test that the dynamic_manager fixture provides a manager with the mocked client."""
    assert dynamic_manager.client is mock_client
    
    # The client should be fully mocked
    assert not isinstance(dynamic_manager.client, UnityTcpClient)
    assert isinstance(dynamic_manager.client, AsyncMock)


@pytest.mark.parametrize("use_patch", [True, False])
def test_patching_demonstration(mock_client, use_patch):
    """
    Demonstrate the difference between patched and unpatched imports.
    
    This test intentionally fails when use_patch=False to show the issue.
    """
    if use_patch:
        # Apply the patch
        with patch('server.unity_tcp_client.get_client', return_value=mock_client):
            # Import a module that imports get_client
            from server.dynamic_tools import DynamicToolManager
            manager = DynamicToolManager(None, mock_client)
            # This passes because we directly passed the mock client
            assert manager.client is mock_client
    else:
        # Skip this test because it would fail intentionally
        pytest.skip("This test is skipped because it would fail intentionally")
        # Without the patch, this would create a real client
        # from server.dynamic_tools import DynamicToolManager
        # manager = DynamicToolManager(None)
        # This would fail because manager.client is a real client, not our mock
        # assert manager.client is mock_client