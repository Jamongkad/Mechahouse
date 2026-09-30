using Mechahouse.Cli;
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
    ("thermal telemetry follows system state", ThermalTelemetryFollowsSystemState),
    ("initial interior entities are componentized", InitialInteriorEntitiesAreComponentized),
    ("power system selects components rather than names", PowerSystemUsesComponents),
    ("engine outage propagates through resource systems", EngineOutagePropagates),
    ("core has no Godot dependency", CoreHasNoGodotDependency),
    ("medium layout numbers from bottom to top", MediumLayoutNumbersBottomToTop),
    ("unoccupied quadrants default to Omni Slots", UnoccupiedQuadrantsDefaultToOmniSlots),
    ("explicit module placement is preserved", ExplicitModulePlacementIsPreserved),
    ("building requires the player to be present", BuildingRequiresPlayerPresence),
    ("parts catalog contains buildable modules", PartsCatalogContainsBuildableModules),
    ("storage is a one-slot usable inventory module", StorageIsUsableInventoryModule),
    ("starter modules carry arrival narration", StarterModulesCarryArrivalNarration),
    ("moving into a room displays its narration", MovingIntoRoomDisplaysNarration),
    ("moving within one room does not repeat narration", MovingWithinOneRoomDoesNotRepeatNarration),
    ("custom module narration is definition driven", CustomModuleNarrationIsDefinitionDriven),
    ("engine toggle narration follows its powered state", EngineToggleNarrationFollowsPoweredState),
    ("unpowered toggle narration does not claim machinery started", UnpoweredToggleNarrationDoesNotClaimStartup),
    ("interaction choices come from module capabilities", InteractionChoicesComeFromCapabilities),
    ("critical components gate unsafe interactions", CriticalComponentsGateUnsafeInteractions),
    ("interaction intents require the current actor and location", InteractionIntentsRequireActorAndLocation),
    ("repair interaction changes examination narrative", RepairInteractionChangesExamination),
    ("critical engine narrative follows simulated integrity", CriticalEngineNarrativeFollowsIntegrity),
    ("starved hydroponics receives critical examination", StarvedHydroponicsReceivesCriticalExamination),
    ("toggle interaction feeds existing resource systems", ToggleInteractionFeedsResourceSystems),
    ("sleep interaction advances the simulation", SleepInteractionAdvancesSimulation),
    ("Omni Slot installation and removal preserve quadrant occupancy", OmniSlotInstallationAndRemoval),
    ("interior layout has a fixed console shape", InteriorLayoutHasFixedConsoleShape),
    ("interior avatar follows the player between quadrants", InteriorAvatarFollowsPlayer),
    ("interior layout renders every starter module glyph", InteriorLayoutRendersStarterGlyphs),
    ("interior water tank follows simulated reserves", InteriorWaterTankFollowsReserves),
    ("interior layout marks worn and offline modules", InteriorLayoutMarksModuleStatus),
    ("interior engine footprint connects across cell boundaries", InteriorEngineFootprintConnects),
    ("starter interior layout matches its console snapshot", StarterInteriorLayoutMatchesSnapshot),
    ("rendering the interior does not mutate simulation state", InteriorRenderingDoesNotMutateState)
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
    for (var slot = 1; slot <= 8; slot++)
    {
        BuildAt(simulation, "bed", slot);
    }

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

static void ThermalTelemetryFollowsSystemState()
{
    var simulation = new GameSimulation();
    var initial = simulation.State.Metrics;

    Assert(initial.OilTemperatureC > initial.CoolantTemperatureC, "Running engine oil should be hotter than coolant.");
    Assert(initial.OilPressureKPa > 0, "A running engine should report oil pressure.");
    Assert(initial.WaterTemperatureC > ThermalSystem.AmbientTemperatureC, "The active water processor should warm process water.");

    Assert(simulation.Execute(new ToggleModuleCommand("engine_room", false)).Succeeded, "Engine Room should disable.");
    Assert(simulation.Execute(new AdvanceTimeCommand(8)).Succeeded, "The simulation should advance while the engine is offline.");

    Assert(simulation.State.Metrics.OilTemperatureC < initial.OilTemperatureC, "Oil temperature should fall after shutdown.");
    Assert(simulation.State.Metrics.OilPressureKPa < initial.OilPressureKPa, "Oil pressure should fall after shutdown.");
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
    var module = simulation.State.Mech.Modules.Single(candidate => candidate.DefinitionId == "hydroponics_small");
    Assert(module.OccupiedSlots.SequenceEqual([new SlotId(8), new SlotId(9)]), "The requested slot footprint should be preserved.");
}

