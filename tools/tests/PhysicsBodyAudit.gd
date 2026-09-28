extends Node

# Isoler le coût natif des corps sans formes, sans Main, sprites ni wrappers C# par corps.
func _ready() -> void:
	var rows: Array = []
	for trial in range(8):
		var body: bool = trial % 4 == 1 or trial % 4 == 2
		await get_tree().process_frame
		await get_tree().process_frame
		var before := Performance.get_monitor(Performance.MEMORY_STATIC)
		var nodes: Array[Node2D] = []
		for i in range(10000):
			var node: Node2D
			if body:
				var physics := StaticBody2D.new()
				physics.collision_layer = 0
				physics.collision_mask = 0
				node = physics
			else:
				node = Node2D.new()
			add_child(node)
			nodes.append(node)
		await get_tree().process_frame
		await get_tree().process_frame
		rows.append({"trial": trial, "kind": "body" if body else "node", "count": nodes.size(), "native_delta_bytes": Performance.get_monitor(Performance.MEMORY_STATIC) - before})
		for node in nodes:
			node.free()
		nodes.clear()
	var args := OS.get_cmdline_user_args()
	var file := FileAccess.open(args[args.find("--output") + 1], FileAccess.WRITE)
	file.store_string(JSON.stringify({"evidence": "reproduction isolée headless, pas de coût broadphase mesuré", "intervals": rows}, "\t"))
	file.close()
	print("[PhysicsBodyAudit] RESULT")
	get_tree().quit()
