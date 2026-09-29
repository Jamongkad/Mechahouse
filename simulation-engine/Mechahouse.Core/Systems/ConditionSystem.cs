using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Systems;

public static class ConditionSystem
{
    public const double CriticalIntegrityThreshold = 35;
    public const double WornIntegrityThreshold = 85;

    public static void Run(GameState state)
    {
        foreach (var entity in state.Entities)
        {
            if (!entity.TryGetComponent<EntityConditionComponent>(out var condition) || condition is null)
            {
                continue;
            }

            condition.Condition = DetermineCondition(state, entity);
        }
    }

    private static EntityCondition DetermineCondition(GameState state, Entity entity)
    {
        if (entity.TryGetComponent<OperationalComponent>(out var operational) && operational is not null &&
            operational.Durability <= CriticalIntegrityThreshold)
        {
            return EntityCondition.Critical;
        }

        if (entity.TryGetComponent<WaterConsumerComponent>(out var water) && water is not null && !water.HasWater)
        {
            return state.Mech.Water <= 0.001 ? EntityCondition.Critical : EntityCondition.Worn;
        }

        if (entity.TryGetComponent<OperationalComponent>(out operational) && operational is not null &&
            operational.Durability < WornIntegrityThreshold)
        {
            return EntityCondition.Worn;
        }

        return EntityCondition.Normal;
    }
}
