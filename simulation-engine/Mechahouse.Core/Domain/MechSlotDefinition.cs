namespace Mechahouse.Core.Domain;

public sealed record MechSlotDefinition(
    SlotId Id,
    SlotCoordinate Coordinate,
    string Band);
