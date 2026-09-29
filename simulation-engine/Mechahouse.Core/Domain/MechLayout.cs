namespace Mechahouse.Core.Domain;

public sealed class MechLayout
{
    private readonly IReadOnlyList<MechSlotDefinition> _slots;

    internal MechLayout(IEnumerable<MechSlotDefinition> slots)
    {
        _slots = slots.OrderBy(slot => slot.Id.Number).ToArray();
    }

    public IReadOnlyList<MechSlotDefinition> Slots => _slots;

    public MechSlotDefinition? Find(SlotId id) => _slots.FirstOrDefault(slot => slot.Id == id);

    public bool Contains(SlotId id) => Find(id) is not null;

    public bool AreContiguous(IReadOnlyCollection<SlotId> slotIds)
    {
        if (slotIds.Count == 0 || slotIds.Distinct().Count() != slotIds.Count || slotIds.Any(id => !Contains(id)))
        {
            return false;
        }

        var remaining = new HashSet<SlotId>(slotIds);
        var visited = new HashSet<SlotId>();
        var pending = new Queue<SlotId>();
        pending.Enqueue(slotIds.First());

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            foreach (var neighbor in OrthogonalNeighbors(current).Where(remaining.Contains))
            {
                pending.Enqueue(neighbor);
            }
        }

        return visited.Count == slotIds.Count;
    }

    public IReadOnlyList<SlotId>? FindFirstAvailablePlacement(
        IReadOnlyCollection<SlotId> occupied,
        int slotsRequired,
        SlotId? requiredSlot = null)
    {
        if (slotsRequired <= 0 || slotsRequired > _slots.Count)
        {
            return null;
        }

        foreach (var candidate in GetCombinations(_slots.Select(slot => slot.Id).ToArray(), slotsRequired))
        {
            if (candidate.All(slot => !occupied.Contains(slot)) &&
                (requiredSlot is null || candidate.Contains(requiredSlot.Value)) &&
                AreContiguous(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private IEnumerable<SlotId> OrthogonalNeighbors(SlotId id)
    {
        var coordinate = Find(id)!.Coordinate;
        return _slots
            .Where(slot =>
                Math.Abs(slot.Coordinate.Column - coordinate.Column) +
                Math.Abs(slot.Coordinate.Row - coordinate.Row) == 1)
            .Select(slot => slot.Id);
    }

    private static IEnumerable<IReadOnlyList<SlotId>> GetCombinations(IReadOnlyList<SlotId> source, int count)
    {
        var current = new List<SlotId>(count);
        return BuildCombinations(source, count, 0, current);
    }

    private static IEnumerable<IReadOnlyList<SlotId>> BuildCombinations(
        IReadOnlyList<SlotId> source,
        int count,
        int start,
        List<SlotId> current)
    {
        if (current.Count == count)
        {
            yield return current.ToArray();
            yield break;
        }

        for (var index = start; index < source.Count; index++)
        {
            current.Add(source[index]);
            foreach (var combination in BuildCombinations(source, count, index + 1, current))
            {
                yield return combination;
            }

            current.RemoveAt(current.Count - 1);
        }
    }
}
