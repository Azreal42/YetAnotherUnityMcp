"""Test script for dynamic tool registration"""

import asyncio
import logging
import sys
import json
from server.unity_websocket_client import get_client
from server.mcp.dynamic_tools import DynamicToolManager
from server.mcp.dynamic_tool_invoker import invoke_dynamic_tool
from mcp.server.fastmcp import FastMCP

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("test_dynamic_tools")

# Create FastMCP instance
mcp = FastMCP("Test Dynamic Tools", description="Test dynamic tool registration")

async def main():
    """Main test function"""
    client = get_client()
    
    # Connect to Unity
    logger.info("Connecting to Unity...")
    connected = await client.connect()
    if not connected:
        logger.error("Failed to connect to Unity")
        return
        
    logger.info("Connected to Unity")
    
    # Get Unity info
    logger.info("Getting Unity info...")
    info = await client.get_unity_info()
    logger.info(f"Unity info: {json.dumps(info, indent=2)}")
    
    # Get schema
    logger.info("Getting schema...")
    schema = await client.get_schema()
    
    # Print tools from schema
    tools = schema.get('tools', [])
    logger.info(f"Found {len(tools)} tools in schema:")
    for tool in tools:
        logger.info(f"  - {tool.get('name')}: {tool.get('description')}")
        
    # Print resources from schema
    resources = schema.get('resources', [])
    logger.info(f"Found {len(resources)} resources in schema:")
    for resource in resources:
        logger.info(f"  - {resource.get('name')} ({resource.get('urlPattern')}): {resource.get('description')}")
        
    # Create dynamic tool manager
    logger.info("Creating dynamic tool manager...")
    manager = DynamicToolManager(mcp)
    
    # Register tools from schema
    logger.info("Registering tools from schema...")
    result = await manager.register_from_schema()
    
    if result:
        logger.info(f"Successfully registered {len(manager.registered_tools)} tools and {len(manager.registered_resources)} resources")
        
        # List registered tools
        logger.info("Registered tools:")
        for name, desc in manager.registered_tools.items():
            logger.info(f"  - {name}: {desc}")
            
        # Test invoking a tool
        if "execute_code" in manager.registered_tools:
            logger.info("Testing execute_code tool...")
            code = "Debug.Log(\\\"Hello from dynamic tool\\\"); return 42;"
            result = await invoke_dynamic_tool("execute_code", {"code": code})
            logger.info(f"Result: {json.dumps(result, indent=2)}")
        else:
            logger.warning("execute_code tool not found")
            
        # Try invoking an unknown tool
        logger.info("Testing non-existent tool...")
        result = await invoke_dynamic_tool("non_existent_tool", {})
        logger.info(f"Result: {json.dumps(result, indent=2)}")
    else:
        logger.error("Failed to register tools from schema")
        
    # Disconnect
    logger.info("Disconnecting from Unity...")
    await client.disconnect()
    logger.info("Disconnected from Unity")

if __name__ == "__main__":
    # Set Windows event loop policy if needed
    if sys.platform == 'win32':
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())
        
    # Run the test
    asyncio.run(main())