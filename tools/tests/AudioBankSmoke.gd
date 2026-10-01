extends SceneTree

# Contrôle du chargement, du gain et de la limitation des sons réellement utilisés.
func _initialize() -> void:
    call_deferred("verify_bank")

func verify_bank() -> void:
    var manager = root.get_node("AudioManager")
    var bank = JSON.parse_string(FileAccess.get_file_as_string("res://data/audio/sounds.json"))
    var checked := 0
    for key in bank:
        var entry: Dictionary = bank[key]
        var stream = load(entry.path)
        if stream == null:
            fail("Fichier absent : " + entry.path)
            return
        if key.begins_with("mus_"):
            continue
        stop_pool(manager, "Sfx")
        manager.call("PlaySfx", key, 0.0, -2.0, 1.0)
        var player = manager.get_node("Sfx0")
        if player.stream != stream or not player.playing:
            fail("Lecture absente : " + key)
            return
        if not is_equal_approx(player.volume_db, float(entry.volume_db) - 2.0):
            fail("Gain incorrect : " + key)
            return
        if entry.min_interval_ms > 0:
            stop_pool(manager, "Sfx")
            manager.call("PlaySfx", key, 0.0, -2.0, 1.0)
            if player.playing:
                fail("Limitation absente : " + key)
                return
        stop_pool(manager, "UiSfx")
        paused = true
        manager.call("PlayUiSfx", key, 0.0, -3.0)
        var ui = manager.get_node("UiSfx0")
        if ui.stream != stream or not ui.playing or ui.process_mode != Node.PROCESS_MODE_ALWAYS:
            fail("Lecture UI en pause absente : " + key)
            return
        if not is_equal_approx(ui.volume_db, float(entry.volume_db) - 3.0):
            fail("Gain UI incorrect : " + key)
            return
        paused = false
        checked += 1
    print("[AudioBankSmoke] OK : %d effets, chargement, gains, limitation et UI en pause." % checked)
    quit(0)

func stop_pool(manager: Node, prefix: String) -> void:
    for child in manager.get_children():
        if str(child.name).begins_with(prefix):
            child.stop()

func fail(message: String) -> void:
    push_error("[AudioBankSmoke] " + message)
    quit(1)
