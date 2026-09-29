using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Systems;

public static class HydroponicsSystem
{
    public static double Run(GameState state, double hours)
    {
        foreach (var entity in state.Entities)
        {
            if (entity.TryGetComponent<FoodProducerComponent>(out var producer) && producer is not null)
            {
                producer.CurrentOutput = 0;
            }
        }

        var production = state.Entities
            .Where(entity => entity.HasComponent<WaterConsumerComponent>() && entity.HasComponent<FoodProducerComponent>())
            .Where(ComponentRules.IsOperational)
            .Where(ComponentRules.IsPowered)
            .Where(entity => entity.GetComponent<WaterConsumerComponent>().HasWater)
            .Sum(entity => Activate(entity));

        state.Mech.Food = Math.Max(0, state.Mech.Food + production * hours / 24);
        return production;
    }

    private static double Activate(Entity entity)
    {
        var producer = entity.GetComponent<FoodProducerComponent>();
        producer.CurrentOutput = producer.Output;
        return producer.CurrentOutput;
    }
}
