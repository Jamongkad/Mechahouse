using Mechahouse.Core;
using Mechahouse.Core.Commands;
using Mechahouse.Core.Definitions;
using Mechahouse.Core.Domain;
using Mechahouse.Core.Systems;

var tests = new (string Name, Action Body)[]
{
    ("building deducts scrap", BuildingDeductsScrap),
    ("building occupies slots", BuildingOccupiesSlots),
    ("building rejects insufficient space", BuildingRejectsInsufficientSpace),
    ("building rejects insufficient scrap", BuildingRejectsInsufficientScrap),
    ("module weight contributes to total", ModuleWeightContributes),
    ("disabled modules do not consume power", DisabledModulesDoNotConsumePower),
    ("powered hydroponics consumes water and produces food", PoweredHydroponicsChangesResources),
    ("hydroponics reports insufficient water", HydroponicsReportsInsufficientWater),
    ("advance time is deterministic", AdvanceTimeIsDeterministic),
    ("initial interior entities are componentized", InitialInteriorEntitiesAreComponentized),
    ("power system selects components rather than names", PowerSystemUsesComponents),
    ("engine outage propagates through resource systems", EngineOutagePropagates),
    ("core has no Godot dependency", CoreHasNoGodotDependency),
    ("medium layout numbers from bottom to top", MediumLayoutNumbersBottomToTop),
    ("explicit module placement is preserved", ExplicitModulePlacementIsPreserved),
    ("building requires the player to be present", BuildingRequiresPlayerPresence),
    ("parts catalog contains buildable modules", PartsCatalogContainsBuildableModules),
    ("starter modules carry arrival narration", StarterModulesCarryArrivalNarration),
    ("moving into a room displays its narration", MovingIntoRoomDisplaysNarration),
    ("moving within one room does not repeat narration", MovingWithinOneRoomDoesNotRepeatNarration),
    ("custom module narration is definition driven", CustomModuleNarrationIsDefinitionDriven),
    ("interaction choices come from module capabilities", InteractionChoicesComeFromCapabilities),
    ("critical components gate unsafe interactions", CriticalComponentsGateUnsafeInteractions),
    ("interaction intents require the current actor and location", InteractionIntentsRequireActorAndLocation),
    ("repair interaction changes examination narrative", RepairInteractionChangesExamination),
    ("critical engine narrative follows simulated integrity", CriticalEngineNarrativeFollowsIntegrity),
    ("starved hydroponics receives critical examination", StarvedHydroponicsReceivesCriticalExamination),
    ("toggle interaction feeds existing resource systems", ToggleInteractionFeedsResourceSystems),
    ("sleep interaction advances the simulation", SleepInteractionAdvancesSimulation),
    ("Omni Slot installation and removal preserve quadrant occupancy", OmniSlotInstallationAndRemoval)
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS  {test.Name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.WriteLine($"FAIL  {test.Name}: {exception.Message}");
    }
}

Console.WriteLine($"\n{tests.Length - failures}/{tests.Length} tests passed.");
return failures == 0 ? 0 : 1;

static void BuildingDeductsScrap()
{
    var simulation = new GameSimulation(installStarterModules: false);
    var result = simulation.Execute(new BuildModuleCommand("hydroponics_small", [new SlotId(4), new SlotId(5)]));
    Assert(result.Succeeded, result.Message);
    Assert(simulation.State.Mech.Scrap == 42, "Expected 42 scrap after an 8-scrap build.");
}

static void BuildingOccupiesSlots()
{
    var simulation = new GameSimulation(installStarterModules: false);
    var result = simulation.Execute(new BuildModuleCommand("hydroponics_small", [new SlotId(4), new SlotId(5)]));
    Assert(result.Succeeded, result.Message);
    Assert(simulation.State.Metrics.UsedSlots == 2, "Hydroponics should use two slots.");
}

