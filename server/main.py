"""
Unity Model Context Protocol (MCP) Server Implementation
Main application entry point
"""

from fastapi import FastAPI
import uvicorn
from typing import Dict, Any, List

from server.api.handlers import router as mcp_router

app = FastAPI(title="Unity MCP Server")

# Include MCP protocol router
app.include_router(mcp_router)


@app.get("/")
async def root() -> Dict[str, str]:
    """Root endpoint that returns basic server information"""
    return {"status": "running", "server": "Unity MCP"}


@app.get("/get_unity_infos")
async def get_unity_infos() -> Dict[str, Any]:
    """Get information about the connected Unity instance"""
    # This will be implemented to return real data from Unity
    return {
        "unity_version": "2022.3.16f1",
        "platform": "Windows",
        "project_name": "MCP Demo",
    }


@app.get("/get_logs")
async def get_logs() -> List[Dict[str, Any]]:
    """Get logs from the Unity editor"""
    # This will be implemented to return real logs from Unity
    return [
        {
            "timestamp": "2023-03-16T10:30:45",
            "level": "Info",
            "message": "Application started",
        }
    ]


def start() -> None:
    """Start the MCP server"""
    uvicorn.run("server.main:app", host="0.0.0.0", port=8000, reload=True)


if __name__ == "__main__":
    start()