static void UnoccupiedQuadrantsDefaultToOmniSlots()
{
    var simulation = new GameSimulation(installStarterModules: false);
    Assert(simulation.State.Mech.Modules.Count == simulation.State.Mech.SlotCapacity, "Every empty quadrant should have a default Omni Slot entity.");
    Assert(simulation.State.Mech.Modules.All(module => module.DefinitionId == "omni_slot"), "Default empty quadrants should be represented as Omni Slots.");
    Assert(simulation.State.Mech.UsedSlots == 0 && simulation.State.Mech.AvailableSlots == simulation.State.Mech.SlotCapacity, "Default Omni Slots should remain available for construction.");
    Assert(RenderInterior(simulation.State).Take(14).Count(line => line.Contains('.', StringComparison.Ordinal)) == 3, "The cutaway should show an Omni marker in every empty cell row that contains Omni glyphs.");
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
    Assert(partIds.Contains("storage"), "Parts should include Storage.");
    Assert(!partIds.Contains("autocannon"), "Parts should not include external combat weapons in the interior prototype.");

    var installableIds = new GameSimulation(installStarterModules: false)
        .GetInstallablePartDefinitions()
        .Select(definition => definition.Id)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    Assert(installableIds.IsSupersetOf(["cockpit", "bed", "engine_room", "water_processing", "hydroponics_small", "storage"]), "Every module type should be available from an Omni Slot.");
}