static void BuildingRejectsInsufficientSpace()
{
    var simulation = new GameSimulation(installStarterModules: false);
    BuildAt(simulation, "omni_slot", 1);
    BuildAt(simulation, "omni_slot", 2);
    BuildAt(simulation, "omni_slot", 3);
    BuildAt(simulation, "omni_slot", 4);
    BuildAt(simulation, "omni_slot", 5);
    BuildAt(simulation, "omni_slot", 6);
    BuildAt(simulation, "omni_slot", 7);
    BuildAt(simulation, "omni_slot", 8);
    Assert(simulation.Execute(new MovePlayerCommand(new SlotId(9))).Succeeded, "Player should move to the last free slot.");

    var result = simulation.Execute(new BuildModuleCommand("hydroponics_small"));
    Assert(!result.Succeeded, "A two-slot module should not fit when only one slot remains.");
}

static void BuildingRejectsInsufficientScrap()
{
    var simulation = new GameSimulation(installStarterModules: false);
    BuildAt(simulation, "engine_room", 1, 2);
    BuildAt(simulation, "water_processing", 3, 6);
    BuildAt(simulation, "hydroponics_small", 8, 9);
    Assert(simulation.Execute(new MovePlayerCommand(new SlotId(4))).Succeeded, "Player should move to a free placement.");
    var result = simulation.Execute(new BuildModuleCommand("engine_room", [new SlotId(4), new SlotId(5)]));
    Assert(!result.Succeeded && result.Message.Contains("scrap", StringComparison.OrdinalIgnoreCase), "The additional engine room should fail for insufficient scrap.");
}

static void ModuleWeightContributes()
{
    var simulation = new GameSimulation(installStarterModules: false);
    BuildAt(simulation, "hydroponics_small", 4, 5);
    Assert(Math.Abs(simulation.State.Metrics.CurrentWeight - 5) < 0.001, "Hydroponics should add five weight.");
}

static void DisabledModulesDoNotConsumePower()
{
    var simulation = new GameSimulation(installStarterModules: false);
    BuildAt(simulation, "engine_room", 1, 2);
    BuildAt(simulation, "water_processing", 3, 6);
    var before = simulation.State.Metrics.PowerDemand;
    Assert(simulation.Execute(new ToggleModuleCommand("water_processing", false)).Succeeded, "Water Processing should disable.");
    var after = simulation.State.Metrics.PowerDemand;
    Assert(before == 3 && after == 0, "Disabled Water Processing should remove its demand.");
}

static void PoweredHydroponicsChangesResources()
{
    var simulation = CreatePoweredHydroponics();
    var result = simulation.Execute(new AdvanceTimeCommand(24));
    Assert(result.Succeeded, result.Message);
    Assert(simulation.State.Turn == 156 && simulation.State.Events.Count == 156, "Setup actions and a one-day wait should resolve as 156 ten-minute ticks.");
    Assert(Math.Abs(simulation.State.Mech.Water - 88.5) < 0.001, "Powered Water Processing should produce water during setup and the day while Hydroponics consumes it.");
    Assert(Math.Abs(simulation.State.Mech.Food - 24.0208333333) < 0.001, "Hydroponics should produce food during its build time and the following day, less crew consumption.");
}

static void HydroponicsReportsInsufficientWater()
{
    var simulation = new GameSimulation(installStarterModules: false);
    BuildAt(simulation, "hydroponics_small", 8, 9);
    Assert(simulation.Execute(new AdvanceTimeCommand(24 * 21)).Succeeded, "Advance should succeed.");
    var hydroponics = simulation.State.Entities.Single(entity => entity.Name == "Hydroponics");
    Assert(Math.Abs(simulation.State.Mech.Water) < 0.001, "Hydroponics should eventually exhaust the starting water supply.");
    Assert(!hydroponics.GetComponent<WaterConsumerComponent>().HasWater, "Hydroponics should report insufficient water after the reservoir is empty.");
}

static void AdvanceTimeIsDeterministic()
{
    var first = CreatePoweredHydroponics();
    var second = CreatePoweredHydroponics();
    first.Execute(new AdvanceTimeCommand(24));
    second.Execute(new AdvanceTimeCommand(24));
    Assert(Math.Abs(first.State.Mech.Water - second.State.Mech.Water) < 0.001, "Water should be deterministic.");
    Assert(Math.Abs(first.State.Mech.Food - second.State.Mech.Food) < 0.001, "Food should be deterministic.");
}

