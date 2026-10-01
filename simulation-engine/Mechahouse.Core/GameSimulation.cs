using Mechahouse.Core.Commands;
using Mechahouse.Core.Definitions;
using Mechahouse.Core.Domain;
using Mechahouse.Core.Systems;

namespace Mechahouse.Core;

public sealed class GameSimulation
{
    private readonly IReadOnlyDictionary<string, ModuleDefinition> _definitions;
    private readonly MechState _mech;
    private readonly PlayerState _player;
    private readonly GameState _state;

    public GameSimulation(bool installStarterModules = true, IReadOnlyDictionary<string, ModuleDefinition>? definitions = null)
    {
        _definitions = definitions ?? ModuleCatalog.CreateDefault();
        _mech = new MechState(
            ChassisType.Medium,
            MechLayouts.Medium,
            weightCapacity: 40,
            waterCapacity: 100,
            scrap: 50,
            water: 80,
            food: 20);
        _player = new PlayerState(new SlotId(5));
        _state = new GameState(_mech, _player);
        InstallDefaultOmniSlots();

        if (installStarterModules)
        {
            InstallStarterModules();
        }

        RefreshDerivedState();
        ThermalSystem.Initialize(_state);
        RefreshDerivedState();
    }

    public GameState State => _state;

    public IReadOnlyCollection<ModuleDefinition> Definitions => _definitions.Values.ToArray();

    public SimulationResult Execute(SimulationCommand command)
    {
        var startingMinute = _state.Clock.ElapsedMinutes;
        var result = command switch
        {
            BuildModuleCommand build => BuildModule(build),
            ToggleModuleCommand toggle => ToggleModule(toggle),
            RepairModuleCommand repair => RepairModule(repair),
            AdvanceTimeCommand advance => AdvanceTime(advance),
            MovePlayerCommand move => MovePlayer(move),
            InteractionIntent interaction => Interact(interaction),
            _ => SimulationResult.Failure($"Unsupported command: {command.GetType().Name}.", State)
        };

        if (!result.Succeeded)
        {
            return result with { State = _state };
        }

        var actionCost = ActionTimeCosts.For(command);
        if (actionCost > 0)
        {
            SimulateElapsedMinutes(actionCost);
        }

        return result with
        {
            State = _state,
            TimeAdvancedMinutes = _state.Clock.ElapsedMinutes - startingMinute
        };
    }

    public SimulationResult AdvanceTime(double hours) => Execute(new AdvanceTimeCommand(hours));

