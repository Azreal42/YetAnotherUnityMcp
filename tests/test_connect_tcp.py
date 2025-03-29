import asyncio
import logging
import json
import time
import sys

from server.resource_context import ResourceContext
from server.low_level_tcp_client import LowLevelTcpClient
from server.unity_tcp_client import UnityTcpClient
from server.connection_manager import UnityConnectionManager
from server.dynamic_tool_invoker import DynamicToolInvoker
from server.dynamic_tools import DynamicToolManager
from mcp.server.fastmcp import Context, FastMCP

# Extend UnityTcpClient to support direct LowLevelTcpClient injection for testing
class TestUnityTcpClient(UnityTcpClient):
    """UnityTcpClient subclass that supports direct LowLevelTcpClient injection for testing"""
    
    def __init__(self, low_level_client_or_url):
        """
        Initialize with either a URL string or a LowLevelTcpClient instance
        
        Args:
            low_level_client_or_url: Either a URL string or a LowLevelTcpClient instance
        """
        # Skip parent's __init__ since we're customizing it
        self.callbacks = {
            "connected": [],
            "disconnected": [],
            "error": []
        }
        self.connected = False
        
        # Check if it's a client instance or a URL string
        if isinstance(low_level_client_or_url, LowLevelTcpClient):
            self.tcp_client = low_level_client_or_url
        else:
            # It's a URL string
            self.tcp_client = LowLevelTcpClient(low_level_client_or_url)
            
        # Register TCP event handlers
        self.tcp_client.on("connected", self._on_tcp_connected)
        self.tcp_client.on("disconnected", self._on_tcp_disconnected)
        self.tcp_client.on("error", self._on_tcp_error)

# Configure more detailed logging for debugging
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger("tcp_test")

# Set TCP client logging to DEBUG
mcp_logger = logging.getLogger("mcp_client")
mcp_logger.setLevel(logging.DEBUG)

# If debug flag is passed, print all messages to stdout as hex
if len(sys.argv) > 1 and sys.argv[1] == "--trace":
    # Add a custom handler to log raw bytes
    def trace_data(data, prefix=""):
        if isinstance(data, bytes):
            hex_data = ' '.join([f'{byte:02x}' for byte in data])
            ascii_data = ''.join([chr(byte) if 32 <= byte < 127 else '.' for byte in data])
            logger.debug(f"{prefix} Hex: {hex_data}")
            logger.debug(f"{prefix} ASCII: {ascii_data}")
        elif isinstance(data, str):
            trace_data(data.encode('utf-8'), prefix)
            
    logger.debug("Trace mode enabled - will log raw message bytes")

async def main():
    """
    Test connection with Unity TCP server using the UnityTcpClient class 
    with the new dependency injection architecture.
    """
    try:
        # Create the component hierarchy with dependency injection
        tcp_url = "tcp://127.0.0.1:8080/"
        low_level_client = LowLevelTcpClient(tcp_url)
        unity_client = TestUnityTcpClient(low_level_client)  # Use our test class with direct client injection
        connection_manager = UnityConnectionManager(unity_client)
        tool_invoker = DynamicToolInvoker(connection_manager)
        
        logger.info("Created client components with dependency injection")
        
        # Step 1: Connect to the server (includes handshake)
        logger.info("Connecting to Unity TCP server...")
        # Just connect the low_level_client - our TestUnityTcpClient will use it directly
        connected = await low_level_client.connect()
        if not connected:
            logger.error("Failed to connect to Unity TCP server")
            return
        
        logger.info("Connected to Unity TCP server successfully")
        
        # Step 2: Get schema information (list all tools and resources)
        logger.info("Retrieving schema information (tools and resources)...")
        try:
            schema = await unity_client.get_schema()
            
            # Check if schema is a string and parse it
            if isinstance(schema, str):
                logger.info("Schema returned as string, parsing JSON...")
                try:
                    schema = json.loads(schema)
                except json.JSONDecodeError as e:
                    logger.error(f"Failed to parse schema JSON: {e}")
                    raise
            
            # Log tools
            logger.info(f"Found {len(schema.get('tools', []))} tools:")
            for i, tool in enumerate(schema.get('tools', []), 1):
                tool_name = tool.get('name', 'Unknown')
                tool_description = tool.get('description', 'No description')
                logger.info(f"  {i}. {tool_name}: {tool_description}")
                
            # Log resources
            logger.info(f"Found {len(schema.get('resources', []))} resources:")
            for i, resource in enumerate(schema.get('resources', []), 1):
                resource_name = resource.get('name', 'Unknown')
                resource_description = resource.get('description', 'No description')
                resource_url = resource.get('urlPattern', 'No URL pattern')
                logger.info(f"  {i}. {resource_name}: {resource_description}")
                logger.info(f"     URL Pattern: {resource_url}")
            
            # Save schema to file for inspection
            with open('schema_output.json', 'w') as f:
                json.dump(schema, f, indent=2)
            logger.info("Schema saved to schema_output.json")
            
        except Exception as e:
            logger.error(f"Error getting schema: {str(e)}")
        
        # Create a mock context for the DynamicToolInvoker
        test_ctx = Context(request_id="test_req_123", function="test_tcp_function")
        
        # Use ResourceContext to manage context for resource access
        with ResourceContext.with_context(test_ctx):
            # Step 3: Use the tool_invoker to access resources
            logger.info("Getting Unity info using tool invoker...")
            try:
                result = await tool_invoker.invoke_tool("global_unity_info", ctx=test_ctx)
                logger.info(f"Received Unity info: {json.dumps(result, indent=2)}")
            except Exception as e:
                logger.error(f"Error accessing unity_info resource: {str(e)}")
                
            # Step 4: Get logs using resource instead of direct client call
            logger.info("Getting logs using tool invoker...")
            try:
                logs = await tool_invoker.invoke_tool("global_unity_logs", {"max_logs": 10}, ctx=test_ctx)
                logger.info(f"Received logs: {json.dumps(logs, indent=2)}")
            except Exception as e:
                logger.error(f"Error accessing logs resource: {str(e)}")
                
            # Step 5: Execute code using tool invoker
            logger.info("Executing code using tool invoker...")
            try:
                code_result = await tool_invoker.invoke_tool("editor_execute_code", {
                    "code": "Debug.Log(\"Hello from TCP test\"); return DateTime.Now.ToString();"
                }, ctx=test_ctx)
                logger.info(f"Code execution result: {json.dumps(code_result, indent=2)}")
            except Exception as e:
                logger.error(f"Error executing code: {str(e)}")
        
        # Cleanup and disconnect
        ResourceContext.clear_all_contexts()
        logger.info("Disconnecting from Unity TCP server...")
        # Disconnect only the low-level client since it's the one managing the actual connection
        await low_level_client.disconnect()
        logger.info("Test completed successfully")
            
    except Exception as ex:
        logger.error(f"Test failed: {str(ex)}")

