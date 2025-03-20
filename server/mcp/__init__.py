"""MCP module for Unity integration"""

from server.mcp.tools import init_mcp as init_tools
from server.mcp.resources import init_mcp as init_resources

def init_mcp(mcp_instance):
    """Initialize MCP instance with all tools and resources"""
    init_tools(mcp_instance)
    init_resources(mcp_instance)

__all__ = ["init_mcp"]