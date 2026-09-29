# Mechahouse Simulation Engine

This directory contains the standalone C# simulation prototype. It does not reference Godot and does not modify or integrate with the existing Godot project.

## Projects

- `Mechahouse.Core`: authoritative game state, entities, components, commands, definitions, and simulation systems.
- `Mechahouse.Cli`: a thin text adapter that renders the Core state and sends player commands.
- `Mechahouse.Tests`: a dependency-free automated test executable.

The architectural rule for this phase is:

> Entities hold data. Systems hold rules. GameState holds the current truth. Rendering displays that truth.

## Interior entity model

The initial Medium mech uses the existing 3×3 logical grid. Slot numbers increase left-to-right, then bottom-to-top:

```text
[7][8][9]  top
[4][5][6]  middle
[1][2][3]  bottom
```

The starter interior contains exactly these entities:

```text
[COC][HYD][HYD]
[OMN][ @ ][WAT]
[ENG][ENG][WAT]
```

The labels above are a compact CLI view of the actual footprints:

- Engine Room: slots 1, 2
- Water Processing: slots 3, 6
- Omni Slot: slot 4
- Bed: slot 5
- Cockpit: slot 7
- Hydroponics: slots 8, 9

Each interior entity is an `Entity` with a component collection. The initial component types are deliberately small:

- `PositionComponent`
- `OperationalComponent`
- `PowerGeneratorComponent`
- `PowerConsumerComponent`
- `WaterProducerComponent`
- `WaterConsumerComponent`
- `FoodProducerComponent`
- `EntityConditionComponent`
- `InteractionCapabilitiesComponent`
- `ExaminableComponent`
- `NarratableComponent`

The systems query these components rather than checking entity names. Omni Slots remain generic locations; they are not special-cased for Hydroponics.

### Room-entry narration

`ModuleDefinition` supports an optional `NarrationProfile? Narration = null` property.
The profile currently contains the short `OnEnter` description shown when the player
enters that module's room. When a `ModuleInstance` is created, the profile becomes
a `NarratableComponent`; movement reads that component from the destination entity
and displays the narration.

This keeps room flavor data pluggable with module creation:

```csharp
new ModuleDefinition(
    "custom_room",
    "Custom Room",
    1,
    0,
    0,
    Narration: new NarrationProfile(
        "A custom room description supplied by its module definition."));
```

Narration appears when entering a new module footprint. Moving between quadrants
belonging to the same multi-slot module does not repeat it. Definitions without a
narration profile remain valid and simply produce no arrival text.

## Turn processing

`GameSimulation.SimulateTurn` runs the phases in this fixed order:

```text
PowerSystem
    ↓
WaterSystem
    ↓
HydroponicsSystem
    ↓
FoodSystem
    ↓
CleanupSystem
```

This makes the first emergent chain explicit:

```text
Engine Room generates power
    → Water Processing receives power
    → Water Processing produces water
    → Hydroponics receives water
    → Hydroponics produces food
```

Game time is action-driven and paused between player commands. The default action costs are:

- Move within the mech: 10 minutes
- Enable or disable a module: 10 minutes
- Build or repair a module: 30 minutes
- Inspect state (`interior`, `status`, `modules`, `parts`, `help`): no time
- Examine an entity: no time
- Open an interaction menu: the selected action uses its normal action cost

Only successful actions consume time. The simulation resolves elapsed time in 10-minute ticks, running the fixed systems phase order for every tick. `wait <hours>` advances time explicitly; `advance <hours>` remains an alias. For example, `wait 1` advances six ticks. The clock starts at Day 1, 00:00 and is displayed in status and after timed actions.

Turning off the Engine Room does not call methods that manually disable downstream entities. Power allocation and resource systems observe the changed components and produce the consequences naturally.

`GameState.Events` keeps a small turn-completion event log for consumers that want to observe simulation progress. It is intentionally not a generalized event bus.

## CLI

From this directory:

```text
dotnet build Mechahouse.sln
dotnet run --project Mechahouse.Tests
dotnet run --project Mechahouse.Cli
```

Useful commands:

```text
status
interior
modules
parts
move <slot-number>
build <definition-id> [slot-number ...]
disable <module-id>
enable <module-id>
repair <module-id>
examine <entity-name|definition-id|quadrant>
interactions [entity-name|definition-id|quadrant]
interact <target> <examine|toggle|repair|sleep|install|remove> [part-id]
wait <hours>
advance <hours>   # alias for wait
```

`interior` displays the 3×3 mech layout and the player's avatar. `status` displays
the current resources, capacity, power, water, food, and simulation clock.

`interactions` opens a menu for the entity at the player's current quadrant. A
target can be supplied when the entity occupies another quadrant, but the player
must move there before selecting an action. Use Up/Down to change the selection,
Enter to choose it, or Escape to cancel. The menu always includes Cancel. In a
redirected or non-interactive terminal, use the direct `interact` command instead.


The CLI avatar is presentation-only. Its ASCII glyph is owned by `Mechahouse.Cli/AsciiPlayerAvatar.cs`; the player’s actual location remains in Core `PlayerState`.

The CLI does not calculate power, water, or food outcomes. It reads the state produced by the simulation systems and renders it.

## Scope

This phase is intentionally limited to the mech interior. It does not model combat, weapons, arms, legs, external armor, enemies, projectiles, fire, heat, networking, persistence, procedural generation, or a generalized ECS framework.
