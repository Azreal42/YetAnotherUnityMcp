import logging
from typing import List
from fastapi import WebSocket

logger = logging.getLogger("mcp_server")

class ConnectionManager:
    def __init__(self) -> None:
        self.active_connections: List[WebSocket] = []

    async def connect(self, websocket: WebSocket) -> None:
        try:
            await websocket.accept()
            self.active_connections.append(websocket)
            logger.info(f"WebSocket connected. Active connections: {len(self.active_connections)}")
            # Log the client details
            client = websocket.client
            logger.info(f"Client connected from: {client.host}:{client.port}")
        except Exception as e:
            logger.error(f"Error accepting WebSocket connection: {str(e)}")

    def disconnect(self, websocket: WebSocket) -> None:
        try:
            if websocket in self.active_connections:
                self.active_connections.remove(websocket)
                # Try to get client info before disconnection
                client_info = "Unknown"
                try:
                    if hasattr(websocket, 'client') and websocket.client:
                        client_info = f"{websocket.client.host}:{websocket.client.port}"
                except:
                    pass
                
                logger.info(f"WebSocket disconnected from {client_info}. Active connections: {len(self.active_connections)}")
            else:
                logger.warning("Attempted to disconnect a WebSocket that was not in active_connections")
        except Exception as e:
            logger.error(f"Error during WebSocket disconnection: {str(e)}")

    async def send_message(self, websocket: WebSocket, message: str) -> None:
        try:
            # Add timing information
            import time
            start_time = time.time()
            
            # Log message size
            logger.debug(f"Sending message of length {len(message)} to client")
            
            # Send the message
            await websocket.send_text(message)
            
            # Log the time it took to send
            elapsed = time.time() - start_time
            if elapsed > 0.5:  # Log if it took more than 500ms
                logger.warning(f"Slow message send: {elapsed:.2f}s for {len(message)} bytes")
        except Exception as e:
            logger.error(f"Error sending message: {str(e)}")

    async def broadcast(self, message: str) -> None:
        if not self.active_connections:
            logger.warning("Attempted to broadcast message but no active connections exist")
            return
            
        logger.info(f"Broadcasting message of length {len(message)} to {len(self.active_connections)} clients")
        
        failed_connections = []
        for connection in self.active_connections:
            try:
                await connection.send_text(message)
            except Exception as e:
                logger.error(f"Error broadcasting to client: {str(e)}")
                failed_connections.append(connection)
        
        # Remove any connections that failed
        for failed in failed_connections:
            if failed in self.active_connections:
                self.active_connections.remove(failed)
                logger.warning("Removed failed connection from active_connections") 