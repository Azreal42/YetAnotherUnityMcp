"""Test thread-local context handling for resources"""

import asyncio
import threading
import logging
import pytest
import sys
import sys
from typing import Dict, Any
from unittest.mock import AsyncMock, MagicMock, patch

from server.dynamic_tools import ResourceContext, DynamicToolManager
from mcp.server.fastmcp import Context

# Configure logging
logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger("test_resource_context")

# Test context data
class MockContext:
    def __init__(self, context_id: str = "default"):
        self.context_id = context_id
        self.logs = []
        
    def info(self, message: str) -> None:
        self.logs.append(f"INFO: {message}")
        
    def error(self, message: str) -> None:
        self.logs.append(f"ERROR: {message}")
        
    def debug(self, message: str) -> None:
        self.logs.append(f"DEBUG: {message}")
        
    def get_logs(self) -> list:
        return self.logs

class TestResourceContext:
    """Tests for ResourceContext thread-local storage"""
    
    def test_single_thread_context(self):
        """Test context handling in a single thread"""
        # Create contexts
        ctx1 = MockContext("context1")
        ctx2 = MockContext("context2")
        
        # Check initial state
        assert ResourceContext.get_current_ctx() is None
        
        # Set and get context
        ResourceContext.set_current_ctx(ctx1)
        assert ResourceContext.get_current_ctx() is ctx1
        assert ResourceContext.get_current_ctx().context_id == "context1"
        
        # Change context
        ResourceContext.set_current_ctx(ctx2)
        assert ResourceContext.get_current_ctx() is ctx2
        assert ResourceContext.get_current_ctx().context_id == "context2"
        
        # Clear context
        ResourceContext.set_current_ctx(None)
        assert ResourceContext.get_current_ctx() is None
    
    def test_context_manager(self):
        """Test context manager for ResourceContext"""
        ctx1 = MockContext("context1")
        ctx2 = MockContext("context2")
        
        # Initial state
        assert ResourceContext.get_current_ctx() is None
        
        # Use context manager
        with ResourceContext.with_context(ctx1):
            assert ResourceContext.get_current_ctx() is ctx1
            
            # Nested context
            with ResourceContext.with_context(ctx2):
                assert ResourceContext.get_current_ctx() is ctx2
                
            # Verify outer context restored
            assert ResourceContext.get_current_ctx() is ctx1
            
        # Verify context cleared after exit
        assert ResourceContext.get_current_ctx() is None
        
        # Test with exception
        try:
            with ResourceContext.with_context(ctx1):
                assert ResourceContext.get_current_ctx() is ctx1
                raise ValueError("Test exception")
        except ValueError:
            pass
            
        # Verify context cleared after exception
        assert ResourceContext.get_current_ctx() is None
    
    def test_multi_thread_isolation(self):
        """Test that contexts are isolated between threads"""
        ctx1 = MockContext("thread1")
        ctx2 = MockContext("thread2")
        
        results = {}
        threads_ready = threading.Event()
        threads_finished = threading.Event()
        thread_count = 2
        
        def thread_func(thread_id, ctx):
            # Save our id -> context mapping
            results[thread_id] = {
                "ctx": ctx,
                "seen_contexts": []
            }
            
            # Set context for this thread
            ResourceContext.set_current_ctx(ctx)
            
            # Report initial context
            results[thread_id]["seen_contexts"].append(
                ResourceContext.get_current_ctx().context_id if ResourceContext.get_current_ctx() else None
            )
            
            # Signal ready and wait for all threads
            threads_ready.set()
            threads_ready.wait()
            
            # Sleep a bit to allow threads to interleave
            import time
            time.sleep(0.01)
            
            # Check context again
            results[thread_id]["seen_contexts"].append(
                ResourceContext.get_current_ctx().context_id if ResourceContext.get_current_ctx() else None
            )
            
            # Signal done and wait for all threads
            threads_finished.set()
            threads_finished.wait()
            
            # Final context check
            results[thread_id]["seen_contexts"].append(
                ResourceContext.get_current_ctx().context_id if ResourceContext.get_current_ctx() else None
            )
        
        # Start threads
        t1 = threading.Thread(target=thread_func, args=(1, ctx1))
        t2 = threading.Thread(target=thread_func, args=(2, ctx2))
        
        t1.start()
        t2.start()
        
        # Wait for threads to finish
        t1.join()
        t2.join()
        
        # Verify thread isolation
        assert results[1]["seen_contexts"][0] == "thread1"
        assert results[1]["seen_contexts"][1] == "thread1"
        assert results[1]["seen_contexts"][2] == "thread1"
        
        assert results[2]["seen_contexts"][0] == "thread2"
        assert results[2]["seen_contexts"][1] == "thread2"
        assert results[2]["seen_contexts"][2] == "thread2"

