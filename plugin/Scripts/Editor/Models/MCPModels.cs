using System;

namespace YetAnotherUnityMcp.Editor.Models
{
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
}