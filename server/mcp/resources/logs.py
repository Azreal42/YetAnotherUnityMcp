"""Unity logs MCP resource implementation"""

import logging
from typing import Dict, Any, List
from server.websocket_utils import send_request
from server.async_utils import AsyncExecutor

logger = logging.getLogger("mcp_server")

def get_logs_handler(max_logs: int = 100) -> List[Dict[str, Any]]:
    """Get logs from the Unity Editor

    Args:
        max_logs: Maximum number of logs to return

    Returns:
        List of log entries
    """
    try:
        # Use our utility to run the coroutine safely across thread boundaries
        return AsyncExecutor.run_in_thread_or_loop(
            lambda: get_logs(max_logs),
            timeout=30.0
        )
    except Exception as e:
        logger.error(f"Error getting logs: {str(e)}")
        return [{"error": str(e)}]

async def get_logs(max_logs: int) -> List[Dict[str, Any]]:
    """Get logs from the Unity Editor implementation"""
    from server.mcp_server import manager, pending_requests
    
    logger.info(f"Getting logs (max: {max_logs})")
    
    try:
        if manager.active_connections:
            response: Dict[str, Any] = await send_request(
                manager, 
                "unity://logs", 
                {"max_logs": max_logs},
                pending_requests
            )
            
            if response.get("status") == "error":
                logger.error(f"Error getting Unity logs: {response.get('error')}")
            else:
                return response.get("result", [])
        
        return [{"error": "No Unity clients connected"}]
    except Exception as e:
        logger.error(f"Error getting Unity logs: {str(e)}")
        return [{"error": str(e)}]