    public void SimulateTurn(double minutes = 10)
    {
        if (!double.IsFinite(minutes) || minutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minutes), "Simulation time must be a finite, non-negative number of minutes.");
        }

        var hours = minutes / 60;
        // The phase order is deliberate: consumers can only use resource state
        // produced by the phase immediately before them.
        PowerSystem.Run(_state);
        WaterSystem.Run(_state, hours);
        ThermalSystem.Run(_state, hours);
        HydroponicsSystem.Run(_state, hours);
        FoodSystem.Run(_state, hours);
        CleanupSystem.Run(_state);
        ConditionSystem.Run(_state);
        _state.Clock.Advance(minutes);
        _state.Turn++;
        _state.RecordEvent($"Completed turn {_state.Turn}.");
    }

    private SimulationResult BuildModule(BuildModuleCommand command)
    {
        if (!_definitions.TryGetValue(command.DefinitionId, out var definition))
        {
            return Failure($"Unknown module definition '{command.DefinitionId}'.");
        }

        var slotIds = command.SlotIds ?? _mech.FindFirstAvailablePlacement(definition.SlotsRequired, _player.CurrentSlot);
        if (slotIds is null || slotIds.Count != definition.SlotsRequired)
        {
            return Failure($"'{definition.Name}' does not have a valid placement in the available slots.");
        }

        if (!_mech.IsSlotPlacementAvailable(slotIds))
        {
            return Failure($"'{definition.Name}' cannot occupy the requested slots.");
        }

        if (!slotIds.Contains(_player.CurrentSlot))
        {
            return Failure($"Player must be in one of the requested slots before building '{definition.Name}'.");
        }

        if (_mech.Scrap < definition.BuildCost)
        {
            return Failure($"Not enough scrap for '{definition.Name}'. Need {definition.BuildCost}, have {_mech.Scrap}.");
        }

        if (_mech.CurrentWeight + definition.Weight > _mech.WeightCapacity)
        {
            return Failure($"'{definition.Name}' would exceed the {Format(_mech.WeightCapacity)} weight capacity.");
        }

        var emptyOmniSlots = _mech.Modules
            .Where(module => _mech.IsEmptyOmniSlot(module))
            .Where(module => module.OccupiedSlots.Any(slotIds.Contains))
            .ToArray();
        foreach (var emptyOmni in emptyOmniSlots)
        {
            _mech.RemoveModule(emptyOmni);
        }

        _mech.Scrap -= definition.BuildCost;
        var built = new ModuleInstance(definition, slotIds);
        if (emptyOmniSlots.Length > 0 && definition.Id != "omni_slot")
        {
            built.MarkInstalledInOmniSlot();
        }

        _mech.AddModule(built);
        RefreshDerivedState();
        return Success($"Built {definition.Name} in slots {FormatSlots(slotIds)}.");
    }

    private SimulationResult ToggleModule(ToggleModuleCommand command)
    {
        var module = FindModule(command.ModuleReference);
        if (module is null)
        {
            return Failure($"Module '{command.ModuleReference}' was not found.");
        }

        module.Enabled = command.Enabled;
        RefreshDerivedState();
        return Success(ToggleNarration(module, command.Enabled));
    }

    private SimulationResult RepairModule(RepairModuleCommand command)
    {
        var module = FindModule(command.ModuleReference);
        if (module is null)
        {
            return Failure($"Module '{command.ModuleReference}' was not found.");
        }

        if (!double.IsFinite(command.Amount) || command.Amount <= 0)
        {
            return Failure("Repair amount must be greater than zero.");
        }

        var repair = RepairSystem.Run(module, command.Amount);
        RefreshDerivedState();
        return Success($"Repaired {module.Definition.Name} from {Format(repair.PreviousDurability)}% to {Format(repair.CurrentDurability)}%.");
    }

    private static string ToggleNarration(ModuleInstance module, bool enabled)
    {
        var fallback = $"{module.Definition.Name} {(enabled ? "enabled" : "disabled")}.";
        if (!module.TryGetComponent<NarratableComponent>(out var narration) || narration is null)
        {
            return fallback;
        }

        if (enabled &&
            module.TryGetComponent<PowerConsumerComponent>(out var power) && power is not null && !power.IsPowered)
        {
            return narration.Profile.OnEnabledWithoutPower ?? fallback;
        }

        return enabled
            ? narration.Profile.OnEnabled ?? fallback
            : narration.Profile.OnDisabled ?? fallback;
    }

    private SimulationResult Interact(InteractionIntent intent)
    {
        if (intent.ActorId != _player.Id)
        {
            return Failure("That interaction was not requested by the current player.");
        }

        var target = _state.Entities.SingleOrDefault(entity => entity.Id == intent.TargetEntityId);
        if (target is null)
        {
            return Failure("The selected entity is no longer in the mech.");
        }

        if (!InteractionSystem.IsWithinReach(_state, target))
        {
            return Failure($"Move to one of {FormatSlots(target.GetComponent<PositionComponent>().Quadrants)} to interact with {target.Name}.");
        }

        if (!InteractionSystem.GetAvailableInteractions(target).Contains(intent.Interaction))
        {
            return Failure($"{target.Name} does not support {intent.Interaction}.");
        }

        return intent.Interaction switch
        {
            InteractionType.Examine => Success($"You examine {target.Name}.") with
            {
                Examination = ExamineSystem.Examine(_state, target)
            },
            InteractionType.Toggle => ToggleModule(new ToggleModuleCommand(
                target.Id.ToString(),
                !target.GetComponent<OperationalComponent>().Enabled)),
            InteractionType.Use => UseStorage(target),
            InteractionType.Repair => RepairModule(new RepairModuleCommand(target.Id.ToString())),
            InteractionType.Sleep => Sleep(),
            InteractionType.Install => InstallInOmniSlot(target, intent.PartId),
            InteractionType.Remove => RemoveFromOmniSlot(target),
            _ => Failure($"{intent.Interaction} is not implemented for this mech interior.")
        };
    }

    public IReadOnlyList<ModuleDefinition> GetInstallablePartDefinitions() => _definitions.Values
        .Where(definition => definition.Interactions?.Contains(InteractionType.Install) != true)
        .OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private SimulationResult Sleep()
    {
        const double sleepHours = 8;
        SimulateElapsedMinutes(sleepHours * 60);
        return Success("You sleep for eight hours. The mech's systems keep ticking around you.");
    }

    private SimulationResult UseStorage(Entity target)
    {
        if (!target.TryGetComponent<StorageComponent>(out var storage) || storage is null)
        {
            return Failure($"{target.Name} has no usable storage.");
        }

        var inventory = new List<string>();
        if (storage.Profile.StoresScrap)
        {
            inventory.Add($"Scrap: {_mech.Scrap}");
        }

        if (storage.Profile.StoresFood)
        {
            inventory.Add($"Food: {Format(_mech.Food)}");
        }

        return Success($"{storage.Profile.InventoryNarration}\n{string.Join('\n', inventory)}");
    }

    private SimulationResult InstallInOmniSlot(Entity target, string? partId)
    {
        if (target is not ModuleInstance omniSlot ||
            !target.HasComponent<OmniSlotComponent>() ||
            target.HasComponent<InstalledInOmniSlotComponent>())
        {
            return Failure("Select an empty Omni Slot to install a part.");
        }

        if (string.IsNullOrWhiteSpace(partId) || !_definitions.TryGetValue(partId, out var definition))
        {
            return Failure($"Choose an installable part: {string.Join(", ", GetInstallablePartDefinitions().Select(part => part.Id))}.");
        }

        if (definition.Interactions?.Contains(InteractionType.Install) == true)
        {
            return Failure($"'{definition.Name}' is not an installable module.");
        }

        if (omniSlot.OccupiedSlots.Count != 1)
        {
            return Failure("This Omni Slot does not occupy exactly one quadrant.");
        }

        var slot = target.GetComponent<PositionComponent>().Quadrant;
        if (_mech.Scrap < definition.BuildCost)
        {
            return Failure($"Not enough scrap for '{definition.Name}'. Need {definition.BuildCost}, have {_mech.Scrap}.");
        }

        var availableOccupiedSlots = _mech.Modules
            .Where(module => !_mech.IsEmptyOmniSlot(module))
            .SelectMany(module => module.OccupiedSlots)
            .ToArray();
        var placement = _mech.Layout.FindFirstAvailablePlacement(
            availableOccupiedSlots,
            definition.SlotsRequired,
            slot);
        if (placement is null)
        {
            return Failure($"'{definition.Name}' does not have a contiguous footprint available from quadrant {slot.Number}.");
        }

        var anchoredPlacement = new[] { slot }
            .Concat(placement.Where(placedSlot => placedSlot != slot))
            .ToArray();

        if (_mech.CurrentWeight - omniSlot.Definition.Weight + definition.Weight > _mech.WeightCapacity)
        {
            return Failure($"'{definition.Name}' would exceed the {Format(_mech.WeightCapacity)} weight capacity.");
        }

        RemoveEmptyOmniSlots(anchoredPlacement);
        var installed = new ModuleInstance(definition, anchoredPlacement);
        installed.MarkInstalledInOmniSlot();
        _mech.AddModule(installed);
        _mech.Scrap -= definition.BuildCost;
        RefreshDerivedState();
        return Success($"Installed {definition.Name} from the Omni Slot at quadrant {slot.Number}; it occupies {FormatSlots(anchoredPlacement)}.");
    }

    private SimulationResult RemoveFromOmniSlot(Entity target)
    {
        if (target is not ModuleInstance module || !module.HasComponent<InstalledInOmniSlotComponent>())
        {
            return Failure("Select a part installed in an Omni Slot to remove it.");
        }

        if (!_definitions.TryGetValue("omni_slot", out var omniDefinition))
        {
            return Failure("The Omni Slot part definition is missing.");
        }

        var slot = module.GetComponent<PositionComponent>().Quadrant;
        var restoredSlots = module.OccupiedSlots.ToArray();
        _mech.RemoveModule(module);
        foreach (var restoredSlot in restoredSlots)
        {
            _mech.AddModule(new ModuleInstance(omniDefinition, [restoredSlot]));
        }
        RefreshDerivedState();
        return Success($"Removed {module.Definition.Name}; the Omni Slot at quadrant {slot.Number} is empty again. No scrap is refunded.");
    }

    private SimulationResult AdvanceTime(AdvanceTimeCommand command)
    {
        if (!double.IsFinite(command.Hours) || command.Hours < 0 || !double.IsFinite(command.Hours * 60))
        {
            return Failure("Advance time requires a finite, non-negative number of hours.");
        }

        SimulateElapsedMinutes(command.Hours * 60);
        return Success($"Advanced time by {Format(command.Hours)} hour(s).");
    }

    private SimulationResult MovePlayer(MovePlayerCommand command)
    {
        if (!_mech.Layout.Contains(command.DestinationSlot))
        {
            return Failure($"Slot {command.DestinationSlot.Number} does not exist in the Medium mech.");
        }

        var previousModule = FindModuleAt(_player.CurrentSlot);
        _player.CurrentSlot = command.DestinationSlot;
        var destinationModule = FindModuleAt(_player.CurrentSlot);
        var message = $"Player moved to slot {_player.CurrentSlot.Number}.";
        if (destinationModule is not null && destinationModule.Id != previousModule?.Id &&
            destinationModule.TryGetComponent<NarratableComponent>(out var narration) && narration is not null)
        {
            message += $"\n\n{destinationModule.Name}\n{narration.Profile.OnEnter}";
        }

        return Success(message);
    }

    private ModuleInstance? FindModuleAt(SlotId slot) => _mech.Modules
        .FirstOrDefault(module => module.OccupiedSlots.Contains(slot));

    private ModuleInstance? FindModule(string reference)
    {
        if (Guid.TryParse(reference, out var instanceId))
        {
            return _mech.Modules.FirstOrDefault(module => module.InstanceId == instanceId);
        }

        return _mech.Modules.FirstOrDefault(module =>
            string.Equals(module.DefinitionId, reference, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(module.Definition.Name, reference, StringComparison.OrdinalIgnoreCase));
    }

    private void InstallStarterModules()
    {
        InstallStarter("engine_room", new SlotId(1), new SlotId(2));
        InstallStarter("water_processing", new SlotId(3));
        InstallStarter("bed", new SlotId(5));
        InstallStarter("cockpit", new SlotId(7));
        InstallStarter("hydroponics_small", new SlotId(8));
    }

    private void InstallStarter(string definitionId, params SlotId[] slotIds)
    {
        RemoveEmptyOmniSlots(slotIds);
        _mech.AddModule(new ModuleInstance(_definitions[definitionId], slotIds));
    }

    private void InstallDefaultOmniSlots()
    {
        if (!_definitions.TryGetValue("omni_slot", out var omniDefinition))
        {
            return;
        }

        foreach (var slot in _mech.Layout.Slots)
        {
            _mech.AddModule(new ModuleInstance(omniDefinition, [slot.Id]));
        }
    }

    private void RemoveEmptyOmniSlots(IEnumerable<SlotId> slots)
    {
        var slotSet = slots.ToHashSet();
        var emptyOmniSlots = _mech.Modules
            .Where(module => _mech.IsEmptyOmniSlot(module))
            .Where(module => module.OccupiedSlots.Any(slotSet.Contains))
            .ToArray();
        foreach (var emptyOmni in emptyOmniSlots)
        {
            _mech.RemoveModule(emptyOmni);
        }
    }

    private void RefreshDerivedState()
    {
        PowerSystem.Run(_state);
        WaterSystem.Run(_state, 0);
        HydroponicsSystem.Run(_state, 0);
        CleanupSystem.Run(_state);
        ConditionSystem.Run(_state);
    }

    private void SimulateElapsedMinutes(double minutes)
    {
        const double minutesPerTick = 10;
        const double epsilon = 1e-9;
        var remaining = minutes;
        while (remaining > epsilon)
        {
            var tickMinutes = Math.Min(minutesPerTick, remaining);
            SimulateTurn(tickMinutes);
            remaining -= tickMinutes;
        }
    }

    private SimulationResult Success(string message) => SimulationResult.Success(message, State);

    private SimulationResult Failure(string message) => SimulationResult.Failure(message, State);

    private static string Format(double value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static string FormatSlots(IEnumerable<SlotId> slotIds) => string.Join(", ", slotIds.Select(slot => slot.Number));
}
