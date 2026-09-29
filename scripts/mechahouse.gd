extends Node2D

const MechSimulationScript = preload("res://scripts/mech_simulation.gd")
const MechPlayerScript = preload("res://scripts/player.gd")

var simulation: MechSimulation
var player: MechPlayer
var font: Font
var message: String = ""
var message_color := Color("#8fe3ff")
var interaction_prompt := ""
var room_label := "COCKPIT"
var repair_target := ""
var holding_repair := false
var elapsed := 0.0

var room_rects := {
	"cockpit": Rect2(118, 170, 275, 156),
	"bedroom": Rect2(397, 170, 275, 156),
	"purifier": Rect2(676, 170, 350, 156),
	"storage": Rect2(118, 383, 275, 150),
	"generator": Rect2(397, 383, 275, 150),
	"weapon": Rect2(676, 383, 350, 150)
}

func _ready() -> void:
	font = ThemeDB.fallback_font
	simulation = MechSimulationScript.new()
	add_child(simulation)
	player = MechPlayerScript.new()
	add_child(player)
	simulation.alert_message.connect(_on_alert)
	_on_alert(simulation.last_alert, simulation.last_alert_severity)
	queue_redraw()

func _process(delta: float) -> void:
	elapsed += delta
	if not is_instance_valid(player):
		return
	var module_id := player.get_nearby_module()
	interaction_prompt = player.get_prompt(simulation)
	room_label = _room_display_name(player.current_room)
	simulation.set_resting(module_id == "bedroom" and Input.is_key_pressed(KEY_E))
	holding_repair = Input.is_key_pressed(KEY_F)
	if holding_repair and module_id in ["generator", "purifier", "weapon"]:
		repair_target = module_id
		simulation.repair_module(module_id, delta)
	else:
		repair_target = ""
	if Input.is_key_pressed(KEY_E):
		_handle_interaction(module_id)
	elif has_meta("interact_lock"):
		remove_meta("interact_lock")
	queue_redraw()

func _input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
		if event.position.y >= 155.0 and event.position.y <= 535.0:
			player.set_move_target(event.position)
		return
	if event is InputEventKey and event.pressed and not event.echo:
		match event.keycode:
			KEY_1: simulation.damage_system("generator")
			KEY_2: simulation.damage_system("purifier")
			KEY_3: simulation.damage_system("weapon")
			KEY_4: simulation.restore_all()
			KEY_5: simulation.damage_random_system()
			KEY_Q: simulation.adjust_resource("water", -10.0)
			KEY_R: simulation.adjust_resource("water", 10.0)
			KEY_Z: simulation.adjust_resource("power", -15.0)
			KEY_X: simulation.adjust_resource("power", 15.0)
			KEY_SPACE:
				if player.get_nearby_module() == "weapon":
					simulation.fire_weapon()

func _handle_interaction(module_id: String) -> void:
	if module_id == "":
		return
	if module_id == "bedroom":
		return
	# A one-shot guard prevents toggling every rendered frame while E is held.
	if not has_meta("interact_lock"):
		set_meta("interact_lock", true)
		if module_id in ["purifier", "weapon", "generator"]:
			simulation.toggle_module(module_id)
	else:
		if not Input.is_key_pressed(KEY_E):
			remove_meta("interact_lock")

func _on_alert(text: String, severity: String) -> void:
	message = text
	match severity:
		"danger": message_color = Color("#ff755d")
		"warning": message_color = Color("#ffd15c")
		"good": message_color = Color("#8df0a3")
		_: message_color = Color("#8fe3ff")
	queue_redraw()

func _room_display_name(id: String) -> String:
	match id:
		"cockpit": return "COCKPIT"
		"bedroom": return "BEDROOM"
		"purifier": return "WATER PURIFIER"
		"storage": return "STORAGE"
		"generator": return "GENERATOR"
		"weapon": return "WEAPON ROOM"
	return "MECH INTERIOR"

func _draw() -> void:
	_draw_background()
	_draw_header()
	_draw_mech()
	_draw_hud()
	_draw_debug_panel()
	_draw_player_callouts()

