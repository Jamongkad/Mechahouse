class_name MechPlayer
extends CharacterBody2D

signal interaction_changed(text: String)
signal room_changed(room_id: String)

const TOP_FLOOR := 239.0
const LOWER_FLOOR := 457.0
const LEFT_LIMIT := 126.0
const RIGHT_LIMIT := 1026.0
const LADDER_X := [260.0, 548.0, 836.0]
const NAV_POINTS := [
	Vector2(260.0, TOP_FLOOR), Vector2(548.0, TOP_FLOOR), Vector2(836.0, TOP_FLOOR),
	Vector2(260.0, LOWER_FLOOR), Vector2(548.0, LOWER_FLOOR), Vector2(836.0, LOWER_FLOOR)
]

var speed := 185.0
var active_floor := 0
var facing := 1
var current_room := "cockpit"
var interaction_text := ""
var repair_held := false
var move_target := Vector2(-1.0, -1.0)
var move_path: Array[Vector2] = []
var move_path_index := 0
var manual_climb_target_y := -1.0

func _ready() -> void:
	z_index = 20
	position = Vector2(186.0, TOP_FLOOR)

func _physics_process(delta: float) -> void:
	if _keyboard_input_active():
		_cancel_mouse_move()
		_keyboard_move(delta)
	elif move_path_index < move_path.size():
		_follow_move_path(delta)
	else:
		_keyboard_move(delta)
	_update_room()
	queue_redraw()

func _keyboard_input_active() -> bool:
	return Input.is_key_pressed(KEY_LEFT) or Input.is_key_pressed(KEY_RIGHT) or Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_S)

func _keyboard_move(delta: float) -> void:
	if manual_climb_target_y >= 0.0:
		position.y = move_toward(position.y, manual_climb_target_y, speed * delta)
		if abs(position.y - manual_climb_target_y) < 0.5:
			position.y = manual_climb_target_y
			active_floor = 0 if position.y == TOP_FLOOR else 1
			manual_climb_target_y = -1.0
		return
	var axis := 0.0
	if Input.is_key_pressed(KEY_LEFT) or Input.is_key_pressed(KEY_A):
		axis -= 1.0
	if Input.is_key_pressed(KEY_RIGHT) or Input.is_key_pressed(KEY_D):
		axis += 1.0
	axis = clamp(axis, -1.0, 1.0)
	velocity.x = axis * speed
	if axis != 0.0:
		facing = 1 if axis > 0.0 else -1
	position.x = clamp(position.x + velocity.x * delta, LEFT_LIMIT, RIGHT_LIMIT)
	position.y = TOP_FLOOR if active_floor == 0 else LOWER_FLOOR
	if _near_ladder() and (Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_S)):
		if Input.is_key_pressed(KEY_W) and active_floor == 1:
			manual_climb_target_y = TOP_FLOOR
		if Input.is_key_pressed(KEY_S) and active_floor == 0:
			manual_climb_target_y = LOWER_FLOOR

func set_move_target(world_position: Vector2) -> void:
	var destination_floor := 0 if world_position.y < (TOP_FLOOR + LOWER_FLOOR) * 0.5 else 1
	var destination := Vector2(clamp(world_position.x, LEFT_LIMIT, RIGHT_LIMIT), TOP_FLOOR if destination_floor == 0 else LOWER_FLOOR)
	move_target = destination
	move_path.clear()
	move_path_index = 0
	manual_climb_target_y = -1.0

	if destination_floor == active_floor and abs(position.y - destination.y) < 1.0:
		move_path.append(destination)
		return

	var start_node := _nearest_nav_point(active_floor, position.x)
	var destination_node := _nearest_nav_point(destination_floor, destination.x)
	var node_path := _find_nav_path(start_node, destination_node)
	for node_index in node_path:
		move_path.append(NAV_POINTS[node_index])
	move_path.append(destination)

func _follow_move_path(delta: float) -> void:
	var waypoint := move_path[move_path_index]
	var horizontal_distance := waypoint.x - position.x
	var vertical_distance := waypoint.y - position.y
	if abs(vertical_distance) > 1.0:
		position.y = move_toward(position.y, waypoint.y, speed * delta)
	else:
		position.x = move_toward(position.x, waypoint.x, speed * delta)
		if abs(horizontal_distance) > 0.5:
			facing = 1 if horizontal_distance > 0.0 else -1
	if position.distance_to(waypoint) < 1.0:
		position = waypoint
		if abs(waypoint.y - TOP_FLOOR) < 1.0:
			active_floor = 0
		elif abs(waypoint.y - LOWER_FLOOR) < 1.0:
			active_floor = 1
		move_path_index += 1
		if move_path_index >= move_path.size():
			move_path.clear()
			move_path_index = 0
			move_target = Vector2(-1.0, -1.0)

