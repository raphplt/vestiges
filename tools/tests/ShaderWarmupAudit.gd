extends Node

# Reproduit le placement de WarmupShaders dans un viewport réellement rendu.
# Les compteurs prouvent la soumission au rendu, pas la durée de compilation du pilote.
func _ready() -> void:
	var viewport := SubViewport.new()
	viewport.size = Vector2i(256, 256)
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	add_child(viewport)
	var container := Node2D.new()
	viewport.add_child(container)
	var source := FileAccess.get_file_as_string("res://scripts/World/GameBootstrap.cs")
	var block := source.split("private void WarmupShaders()")[1].split("private void InitializeCharacterAndRun")[0]
	var regex := RegEx.new()
	regex.compile('res://assets/shaders/[^"\\s]+\\.gdshader')
	var paths: Array[String] = []
	for match_result in regex.search_all(block):
		var path: String = match_result.get_string()
		paths.append(path)
		var sprite := Sprite2D.new()
		sprite.texture = load("res://icon.svg")
		var material := ShaderMaterial.new()
		material.shader = load(path)
		sprite.material = material
		container.add_child(sprite)
	var rows: Array = []
	for mode in ["offscreen", "visible", "visible", "offscreen"]:
		container.position = Vector2(-9999, -9999) if mode == "offscreen" else Vector2(128, 128)
		for frame in range(5):
			await RenderingServer.frame_post_draw
			rows.append({"mode": mode, "frame": frame,
				"objects": RenderingServer.viewport_get_render_info(viewport.get_viewport_rid(), RenderingServer.VIEWPORT_RENDER_INFO_TYPE_CANVAS, RenderingServer.VIEWPORT_RENDER_INFO_OBJECTS_IN_FRAME),
				"draw_calls": RenderingServer.viewport_get_render_info(viewport.get_viewport_rid(), RenderingServer.VIEWPORT_RENDER_INFO_TYPE_CANVAS, RenderingServer.VIEWPORT_RENDER_INFO_DRAW_CALLS_IN_FRAME)})
	var args := OS.get_cmdline_user_args()
	var output := args[args.find("--output") + 1]
	var file := FileAccess.open(output, FileAccess.WRITE)
	file.store_string(JSON.stringify({"renderer": RenderingServer.get_current_rendering_method(), "paths": paths, "intervals": rows}, "\t"))
	file.close()
	print("[ShaderWarmupAudit] RESULT ", output)
	get_tree().quit()
