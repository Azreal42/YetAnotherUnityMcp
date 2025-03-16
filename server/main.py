"""
Unity Model Context Protocol (MCP) Server Implementation
Main application entry point
"""

from fastapi import FastAPI
import uvicorn
import sys
import os

# Import the WebSocket-based MCP server
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from server.mcp_server import app as mcp_app

def start() -> None:
    """Start the WebSocket MCP server"""
    uvicorn.run("server.websocket_mcp_server:app", host="0.0.0.0", port=8000, reload=True)

if __name__ == "__main__":
    start()
