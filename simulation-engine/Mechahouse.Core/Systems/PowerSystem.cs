using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Systems;

public static class PowerSystem
{
    public static PowerSummary Run(GameState state)
    {
        foreach (var entity in state.Entities)
        {
            if (entity.TryGetComponent<PowerConsumerComponent>(out var consumer) && consumer is not null)
            {
                consumer.IsPowered = false;
            }

            if (entity.TryGetComponent<PowerGeneratorComponent>(out var generator) && generator is not null)
            {
                var integrity = entity.TryGetComponent<OperationalComponent>(out var operational) && operational is not null
                    ? operational.Durability / 100
                    : 1;
                generator.CurrentOutput = ComponentRules.IsOperational(entity) ? generator.Output * integrity : 0;
            }
        }

        var production = state.Entities
            .Where(entity => entity.TryGetComponent<PowerGeneratorComponent>(out _))
            .Sum(entity => entity.GetComponent<PowerGeneratorComponent>().CurrentOutput);

        var demand = state.Entities
            .Where(entity => entity.TryGetComponent<PowerConsumerComponent>(out _))
            .Where(ComponentRules.IsOperational)
            .Sum(entity => entity.GetComponent<PowerConsumerComponent>().RequiredPower);

        var remaining = production;
        foreach (var entity in state.Entities
                     .Where(entity => entity.TryGetComponent<PowerConsumerComponent>(out _))
                     .Where(ComponentRules.IsOperational)
                     .OrderBy(ComponentRules.SortKey))
        {
            var consumer = entity.GetComponent<PowerConsumerComponent>();
            if (consumer.RequiredPower <= 0 || remaining >= consumer.RequiredPower)
            {
                consumer.IsPowered = true;
                remaining -= consumer.RequiredPower;
            }
        }

        return Measure(state) with { Production = production, Demand = demand };
    }

    public static PowerSummary Measure(GameState state)
    {
        var production = state.Entities
            .Where(entity => entity.TryGetComponent<PowerGeneratorComponent>(out _))
            .Sum(entity => entity.GetComponent<PowerGeneratorComponent>().CurrentOutput);

        var demand = state.Entities
            .Where(entity => entity.TryGetComponent<PowerConsumerComponent>(out _))
            .Where(ComponentRules.IsOperational)
            .Sum(entity => entity.GetComponent<PowerConsumerComponent>().RequiredPower);

        var poweredDemand = state.Entities
            .Where(entity => entity.TryGetComponent<PowerConsumerComponent>(out var consumer) && consumer is not null && consumer.IsPowered)
            .Sum(entity => entity.GetComponent<PowerConsumerComponent>().RequiredPower);

        return new PowerSummary(production, demand, poweredDemand, production - poweredDemand);
    }
}

internal static class ComponentRules
{
    public static bool IsOperational(Entity entity)
    {
        return !entity.TryGetComponent<OperationalComponent>(out var operational) || operational is null || operational.IsOperational;
    }

    public static bool IsPowered(Entity entity)
    {
        return !entity.TryGetComponent<PowerConsumerComponent>(out var consumer) || consumer is null || consumer.IsPowered;
    }

    public static int SortKey(Entity entity)
    {
        return entity.TryGetComponent<PositionComponent>(out var position) && position is not null
            ? position.Quadrants.Min(quadrant => quadrant.Number)
            : int.MaxValue;
    }
}

public sealed record PowerSummary(double Production, double Demand, double PoweredDemand, double Available);
