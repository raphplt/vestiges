extends Node

# Les compteurs prouvent la soumission, pas une durée de compilation du pilote.
# --legacy-source : fichier GameBootstrap.cs de référence, mêmes modes et même renderer.
func _ready() -> void:
	var args := OS.get_cmdline_user_args()
	var legacy := args.find("--legacy-source")
	var viewport: SubViewport
	var container: Node2D
	var paths: Array[String] = []
	if legacy >= 0:
		viewport = SubViewport.new()
		viewport.size = Vector2i(256, 256)
		viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
		add_child(viewport)
		container = Node2D.new()
		viewport.add_child(container)
		var source := FileAccess.get_file_as_string(args[legacy + 1])
		var block := source.split("private void WarmupShaders()")[1].split("private void InitializeCharacterAndRun")[0]
		var regex := RegEx.new()
		regex.compile('res://assets/shaders/[^"\\s]+\\.gdshader')
		for match_result in regex.search_all(block):
			var path: String = match_result.get_string()
			paths.append(path)
			var sprite := Sprite2D.new()
			sprite.texture = load("res://icon.svg")
			var material := ShaderMaterial.new()
			material.shader = load(path)
			sprite.material = material
			sprite.position = Vector2(128, 128)
			container.add_child(sprite)
	else:
		viewport = load("res://scripts/Infrastructure/ShaderWarmup.cs").new()
		add_child(viewport)
		container = viewport.get_node("Samples")
		for item in container.get_children():
			if item.material is ShaderMaterial:
				paths.append(item.material.shader.resource_path)
	var rows: Array = []
	for mode in ["offscreen", "visible", "visible", "offscreen"]:
		container.position = Vector2(-9999, -9999) if mode == "offscreen" else Vector2.ZERO
		for frame in range(5):
			await RenderingServer.frame_post_draw
			var row := counters(viewport)
			row.merge({"mode": mode, "frame": frame})
			rows.append(row)
	# Chaque commande est également montrée seule : aucune entrée silencieusement hors champ.
	container.position = Vector2.ZERO
	for item in container.get_children():
		item.hide()
	var samples: Array = []
	for item in container.get_children():
		item.show()
		for frame in range(3):
			await RenderingServer.frame_post_draw
		var row := counters(viewport)
		row["name"] = item.name
		if item.material is ShaderMaterial:
			row["shader"] = item.material.shader.resource_path
		samples.append(row)
		item.hide()
	for item in container.get_children():
		item.show()
	for frame in range(3):
		await RenderingServer.frame_post_draw
	var output := args[args.find("--output") + 1]
	viewport.get_texture().get_image().save_png(output.get_basename() + ".png")
	viewport.queue_free()
	await get_tree().process_frame
	await get_tree().process_frame
	var valid := not is_instance_valid(viewport)
	for row in samples:
		valid = valid and row["draw_calls"] > 0
	var file := FileAccess.open(output, FileAccess.WRITE)
	file.store_string(JSON.stringify({"renderer": RenderingServer.get_current_rendering_method(), "legacy": legacy >= 0,
		"paths": paths, "intervals": rows, "samples": samples, "freed": not is_instance_valid(viewport), "valid": valid}, "\t"))
	file.close()
	print("[ShaderWarmupAudit] RESULT valid=", valid, " ", output)
	get_tree().quit(0 if valid else 1)

func counters(viewport: SubViewport) -> Dictionary:
	return {"objects": RenderingServer.viewport_get_render_info(viewport.get_viewport_rid(), RenderingServer.VIEWPORT_RENDER_INFO_TYPE_CANVAS, RenderingServer.VIEWPORT_RENDER_INFO_OBJECTS_IN_FRAME),
		"draw_calls": RenderingServer.viewport_get_render_info(viewport.get_viewport_rid(), RenderingServer.VIEWPORT_RENDER_INFO_TYPE_CANVAS, RenderingServer.VIEWPORT_RENDER_INFO_DRAW_CALLS_IN_FRAME)}