func _nearest_nav_point(floor_index: int, x_position: float) -> int:
	var closest_index := floor_index * 3
	var closest_distance := 1e20
	for index in range(NAV_POINTS.size()):
		if floori(index / 3.0) != floor_index:
			continue
		var distance: float = abs(NAV_POINTS[index].x - x_position)
		if distance < closest_distance:
			closest_distance = distance
			closest_index = index
	return closest_index

func _find_nav_path(start: int, goal: int) -> Array:
	if start == goal:
		return [start]
	var open: Array[int] = [start]
	var closed := {}
	var came_from := {}
	var g_score := {start: 0.0}
	var f_score := {start: NAV_POINTS[start].distance_to(NAV_POINTS[goal])}
	while not open.is_empty():
		var current: int = open[0]
		for candidate in open:
			if f_score.get(candidate, 1e20) < f_score.get(current, 1e20):
				current = candidate
		if current == goal:
			var result: Array[int] = [current]
			while came_from.has(current):
				current = came_from[current]
				result.push_front(current)
			return result
		open.erase(current)
		closed[current] = true
		for neighbor in _nav_neighbors(current):
			if closed.has(neighbor):
				continue
			var tentative_g: float = g_score.get(current, 1e20) + NAV_POINTS[current].distance_to(NAV_POINTS[neighbor])
			if tentative_g < g_score.get(neighbor, 1e20):
				came_from[neighbor] = current
				g_score[neighbor] = tentative_g
				f_score[neighbor] = tentative_g + NAV_POINTS[neighbor].distance_to(NAV_POINTS[goal])
				if not open.has(neighbor):
					open.append(neighbor)
	return [start, goal]

func _nav_neighbors(index: int) -> Array[int]:
	var neighbors: Array[int] = []
	var floor_index := floori(index / 3.0)
	var column := index % 3
	if column > 0:
		neighbors.append(index - 1)
	if column < 2:
		neighbors.append(index + 1)
	neighbors.append(index + 3 if floor_index == 0 else index - 3)
	return neighbors

func _cancel_mouse_move() -> void:
	move_path.clear()
	move_path_index = 0
	move_target = Vector2(-1.0, -1.0)

func has_move_target() -> bool:
	return move_target.x >= 0.0

func get_move_target() -> Vector2:
	return move_target

func _near_ladder() -> bool:
	for ladder_x in LADDER_X:
		if abs(position.x - ladder_x) < 28.0:
			return true
	return false

func _update_room() -> void:
	var previous := current_room
	if active_floor == 0:
		if position.x < 400.0:
			current_room = "cockpit"
		elif position.x < 695.0:
			current_room = "bedroom" if position.x < 548.0 else "purifier"
		else:
			current_room = "purifier"
	else:
		if position.x < 400.0:
			current_room = "storage"
		elif position.x < 695.0:
			current_room = "generator"
		else:
			current_room = "weapon"
	if previous != current_room:
		room_changed.emit(current_room)

func get_nearby_module() -> String:
	if active_floor == 0:
		if position.x >= 400.0 and position.x < 548.0:
			return "bedroom"
		if position.x >= 548.0:
			return "purifier"
	else:
		if position.x >= 400.0 and position.x < 695.0:
			return "generator"
		if position.x >= 695.0:
			return "weapon"
	return ""

func get_prompt(simulation: MechSimulation) -> String:
	var module_id := get_nearby_module()
	if module_id == "":
		if _near_ladder():
			return "W / S  CLIMB LADDER"
		return ""
	var module: MechSimulation.ModuleState = simulation.get_module(module_id)
	if module_id == "bedroom":
		return "E  REST  •  FATIGUE " + str(round(simulation.fatigue)) + "%"
	if module_id == "weapon":
		return "E  TOGGLE WEAPON  •  SPACE  FIRE"
	if module.current_durability < module.definition.max_durability:
		return "F  HOLD TO REPAIR  •  " + module.definition.module_name.to_upper()
	return "E  TOGGLE " + module.definition.module_name.to_upper()

func _draw() -> void:
	var body := Color("#f2bc67")
	var shadow := Color("#8a422f")
	var dark := Color("#142237")
	var skin := Color("#f0c38a")
	draw_circle(Vector2(0, -18), 7.0, skin)
	draw_rect(Rect2(-7, -11, 14, 20), body)
	draw_rect(Rect2(-7, 9, 5, 12), shadow)
	draw_rect(Rect2(2, 9, 5, 12), shadow)
	draw_rect(Rect2(-9, -9, 3, 10), dark)
	draw_rect(Rect2(6, -9, 3, 10), dark)
	draw_rect(Rect2(-7, -26, 14, 4), dark)
