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

    public IReadOnlyList<ModuleInstance> Modules => _modules;

    public IReadOnlyList<Entity> Entities => _modules;

    public int UsedSlots => _modules.Sum(module => module.OccupiedSlots.Count);

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

        return _modules.All(module => module.OccupiedSlots.All(slot => !slotIds.Contains(slot)));
    }

    internal IReadOnlyList<SlotId>? FindFirstAvailablePlacement(int slotsRequired, SlotId? requiredSlot = null)
    {
        return Layout.FindFirstAvailablePlacement(_modules.SelectMany(module => module.OccupiedSlots).ToArray(), slotsRequired, requiredSlot);
    }
}
