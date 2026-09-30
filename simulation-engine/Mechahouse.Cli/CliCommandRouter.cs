using System.Globalization;
using Mechahouse.Core;
using Mechahouse.Core.Commands;
using Mechahouse.Core.Domain;
using Mechahouse.Core.Systems;

namespace Mechahouse.Cli;

public sealed class CliCommandRouter
{
    private readonly GameSimulation _simulation;
    private readonly CommandLineReader _commandLine;
    private readonly CliRenderer _renderer;
    private readonly EntityReferenceResolver _entityReferenceResolver = new();

    public CliCommandRouter(GameSimulation simulation, CommandLineReader commandLine, CliRenderer renderer)
    {
        _simulation = simulation;
        _commandLine = commandLine;
        _renderer = renderer;
    }

    public bool Handle(string line)
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
                _renderer.PrintDashboard(_simulation.State);
                break;
            case "interior":
                _renderer.PrintInterior(_simulation.State);
                break;
            case "move":
            case "go":
                if (parts.Length < 2 || !int.TryParse(parts[1], out var destinationSlot))
                {
                    _renderer.PrintMessage("Usage: move <slot-number>");
                    break;
                }

                _renderer.PrintResult(_simulation.Execute(new MovePlayerCommand(new SlotId(destinationSlot))));
                _renderer.PrintInterior(_simulation.State);
                break;
            case "modules":
                _renderer.PrintModules(_simulation.State.Mech.Modules);
                break;
            case "parts":
                _renderer.PrintParts(_simulation.Definitions);
                break;
            case "build":
                HandleBuild(parts);
                break;
            case "disable":
            case "enable":
                if (parts.Length < 2)
                {
                    _renderer.PrintMessage($"Usage: {command} <module-id>");
                    break;
                }

                _renderer.PrintResult(_simulation.Execute(new ToggleModuleCommand(parts[1], command == "enable")));
                break;
            case "repair":
                if (parts.Length < 2)
                {
                    _renderer.PrintMessage("Usage: repair <module-id>");
                    break;
                }

