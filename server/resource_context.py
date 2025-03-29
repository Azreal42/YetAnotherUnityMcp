# Add ResourceContext class to store context in thread-local storage
from mcp.server.fastmcp import Context
from typing import Optional

import asyncio
import threading


class ResourceContext:
    """Thread-local and task-local storage for resource context"""
    _thread_local = threading.local()
    _task_contexts = {}  # Dictionary to store task_id -> context mapping

    @classmethod
    def get_current_ctx(cls) -> Optional[Context]:
        """Get the current context from thread-local or task-local storage"""
        # First try to get task-specific context if in an asyncio task
        import asyncio
        try:
            current_task = asyncio.current_task()
            if current_task and id(current_task) in cls._task_contexts:
                return cls._task_contexts[id(current_task)]
        except (RuntimeError, ImportError):
            # If we're not in an asyncio event loop or asyncio is not available
            pass

        # Fall back to thread-local storage
        return getattr(cls._thread_local, "ctx", None)

    @classmethod
    def set_current_ctx(cls, ctx: Optional[Context]) -> None:
        """Set the current context in both thread-local and task-local storage"""
        # Store in thread-local storage
        cls._thread_local.ctx = ctx

        # Also store in task-specific storage if in an asyncio task
        import asyncio
        try:
            current_task = asyncio.current_task()
            if current_task:
                if ctx is None and id(current_task) in cls._task_contexts:
                    # Remove task from contexts when setting to None
                    del cls._task_contexts[id(current_task)]
                else:
                    cls._task_contexts[id(current_task)] = ctx
        except (RuntimeError, ImportError):
            # If we're not in an asyncio event loop or asyncio is not available
            pass

    @classmethod
    def with_context(cls, ctx: Context):
        """Context manager for setting and restoring context"""
        class ContextManager:
            def __init__(self, ctx):
                self.ctx = ctx
                self.prev_ctx = None

            def __enter__(self):
                self.prev_ctx = cls.get_current_ctx()
                cls.set_current_ctx(self.ctx)
                return self.ctx

            def __exit__(self, exc_type, exc_val, exc_tb):
                cls.set_current_ctx(self.prev_ctx)

        return ContextManager(ctx)

    @classmethod
    def clear_all_contexts(cls):
        """Clear all stored contexts - useful for testing and cleanup"""
        # Clear thread-local storage
        if hasattr(cls._thread_local, "ctx"):
            delattr(cls._thread_local, "ctx")

        # Clear task contexts dictionary
        cls._task_contexts.clear()