@tool
extends Node
## A reusable JSON-RPC TCP server node.
##
## Accepts one client connection at a time (the Wick MCP provider).
## Reads newline-delimited JSON-RPC requests, dispatches them to handler
## methods, and writes newline-delimited JSON-RPC responses back.
##
## Used by both the Editor plugin (port 6505) and the Runtime autoload (port 7777).
## Editor-only functionality is injected via callables from the EditorPlugin.

var _tcp_server: TCPServer
var _peer: StreamPeerTCP
var _buffer: String = ""

## Injected by the EditorPlugin to provide the edited scene root.
var get_scene_root_override: Callable = Callable()
## Injected by the EditorPlugin for run/stop commands.
var run_scene_override: Callable = Callable()
var stop_scene_override: Callable = Callable()


func start(port: int) -> void:
	_tcp_server = TCPServer.new()
	var err := _tcp_server.listen(port, "127.0.0.1")
	if err != OK:
		push_error("[Wick] Failed to listen on port %d: %s" % [port, error_string(err)])
	else:
		print("[Wick] Listening on 127.0.0.1:%d" % port)


func stop() -> void:
	if _peer:
		_peer.disconnect_from_host()
		_peer = null
	if _tcp_server:
		_tcp_server.stop()
		_tcp_server = null
	_buffer = ""


func _process(_delta: float) -> void:
	if _tcp_server == null:
		return

	if _tcp_server.is_connection_available():
		if _peer:
			_peer.disconnect_from_host()
		_peer = _tcp_server.take_connection()
		_buffer = ""
		print("[Wick] MCP client connected.")

	if _peer == null:
		return

	_peer.poll()
	var status := _peer.get_status()
	if status != StreamPeerTCP.STATUS_CONNECTED:
		if status == StreamPeerTCP.STATUS_NONE or status == StreamPeerTCP.STATUS_ERROR:
			print("[Wick] MCP client disconnected.")
			_peer = null
		return

	var available := _peer.get_available_bytes()
	if available <= 0:
		return

	var result := _peer.get_data(available)
	if result[0] != OK:
		return

	_buffer += (result[1] as PackedByteArray).get_string_from_utf8()

	while true:
		var newline_pos := _buffer.find("\n")
		if newline_pos == -1:
			break
		var line := _buffer.substr(0, newline_pos).strip_edges()
		_buffer = _buffer.substr(newline_pos + 1)
		if line.is_empty():
			continue
		_handle_request(line)


func _handle_request(json_string: String) -> void:
	var json := JSON.new()
	if json.parse(json_string) != OK:
		push_warning("[Wick] Malformed JSON: %s" % json.get_error_message())
		return

	var request: Dictionary = json.data
	var id = request.get("id")
	var method: String = request.get("method", "")
	var params: Dictionary = request.get("params", {})

	var response_result = _dispatch(method, params)

	if id != null:
		# GDScript's JSON parser converts all numbers to float (e.g. 2 -> 2.0).
		# StreamJsonRpc requires the id to be an integer, not a float.
		# Without this cast, the response would contain "id": 2.0 which breaks the C# client.
		var response := {"jsonrpc": "2.0", "id": int(id), "result": response_result}
		_send(JSON.stringify(response))


func _dispatch(method: String, params: Dictionary) -> Variant:
	match method:
		"editor_scene_tree":
			return _rpc_scene_tree(params)
		"editor_node_properties":
			return _rpc_node_properties(params)
		"editor_call_method":
			return _rpc_call_method(params)
		"editor_set_property":
			return _rpc_set_property(params)
		"editor_run_scene":
			return _rpc_run_scene(params)
		"editor_stop":
			return _rpc_stop(params)
		"editor_performance":
			return _rpc_performance(params)
		_:
			return {"error": "Unknown method: %s" % method}


func _send(json_string: String) -> void:
	if _peer == null or _peer.get_status() != StreamPeerTCP.STATUS_CONNECTED:
		return
	_peer.put_data((json_string + "\n").to_utf8_buffer())


# ---------------------------------------------------------------------------
# RPC Handlers
# ---------------------------------------------------------------------------

func _rpc_scene_tree(_params: Dictionary) -> Variant:
	var root := _get_scene_root()
	if root == null:
		return {"error": "No scene root available."}
	return _serialize_node(root)