static void InitialInteriorEntitiesAreComponentized()
{
    var state = new GameSimulation().State;
    var names = state.Entities.Select(entity => entity.Name).ToHashSet(StringComparer.Ordinal);
    var expected = new[] { "Cockpit", "Bed", "Omni Slot", "Hydroponics", "Engine Room", "Water Processing" };

    Assert(names.SetEquals(expected), "The starter mech should contain exactly the six requested interior entities.");
    Assert(state.Entities.All(entity => entity.HasComponent<PositionComponent>()), "Every interior entity should have a position component.");

    var engine = state.Entities.Single(entity => entity.Name == "Engine Room");
    Assert(engine.HasComponent<PowerGeneratorComponent>(), "Engine Room should be composed with a power generator component.");

    var waterProcessing = state.Entities.Single(entity => entity.Name == "Water Processing");
    Assert(waterProcessing.HasComponent<PowerConsumerComponent>(), "Water Processing should be composed with a power consumer component.");
    Assert(waterProcessing.HasComponent<WaterProducerComponent>(), "Water Processing should be composed with a water producer component.");

    var hydroponics = state.Entities.Single(entity => entity.Name == "Hydroponics");
    Assert(hydroponics.HasComponent<WaterConsumerComponent>(), "Hydroponics should be composed with a water consumer component.");
    Assert(hydroponics.HasComponent<FoodProducerComponent>(), "Hydroponics should be composed with a food producer component.");
}

static void PowerSystemUsesComponents()
{
    var definitions = new[]
    {
        new ModuleDefinition("source", "Unrelated Source", 1, 0, 0, PowerProduction: 5),
        new ModuleDefinition("sink", "Unrelated Sink", 1, 0, 0, PowerConsumption: 5)
    }.ToDictionary(definition => definition.Id, StringComparer.OrdinalIgnoreCase);
    var simulation = new GameSimulation(installStarterModules: false, definitions);

    BuildAt(simulation, "source", 1);
    BuildAt(simulation, "sink", 2);

    var sink = simulation.State.Entities.Single(entity => entity.Name == "Unrelated Sink");
    Assert(sink.GetComponent<PowerConsumerComponent>().IsPowered, "Power should flow between component-compatible entities regardless of their names.");
}

static void EngineOutagePropagates()
{
    var simulation = new GameSimulation();
    Assert(simulation.Execute(new ToggleModuleCommand("engine_room", false)).Succeeded, "Engine Room should disable.");

    var waterProcessing = simulation.State.Entities.Single(entity => entity.Name == "Water Processing");
    Assert(!waterProcessing.GetComponent<PowerConsumerComponent>().IsPowered, "Water Processing should lose power when Engine Room is offline.");
    Assert(simulation.State.Metrics.WaterProduction == 0, "Water production should stop when Water Processing loses power.");

    Assert(simulation.Execute(new AdvanceTimeCommand(24 * 21)).Succeeded, "Advance should succeed.");
    var hydroponics = simulation.State.Entities.Single(entity => entity.Name == "Hydroponics");
    Assert(!hydroponics.GetComponent<WaterConsumerComponent>().HasWater, "Hydroponics should eventually become water-starved without upstream production.");
}

static void CoreHasNoGodotDependency()
{
    var godotReference = typeof(GameSimulation).Assembly
        .GetReferencedAssemblies()
        .FirstOrDefault(assembly => assembly.Name?.Contains("Godot", StringComparison.OrdinalIgnoreCase) == true);
    Assert(godotReference is null, "Core must not reference Godot.");
}

static void MediumLayoutNumbersBottomToTop()
{
    var layout = new GameSimulation(installStarterModules: false).State.Mech.Layout;
    Assert(layout.Find(new SlotId(1))?.Coordinate == new SlotCoordinate(0, 0), "Slot 1 should be bottom-left.");
    Assert(layout.Find(new SlotId(5))?.Coordinate == new SlotCoordinate(1, 1), "Slot 5 should be center.");
    Assert(layout.Find(new SlotId(9))?.Coordinate == new SlotCoordinate(2, 2), "Slot 9 should be top-right.");
    Assert(layout.AreContiguous([new SlotId(1), new SlotId(2)]), "Horizontal neighbors should be contiguous.");
    Assert(!layout.AreContiguous([new SlotId(3), new SlotId(4)]), "Row wrap should not be considered contiguous.");
}

