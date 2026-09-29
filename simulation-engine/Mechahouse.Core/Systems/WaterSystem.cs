using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Systems;

public static class WaterSystem
{
    public static WaterSummary Run(GameState state, double hours)
    {
        var dayFraction = hours / 24;
        foreach (var entity in state.Entities)
        {
            if (entity.TryGetComponent<WaterProducerComponent>(out var producer) && producer is not null)
            {
                producer.CurrentOutput = ComponentRules.IsOperational(entity) && ComponentRules.IsPowered(entity)
                    ? producer.Output
                    : 0;
            }
        }

        var production = state.Entities
            .Where(entity => entity.TryGetComponent<WaterProducerComponent>(out _))
            .Sum(entity => entity.GetComponent<WaterProducerComponent>().CurrentOutput);

        state.Mech.Water = Math.Clamp(
            state.Mech.Water + production * dayFraction,
            0,
            state.Mech.WaterCapacity);

        var consumption = 0d;
        foreach (var entity in state.Entities
                     .Where(entity => entity.TryGetComponent<WaterConsumerComponent>(out _))
                     .OrderBy(ComponentRules.SortKey))
        {
            var consumer = entity.GetComponent<WaterConsumerComponent>();
            consumer.HasWater = false;

            if (!ComponentRules.IsOperational(entity) || !ComponentRules.IsPowered(entity))
            {
                continue;
            }

            var requested = consumer.RequiredWater * dayFraction;
            if (requested <= 0)
            {
                consumer.HasWater = state.Mech.Water >= consumer.RequiredWater;
                continue;
            }

            if (state.Mech.Water < requested)
            {
                continue;
            }

            state.Mech.Water -= requested;
            consumer.HasWater = true;
            consumption += consumer.RequiredWater;
        }

        return new WaterSummary(state.Mech.WaterCapacity, production, consumption);
    }

    public static WaterSummary Measure(GameState state)
    {
        var production = state.Entities
            .Where(entity => entity.TryGetComponent<WaterProducerComponent>(out _))
            .Sum(entity => entity.GetComponent<WaterProducerComponent>().CurrentOutput);

        var consumption = state.Entities
            .Where(entity => entity.TryGetComponent<WaterConsumerComponent>(out _))
            .Where(ComponentRules.IsOperational)
            .Where(ComponentRules.IsPowered)
            .Sum(entity => entity.GetComponent<WaterConsumerComponent>().RequiredWater);

        return new WaterSummary(state.Mech.WaterCapacity, production, consumption);
    }
}

public sealed record WaterSummary(double Capacity, double Production, double Consumption);
