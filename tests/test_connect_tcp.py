import asyncio
import logging
import json
import time

from server.websocket_client import UnityTcpClient

logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger("tcp_test")

async def main():
    """
    Test connection with Unity TCP server using the UnityTcpClient class.
    """
    try:
        # Create a client with the TCP URL
        client = UnityTcpClient("tcp://localhost:8080/")
        logger.info("Created TCP client instance")
        
        # Step 1: Connect to the server (includes handshake)
        logger.info("Connecting to Unity TCP server...")
        connected = await client.connect()
        
        if not connected:
            logger.error("Failed to connect to Unity TCP server")
            return
            
        logger.info("Connected to Unity TCP server successfully")
        
        # Step 2: Send a test command (get_unity_info)
        logger.info("Sending get_unity_info command...")
        try:
            result = await client.get_unity_info()
            logger.info(f"Received Unity info: {json.dumps(result, indent=2)}")
        except Exception as e:
            logger.error(f"Error sending command: {str(e)}")
            
        # Step 3: Send another command (get_logs)
        logger.info("Sending get_logs command...")
        try:
            logs = await client.get_logs(max_logs=10)
            logger.info(f"Received logs: {json.dumps(logs, indent=2)}")
        except Exception as e:
            logger.error(f"Error getting logs: {str(e)}")
            
        # Step 4: Send an execute_code command
        logger.info("Sending execute_code command...")
        try:
            code_result = await client.execute_code("Debug.Log(\"Hello from TCP test\"); return DateTime.Now.ToString();")
            logger.info(f"Code execution result: {json.dumps(code_result, indent=2)}")
        except Exception as e:
            logger.error(f"Error executing code: {str(e)}")
            
        # Finally: Disconnect
        logger.info("Disconnecting from Unity TCP server...")
        await client.disconnect()
        logger.info("Test completed successfully")
            
    except Exception as ex:
        logger.error(f"Test failed: {str(ex)}")

if __name__ == "__main__":
    asyncio.run(main())