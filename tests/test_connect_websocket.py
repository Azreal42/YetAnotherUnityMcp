import asyncio
import logging
import struct
import json
import time
import socket

logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger("tcp_test")

# Protocol constants
START_MARKER = 0x02  # STX (Start of Text)
END_MARKER = 0x03    # ETX (End of Text)
HANDSHAKE_REQUEST = "YAUM_HANDSHAKE_REQUEST"
HANDSHAKE_RESPONSE = "YAUM_HANDSHAKE_RESPONSE"

async def send_frame(writer, message):
    """Send a framed message to the server"""
    # Convert message to bytes
    message_bytes = message.encode('utf-8')
    
    # Create frame: STX + [LENGTH:4] + [MESSAGE] + ETX
    frame = bytearray()
    frame.append(START_MARKER)
    frame.extend(struct.pack("<I", len(message_bytes)))  # Length as 4-byte little-endian
    frame.extend(message_bytes)
    frame.append(END_MARKER)
    
    # Send the frame
    writer.write(frame)
    await writer.drain()
    
async def receive_frame(reader):
    """Receive a framed message from the server"""
    try:
        # Read until start marker (STX)
        while True:
            b = await reader.readexactly(1)
            if b[0] == START_MARKER:
                break
            
        # Read message length (4 bytes)
        length_bytes = await reader.readexactly(4)
        message_length = struct.unpack("<I", length_bytes)[0]
        
        # Sanity check for message length
        if message_length <= 0 or message_length > 10 * 1024 * 1024:  # Max 10 MB
            logger.error(f"Invalid message length: {message_length}")
            return None
        
        # Read message data
        message_bytes = await reader.readexactly(message_length)
        
        # Read end marker (ETX)
        end_marker = await reader.readexactly(1)
        if end_marker[0] != END_MARKER:
            logger.error(f"Missing end marker, got: {end_marker[0]}")
            return None
        
        # Convert to string
        return message_bytes.decode('utf-8')
        
    except asyncio.IncompleteReadError:
        # Connection closed
        return None
    except Exception as e:
        logger.error(f"Error receiving frame: {str(e)}")
        return None

async def main():
    host = "localhost"
    port = 8080
    
    try:
        logger.info(f"Connecting to Unity TCP server at {host}:{port}")
        
        # Create a TCP connection
        reader, writer = await asyncio.open_connection(host, port)

        # Step 1: Perform handshake
        logger.info("Sending handshake request")
        writer.write(HANDSHAKE_REQUEST.encode('utf-8'))
        await writer.drain()
        
        # Read handshake response
        response_bytes = await reader.read(1024)
        response = response_bytes.decode('utf-8')
        
        if response == HANDSHAKE_RESPONSE:
            logger.info("Handshake successful")
        else:
            logger.error(f"Invalid handshake response: {response}")
            writer.close()
            await writer.wait_closed()
            return
            
        # Step 2: Send a test command (get_unity_info)
        command = {
            "id": "test1",
            "command": "get_unity_info",
            "client_timestamp": int(time.time() * 1000)
        }
        
        logger.info(f"Sending command: {command}")
        await send_frame(writer, json.dumps(command))
        
        # Step 3: Receive the response
        logger.info("Waiting for response...")
        response = await receive_frame(reader)
        
        if response:
            logger.info(f"Received response: {response}")
            
            # Parse the response
            try:
                data = json.loads(response)
                if data.get("status") == "success":
                    logger.info("Command executed successfully")
                    if data.get("result"):
                        logger.info(f"Result: {data['result']}")
                else:
                    logger.error(f"Command error: {data.get('error', 'Unknown error')}")
            except json.JSONDecodeError:
                logger.error(f"Invalid JSON response: {response}")
        else:
            logger.error("No response received")
            
        # Close the connection
        writer.close()
        await writer.wait_closed()
            
    except Exception as ex:
        logger.error(f"TCP connection failed: {ex}")

if __name__ == "__main__":
    asyncio.run(main())