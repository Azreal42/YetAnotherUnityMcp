"""MCP tools for Unity integration"""

from fastmcp import FastMCP
from server.mcp.tools.execute_code import register_execute_code
from server.mcp.tools.take_screenshot import register_take_screenshot
from server.mcp.tools.modify_object import register_modify_object
from server.mcp.tools.get_logs import register_get_logs
from server.mcp.tools.get_unity_info import register_get_unity_info

def init_mcp(mcp_instance: FastMCP) -> None:
    """Register all MCP tools with the given MCP instance"""
    register_execute_code(mcp_instance)
    register_take_screenshot(mcp_instance)
    register_modify_object(mcp_instance)
    register_get_logs(mcp_instance)
    register_get_unity_info(mcp_instance)

__all__ = ["init_mcp"]