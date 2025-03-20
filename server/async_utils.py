"""
Utility functions and classes for handling asyncio operations.
Provides consistent patterns for running coroutines across threads and event loops.
"""

import asyncio
import concurrent.futures
import logging
import time
from typing import Any, Callable, Dict, TypeVar, Generic, Optional

logger = logging.getLogger("mcp_server")

T = TypeVar('T')
R = TypeVar('R')

class AsyncExecutor:
    """
    Utility class for executing async coroutines in a consistent way,
    particularly useful when crossing thread boundaries.
    """
    
    @staticmethod
    async def run_async_with_timeout(coro, timeout: float = 30.0) -> Any:
        """
        Run an async coroutine with a timeout.
        
        Args:
            coro: The coroutine to run
            timeout: Maximum time to wait for the coroutine to complete
            
        Returns:
            The result of the coroutine
            
        Raises:
            TimeoutError: If the coroutine doesn't complete within the timeout
        """
        try:
            return await asyncio.wait_for(coro, timeout=timeout)
        except asyncio.TimeoutError:
            logger.error(f"Coroutine timed out after {timeout} seconds")
            raise TimeoutError(f"Operation timed out after {timeout} seconds")
    
    @staticmethod
    def run_in_executor(func: Callable[[], T], timeout: float = 30.0) -> T:
        """
        Run a function in a ThreadPoolExecutor with a timeout.
        
        Args:
            func: The function to run
            timeout: Maximum time to wait for the function to complete
            
        Returns:
            The result of the function
            
        Raises:
            TimeoutError: If the function doesn't complete within the timeout
            Exception: Any exception raised by the function
        """
        with concurrent.futures.ThreadPoolExecutor() as executor:
            future = executor.submit(func)
            try:
                return future.result(timeout=timeout)
            except concurrent.futures.TimeoutError:
                logger.error(f"Function timed out after {timeout} seconds")
                raise TimeoutError(f"Operation timed out after {timeout} seconds")
    
    @staticmethod
    def run_coroutine_in_new_loop(coro_func: Callable[[], Any], timeout: float = 30.0) -> Any:
        """
        Run a coroutine function in a new event loop in the current thread.
        
        Args:
            coro_func: A function that returns a coroutine
            timeout: Maximum time to wait for the coroutine to complete
            
        Returns:
            The result of the coroutine
            
        Raises:
            TimeoutError: If the coroutine doesn't complete within the timeout
            Exception: Any exception raised by the coroutine
        """
        def run_in_new_loop():
            new_loop = asyncio.new_event_loop()
            asyncio.set_event_loop(new_loop)
            try:
                return new_loop.run_until_complete(coro_func())
            finally:
                new_loop.close()
        
        return AsyncExecutor.run_in_executor(run_in_new_loop, timeout)
    
    @staticmethod
    def run_in_thread_or_loop(coro_func: Callable[[], Any], timeout: float = 30.0) -> Any:
        """
        Safely run a coroutine in the current event loop if it exists and is running,
        otherwise create a new loop in a separate thread.
        
        This is useful when you need to ensure a coroutine runs correctly
        regardless of the current thread or loop state.
        
        Args:
            coro_func: A function that returns a coroutine
            timeout: Maximum time to wait for the coroutine to complete
            
        Returns:
            The result of the coroutine
            
        Raises:
            TimeoutError: If the coroutine doesn't complete within the timeout
            Exception: Any exception raised by the coroutine
        """
        start_time = time.time()
        logger.debug(f"Running coroutine with timeout {timeout}s")
        
        try:
            loop = asyncio.get_event_loop()
            if loop.is_running():
                # We're in an event loop that's already running
                # Create a new thread with its own event loop
                logger.debug("Event loop already running, using ThreadPoolExecutor")
                result = AsyncExecutor.run_coroutine_in_new_loop(coro_func, timeout)
            else:
                # We have an event loop but it's not running
                logger.debug("Using existing event loop")
                result = loop.run_until_complete(AsyncExecutor.run_async_with_timeout(coro_func(), timeout))
        except RuntimeError:
            # No event loop in this thread, create a new one
            logger.debug("No event loop in thread, creating new one")
            result = AsyncExecutor.run_coroutine_in_new_loop(coro_func, timeout)
        
        elapsed = time.time() - start_time
        logger.debug(f"Coroutine completed in {elapsed:.2f}s")
        
        return result

class AsyncOperation(Generic[T]):
    """
    A class to represent an asynchronous operation with timing and logging.
    
    Example:
        with AsyncOperation("get_logs", {"max_logs": 100}) as op:
            result = await op.run(get_logs_coroutine)
            return result
    """
    
    def __init__(self, operation_name: str, params: Optional[Dict[str, Any]] = None, timeout: float = 30.0):
        """
        Initialize the AsyncOperation.
        
        Args:
            operation_name: Name of the operation for logging
            params: Optional dictionary of parameters for logging
            timeout: Maximum time to wait for the operation to complete
        """
        self.operation_name = operation_name
        self.params = params or {}
        self.timeout = timeout
        self.start_time = 0.0
        
    def __enter__(self):
        """Start timing the operation."""
        self.start_time = time.time()
        param_str = ", ".join(f"{k}={v}" for k, v in self.params.items())
        logger.info(f"Starting operation {self.operation_name}({param_str})")
        return self
        
    def __exit__(self, exc_type, exc_val, exc_tb):
        """Log the completion time and any exceptions."""
        elapsed = time.time() - self.start_time
        if exc_type:
            logger.error(f"Operation {self.operation_name} failed after {elapsed:.2f}s: {exc_val}")
        else:
            if elapsed > 1.0:  # Only log slow operations
                logger.warning(f"Operation {self.operation_name} completed in {elapsed:.2f}s")
            else:
                logger.debug(f"Operation {self.operation_name} completed in {elapsed:.2f}s")
        
    async def run(self, coro) -> T:
        """
        Run the coroutine with timing and exception handling.
        
        Args:
            coro: The coroutine to run
            
        Returns:
            The result of the coroutine
            
        Raises:
            TimeoutError: If the coroutine doesn't complete within the timeout
            Exception: Any exception raised by the coroutine
        """
        try:
            return await asyncio.wait_for(coro, timeout=self.timeout)
        except asyncio.TimeoutError:
            logger.error(f"Operation {self.operation_name} timed out after {self.timeout}s")
            raise TimeoutError(f"Operation {self.operation_name} timed out after {self.timeout}s")