static void ExplicitModulePlacementIsPreserved()
{
    var simulation = new GameSimulation(installStarterModules: false);
    Assert(simulation.Execute(new MovePlayerCommand(new SlotId(8))).Succeeded, "Player should move to slot 8.");
    var result = simulation.Execute(new BuildModuleCommand("hydroponics_small", [new SlotId(8), new SlotId(9)]));
    Assert(result.Succeeded, result.Message);
    var module = simulation.State.Mech.Modules.Single();
    Assert(module.OccupiedSlots.SequenceEqual([new SlotId(8), new SlotId(9)]), "The requested slot footprint should be preserved.");
}

static void BuildingRequiresPlayerPresence()
{
    var simulation = new GameSimulation(installStarterModules: false);
    var blocked = simulation.Execute(new BuildModuleCommand("hydroponics_small", [new SlotId(8), new SlotId(9)]));
    Assert(!blocked.Succeeded && blocked.Message.Contains("Player must be", StringComparison.Ordinal), "Building away from the player should fail.");
    Assert(simulation.Execute(new MovePlayerCommand(new SlotId(8))).Succeeded, "Player should move to the build footprint.");
    Assert(simulation.Execute(new BuildModuleCommand("hydroponics_small", [new SlotId(8), new SlotId(9)])).Succeeded, "Building should succeed when the player is present.");
}

static void PartsCatalogContainsBuildableModules()
{
    var partIds = new GameSimulation(installStarterModules: false)
        .Definitions
        .Select(definition => definition.Id)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    Assert(partIds.Contains("cockpit"), "Parts should include Cockpit.");
    Assert(partIds.Contains("bed"), "Parts should include Bed.");
    Assert(partIds.Contains("omni_slot"), "Parts should include Omni Slot.");
    Assert(partIds.Contains("engine_room"), "Parts should include Engine Room.");
    Assert(partIds.Contains("water_processing"), "Parts should include Water Processing.");
    Assert(partIds.Contains("hydroponics_small"), "Parts should include Hydroponics.");
    Assert(!partIds.Contains("autocannon"), "Parts should not include external combat weapons in the interior prototype.");
}

static void StarterModulesCarryArrivalNarration()
{
    var state = new GameSimulation().State;
    Assert(state.Entities.All(entity => entity.HasComponent<NarratableComponent>()), "Every starter interior module should have arrival narration.");
}

static void MovingIntoRoomDisplaysNarration()
{
    var simulation = new GameSimulation();
    var result = simulation.Execute(new MovePlayerCommand(new SlotId(7)));

    Assert(result.Succeeded, result.Message);
    Assert(result.Message.Contains("former hauler cab", StringComparison.OrdinalIgnoreCase), "Entering the Cockpit should display its arrival narration.");
}

static void MovingWithinOneRoomDoesNotRepeatNarration()
{
    var simulation = new GameSimulation();
    Assert(simulation.Execute(new MovePlayerCommand(new SlotId(1))).Succeeded, "Player should enter the Engine Room.");
    var result = simulation.Execute(new MovePlayerCommand(new SlotId(2)));

    Assert(result.Succeeded, result.Message);
    Assert(!result.Message.Contains("engine room meets you", StringComparison.OrdinalIgnoreCase), "Moving between quadrants of one room should not repeat its arrival narration.");
}

static void CustomModuleNarrationIsDefinitionDriven()
{
    var definitions = new Dictionary<string, ModuleDefinition>(StringComparer.OrdinalIgnoreCase)
    {
        ["custom_room"] = new ModuleDefinition(
            "custom_room",
            "Custom Room",
            1,
            0,
            0,
            Narration: new NarrationProfile("A custom room description supplied by its module definition."))
    };
    var simulation = new GameSimulation(installStarterModules: false, definitions: definitions);
    BuildAt(simulation, "custom_room", 5);
    Assert(simulation.Execute(new MovePlayerCommand(new SlotId(6))).Succeeded, "Player should leave the custom room.");
    var result = simulation.Execute(new MovePlayerCommand(new SlotId(5)));

    Assert(result.Message.Contains("custom room description", StringComparison.OrdinalIgnoreCase), "Custom module narration should be copied from its definition and displayed on entry.");
}

