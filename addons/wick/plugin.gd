@tool
extends EditorPlugin
## Wick MCP Bridge — Editor Plugin
##
## Starts a JSON-RPC TCP server on port 6505 when the editor loads.

const EDITOR_PORT := 6505
const McpServer = preload("res://addons/wick/mcp_json_rpc_server.gd")

var _editor_server: Node


func _enter_tree() -> void:
	_editor_server = McpServer.new()
	_editor_server.name = "McpEditorServer"
	_editor_server.get_scene_root_override = Callable(self, "_get_edited_scene_root")
	_editor_server.run_scene_override = Callable(self, "_mcp_run_scene")
	_editor_server.stop_scene_override = Callable(self, "_mcp_stop_scene")
	add_child(_editor_server)
	_editor_server.start(EDITOR_PORT)


func _exit_tree() -> void:
	if _editor_server:
		_editor_server.stop()
		_editor_server.queue_free()
		_editor_server = null
	print("[Wick] Editor bridge stopped.")


func _get_edited_scene_root() -> Node:
	return EditorInterface.get_edited_scene_root()


func _mcp_run_scene(scene_path: String) -> void:
	if scene_path.is_empty():
		EditorInterface.play_main_scene()
	else:
		EditorInterface.play_custom_scene(scene_path)


func _mcp_stop_scene() -> void:
	EditorInterface.stop_playing_scene()
