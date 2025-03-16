using System;
using System.Collections.Generic;
using UnityEngine;

namespace YetAnotherUnityMcp.Editor.Models
{
    /// <summary>
    /// Models for MCP data exchange
    /// </summary>
    [Serializable]
    public class ExecuteCodeRequest
    {
        public string code;
    }

    [Serializable]
    public class ExecuteCodeResponse
    {
        public bool success;
        public string result;
        public string error;
    }

    [Serializable]
    public class ScreenshotRequest
    {
        public string output_path;
        public ScreenshotResolution resolution;

        [Serializable]
        public class ScreenshotResolution
        {
            public int width = 1920;
            public int height = 1080;
        }
    }

    [Serializable]
    public class ScreenshotResponse
    {
        public string result;
    }

    [Serializable]
    public class ModifyObjectRequest
    {
        public string object_id;
        public string property_name;
        public object property_value;
    }

    [Serializable]
    public class ModifyObjectResponse
    {
        public bool success;
        public string object_id;
        public string property;
        public object new_value;
    }

    [Serializable]
    public class UnityLog
    {
        public string timestamp;
        public string level;
        public string message;
        public string stacktrace;
    }

    [Serializable]
    public class UnityInfo
    {
        public string unity_version;
        public string platform;
        public string project_name;
        public bool is_playing;
        public bool is_paused;
        public bool is_editor;
        public string build_target;
    }

    [Serializable]
    public class EditorState
    {
        public string scene_name;
        public List<GameObject> selected_objects;
        public bool play_mode_active;

        [Serializable]
        public class GameObject
        {
            public string id;
            public string name;
            public string type;
        }
    }

    [Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [Serializable]
    public class QuaternionData
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public QuaternionData(Quaternion quaternion)
        {
            x = quaternion.x;
            y = quaternion.y;
            z = quaternion.z;
            w = quaternion.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }
    }

    /// <summary>
    /// Unity object property data
    /// </summary>
    [Serializable]
    public class ObjectProperty
    {
        public string name;
        public string type;
        public object value;
    }
}