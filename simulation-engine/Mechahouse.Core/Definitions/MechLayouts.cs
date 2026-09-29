using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Definitions;

public static class MechLayouts
{
    public static MechLayout Medium { get; } = new(
    [
        new MechSlotDefinition(new SlotId(1), new SlotCoordinate(0, 0), "bottom"),
        new MechSlotDefinition(new SlotId(2), new SlotCoordinate(1, 0), "bottom"),
        new MechSlotDefinition(new SlotId(3), new SlotCoordinate(2, 0), "bottom"),
        new MechSlotDefinition(new SlotId(4), new SlotCoordinate(0, 1), "middle"),
        new MechSlotDefinition(new SlotId(5), new SlotCoordinate(1, 1), "middle"),
        new MechSlotDefinition(new SlotId(6), new SlotCoordinate(2, 1), "middle"),
        new MechSlotDefinition(new SlotId(7), new SlotCoordinate(0, 2), "top"),
        new MechSlotDefinition(new SlotId(8), new SlotCoordinate(1, 2), "top"),
        new MechSlotDefinition(new SlotId(9), new SlotCoordinate(2, 2), "top")
    ]);
}
