using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Systems;

public static class RepairSystem
{
    public static RepairSummary Run(Entity entity, double amount)
    {
        if (!double.IsFinite(amount) || amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Repair amount must be finite and greater than zero.");
        }

        var operational = entity.GetComponent<OperationalComponent>();
        var previous = operational.Durability;
        operational.Durability = Math.Min(100, previous + amount);
        return new RepairSummary(previous, operational.Durability);
    }
}

public sealed record RepairSummary(double PreviousDurability, double CurrentDurability);
