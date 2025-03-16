"""
Unity object models for MCP integration
"""

from pydantic import BaseModel, Field
from typing import Dict, Any, List, Optional, Union


class Vector3(BaseModel):
    """Represents a 3D vector in Unity"""

    x: float = 0.0
    y: float = 0.0
    z: float = 0.0


class Quaternion(BaseModel):
    """Represents a quaternion rotation in Unity"""

    x: float = 0.0
    y: float = 0.0
    z: float = 0.0
    w: float = 1.0


class Transform(BaseModel):
    """Represents a Unity Transform component"""

    position: Vector3 = Field(default_factory=Vector3)
    rotation: Quaternion = Field(default_factory=Quaternion)
    scale: Vector3 = Field(default_factory=lambda: Vector3(x=1.0, y=1.0, z=1.0))


class UnityComponent(BaseModel):
    """Base model for Unity components"""

    id: str
    type: str
    enabled: bool = True
    properties: Dict[str, Any] = Field(default_factory=dict)


class UnityGameObject(BaseModel):
    """Represents a Unity GameObject"""

    id: str
    name: str
    active: bool = True
    tag: str = "Untagged"
    layer: int = 0
    transform: Transform = Field(default_factory=Transform)
    components: List[UnityComponent] = Field(default_factory=list)
    children: List[str] = Field(default_factory=list)  # List of child object IDs


class UnityLog(BaseModel):
    """Represents a log entry from Unity"""

    timestamp: str
    level: str
    message: str
    stacktrace: Optional[str] = None


class UnityInfo(BaseModel):
    """Information about Unity environment"""

    unity_version: str
    platform: str
    project_name: str
    is_playing: bool = False
    is_paused: bool = False
    is_editor: bool = True
    build_target: str = "StandaloneWindows64"