func _draw_background() -> void:
	draw_rect(Rect2(0, 0, 1152, 648), Color("#081321"))
	for y in range(100, 648, 32):
		draw_line(Vector2(0, y), Vector2(1152, y), Color(0.08, 0.14, 0.21, 0.24), 1.0)
	for x in range(0, 1152, 32):
		draw_line(Vector2(x, 100), Vector2(x, 648), Color(0.08, 0.14, 0.21, 0.18), 1.0)
	# distant industrial skyline
	for i in range(12):
		var x := float(i * 108 - 20)
		var h := float(22 + (i * 17) % 64)
		draw_rect(Rect2(x, 566 - h, 72, h), Color("#0d2030"))
		draw_rect(Rect2(x + 12, 580 - h, 6, 4), Color("#e69b4f"))
		draw_rect(Rect2(x + 34, 580 - h, 6, 4), Color("#e69b4f"))

func _draw_header() -> void:
	draw_string(font, Vector2(42, 51), "MECHA", HORIZONTAL_ALIGNMENT_LEFT, -1, 38, Color("#f3ead5"))
	draw_string(font, Vector2(205, 51), "HOUSE", HORIZONTAL_ALIGNMENT_LEFT, -1, 38, Color("#e35e36"))
	draw_string(font, Vector2(44, 73), "YOUR HOME WALKS WITH YOU  /  SYSTEMS TEST BUILD", HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color("#8da8bb"))
	draw_string(font, Vector2(745, 38), "STATIONARY CUTAWAY  //  MEDIUM CLASS", HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color("#8da8bb"))
	draw_line(Vector2(40, 89), Vector2(1112, 89), Color("#294a63"), 2.0)

func _draw_mech() -> void:
	# Outer shell and feet.
	draw_rect(Rect2(90, 134, 980, 423), Color("#162b3d"), true)
	draw_rect(Rect2(90, 134, 980, 423), Color("#4d7892"), false, 3.0)
	draw_rect(Rect2(101, 145, 958, 398), Color("#0b1726"), false, 2.0)
	draw_rect(Rect2(120, 541, 170, 18), Color("#213649"))
	draw_rect(Rect2(858, 541, 170, 18), Color("#213649"))
	draw_line(Vector2(235, 558), Vector2(180, 604), Color("#3e6074"), 12.0)
	draw_line(Vector2(935, 558), Vector2(990, 604), Color("#3e6074"), 12.0)
	for room_id in room_rects:
		_draw_room(room_id, room_rects[room_id])
	_draw_ladders()
	_draw_module_icons()
	if player.has_move_target():
		var target := player.get_move_target()
		draw_circle(target, 8.0, Color(0.55, 0.9, 1.0, 0.22))
		draw_arc(target, 8.0, 0.0, TAU, 16, Color("#8fe3ff"), 2.0)
	if simulation.weapon_flash > 0.0:
		draw_colored_polygon(PackedVector2Array([Vector2(1027, 421), Vector2(1110, 398), Vector2(1110, 444)]), Color(1.0, 0.82, 0.36, simulation.weapon_flash / 0.22))