static void InteractionChoicesComeFromCapabilities()
{
    var state = new GameSimulation().State;
    var cockpit = state.Entities.Single(entity => entity.HasComponent<InteractionCapabilitiesComponent>() &&
        entity.GetComponent<InteractionCapabilitiesComponent>().Supports(InteractionType.Examine) &&
        entity.Name == "Cockpit");
    var bed = state.Entities.Single(entity => entity.Name == "Bed");
    var hydroponics = state.Entities.Single(entity => entity.HasComponent<FoodProducerComponent>());
    var engine = state.Entities.Single(entity => entity.HasComponent<PowerGeneratorComponent>());
    var waterProcessing = state.Entities.Single(entity => entity.HasComponent<WaterProducerComponent>());
    var omniSlot = state.Entities.Single(entity => entity.HasComponent<OmniSlotComponent>());

    Assert(InteractionSystem.GetAvailableInteractions(cockpit).SequenceEqual([InteractionType.Examine]), "Cockpit should only offer its implemented examination action.");
    Assert(InteractionSystem.GetAvailableInteractions(bed).SequenceEqual([InteractionType.Examine, InteractionType.Sleep]), "Bed should offer Examine and Sleep.");
    Assert(InteractionSystem.GetAvailableInteractions(hydroponics).SequenceEqual([InteractionType.Examine, InteractionType.Repair]), "Hydroponics should offer Examine and Repair.");
    Assert(InteractionSystem.GetAvailableInteractions(engine).SequenceEqual([InteractionType.Examine, InteractionType.Toggle, InteractionType.Repair]), "Engine Room should offer Examine, Toggle, and Repair.");
    Assert(InteractionSystem.GetAvailableInteractions(waterProcessing).SequenceEqual([InteractionType.Examine, InteractionType.Toggle, InteractionType.Repair]), "Water Processing should offer Examine, Toggle, and Repair.");
    Assert(InteractionSystem.GetAvailableInteractions(omniSlot).SequenceEqual([InteractionType.Examine, InteractionType.Install]), "An empty Omni Slot should offer Examine and Install.");
}

static void CriticalComponentsGateUnsafeInteractions()
{
    var definitions = ModuleCatalog.CreateDefault().ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    definitions["bed"] = definitions["bed"] with { InitialDurability = 30 };
    definitions["omni_slot"] = definitions["omni_slot"] with { InitialDurability = 30 };
    var state = new GameSimulation(definitions: definitions).State;
    var bed = state.Entities.Single(entity => entity.Name == "Bed");
    var omniSlot = state.Entities.Single(entity => entity.HasComponent<OmniSlotComponent>());
    var bedActions = InteractionSystem.GetAvailableInteractions(bed);
    var omniActions = InteractionSystem.GetAvailableInteractions(omniSlot);

    Assert(bedActions.Contains(InteractionType.Repair) && !bedActions.Contains(InteractionType.Sleep), "A critical bed should be repairable but unavailable for sleeping.");
    Assert(omniActions.Contains(InteractionType.Repair) && !omniActions.Contains(InteractionType.Install), "A critical Omni Slot should be repairable but unavailable for installation.");
}

