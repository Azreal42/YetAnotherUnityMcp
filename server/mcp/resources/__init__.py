"""MCP resources for Unity integration"""

from fastmcp import FastMCP
from server.mcp.resources.unity_info import register_unity_info
from server.mcp.resources.unity_logs import register_unity_logs
from server.mcp.resources.unity_scene import register_unity_scene
from server.mcp.resources.unity_object import register_unity_object

def init_mcp(mcp_instance: FastMCP) -> None:
    """Register all MCP resources with the given MCP instance"""
    register_unity_info(mcp_instance)
    register_unity_logs(mcp_instance)
    register_unity_scene(mcp_instance)
    register_unity_object(mcp_instance)

__all__ = ["init_mcp"]