func _rpc_node_properties(params: Dictionary) -> Variant:
	var node_path: String = params.get("nodePath", "")
	if node_path.is_empty():
		return {"error": "Missing 'nodePath' parameter."}
	var root := _get_scene_root()
	if root == null:
		return {"error": "No scene root available."}
	var node := root.get_node_or_null(NodePath(node_path))
	if node == null:
		return {"error": "Node not found: %s" % node_path}

	var props := {}
	for prop in node.get_property_list():
		var pname: String = prop["name"]
		if pname.begins_with("_"):
			continue
		var value = node.get(pname)
		if value is float or value is int or value is bool or value is String:
			props[pname] = value
		elif value is Vector2:
			props[pname] = {"x": value.x, "y": value.y}
		elif value is Vector3:
			props[pname] = {"x": value.x, "y": value.y, "z": value.z}
		elif value is Color:
			props[pname] = {"r": value.r, "g": value.g, "b": value.b, "a": value.a}
		elif value is NodePath:
			props[pname] = str(value)
	return props


func _rpc_call_method(params: Dictionary) -> Variant:
	var node_path: String = params.get("nodePath", "")
	var method_name: String = params.get("method", "")
	var args: Array = params.get("args", [])
	if node_path.is_empty() or method_name.is_empty():
		return {"error": "Missing 'nodePath' or 'method'."}
	var root := _get_scene_root()
	if root == null:
		return {"error": "No scene root available."}
	var node := root.get_node_or_null(NodePath(node_path))
	if node == null:
		return {"error": "Node not found: %s" % node_path}
	if not node.has_method(method_name):
		return {"error": "Method '%s' not found on '%s'." % [method_name, node_path]}
	var result = node.callv(method_name, args)
	return {"result": str(result)}


func _rpc_set_property(params: Dictionary) -> Variant:
	var node_path: String = params.get("nodePath", "")
	var property: String = params.get("property", "")
	var value = params.get("value")
	if node_path.is_empty() or property.is_empty():
		return {"error": "Missing 'nodePath' or 'property'."}
	var root := _get_scene_root()
	if root == null:
		return {"error": "No scene root available."}
	var node := root.get_node_or_null(NodePath(node_path))
	if node == null:
		return {"error": "Node not found: %s" % node_path}
	node.set(property, value)
	return {"success": true, "property": property, "value": str(value)}


func _rpc_run_scene(params: Dictionary) -> Variant:
	if not run_scene_override.is_valid():
		return {"error": "editor_run_scene is only available from the Editor bridge."}
	var scene_path: String = params.get("scenePath", "")
	run_scene_override.call(scene_path)
	return {"success": true, "action": "play", "scene": scene_path}


func _rpc_stop(_params: Dictionary) -> Variant:
	if not stop_scene_override.is_valid():
		return {"error": "editor_stop is only available from the Editor bridge."}
	stop_scene_override.call()
	return {"success": true, "action": "stop"}


func _rpc_performance(_params: Dictionary) -> Dictionary:
	return {
		"fps": Performance.get_monitor(Performance.TIME_FPS),
		"process_time": Performance.get_monitor(Performance.TIME_PROCESS),
		"physics_time": Performance.get_monitor(Performance.TIME_PHYSICS_PROCESS),
		"render_objects": Performance.get_monitor(Performance.RENDER_TOTAL_OBJECTS_IN_FRAME),
		"render_draw_calls": Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME),
		"memory_static": Performance.get_monitor(Performance.MEMORY_STATIC),
		"memory_static_max": Performance.get_monitor(Performance.MEMORY_STATIC_MAX),
	}


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

func _get_scene_root() -> Node:
	if get_scene_root_override.is_valid():
		return get_scene_root_override.call()
	return get_tree().current_scene


func _serialize_node(node: Node) -> Dictionary:
	var data := {
		"name": node.name,
		"type": node.get_class(),
		"path": str(node.get_path()),
	}
	var children_arr: Array[Dictionary] = []
	for child in node.get_children():
		children_arr.append(_serialize_node(child))
	if not children_arr.is_empty():
		data["children"] = children_arr
	return data