static void RepairInteractionChangesExamination()
{
    var simulation = new GameSimulation();
    var engine = simulation.State.Entities.Single(entity => entity.HasComponent<PowerGeneratorComponent>());
    Assert(simulation.Execute(new MovePlayerCommand(new SlotId(1))).Succeeded, "Player should move to the engine room.");

    var worn = simulation.Execute(new InteractionIntent(simulation.State.Player.Id, engine.Id, InteractionType.Examine));
    var wornExamination = worn.Examination ?? throw new InvalidOperationException("Examine should return a report.");
    Assert(worn.Succeeded && wornExamination.Condition == EntityCondition.Worn, "The engine should begin in a worn state.");
    Assert(wornExamination.Observation.Contains("metallic scrape", StringComparison.OrdinalIgnoreCase), "Worn engine narrative should describe the scrape.");
    Assert(Math.Abs(simulation.State.Metrics.PowerProduction - 9.84) < 0.001, "Worn engine integrity should reduce its available power output.");

    var repaired = simulation.Execute(new InteractionIntent(simulation.State.Player.Id, engine.Id, InteractionType.Repair));
    Assert(repaired.Succeeded && Math.Abs(engine.GetComponent<OperationalComponent>().Durability - 100) < 0.001, "Repair should restore engine integrity.");
    Assert(Math.Abs(simulation.State.Metrics.PowerProduction - 12) < 0.001, "Repair should restore the engine's rated power output through PowerSystem.");
    var normal = simulation.Execute(new InteractionIntent(simulation.State.Player.Id, engine.Id, InteractionType.Examine));
    var normalExamination = normal.Examination ?? throw new InvalidOperationException("Examine should return a report.");
    Assert(normalExamination.Condition == EntityCondition.Normal, "The examination should reflect repaired integrity.");
    Assert(normalExamination.Observation.Contains("faint squeak", StringComparison.OrdinalIgnoreCase), "Normal engine narrative should retain its familiar sound.");
}

static void InteractionIntentsRequireActorAndLocation()
{
    var simulation = new GameSimulation();
    var engine = simulation.State.Entities.Single(entity => entity.HasComponent<PowerGeneratorComponent>());
    var remote = simulation.Execute(new InteractionIntent(simulation.State.Player.Id, engine.Id, InteractionType.Examine));
    Assert(!remote.Succeeded && remote.TimeAdvancedMinutes == 0, "The player should not examine a remote quadrant through the interaction system.");

    var bed = simulation.State.Entities.Single(entity => entity.Name == "Bed");
    var wrongActor = simulation.Execute(new InteractionIntent(Guid.NewGuid(), bed.Id, InteractionType.Sleep));
    Assert(!wrongActor.Succeeded && simulation.State.Clock.ElapsedMinutes == 0, "An intent from a different actor should not change the state.");
}

static void CriticalEngineNarrativeFollowsIntegrity()
{
    var definitions = ModuleCatalog.CreateDefault().ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    definitions["engine_room"] = definitions["engine_room"] with { InitialDurability = 30 };
    var simulation = new GameSimulation(definitions: definitions);
    var engine = simulation.State.Entities.Single(entity => entity.HasComponent<PowerGeneratorComponent>());
    Assert(simulation.Execute(new MovePlayerCommand(new SlotId(1))).Succeeded, "Player should move to the engine room.");

    var result = simulation.Execute(new InteractionIntent(simulation.State.Player.Id, engine.Id, InteractionType.Examine));
    var examination = result.Examination ?? throw new InvalidOperationException("Examine should return a report.");
    Assert(examination.Condition == EntityCondition.Critical, "Integrity below the critical threshold should be represented in simulation state.");
    Assert(examination.Observation.Contains("shriek", StringComparison.OrdinalIgnoreCase), "Critical engine narrative should describe the escalating sound.");
}

static void StarvedHydroponicsReceivesCriticalExamination()
{
    var simulation = new GameSimulation(installStarterModules: false);
    BuildAt(simulation, "hydroponics_small", 8, 9);
    Assert(simulation.Execute(new AdvanceTimeCommand(24 * 21)).Succeeded, "Advance should succeed.");
    var hydroponics = simulation.State.Entities.Single(entity => entity.HasComponent<FoodProducerComponent>());

    var result = simulation.Execute(new InteractionIntent(simulation.State.Player.Id, hydroponics.Id, InteractionType.Examine));
    var examination = result.Examination ?? throw new InvalidOperationException("Examine should return a report.");
    Assert(result.Succeeded && examination.Condition == EntityCondition.Critical, "Dry hydroponics should be described as critical.");
    Assert(examination.Observation.Contains("dry", StringComparison.OrdinalIgnoreCase), "Critical hydroponics narrative should mention dry trays.");
    Assert(examination.Facts.Any(fact => fact.Label == "Water supply" && fact.Value == "Insufficient"), "Examination should include the current water fact.");
}

