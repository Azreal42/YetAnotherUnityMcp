# YetAnotherUnityMcp - Server Module

## Overview
This module provides the Python client implementation for communicating with the Unity TCP server. It implements the Model Context Protocol (MCP) for AI integration with Unity.

## Client Architecture
The main components of the client are:

### Core Client
- `websocket_client.py`: Contains the `UnityTcpClient` class that handles the low-level TCP communication, message framing, and command execution. (Despite the file name, it uses TCP sockets, not WebSockets - the filename is kept for backward compatibility).

### Legacy Support
- `unity_socket_client.py`: Legacy wrapper that delegates to `UnityTcpClient` for backward compatibility.

## Connection Protocol
The client uses a custom framing protocol for reliable communication over TCP:

1. **Connection Handshake**:
   - Client sends `YAUM_HANDSHAKE_REQUEST`
   - Server responds with `YAUM_HANDSHAKE_RESPONSE`

2. **Message Framing**:
   ```
   [STX (0x02)] + [LENGTH: 4 bytes] + [MESSAGE: JSON] + [ETX (0x03)]
   ```

3. **Keep-Alive**:
   - Client sends `PING` messages periodically
   - Server responds with `PONG`

## Usage

```python
from server.websocket_client import UnityTcpClient

# Create client instance
client = UnityTcpClient("tcp://localhost:8080/")

# Connect to Unity TCP server
await client.connect()

# Execute a command
result = await client.execute_code("Debug.Log(\"Hello World\"); return 42;")

# Get Unity information
info = await client.get_unity_info()

# Disconnect when done
await client.disconnect()
```

## Testing
Use the `tests/test_connect_tcp.py` script to test the TCP connection.