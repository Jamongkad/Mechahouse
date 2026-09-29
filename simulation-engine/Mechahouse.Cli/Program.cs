using System.Globalization;
using Mechahouse.Cli;
using Mechahouse.Core;
using Mechahouse.Core.Commands;
using Mechahouse.Core.Domain;
using Mechahouse.Core.Systems;

var simulation = new GameSimulation();
var commandLine = new Mechahouse.Cli.CommandLineReader();
var playerAvatar = new AsciiPlayerAvatar("Player", '@');
Console.WriteLine("MECHAHOUSE SIMULATION CLI");
Console.WriteLine("Type 'help' for commands. Use Up/Down to browse command history.\n");
PrintStatus(simulation.State);

while (true)
{
    var line = commandLine.ReadLine();
    if (line is null)
    {
        break;
    }

    if (HandleCommand(simulation, playerAvatar, commandLine, line))
    {
        break;
    }
}

static bool HandleCommand(GameSimulation simulation, AsciiPlayerAvatar playerAvatar, CommandLineReader commandLine, string line)
{
    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length == 0)
    {
        return false;
    }

    var command = parts[0].TrimStart('$').ToLowerInvariant();
    switch (command)
    {
        case "status":
            PrintStatus(simulation.State);
            break;
        case "interior":
            PrintInterior(simulation.State, playerAvatar);
            break;
        case "move":
        case "go":
            if (parts.Length < 2 || !int.TryParse(parts[1], out var destinationSlot))
            {
                Console.WriteLine("Usage: move <slot-number>");
                break;
            }

            PrintResult(simulation.Execute(new MovePlayerCommand(new SlotId(destinationSlot))));
            PrintInterior(simulation.State, playerAvatar);
            break;
        case "modules":
            PrintModules(simulation.State.Mech.Modules);
            break;
        case "parts":
            Console.WriteLine("Available parts:");
            foreach (var definition in simulation.Definitions.OrderBy(definition => definition.Id))
            {
                Console.WriteLine($"{definition.Id,-22} {definition.Name,-22} slots {definition.SlotsRequired}, cost {definition.BuildCost}, weight {definition.Weight:0.##}");
            }
            break;
        case "build":
            if (parts.Length < 2)
            {
                Console.WriteLine("Usage: build <definition-id> [slot-number ...]");
                PrintInterior(simulation.State, playerAvatar);
                break;
            }

            IReadOnlyList<SlotId>? slotIds = null;
            if (parts.Length >= 3)
            {
                var parsedSlots = new List<SlotId>();
                foreach (var slotText in parts.Skip(2))
                {
                    if (!int.TryParse(slotText, out var slotNumber))
                    {
                    Console.WriteLine("Slot numbers must be integers.");
                    parsedSlots.Clear();
                        break;
                    }

                    parsedSlots.Add(new SlotId(slotNumber));
                }

                if (parsedSlots.Count == 0)
                {
                    PrintInterior(simulation.State, playerAvatar);
                    break;
                }

                slotIds = parsedSlots;
            }

            PrintResult(simulation.Execute(new BuildModuleCommand(parts[1], slotIds)));
            PrintInterior(simulation.State, playerAvatar);
            break;
        case "disable":
        case "enable":
            if (parts.Length < 2)
            {
                Console.WriteLine($"Usage: {command} <module-id>");
                break;
            }

            PrintResult(simulation.Execute(new ToggleModuleCommand(parts[1], command == "enable")));
            break;
        case "repair":
            if (parts.Length < 2)
            {
                Console.WriteLine("Usage: repair <module-id>");
                break;
            }

            PrintResult(simulation.Execute(new RepairModuleCommand(parts[1])));
            break;
        case "examine":
        case "inspect":
            if (parts.Length < 2)
            {
                Console.WriteLine("Usage: examine <entity-name|definition-id|quadrant>");
                break;
            }

            var examineReference = string.Join(' ', parts.Skip(1));
            if (TryResolveTarget(simulation.State, examineReference, out var examineTarget))
            {
                PrintResult(simulation.Execute(new InteractionIntent(
                    simulation.State.Player.Id,
                    examineTarget!.Id,
                    InteractionType.Examine)));
            }
            break;
        case "interactions":
            Entity? actionTarget;
            if (parts.Length >= 2)
            {
                var targetReference = string.Join(' ', parts.Skip(1));
                if (!TryResolveTarget(simulation.State, targetReference, out actionTarget))
                {
                    break;
                }
            }
            else
            {
                actionTarget = simulation.State.Entities.FirstOrDefault(entity =>
                    InteractionSystem.IsWithinReach(simulation.State, entity));
            }

            if (actionTarget is null)
            {
                Console.WriteLine("There is no entity at the player's current quadrant.");
                break;
            }

            var availableInteractions = InteractionSystem.GetAvailableInteractions(actionTarget);
            if (availableInteractions.Count == 0)
            {
                Console.WriteLine($"{actionTarget.Name} has no available interactions.");
                break;
            }

            if (!InteractionSystem.IsWithinReach(simulation.State, actionTarget))
            {
                Console.WriteLine($"Move to one of {string.Join(", ", actionTarget.GetComponent<PositionComponent>().Quadrants.Select(slot => slot.Number))} before interacting.");
                break;
            }

            var selectedMenuInteraction = InteractionMenu.Select(actionTarget.Name, availableInteractions);
            if (selectedMenuInteraction is null)
            {
                Console.WriteLine("Interaction cancelled.");
                break;
            }

            ExecuteInteraction(simulation, playerAvatar, commandLine, actionTarget, selectedMenuInteraction.Value);
            break;
        case "interact":
            var actionIndex = Array.FindIndex(parts, 1, part => TryParseInteraction(part, out _));
            if (actionIndex < 2 || !TryParseInteraction(parts[actionIndex], out var selectedInteraction))
            {
                Console.WriteLine("Usage: interact <target> <examine|toggle|repair|sleep|install|remove> [part-id]");
                break;
            }

            var selectedReference = string.Join(' ', parts.Skip(1).Take(actionIndex - 1));
            if (!TryResolveTarget(simulation.State, selectedReference, out var selectedTarget))
            {
                break;
            }

            ExecuteInteraction(
                simulation,
                playerAvatar,
                commandLine,
                selectedTarget!,
                selectedInteraction,
                actionIndex + 1 < parts.Length ? parts[actionIndex + 1] : null);
            break;
        case "wait":
        case "advance":
            if (parts.Length < 2 || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var hours))
            {
                Console.WriteLine("Usage: wait <hours> (or advance <hours>)");
                break;
            }

            PrintResult(simulation.Execute(new AdvanceTimeCommand(hours)), showClock: false);
            PrintStatus(simulation.State);
            break;
        case "help":
            PrintHelp();
            break;
        case "quit":
        case "exit":
            return true;
        default:
            Console.WriteLine($"Unknown command '{parts[0]}'. Type 'help'.");
            break;
    }

    return false;
}

