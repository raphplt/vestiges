extends SceneTree

var frames := 0

func _initialize():
    root.size = Vector2i(1024, 512)
    root.content_scale_mode = Window.CONTENT_SCALE_MODE_DISABLED
    root.canvas_item_default_texture_filter = Viewport.DEFAULT_CANVAS_ITEM_TEXTURE_FILTER_NEAREST
    var before := Shader.new()
    before.code = FileAccess.get_file_as_string("/tmp/vestiges-q0-r2cryh57/player_projectile-before.gdshader")
    var after := load("res://assets/shaders/player_projectile.gdshader")
    var texture := load("res://assets/vfx/projectiles/proj_note.png")
    for index in range(2):
        var sample := Sprite2D.new()
        sample.texture = texture
        sample.scale = Vector2(4, 4)
        sample.position = Vector2(256 + index * 512, 256)
        var material := ShaderMaterial.new()
        material.shader = before if index == 0 else after
        sample.material = material
        root.add_child(sample)

func _process(_delta):
    frames += 1
    if frames < 15:
        return false
    RenderingServer.force_draw()
    var frame := root.get_texture().get_image()
    frame.save_png("/tmp/vestiges-q0-r2cryh57/projectile-comparison.png")
    var before := frame.get_region(Rect2i(0, 0, 512, 512))
    var after := frame.get_region(Rect2i(512, 0, 512, 512))
    var equal := before.get_data() == after.get_data()
    var content := false
    for x in range(512):
        for y in range(512):
            if before.get_pixel(x, y) != before.get_pixel(0, 0):
                content = true
                break
        if content:
            break
    print("[ProjectileShaderComparison] RESULT equal=", equal, " content=", content, " size=", frame.get_size())
    quit(0 if equal and content else 1)
    return true
