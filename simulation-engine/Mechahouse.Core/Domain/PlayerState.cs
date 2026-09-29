namespace Mechahouse.Core.Domain;

public sealed class PlayerState
{
    internal PlayerState(SlotId currentSlot)
    {
        Id = Guid.NewGuid();
        CurrentSlot = currentSlot;
    }

    public Guid Id { get; }

    public SlotId CurrentSlot { get; internal set; }
}
