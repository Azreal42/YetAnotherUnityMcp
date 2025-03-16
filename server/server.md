language : python

handlers = 
[
    "get_editor_state",
    "execute_code",
    "screen_shot_editor" output_path, resolution -> result,
    "get_logs", # void -> list[StructuredLogs]
    "get_unity_infos", # void -> { "unity_version":, etc. }
    "modify_object", # object_id, property_name, property_value -> result
    "list_object_properties", # object_id, property_path -> list[]
]