static void ExecuteInteraction(
    GameSimulation simulation,
    AsciiPlayerAvatar playerAvatar,
    CommandLineReader commandLine,
    Entity target,
    InteractionType interaction,
    string? partId = null)
{
    if (interaction == InteractionType.Install && string.IsNullOrWhiteSpace(partId))
    {
        var installableParts = simulation.GetInstallablePartDefinitions().Select(definition => definition.Id).ToArray();
        Console.WriteLine($"Installable parts: {string.Join(", ", installableParts)}");
        partId = commandLine.ReadLine("part id> ");
    }

    var interactionResult = simulation.Execute(new InteractionIntent(
        simulation.State.Player.Id,
        target.Id,
        interaction,
        partId));
    PrintResult(interactionResult);
    if (interaction is InteractionType.Install or InteractionType.Remove)
    {
        PrintInterior(simulation.State, playerAvatar);
    }
}

static void PrintResult(SimulationResult result, bool showClock = true)
{
    Console.WriteLine(result.Message);
    if (showClock && result.Succeeded && result.TimeAdvancedMinutes > 0)
    {
        Console.WriteLine($"Time: {FormatGameTime(result.State.Clock)} (+{result.TimeAdvancedMinutes:0.##} min)");
    }

    if (result.Examination is not null)
    {
        PrintExamination(result.Examination);
    }
}

static void PrintExamination(ExaminationReport report)
{
    Console.WriteLine($"\n{report.EntityName.ToUpperInvariant()} — {report.Condition.ToString().ToUpperInvariant()}");
    Console.WriteLine(report.Observation);
    foreach (var fact in report.Facts)
    {
        Console.WriteLine($"  {fact.Label}: {fact.Value}");
    }

    if (report.AvailableInteractions.Count > 0)
    {
        Console.WriteLine($"  Actions: {string.Join(", ", report.AvailableInteractions.Select(FormatInteraction))}");
    }
}

