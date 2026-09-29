# Mechahouse

An initial Godot 4 prototype for the Mechahouse systems-management loop: the player lives inside a stationary cutaway mech, routes power between modules, responds to failures, and repairs systems by hand.

## Run

Open the project in Godot 4.x and run the main scene. The project has no third-party dependencies.

## Controls

- `A/D` or arrow keys: move
- `W/S`: climb a nearby ladder
- Left mouse: walk to a destination; cross-floor clicks route through ladders
- `E`: toggle a nearby system; hold near the bedroom to rest
- `F`: hold to repair a damaged nearby system
- `Space`: fire the weapon from the weapon room

Debug shortcuts are shown in-game: `1/2/3` damage generator, purifier, or weapon; `4` restore all; `5` trigger a random failure; `Q/R` adjust water; `Z/X` adjust power.

## Structure

- `scenes/mechahouse.tscn`: single playable scene
- `scripts/mechahouse.gd`: generated placeholder visuals, HUD, input routing
- `scripts/mech_simulation.gd`: resources, power grid, failure events, repair and system state
- `scripts/player.gd`: movement, floors, ladders, room proximity
- `scripts/mech_module.gd`: reusable static module definitions