func _draw_room(id: String, rect: Rect2) -> void:
	var active := player.current_room == id
	var fill := Color("#10263a") if not active else Color("#173b4c")
	var border := Color("#38657d") if not active else Color("#efb65f")
	draw_rect(rect, fill, true)
	draw_rect(rect, border, false, 2.0)
	draw_string(font, rect.position + Vector2(12, 24), _room_display_name(id), HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color("#e5ecdf"))
	if active:
		draw_rect(Rect2(rect.position + Vector2(10, 31), Vector2(92, 2)), Color("#eab55c"))
	# floor deck
	draw_line(Vector2(rect.position.x + 7, rect.end.y - 18), Vector2(rect.end.x - 7, rect.end.y - 18), Color("#6c4836"), 4.0)
	draw_line(Vector2(rect.position.x + 7, rect.end.y - 14), Vector2(rect.end.x - 7, rect.end.y - 14), Color("#d08a45"), 2.0)
	# simple room props
	match id:
		"cockpit":
			draw_rect(Rect2(rect.position + Vector2(32, 62), Vector2(84, 43)), Color("#253f52"), true)
			draw_rect(Rect2(rect.position + Vector2(44, 72), Vector2(60, 21)), Color("#7ad0d4"), true)
			draw_circle(rect.position + Vector2(198, 88), 18, Color("#d35d3c"))
		"bedroom":
			draw_rect(Rect2(rect.position + Vector2(29, 83), Vector2(125, 37)), Color("#7d4c48"), true)
			draw_rect(Rect2(rect.position + Vector2(41, 75), Vector2(38, 15)), Color("#d8b16a"), true)
			draw_circle(rect.position + Vector2(210, 84), 10, Color("#f0bd67"))
		"purifier":
			draw_rect(Rect2(rect.position + Vector2(42, 53), Vector2(108, 67)), Color("#294b5c"), true)
			draw_rect(Rect2(rect.position + Vector2(58, 68), Vector2(76, 32)), Color("#51b6d4"), true)
			draw_circle(rect.position + Vector2(235, 91), 16, Color("#53d7cb"))
		"storage":
			for i in range(3):
				draw_rect(Rect2(rect.position + Vector2(32 + i * 57, 76), Vector2(43, 40)), Color("#a7653f"), true)
				draw_line(rect.position + Vector2(36 + i * 57, 85), rect.position + Vector2(70 + i * 57, 85), Color("#e2a15b"), 2.0)
		"generator":
			draw_circle(rect.position + Vector2(108, 89), 38, Color("#465563"))
			draw_circle(rect.position + Vector2(108, 89), 25, Color("#d27a40"))
			draw_circle(rect.position + Vector2(108, 89), 9, Color("#e8ce74"))
			draw_rect(Rect2(rect.position + Vector2(169, 68), Vector2(64, 47)), Color("#244653"), true)
		"weapon":
			draw_rect(Rect2(rect.position + Vector2(38, 73), Vector2(125, 34)), Color("#374955"), true)
			draw_line(rect.position + Vector2(160, 90), rect.position + Vector2(264, 90), Color("#b9c6be"), 10.0)
			draw_line(rect.position + Vector2(252, 76), rect.position + Vector2(252, 104), Color("#b9c6be"), 6.0)
	# status chip for module rooms
	var module_id := _module_for_room(id)
	if module_id != "":
		var module: MechSimulation.ModuleState = simulation.get_module(module_id)
		var color := Color("#75e69a") if module.status_text() == "OPERATIONAL" else Color("#ff755d") if module.current_durability < 50.0 else Color("#f1ca68")
		draw_circle(Vector2(rect.end.x - 18, rect.position.y + 18), 5.0, color)
		draw_string(font, Vector2(rect.position.x + 12, rect.end.y - 30), module.status_text(), HORIZONTAL_ALIGNMENT_LEFT, -1, 11, color)

func _draw_ladders() -> void:
	for ladder_x in [260.0, 548.0, 836.0]:
		draw_line(Vector2(ladder_x - 10, 318), Vector2(ladder_x - 10, 383), Color("#c3925a"), 3.0)
		draw_line(Vector2(ladder_x + 10, 318), Vector2(ladder_x + 10, 383), Color("#c3925a"), 3.0)
		for rung_y in range(323, 383, 12):
			draw_line(Vector2(ladder_x - 10, rung_y), Vector2(ladder_x + 10, rung_y), Color("#e1bd73"), 2.0)

func _draw_module_icons() -> void:
	var generator: MechSimulation.ModuleState = simulation.get_module("generator")
	var purifier: MechSimulation.ModuleState = simulation.get_module("purifier")
	var weapon: MechSimulation.ModuleState = simulation.get_module("weapon")
	_draw_durability_bar(Vector2(423, 514), 165.0, generator)
	_draw_durability_bar(Vector2(702, 301), 245.0, purifier)
	_draw_durability_bar(Vector2(702, 514), 245.0, weapon)

