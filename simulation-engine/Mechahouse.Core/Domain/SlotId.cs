namespace Mechahouse.Core.Domain;

public readonly record struct SlotId(int Number)
{
    public string StableId => $"medium.slot.{Number:00}";

    public override string ToString() => StableId;
}