                _renderer.PrintResult(_simulation.Execute(new RepairModuleCommand(parts[1])));
                break;
            case "examine":
            case "inspect":
                HandleExamine(parts);
                break;
            case "interact":
                HandleInteract(parts);
                break;
            case "wait":
            case "advance":
                HandleAdvance(parts);
                break;
            case "help":
                _renderer.PrintHelp();
                break;
            case "quit":
            case "exit":
                return true;
            default:
                _renderer.PrintMessage($"Unknown command '{parts[0]}'. Type 'help'.");
                break;
        }

        return false;
    }

    private void HandleBuild(string[] parts)
    {
        if (parts.Length < 2)
        {
            _renderer.PrintMessage("Usage: build <definition-id> [slot-number ...]");
            _renderer.PrintInterior(_simulation.State);
            return;
        }

        IReadOnlyList<SlotId>? slotIds = null;
        if (parts.Length >= 3)
        {
            var parsedSlots = new List<SlotId>();
            foreach (var slotText in parts.Skip(2))
            {
                if (!int.TryParse(slotText, out var slotNumber))
                {
                    _renderer.PrintMessage("Slot numbers must be integers.");
                    parsedSlots.Clear();
                    break;
                }

                parsedSlots.Add(new SlotId(slotNumber));
            }

            if (parsedSlots.Count == 0)
            {
                _renderer.PrintInterior(_simulation.State);
                return;
            }

            slotIds = parsedSlots;
        }

        _renderer.PrintResult(_simulation.Execute(new BuildModuleCommand(parts[1], slotIds)));
        _renderer.PrintInterior(_simulation.State);
    }

    private void HandleExamine(string[] parts)
    {
        if (parts.Length < 2)
        {
            _renderer.PrintMessage("Usage: examine <entity-name|definition-id|quadrant>");
            return;
        }

        var examineReference = string.Join(' ', parts.Skip(1));
        if (!TryResolveTarget(examineReference, out var examineTarget))
        {
            return;
        }

        _renderer.PrintResult(_simulation.Execute(new InteractionIntent(
            _simulation.State.Player.Id,
            examineTarget!.Id,
            InteractionType.Examine)));
    }

    private void HandleInteract(string[] parts)
    {
        var actionIndex = Array.FindIndex(parts, 1, part => TryParseInteraction(part, out _));
        if (actionIndex >= 2)
        {
            HandleDirectInteraction(parts, actionIndex);
            return;
        }

        HandleInteractionMenu(parts);
    }

    private void HandleInteractionMenu(string[] parts)
    {
        Entity? actionTarget;
        if (parts.Length >= 2)
        {
            var targetReference = string.Join(' ', parts.Skip(1));
            if (!TryResolveTarget(targetReference, out actionTarget))
            {
                return;
            }
        }
        else
        {
            actionTarget = _simulation.State.Entities.FirstOrDefault(entity =>
                InteractionSystem.IsWithinReach(_simulation.State, entity));
        }

        if (actionTarget is null)
        {
            _renderer.PrintMessage("There is no entity at the player's current quadrant.");
            return;
        }

        var availableInteractions = InteractionSystem.GetAvailableInteractions(actionTarget);
        if (availableInteractions.Count == 0)
        {
            _renderer.PrintMessage($"{actionTarget.Name} has no available interactions.");
            return;
        }

        if (!InteractionSystem.IsWithinReach(_simulation.State, actionTarget))
        {
            _renderer.PrintMessage($"Move to one of {string.Join(", ", actionTarget.GetComponent<PositionComponent>().Quadrants.Select(slot => slot.Number))} before interacting.");
            return;
        }

        var selectedMenuInteraction = InteractionMenu.Select(actionTarget.Name, availableInteractions);
        if (selectedMenuInteraction is null)
        {
            _renderer.PrintMessage("Interaction cancelled.");
            return;
        }

        ExecuteInteraction(actionTarget, selectedMenuInteraction.Value);
    }

    private void HandleDirectInteraction(string[] parts, int actionIndex)
    {
        if (!TryParseInteraction(parts[actionIndex], out var selectedInteraction))
        {
            _renderer.PrintMessage("Usage: interact <target> <examine|use|toggle|repair|sleep|install|remove> [part-id]");
            return;
        }

        var selectedReference = string.Join(' ', parts.Skip(1).Take(actionIndex - 1));
        if (!TryResolveTarget(selectedReference, out var selectedTarget))
        {
            return;
        }

        ExecuteInteraction(
            selectedTarget!,
            selectedInteraction,
            actionIndex + 1 < parts.Length ? parts[actionIndex + 1] : null);
    }

    private void HandleAdvance(string[] parts)
    {
        if (parts.Length < 2 || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var hours))
        {
            _renderer.PrintMessage("Usage: wait <hours> (or advance <hours>)");
            return;
        }

        _renderer.PrintResult(_simulation.Execute(new AdvanceTimeCommand(hours)), showClock: false);
        _renderer.PrintStatus(_simulation.State);
    }

    private void ExecuteInteraction(Entity target, InteractionType interaction, string? partId = null)
    {
        if (interaction == InteractionType.Install && string.IsNullOrWhiteSpace(partId))
        {
            var installableParts = _simulation.GetInstallablePartDefinitions().Select(definition => definition.Id).ToArray();
            _renderer.PrintMessage($"Installable parts: {string.Join(", ", installableParts)}");
            partId = _commandLine.ReadLine("part id> ");
        }

        var interactionResult = _simulation.Execute(new InteractionIntent(
            _simulation.State.Player.Id,
            target.Id,
            interaction,
            partId));
        _renderer.PrintResult(interactionResult);
        if (interaction is InteractionType.Install or InteractionType.Remove)
        {
            _renderer.PrintInterior(_simulation.State);
        }
    }

    private bool TryResolveTarget(string reference, out Entity? target)
    {
        var resolution = _entityReferenceResolver.Resolve(_simulation.State, reference);
        if (!resolution.Succeeded)
        {
            _renderer.PrintMessage(resolution.Error!);
            target = null;
            return false;
        }

        target = resolution.Entity;
        return true;
    }

    private static bool TryParseInteraction(string value, out InteractionType interaction)
    {
        if (Enum.GetNames<InteractionType>().Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return Enum.TryParse(value, true, out interaction);
        }

        interaction = default;
        return false;
    }
}
