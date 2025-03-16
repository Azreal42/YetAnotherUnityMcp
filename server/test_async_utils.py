"""
Tests for the async_utils module.
"""

import asyncio
import unittest
import time
from server.async_utils import AsyncExecutor, AsyncOperation

class TestAsyncUtils(unittest.TestCase):
    """Test cases for the async_utils module."""
    
    def test_async_operation_context_manager(self):
        """Test that AsyncOperation works as a context manager."""
        with AsyncOperation("test_op", {"param1": "value1"}, timeout=1.0) as op:
            # Just testing that it doesn't raise an exception
            self.assertIsNotNone(op)
    
    def test_run_async_with_timeout_success(self):
        """Test run_async_with_timeout with a successful coroutine."""
        async def test_coro():
            await asyncio.sleep(0.1)
            return "success"
            
        async def run_test():
            result = await AsyncExecutor.run_async_with_timeout(test_coro(), 1.0)
            self.assertEqual(result, "success")
            
        # Run the test in an event loop
        asyncio.run(run_test())
    
    def test_run_async_with_timeout_timeout(self):
        """Test run_async_with_timeout with a timeout."""
        async def test_coro():
            await asyncio.sleep(0.5)
            return "success"
            
        async def run_test():
            with self.assertRaises(TimeoutError):
                await AsyncExecutor.run_async_with_timeout(test_coro(), 0.1)
            
        # Run the test in an event loop
        asyncio.run(run_test())
    
    def test_run_in_executor(self):
        """Test run_in_executor with a synchronous function."""
        def test_func():
            time.sleep(0.1)
            return "success"
            
        result = AsyncExecutor.run_in_executor(test_func, 1.0)
        self.assertEqual(result, "success")
    
    def test_run_in_executor_timeout(self):
        """Test run_in_executor with a timeout."""
        def test_func():
            time.sleep(0.5)
            return "success"
            
        with self.assertRaises(TimeoutError):
            AsyncExecutor.run_in_executor(test_func, 0.1)
    
    def test_run_coroutine_in_new_loop(self):
        """Test run_coroutine_in_new_loop."""
        async def test_coro():
            await asyncio.sleep(0.1)
            return "success"
            
        result = AsyncExecutor.run_coroutine_in_new_loop(lambda: test_coro(), 1.0)
        self.assertEqual(result, "success")
    
    def test_run_in_thread_or_loop(self):
        """Test run_in_thread_or_loop."""
        async def test_coro():
            await asyncio.sleep(0.1)
            return "success"
            
        result = AsyncExecutor.run_in_thread_or_loop(lambda: test_coro(), 1.0)
        self.assertEqual(result, "success")
        
if __name__ == "__main__":
    unittest.main()