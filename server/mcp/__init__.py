"""MCP module for Unity integration"""

import logging
from fastmcp import FastMCP
from server.mcp.tools import init_mcp as init_tools
from server.mcp.resources import init_mcp as init_resources

logger = logging.getLogger("mcp")

def init_mcp(mcp_instance: FastMCP) -> None:
    """
    Initialize MCP instance with all tools and resources.
    
    Args:
        mcp_instance: FastMCP instance
    """
    logger.info("Initializing MCP with standard tools and resources")
    
    # Initialize core tools that don't require dynamic registration
    init_core_tools(mcp_instance)
    
    # Initialize built-in resources
    init_resources(mcp_instance)
    
def init_core_tools(mcp_instance: FastMCP) -> None:
    """
    Initialize only core tools needed for bootstrapping.
    
    Args:
        mcp_instance: FastMCP instance
    """
    # Only register essential tools needed to bootstrap dynamic registration
    # For example, get_schema needs to be available to fetch the schema
    from server.mcp.tools.get_schema import register_get_schema
    register_get_schema(mcp_instance)
    
    # Other essential tools that should be available even if dynamic registration fails
    from server.mcp.tools.get_unity_info import register_get_unity_info
    register_get_unity_info(mcp_instance)

__all__ = ["init_mcp"]