static void ToggleInteractionFeedsResourceSystems()
{
    var simulation = new GameSimulation();
    Assert(simulation.Execute(new MovePlayerCommand(new SlotId(3))).Succeeded, "Player should move to Water Processing.");
    var waterProcessing = simulation.State.Entities.Single(entity => entity.HasComponent<WaterProducerComponent>());

    var result = simulation.Execute(new InteractionIntent(simulation.State.Player.Id, waterProcessing.Id, InteractionType.Toggle));
    Assert(result.Succeeded, result.Message);
    Assert(!waterProcessing.GetComponent<OperationalComponent>().Enabled, "Toggle intent should disable Water Processing.");
    Assert(waterProcessing.GetComponent<WaterProducerComponent>().CurrentOutput == 0, "The water system should observe the disabled producer.");
    Assert(simulation.State.Metrics.WaterProduction == 0, "Derived resource metrics should reflect the interaction.");
}

static void SleepInteractionAdvancesSimulation()
{
    var simulation = new GameSimulation();
    var bed = simulation.State.Entities.Single(entity => entity.Name == "Bed");
    var startMinute = simulation.State.Clock.ElapsedMinutes;

    var result = simulation.Execute(new InteractionIntent(simulation.State.Player.Id, bed.Id, InteractionType.Sleep));
    Assert(result.Succeeded, result.Message);
    Assert(Math.Abs(simulation.State.Clock.ElapsedMinutes - startMinute - 480) < 0.001, "Sleeping should advance eight in-game hours.");
    Assert(simulation.State.Turn == 48, "Sleep should run the ordinary 10-minute simulation ticks.");
    Assert(simulation.State.Mech.Food > 20, "Hydroponics should continue producing food during sleep.");
}

static void OmniSlotInstallationAndRemoval()
{
    var simulation = new GameSimulation();
    Assert(simulation.Execute(new MovePlayerCommand(new SlotId(4))).Succeeded, "Player should move to the Omni Slot.");
    var omni = simulation.State.Entities.Single(entity => entity.HasComponent<OmniSlotComponent>());

    var installed = simulation.Execute(new InteractionIntent(simulation.State.Player.Id, omni.Id, InteractionType.Install, "bed"));
    Assert(installed.Succeeded, installed.Message);
    Assert(simulation.State.Mech.Scrap == 45, "Installation should consume the part's ordinary build cost.");
    var addedBed = simulation.State.Mech.Modules.Single(module => module.OccupiedSlots.Contains(new SlotId(4)));
    Assert(addedBed.DefinitionId == "bed" && addedBed.HasComponent<InstalledInOmniSlotComponent>(), "Installed part should occupy the Omni quadrant and retain its removal capability.");
    Assert(InteractionSystem.GetAvailableInteractions(addedBed).Contains(InteractionType.Remove), "An installed part should expose Remove.");

    var removed = simulation.Execute(new InteractionIntent(simulation.State.Player.Id, addedBed.Id, InteractionType.Remove));
    Assert(removed.Succeeded, removed.Message);
    var restoredOmni = simulation.State.Mech.Modules.Single(module => module.OccupiedSlots.Contains(new SlotId(4)));
    Assert(restoredOmni.DefinitionId == "omni_slot", "Removing a part should restore an empty Omni Slot in the same quadrant.");
    Assert(simulation.State.Mech.Scrap == 45, "Removing a part should not create an implicit scrap refund.");
}

static GameSimulation CreatePoweredHydroponics()
{
    var simulation = new GameSimulation(installStarterModules: false);
    BuildAt(simulation, "engine_room", 1, 2);
    BuildAt(simulation, "water_processing", 3, 6);
    BuildAt(simulation, "hydroponics_small", 8, 9);
    return simulation;
}

static void BuildAt(GameSimulation simulation, string definitionId, params int[] slotNumbers)
{
    Assert(slotNumbers.Length > 0, "A build footprint must contain at least one slot.");
    Assert(simulation.Execute(new MovePlayerCommand(new SlotId(slotNumbers[0]))).Succeeded, "Player should move to the build footprint.");
    var slots = slotNumbers.Select(slotNumber => new SlotId(slotNumber)).ToArray();
    var result = simulation.Execute(new BuildModuleCommand(definitionId, slots));
    Assert(result.Succeeded, result.Message);
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