@pytest.mark.asyncio
async def test_async_context():
    """Test context handling in asynchronous code"""
    ctx1 = MockContext("async1")
    ctx2 = MockContext("async2")
    
    results = {}
    
    async def task1():
        with ResourceContext.with_context(ctx1):
            results["task1_initial"] = ResourceContext.get_current_ctx().context_id
            await asyncio.sleep(0.01)
            results["task1_after_sleep"] = ResourceContext.get_current_ctx().context_id
    
    async def task2():
        with ResourceContext.with_context(ctx2):
            results["task2_initial"] = ResourceContext.get_current_ctx().context_id
            await asyncio.sleep(0.01)
            results["task2_after_sleep"] = ResourceContext.get_current_ctx().context_id
    
    # Run tasks concurrently
    await asyncio.gather(task1(), task2())
    
    # Verify context isolation between tasks
    assert results["task1_initial"] == "async1"
    assert results["task1_after_sleep"] == "async1"
    assert results["task2_initial"] == "async2"
    assert results["task2_after_sleep"] == "async2"
    
    # Verify no lingering context
    assert ResourceContext.get_current_ctx() is None

@pytest.mark.asyncio
async def test_resource_wrapper():
    """Test the resource wrapper function that handles context passing"""
    # Mock necessary components
    mock_client = AsyncMock()
    mock_client.send_command = AsyncMock(return_value={"result": "success"})
    
    mock_fastmcp = MagicMock()
    mock_fastmcp.resource = lambda url_pattern, description="": lambda func: func
    
    # Create a DynamicToolManager with mocked dependencies
    with patch('server.dynamic_tools.get_client', return_value=mock_client):
        manager = DynamicToolManager(mock_fastmcp)
        
        # Manually create the function and wrapper components
        
        # The resource implementation
        async def test_resource(param1, param2):
            # This function should be called with just the parameters, not ctx
            ctx = ResourceContext.get_current_ctx()
            if ctx:
                ctx.info(f"Resource called with {param1}, {param2}")
            
            return {
                "param1": param1,
                "param2": param2,
                "has_context": ctx is not None
            }
        
        # The wrapper function
        async def resource_wrapper(ctx, *args, **kwargs):
            # Set the context, call the function, and restore
            with ResourceContext.with_context(ctx):
                return await test_resource(*args, **kwargs)
        
        # Create test context
        ctx = MockContext("test_wrapper")
        
        # Call through wrapper
        result = await resource_wrapper(ctx, "value1", "value2")
        
        # Verify context was properly passed
        assert result["param1"] == "value1"
        assert result["param2"] == "value2"
        assert result["has_context"] == True
        
        # Check logs in context
        assert len(ctx.get_logs()) > 0
        assert any("Resource called with value1, value2" in log for log in ctx.get_logs())

# Run tests if executed directly
if __name__ == "__main__":
    # Set Windows event loop policy if needed
    if sys.platform == 'win32':
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())
    
    # Run the tests
    pytest.main(["-xvs", __file__])