async def get_schema_only():
    """
    Simple test that just connects and retrieves the schema information
    using the new dependency injection architecture.
    """
    try:
        # Create the component hierarchy with dependency injection
        tcp_url = "tcp://127.0.0.1:8080/"
        low_level_client = LowLevelTcpClient(tcp_url)
        unity_client = TestUnityTcpClient(low_level_client)  # Use our test class with direct client injection
        connection_manager = UnityConnectionManager(unity_client)
        
        # Connect to the server
        logger.info("Connecting to Unity TCP server...")
        # Just connect the low_level_client - our TestUnityTcpClient will use it directly
        connected = await low_level_client.connect()
        if not connected:
            logger.error("Failed to connect to Unity TCP server")
            return
        
        logger.info("Connected to Unity TCP server successfully")
        
        # Create a test context
        test_ctx = Context(request_id="schema_test_req", function="get_schema_test")
        
        # Get schema information
        logger.info("Retrieving schema information (tools and resources)...")
        try:
            # Use the connection manager's execute_with_reconnect to follow the pattern
            async def get_schema_operation():
                return await unity_client.get_schema()
                
            schema = await connection_manager.execute_with_reconnect(get_schema_operation)
            
            # Check if schema is a string and parse it
            if isinstance(schema, str):
                logger.info("Schema returned as string, parsing JSON...")
                try:
                    schema = json.loads(schema)
                except json.JSONDecodeError as e:
                    logger.error(f"Failed to parse schema JSON: {e}")
                    raise
            
            # Log tools
            tools = schema.get('tools', [])
            logger.info(f"Found {len(tools)} tools:")
            for i, tool in enumerate(tools, 1):
                tool_name = tool.get('name', 'Unknown')
                tool_description = tool.get('description', 'No description')
                logger.info(f"  {i}. {tool_name}: {tool_description}")
                
                # Display input/output schema in a simplified way
                if 'inputSchema' in tool and 'properties' in tool['inputSchema']:
                    properties = tool['inputSchema']['properties']
                    logger.info(f"     Inputs: {', '.join(properties.keys())}")
                
            # Log resources
            resources = schema.get('resources', [])
            logger.info(f"Found {len(resources)} resources:")
            for i, resource in enumerate(resources, 1):
                resource_name = resource.get('name', 'Unknown')
                resource_description = resource.get('description', 'No description')
                resource_url = resource.get('urlPattern', 'No URL pattern')
                logger.info(f"  {i}. {resource_name}: {resource_description}")
                logger.info(f"     URL Pattern: {resource_url}")
            
            # Save schema to file for inspection
            with open('schema_output.json', 'w') as f:
                json.dump(schema, f, indent=2)
            logger.info("Schema saved to schema_output.json")
            
            # Use the schema to register tools and resources
            sample_fastmcp = FastMCP("Test MCP")
            tool_manager = DynamicToolManager(sample_fastmcp, connection_manager)
            
            # Just log the dynamic tool registration process without completing it
            # (since we don't want to mock a complete FastMCP instance in this test)
            logger.info("Schema retrieved successfully - in a real app, this would be used to register dynamic tools and resources")
            
        except Exception as e:
            logger.error(f"Error getting schema: {str(e)}")
        
        # Disconnect
        logger.info("Disconnecting from Unity TCP server...")
        # Disconnect only the low-level client since it's the one managing the actual connection
        await low_level_client.disconnect()
        
        # Clean up context
        ResourceContext.clear_all_contexts()
        logger.info("Test completed successfully")
            
    except Exception as ex:
        logger.error(f"Test failed: {str(ex)}")

if __name__ == "__main__":
    # Choose which test to run
    if len(sys.argv) > 1 and sys.argv[1] == "--schema":
        asyncio.run(get_schema_only())
    else:
        asyncio.run(main())