func _draw_durability_bar(pos: Vector2, width: float, module: MechSimulation.ModuleState) -> void:
	draw_rect(Rect2(pos, Vector2(width, 5)), Color("#09121d"), true)
	var color := Color("#75e69a") if module.current_durability > 65.0 else Color("#f1ca68") if module.current_durability > 30.0 else Color("#ff755d")
	draw_rect(Rect2(pos, Vector2(width * module.integrity(), 5)), color, true)

func _draw_hud() -> void:
	var panel := Rect2(40, 108, 1068, 46)
	draw_rect(panel, Color("#0c1c2b"), true)
	draw_rect(panel, Color("#31526a"), false, 2.0)
	_draw_resource(Vector2(58, 126), "POWER", simulation.power, Color("#f4c956"))
	_draw_resource(Vector2(246, 126), "WATER", simulation.water, Color("#55bde6"))
	_draw_resource(Vector2(434, 126), "FOOD", simulation.food, Color("#83d15f"))
	_draw_resource(Vector2(622, 126), "HULL", simulation.hull, Color("#ee6651"))
	_draw_resource(Vector2(810, 126), "FATIGUE", simulation.fatigue, Color("#c695e9"))
	draw_string(font, Vector2(1003, 136), "DAY 01  //  " + _format_time(simulation.elapsed), HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#9bb3c2"))

func _draw_resource(pos: Vector2, label: String, value: float, color: Color) -> void:
	draw_string(font, pos, label, HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#b4c9d3"))
	draw_rect(Rect2(pos + Vector2(0, 7), Vector2(114, 9)), Color("#07121e"), true)
	draw_rect(Rect2(pos + Vector2(2, 9), Vector2(110 * clamp(value / 100.0, 0.0, 1.0), 5)), color, true)
	draw_string(font, pos + Vector2(121, 15), str(round(value)), HORIZONTAL_ALIGNMENT_LEFT, -1, 11, color)

func _draw_debug_panel() -> void:
	draw_rect(Rect2(40, 581, 1070, 44), Color("#0c1c2b"), true)
	draw_rect(Rect2(40, 581, 1070, 44), Color("#31526a"), false, 1.0)
	draw_string(font, Vector2(54, 599), "DEBUG", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#f1ca68"))
	draw_string(font, Vector2(110, 599), "1/2/3 DAMAGE  •  4 RESTORE  •  5 RANDOM FAILURE  •  Q/R WATER  •  Z/X POWER", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#8da8bb"))
	draw_string(font, Vector2(54, 616), "A/D MOVE  •  W/S CLIMB  •  E TOGGLE / REST  •  F HOLD REPAIR  •  SPACE FIRE", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#8da8bb"))

func _draw_player_callouts() -> void:
	if message != "":
		var alert_color := message_color if simulation.alert_time > 0.0 else Color("#6f8998")
		draw_rect(Rect2(378, 56, 400, 25), Color("#101d2b"), true)
		draw_rect(Rect2(378, 56, 400, 25), alert_color, false, 1.0)
		draw_string(font, Vector2(390, 73), message, HORIZONTAL_ALIGNMENT_LEFT, 376, 11, alert_color)
	var prompt_rect := Rect2(332, 548, 488, 27)
	if interaction_prompt != "":
		draw_rect(prompt_rect, Color("#173245"), true)
		draw_rect(prompt_rect, Color("#7bb7ca"), false, 1.0)
		draw_string(font, Vector2(346, 566), interaction_prompt, HORIZONTAL_ALIGNMENT_LEFT, 460, 12, Color("#dbe9e6"))
	draw_string(font, Vector2(960, 576), room_label, HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#e3ba67"))
	if repair_target != "":
		var module: MechSimulation.ModuleState = simulation.get_module(repair_target)
		draw_string(font, Vector2(420, 326), "REPAIRING  " + str(round(module.current_durability)) + "%", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color("#8df0a3"))

func _format_time(seconds: float) -> String:
	var mins := int(seconds) / 60
	var secs := int(seconds) % 60
	return "%02d:%02d" % [mins, secs]

func _module_for_room(room_id: String) -> String:
	match room_id:
		"generator": return "generator"
		"purifier": return "purifier"
		"weapon": return "weapon"
	return ""
