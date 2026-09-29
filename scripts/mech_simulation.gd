class_name MechSimulation
extends Node

signal state_changed
signal alert_message(message: String, severity: String)

class ModuleState:
	var definition: MechModuleDefinition
	var current_durability: float
	var enabled: bool = true
	var powered: bool = false
	var damaged: bool = false
	var efficiency: float = 1.0
	var repair_notified: bool = false

	func _init(new_definition: MechModuleDefinition):
		definition = new_definition
		current_durability = definition.max_durability

	func integrity() -> float:
		return clamp(current_durability / definition.max_durability, 0.0, 1.0)

	func status_text() -> String:
		if current_durability <= 0.0:
			return "DESTROYED"
		if damaged:
			return "DAMAGED"
		if not enabled:
			return "OFFLINE"
		if not powered:
			return "NO POWER"
		return "OPERATIONAL"

var modules: Dictionary = {}
var power: float = 62.0
var water: float = 58.0
var food: float = 76.0
var hull: float = 88.0
var fatigue: float = 24.0
var last_alert: String = "MECH SYSTEMS ONLINE"
var last_alert_severity: String = "good"
var alert_time: float = 4.0
var elapsed: float = 0.0
var random_failure_clock: float = 0.0
var weapon_flash: float = 0.0

func _ready() -> void:
	_add_module("generator", "Generator", 0.0, 100.0, 28.0, "generator")
	_add_module("purifier", "Water Purifier", 3.0, 100.0, 24.0, "purifier")
	_add_module("weapon", "Weapon Array", 5.0, 100.0, 30.0, "weapon")
	_add_module("bedroom", "Bedroom", 0.0, 100.0, 20.0, "habitat")

func _add_module(id: String, title: String, demand: float, durability: float, repair_rate: float, kind: String) -> void:
	var definition := MechModuleDefinition.new()
	definition.module_name = title
	definition.power_consumption = demand
	definition.max_durability = durability
	definition.repair_rate = repair_rate
	definition.module_type = kind
	modules[id] = ModuleState.new(definition)

func _process(delta: float) -> void:
	elapsed += delta
	random_failure_clock += delta
	weapon_flash = max(0.0, weapon_flash - delta)
	if random_failure_clock >= 24.0:
		random_failure_clock = 0.0
		damage_random_system()
	_update_power_grid(delta)
	_update_resources(delta)
	if alert_time > 0.0:
		alert_time -= delta
	state_changed.emit()

func _update_power_grid(delta: float) -> void:
	var generator: ModuleState = modules.generator
	var generator_output := 0.0
	if generator.enabled and generator.current_durability > 0.0:
		generator_output = 10.0 * generator.integrity()

	var baseline_demand := 2.0
	var purifier: ModuleState = modules.purifier
	var weapon: ModuleState = modules.weapon
	var available := generator_output
	var baseline_powered := available >= baseline_demand
	available = max(0.0, available - baseline_demand)

	purifier.powered = purifier.enabled and purifier.current_durability > 0.0 and available >= purifier.definition.power_consumption
	if purifier.powered:
		available -= purifier.definition.power_consumption
	weapon.powered = weapon.enabled and weapon.current_durability > 0.0 and available >= weapon.definition.power_consumption

	var total_demand := baseline_demand
	if purifier.powered:
		total_demand += purifier.definition.power_consumption
	if weapon.powered:
		total_demand += weapon.definition.power_consumption
	power = clamp(power + (generator_output - total_demand) * delta * 1.4, 0.0, 100.0)

	if not baseline_powered:
		power = max(0.0, power - delta * 0.9)
	if generator.current_durability <= 0.0:
		generator.powered = false
	else:
		generator.powered = generator.enabled

func _update_resources(delta: float) -> void:
	var purifier: ModuleState = modules.purifier
	if purifier.powered:
		water = clamp(water + 0.72 * purifier.efficiency * delta, 0.0, 100.0)
	else:
		water = max(0.0, water - 0.24 * delta)
	food = max(0.0, food - 0.045 * delta)
	var bedroom: ModuleState = modules.bedroom
	if bedroom.enabled and _is_player_resting:
		fatigue = max(0.0, fatigue - 6.0 * delta)
	else:
		fatigue = min(100.0, fatigue + 0.8 * delta)
	if water <= 0.0 or food <= 0.0:
		hull = max(0.0, hull - 0.08 * delta)

var _is_player_resting: bool = false

func set_resting(value: bool) -> void:
	_is_player_resting = value

func get_module(id: String) -> ModuleState:
	return modules.get(id)

func toggle_module(id: String) -> void:
	var module: ModuleState = modules.get(id)
	if module == null:
		return
	module.enabled = not module.enabled
	var state := "ONLINE" if module.enabled else "OFFLINE"
	_set_alert(module.definition.module_name + " " + state, "info")

func fire_weapon() -> bool:
	var weapon: ModuleState = modules.weapon
	if not weapon.enabled or weapon.current_durability <= 0.0:
		_set_alert("WEAPON ARRAY IS OFFLINE", "danger")
		return false
	if not weapon.powered or power < 5.0:
		_set_alert("NOT ENOUGH POWER TO FIRE", "warning")
		return false
	power = max(0.0, power - 5.0)
	weapon_flash = 0.22
	_set_alert("WEAPON ARRAY FIRED — 5 POWER", "info")
	return true

func repair_module(id: String, delta: float) -> bool:
	var module: ModuleState = modules.get(id)
	if module == null or module.current_durability >= module.definition.max_durability:
		return false
	module.current_durability = min(module.definition.max_durability, module.current_durability + module.definition.repair_rate * delta)
	module.damaged = module.current_durability < module.definition.max_durability
	module.efficiency = clamp(0.55 + module.integrity() * 0.45, 0.0, 1.0)
	if module.current_durability >= module.definition.max_durability:
		module.damaged = false
		module.repair_notified = false
		_set_alert(module.definition.module_name + " REPAIRED — OPERATIONAL", "good")
	return true

func damage_system(id: String, amount: float = 32.0) -> void:
	var module: ModuleState = modules.get(id)
	if module == null:
		return
	module.current_durability = max(0.0, module.current_durability - amount)
	module.damaged = true
	module.efficiency = clamp(0.55 + module.integrity() * 0.45, 0.0, 1.0)
	_set_alert("WARNING: " + module.definition.module_name.to_upper() + " DAMAGED", "danger")

func damage_random_system() -> void:
	var candidates := ["generator", "purifier", "weapon"]
	var chosen: String = candidates[randi() % candidates.size()]
	damage_system(chosen, 18.0 + randf_range(0.0, 14.0))

func restore_all() -> void:
	for module in modules.values():
		module.current_durability = module.definition.max_durability
		module.damaged = false
		module.efficiency = 1.0
	_set_alert("ALL SYSTEMS RESTORED", "good")

func adjust_resource(resource_name: String, amount: float) -> void:
	match resource_name:
		"water": water = clamp(water + amount, 0.0, 100.0)
		"power": power = clamp(power + amount, 0.0, 100.0)
		"food": food = clamp(food + amount, 0.0, 100.0)
		"hull": hull = clamp(hull + amount, 0.0, 100.0)
	_set_alert(resource_name.to_upper() + (" +" if amount >= 0.0 else " ") + str(snapped(amount, 1)), "info")

func _set_alert(message: String, severity: String) -> void:
	last_alert = message
	last_alert_severity = severity
	alert_time = 5.0
	alert_message.emit(message, severity)
