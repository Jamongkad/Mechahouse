namespace Mechahouse.Core.Domain;

public sealed class MechState
{
    private readonly List<ModuleInstance> _modules = [];
    private readonly double _waterCapacity;

    internal MechState(
        ChassisType chassis,
        MechLayout layout,
        double weightCapacity,
        double waterCapacity,
        int scrap,
        double water,
        double food)
    {
        Chassis = chassis;
        Layout = layout;
        WeightCapacity = weightCapacity;
        _waterCapacity = waterCapacity;
        Scrap = scrap;
        Water = water;
        Food = food;
    }

    public ChassisType Chassis { get; }

    public MechLayout Layout { get; }

    public int SlotCapacity => Layout.Slots.Count;

    public double WeightCapacity { get; }

    public int Scrap { get; internal set; }

    public double Water { get; internal set; }

    public double Food { get; internal set; }

    // These are deliberately kept as sensor readings rather than extra inventory
    // resources. The thermal system moves them toward operating temperatures as
    // the engine and water processor run.
    public double OilTemperatureC { get; internal set; } = 72;

    public double CoolantTemperatureC { get; internal set; } = 75;

    public double OilPressureKPa { get; internal set; } = 250;

    public double WaterTemperatureC { get; internal set; } = 18;

    public double CabinTemperatureC { get; internal set; } = 19;

    public IReadOnlyList<ModuleInstance> Modules => _modules;

    public IReadOnlyList<Entity> Entities => _modules;

    public int UsedSlots => _modules
        .Where(module => !IsEmptyOmniSlot(module))
        .Sum(module => module.OccupiedSlots.Count);

    public int AvailableSlots => SlotCapacity - UsedSlots;

    public double CurrentWeight => _modules.Sum(module => module.Definition.Weight);

    public double WaterCapacity => Math.Max(_waterCapacity, _modules.Sum(module => module.Definition.WaterCapacity));

    internal void AddModule(ModuleInstance module) => _modules.Add(module);

    internal bool RemoveModule(ModuleInstance module) => _modules.Remove(module);

    internal bool IsSlotPlacementAvailable(IReadOnlyCollection<SlotId> slotIds)
    {
        if (slotIds.Count == 0 || slotIds.Distinct().Count() != slotIds.Count || !Layout.AreContiguous(slotIds))
        {
            return false;
        }

        return _modules
            .Where(module => !IsEmptyOmniSlot(module))
            .All(module => module.OccupiedSlots.All(slot => !slotIds.Contains(slot)));
    }

    internal IReadOnlyList<SlotId>? FindFirstAvailablePlacement(int slotsRequired, SlotId? requiredSlot = null)
    {
        var occupied = _modules
            .Where(module => !IsEmptyOmniSlot(module))
            .SelectMany(module => module.OccupiedSlots)
            .ToArray();
        return Layout.FindFirstAvailablePlacement(occupied, slotsRequired, requiredSlot);
    }

    internal bool IsEmptyOmniSlot(ModuleInstance module) =>
        module.DefinitionId == "omni_slot" && !module.HasComponent<InstalledInOmniSlotComponent>();
}