static string FormatInteraction(InteractionType interaction) => interaction.ToString().ToUpperInvariant();

static bool TryParseInteraction(string value, out InteractionType interaction)
{
    if (Enum.GetNames<InteractionType>().Contains(value, StringComparer.OrdinalIgnoreCase))
    {
        return Enum.TryParse(value, true, out interaction);
    }

    interaction = default;
    return false;
}

static bool TryResolveTarget(GameState state, string reference, out Entity? target)
{
    if (int.TryParse(reference, out var slotNumber))
    {
        target = state.Entities.SingleOrDefault(entity =>
            entity.GetComponent<PositionComponent>().Quadrants.Any(slot => slot.Number == slotNumber));
        if (target is null)
        {
            Console.WriteLine($"No entity occupies quadrant {slotNumber}.");
            return false;
        }

        return true;
    }

    var matches = state.Entities.Where(entity =>
            string.Equals(entity.Id.ToString(), reference, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(NormalizeReference(entity.Name), NormalizeReference(reference), StringComparison.OrdinalIgnoreCase) ||
            entity is ModuleInstance module && string.Equals(module.DefinitionId, reference, StringComparison.OrdinalIgnoreCase))
        .ToArray();
    if (matches.Length == 1)
    {
        target = matches[0];
        return true;
    }

    if (matches.Length > 1)
    {
        Console.WriteLine($"'{reference}' matches multiple entities. Use a quadrant number instead.");
    }
    else
    {
        Console.WriteLine($"No entity matches '{reference}'. Use 'interior' to see the quadrants.");
    }

    target = null;
    return false;
}

static string NormalizeReference(string value) => new(value.Where(char.IsLetterOrDigit).ToArray());

static void PrintStatus(GameState state)
{
    var mech = state.Mech;
    var metrics = state.Metrics;
    Console.WriteLine($"Time: {FormatGameTime(state.Clock)} (turn {state.Turn})");
    Console.WriteLine($"Chassis: {mech.Chassis}");
    Console.WriteLine($"Scrap: {mech.Scrap}");
    Console.WriteLine($"Space: {metrics.UsedSlots} / {mech.SlotCapacity}");
    Console.WriteLine($"Weight: {metrics.CurrentWeight:0.##} / {metrics.WeightCapacity:0.##}");
    Console.WriteLine($"Power: {metrics.PowerProduction:0.##} production / {metrics.PowerDemand:0.##} demand / {metrics.PowerAvailable:0.##} available");
    Console.WriteLine($"Water: {mech.Water:0.##} / {metrics.WaterCapacity:0.##} L");
    Console.WriteLine($"Water rate: +{metrics.WaterProduction:0.##} / -{metrics.WaterConsumption:0.##} per day");
    Console.WriteLine($"Food: {mech.Food:0.##}");
    Console.WriteLine($"Food rate: +{metrics.FoodProduction:0.##} / -{metrics.FoodConsumption:0.##} per day");
}

static string FormatGameTime(GameClock clock) => $"Day {clock.DayNumber}, {clock.HourOfDay:00}:{clock.MinuteOfHour:00}";

static void PrintInterior(GameState state, AsciiPlayerAvatar playerAvatar)
{
    var moduleBySlot = new Dictionary<SlotId, ModuleInstance>();
    foreach (var module in state.Mech.Modules)
    {
        foreach (var slot in module.OccupiedSlots)
        {
            moduleBySlot[slot] = module;
        }
    }

    Console.WriteLine("Interior:");
    for (var row = 2; row >= 0; row--)
    {
        for (var column = 0; column < 3; column++)
        {
            var slot = state.Mech.Layout.Slots.Single(definition => definition.Coordinate == new SlotCoordinate(column, row)).Id;
            var label = moduleBySlot.TryGetValue(slot, out var module) ? ModuleLabel(module) : "   ";
            if (state.Player.CurrentSlot == slot)
            {
                label = playerAvatar.RenderInCell(label);
            }

            Console.Write($"[{label}]");
        }

        Console.WriteLine();
    }

    Console.WriteLine(playerAvatar.Legend(state.Player.CurrentSlot.Number));
    PrintInteriorDetails(state);
}

static void PrintInteriorDetails(GameState state)
{
    Console.WriteLine("\nInterior systems:");
    foreach (var entity in state.Entities.OrderBy(entity => entity.GetComponent<PositionComponent>().Quadrants.Min(slot => slot.Number)))
    {
        var position = entity.GetComponent<PositionComponent>();
        var slots = string.Join(",", position.Quadrants.Select(slot => slot.Number));
        Console.WriteLine($"{entity.Name} (slots {slots})");

        var hasDetails = false;
        if (entity.TryGetComponent<PowerGeneratorComponent>(out var generator) && generator is not null)
        {
            Console.WriteLine($"  Power Output: {generator.CurrentOutput:0.##}");
            hasDetails = true;
        }

        if (entity.TryGetComponent<PowerConsumerComponent>(out var powerConsumer) && powerConsumer is not null)
        {
            Console.WriteLine($"  Power: {(powerConsumer.IsPowered ? "ONLINE" : "OFFLINE")}");
            hasDetails = true;
        }

        if (entity.TryGetComponent<WaterProducerComponent>(out var waterProducer) && waterProducer is not null)
        {
            Console.WriteLine($"  Water Production: {waterProducer.CurrentOutput:0.##} L/day");
            hasDetails = true;
        }

        if (entity.TryGetComponent<WaterConsumerComponent>(out var waterConsumer) && waterConsumer is not null)
        {
            Console.WriteLine($"  Water: {(waterConsumer.HasWater ? "SUPPLIED" : "INSUFFICIENT")}");
            hasDetails = true;
        }

        if (entity.TryGetComponent<FoodProducerComponent>(out var foodProducer) && foodProducer is not null)
        {
            Console.WriteLine($"  Food Production: {foodProducer.CurrentOutput:0.##} /day");
            hasDetails = true;
        }

        if (!hasDetails)
        {
            Console.WriteLine("  No resource systems.");
        }
    }
}

static string ModuleLabel(ModuleInstance module) => module.DefinitionId switch
{
    "engine_room" => "ENG",
    "water_processing" => "WAT",
    "cockpit" => "COC",
    "omni_slot" => "OMN",
    "hydroponics_small" => "HYD",
    "bed" => "BED",
    _ => module.Definition.Name[..Math.Min(3, module.Definition.Name.Length)].ToUpperInvariant()
};

static void PrintModules(IReadOnlyList<ModuleInstance> modules)
{
    if (modules.Count == 0)
    {
        Console.WriteLine("No modules installed.");
        return;
    }

    foreach (var module in modules.OrderBy(module => module.OccupiedSlots.Min(slot => slot.Number)))
    {
        var powerState = module.Definition.PowerConsumption > 0 ? (module.Powered ? "powered" : "no power") : "n/a";
        var slots = string.Join(",", module.OccupiedSlots.Select(slot => slot.Number));
        Console.WriteLine($"{module.DefinitionId,-22} slots [{slots}], {(module.Enabled ? "enabled" : "disabled")}, {powerState}, durability {module.Durability:0.#}%");
    }
}

static void PrintHelp()
{
    Console.WriteLine("status");
    Console.WriteLine("interior");
    Console.WriteLine("move <slot-number>");
    Console.WriteLine("modules");
    Console.WriteLine("parts");
    Console.WriteLine("build <definition-id> [slot-number ...]");
    Console.WriteLine("disable <module-id>");
    Console.WriteLine("enable <module-id>");
    Console.WriteLine("repair <module-id>");
    Console.WriteLine("interactions [entity-name|definition-id|quadrant]");
    Console.WriteLine("examine <entity-name|definition-id|quadrant>");
    Console.WriteLine("interact <target> <examine|toggle|repair|sleep|install|remove> [part-id]");
    Console.WriteLine("  Examples: examine 8; interact engine_room repair; interact 4 install bed; interact 4 remove");
    Console.WriteLine("wait <hours> (or advance <hours>)");
    Console.WriteLine("Successful moves/toggles cost 10 minutes; builds/repairs cost 30.");
    Console.WriteLine("Interactions: examine is free; toggle/remove cost 10 minutes; repair/install cost 30; sleep advances 8 hours.");
    Console.WriteLine("Inspection commands are free.");
    Console.WriteLine("Up/Down: browse command history");
    Console.WriteLine("quit");
}