static void StorageIsUsableInventoryModule()
{
    var simulation = new GameSimulation(installStarterModules: false);
    var definition = simulation.Definitions.Single(candidate => candidate.Id == "storage");
    Assert(definition.SlotsRequired == 1, "Storage should occupy exactly one quadrant.");
    Assert(definition.BuildCost == 3, "Storage should cost three scrap to build.");
    Assert(Math.Abs(definition.Weight - 1) < 0.001, "Storage should add one unit of weight.");
    Assert(simulation.GetInstallablePartDefinitions().Any(candidate => candidate.Id == "storage"), "Storage should fit in an Omni Slot.");

    BuildAt(simulation, "omni_slot", 5);
    var emptyOmni = simulation.State.Entities.Single(entity => entity.HasComponent<OmniSlotComponent>() &&
        entity.GetComponent<PositionComponent>().Quadrant == new SlotId(5));
    var installation = simulation.Execute(new InteractionIntent(
        simulation.State.Player.Id,
        emptyOmni.Id,
        InteractionType.Install,
        "storage"));
    Assert(installation.Succeeded, installation.Message);
    var storage = simulation.State.Mech.Modules.Single(module => module.DefinitionId == "storage");
    Assert(storage.HasComponent<StorageComponent>(), "The built module should carry storage data.");
    Assert(storage.HasComponent<InstalledInOmniSlotComponent>(), "Storage should be installed on top of an Omni Slot.");
    Assert(simulation.State.Mech.Scrap == 47, "Building Storage should deduct exactly three scrap.");
    Assert(Math.Abs(simulation.State.Metrics.CurrentWeight - 1) < 0.001, "Built Storage should contribute one unit of mech weight.");
    Assert(CellText(RenderInterior(simulation.State), 5).Contains("[] []", StringComparison.Ordinal), "Storage should have a recognizable locker glyph.");

    var examination = ExamineSystem.Examine(simulation.State, storage);
    Assert(examination.Facts.Any(fact => fact.Label == "Storage" && fact.Value == "Scrap, Food"), "Examining Storage should identify the resources it holds.");
    Assert(examination.AvailableInteractions.Contains(InteractionType.Use), "Operational Storage should offer the Use interaction.");

    var result = simulation.Execute(new InteractionIntent(simulation.State.Player.Id, storage.Id, InteractionType.Use));
    var expectedFood = simulation.State.Mech.Food.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    Assert(result.Succeeded, result.Message);
    Assert(result.Message.Contains("Scrap: 47", StringComparison.Ordinal), "Using Storage should report the manually counted scrap.");
    Assert(result.Message.Contains($"Food: {expectedFood}", StringComparison.Ordinal), "Using Storage should report the manually counted food.");
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

static void EngineToggleNarrationFollowsPoweredState()
{
    var simulation = new GameSimulation();

    var shutdown = simulation.Execute(new ToggleModuleCommand("engine_room", false));
    Assert(shutdown.Succeeded, shutdown.Message);
    Assert(shutdown.Message.Contains("cylinder", StringComparison.OrdinalIgnoreCase), "Engine shutdown should describe its cylinders falling silent.");
    Assert(shutdown.Message.Contains("silence", StringComparison.OrdinalIgnoreCase), "Engine shutdown should end in silence.");
    Assert(!shutdown.Message.Equals("Engine Room disabled.", StringComparison.Ordinal), "Engine shutdown should not use the generic status message.");

    var startup = simulation.Execute(new ToggleModuleCommand("engine_room", true));
    Assert(startup.Succeeded, startup.Message);
    Assert(startup.Message.Contains("creaking behemoth", StringComparison.OrdinalIgnoreCase), "Engine startup should describe the mech coming alive.");
    Assert(startup.Message.Contains("cylinders fire", StringComparison.OrdinalIgnoreCase), "Engine startup should describe its cylinders cascading into motion.");
    Assert(!startup.Message.Equals("Engine Room enabled.", StringComparison.Ordinal), "Engine startup should not use the generic status message.");
}

static void UnpoweredToggleNarrationDoesNotClaimStartup()
{
    var simulation = new GameSimulation();
    Assert(simulation.Execute(new ToggleModuleCommand("engine_room", false)).Succeeded, "Engine Room should disable.");
    Assert(simulation.Execute(new ToggleModuleCommand("water_processing", false)).Succeeded, "Water Processing should disable.");

    var result = simulation.Execute(new ToggleModuleCommand("water_processing", true));
    Assert(result.Succeeded, result.Message);
    Assert(result.Message.Contains("does not answer", StringComparison.OrdinalIgnoreCase), "An unpowered pump should fail to answer its switch.");
    Assert(result.Message.Contains("without power", StringComparison.OrdinalIgnoreCase), "The narration should explain why the enabled pump remains still.");
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
    Assert(result.Message.Contains("patient beat stutters", StringComparison.OrdinalIgnoreCase), "Toggling the purifier through an interaction should use its authored shutdown narration.");
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

    var expandable = new GameSimulation(installStarterModules: false);
    BuildAt(expandable, "omni_slot", 4);
    var emptyOmni = expandable.State.Entities.Single(entity => entity.HasComponent<OmniSlotComponent>() &&
        entity.GetComponent<PositionComponent>().Quadrant == new SlotId(4));
    var installedEngine = expandable.Execute(new InteractionIntent(
        expandable.State.Player.Id,
        emptyOmni.Id,
        InteractionType.Install,
        "engine_room"));
    Assert(installedEngine.Succeeded, installedEngine.Message);
    var engine = expandable.State.Mech.Modules.Single(module => module.DefinitionId == "engine_room");
    Assert(engine.OccupiedSlots.Count == 2 && engine.OccupiedSlots.Contains(new SlotId(4)), "A multi-slot module should expand from the Omni Slot while retaining its anchor quadrant.");
    Assert(engine.HasComponent<InstalledInOmniSlotComponent>(), "A multi-slot module installed from an Omni Slot should retain removal capability.");

    var removedEngine = expandable.Execute(new InteractionIntent(
        expandable.State.Player.Id,
        engine.Id,
        InteractionType.Remove));
    Assert(removedEngine.Succeeded, removedEngine.Message);
    Assert(expandable.State.Mech.Modules.Count == expandable.State.Mech.SlotCapacity &&
           expandable.State.Mech.Modules.All(module => module.DefinitionId == "omni_slot" && module.OccupiedSlots.Count == 1), "Removing an expanded Omni installation should restore every freed quadrant as an Omni Slot.");
}

static void InteriorLayoutHasFixedConsoleShape()
{
    var layout = RenderInterior(new GameSimulation().State);

    Assert(layout.Count == 15, $"Expected 15 layout rows, received {layout.Count}.");
    Assert(layout.All(line => line.Length == 31), "Every interior row should be exactly 31 characters wide.");
    Assert(layout.Take(14).Sum(line => line.Count(character => character == '@')) == 1, "The cutaway itself should contain exactly one player avatar.");
    Assert(layout[14] == "@ YOU Q5  ! WORN  x OFFLINE    ", "The final row should be the padded console legend.");
}

static void InteriorAvatarFollowsPlayer()
{
    var simulation = new GameSimulation();
    var bedLayout = RenderInterior(simulation.State);
    Assert(bedLayout[8][15] == '@', "The player should begin on the bed's open floor in quadrant 5.");

    Assert(simulation.Execute(new MovePlayerCommand(new SlotId(8))).Succeeded, "Player should move to Hydroponics.");
    var hydroponicsLayout = RenderInterior(simulation.State);
    Assert(hydroponicsLayout[2][15] == '@', "Hydroponics should place the player near the top center of quadrant 8.");
    Assert(hydroponicsLayout.Take(14).Sum(line => line.Count(character => character == '@')) == 1, "Moving should not leave a duplicate avatar behind.");

    Assert(simulation.Execute(new MovePlayerCommand(new SlotId(1))).Succeeded, "Player should move to the Engine Room's left quadrant.");
    var leftEngineLayout = RenderInterior(simulation.State);
    Assert(leftEngineLayout[12][9] == '@', "The engine's left machinery segment should use its lower-right safe anchor.");

    Assert(simulation.Execute(new MovePlayerCommand(new SlotId(2))).Succeeded, "Player should move within the Engine Room footprint.");
    var rightEngineLayout = RenderInterior(simulation.State);
    Assert(rightEngineLayout[12][19] == '@', "The avatar should move to the selected quadrant within a multi-slot room.");
    Assert(rightEngineLayout.Take(14).Sum(line => line.Count(character => character == '@')) == 1, "A multi-slot room should still render exactly one avatar.");
}

static void InteriorLayoutRendersStarterGlyphs()
{
    var layout = RenderInterior(new GameSimulation().State);

    Assert(CellText(layout, 7).Contains("/\\", StringComparison.Ordinal), "Cockpit should have a viewport silhouette.");
    Assert(CellText(layout, 8).Contains("|||||", StringComparison.Ordinal), "Hydroponics should have growing trays.");
    Assert(CellText(layout, 5).Contains("_____", StringComparison.Ordinal), "Bed should have a bunk silhouette.");
    Assert(CellText(layout, 6).Contains("~~~~~", StringComparison.Ordinal), "Water Processing should have filled tank bands.");
    Assert(CellText(layout, 1).Contains("[=**=]", StringComparison.Ordinal), "Engine Room should have machinery.");
    Assert(CellText(layout, 4).Contains('.', StringComparison.Ordinal), "Omni Slot should have a centered empty-space marker.");
}

static void InteriorWaterTankFollowsReserves()
{
    var simulation = new GameSimulation();
    var before = CellLines(RenderInterior(simulation.State), 6).Count(line => line.Contains("~~~~~", StringComparison.Ordinal));
    Assert(before == 2, "An 80% tank should show two filled water bands.");

    Assert(simulation.Execute(new ToggleModuleCommand("water_processing", false)).Succeeded, "Water Processing should disable.");
    Assert(simulation.Execute(new AdvanceTimeCommand(24 * 8)).Succeeded, "Time should advance while the purifier is shut down.");

    var after = CellLines(RenderInterior(simulation.State), 6).Count(line => line.Contains("~~~~~", StringComparison.Ordinal));
    Assert(after == 1, "The depleted tank should drop to one filled water band.");
    Assert(simulation.State.Mech.Water < 50, "The simulated reserve should be below half capacity after eight days without purification.");
}

static void InteriorLayoutMarksModuleStatus()
{
    var simulation = new GameSimulation();
    var wornLayout = RenderInterior(simulation.State);
    Assert(CellLines(wornLayout, 1)[0][8] == '!', "The worn Engine Room should carry a top-right warning marker.");
    Assert(CellLines(wornLayout, 8)[0][8] == '!', "Worn Hydroponics should carry a top-right warning marker.");

    Assert(simulation.Execute(new ToggleModuleCommand("engine_room", false)).Succeeded, "Engine Room should disable.");
    var offlineLayout = RenderInterior(simulation.State);
    Assert(CellLines(offlineLayout, 1)[0][8] == 'x', "A disabled module should use the offline marker instead of the worn marker.");
    Assert(CellLines(offlineLayout, 6)[0][8] == 'x', "An unpowered consumer should be marked offline.");
}

static void InteriorEngineFootprintConnects()
{
    var layout = RenderInterior(new GameSimulation().State);
    var machineryRow = layout[11];

    Assert(machineryRow == "|  [=**=]--[=**=]   | |~~~~~| |", "The two engine glyphs should join across their shared machinery boundary.");
    Assert(machineryRow[10] == '-', "The shared horizontal Engine Room boundary should be a connector.");
    Assert(machineryRow[20] == '|' && machineryRow[30] == '|', "Unrelated cell boundaries should remain intact.");
    Assert(layout[2][10] == '|' && layout[2][20] == '|', "Hydroponics cells should retain ordinary grid boundaries.");
    Assert(layout[9] == "+---------+---------+---------+", "The vertical Water Processing footprint should retain its row divider.");
}

static void StarterInteriorLayoutMatchesSnapshot()
{
    var expected = new[]
    {
        "         M-07 INTERIOR         ",
        "+---------+---------+---------+",
        "|   /\\    | \" \" \" \"!| \" \" \" \"!|",
        "|  /__\\   |  |||||  |  |||||  |",
        "|  |[]|   |  |||||  |  |||||  |",
        "+---------+---------+---------+",
        "|         |  _____  | |     |!|",
        "|    .    | |_____| | |~~~~~| |",
        "|         |    @    | |~~~~~| |",
        "+---------+---------+---------+",
        "|  [====]!|[====]  !| |     |!|",
        "|  [=**=]--[=**=]   | |~~~~~| |",
        "|  [====] |[====]   | |~~~~~| |",
        "+---------+---------+---------+",
        "@ YOU Q5  ! WORN  x OFFLINE    "
    };

    var actual = RenderInterior(new GameSimulation().State);
    Assert(actual.SequenceEqual(expected), $"Starter layout snapshot changed.\nExpected:\n{string.Join('\n', expected)}\nActual:\n{string.Join('\n', actual)}");
}

static void InteriorRenderingDoesNotMutateState()
{
    var state = new GameSimulation().State;
    var playerSlot = state.Player.CurrentSlot;
    var water = state.Mech.Water;
    var modules = state.Mech.Modules
        .OrderBy(module => module.InstanceId)
        .Select(ModuleStateSignature)
        .ToArray();

    _ = RenderInterior(state);

    Assert(state.Player.CurrentSlot == playerSlot, "Rendering should not move the player.");
    Assert(Math.Abs(state.Mech.Water - water) < 0.001, "Rendering should not change stored water.");
    Assert(state.Mech.Modules.OrderBy(module => module.InstanceId).Select(ModuleStateSignature).SequenceEqual(modules), "Rendering should not change module positions, durability, or enabled state.");
}

static IReadOnlyList<string> RenderInterior(GameState state) =>
    new InteriorRenderer(new AsciiPlayerAvatar("Player", '@'), new ConsolePanelRenderer()).RenderLayout(state);

static IReadOnlyList<string> CellLines(IReadOnlyList<string> layout, int slotNumber)
{
    var coordinate = MechLayouts.Medium.Find(new SlotId(slotNumber))?.Coordinate
        ?? throw new InvalidOperationException($"Unknown slot {slotNumber}.");
    var outputRow = 2 + (2 - coordinate.Row) * 4;
    var outputColumn = 1 + coordinate.Column * 10;
    return Enumerable.Range(0, ModuleGlyphRenderer.CellHeight)
        .Select(index => layout[outputRow + index].Substring(outputColumn, ModuleGlyphRenderer.CellWidth))
        .ToArray();
}

static string CellText(IReadOnlyList<string> layout, int slotNumber) => string.Join('\n', CellLines(layout, slotNumber));

static string ModuleStateSignature(ModuleInstance module) =>
    $"{module.InstanceId}:{string.Join(',', module.OccupiedSlots.Select(slot => slot.Number))}:{module.Durability:R}:{module.Enabled}";

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
