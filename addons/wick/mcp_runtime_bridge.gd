extends Node
## Wick Runtime Bridge — Autoload
##
## Add this as an autoload to expose the live game scene tree
## to the Wick MCP server on port 7777.

const RUNTIME_PORT := 7777
const McpServer = preload("res://addons/wick/mcp_json_rpc_server.gd")

var _runtime_server: Node


func _ready() -> void:
	_runtime_server = McpServer.new()
	_runtime_server.name = "McpRuntimeServer"
	add_child(_runtime_server)
	_runtime_server.start(RUNTIME_PORT)


func _exit_tree() -> void:
	if _runtime_server:
		_runtime_server.stop()
		_runtime_server.queue_free()
		_runtime_server = null
