using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Systems;

public static class FoodSystem
{
    public const double CrewConsumptionPerDay = 1;

    public static double Run(GameState state, double hours)
    {
        var consumption = CrewConsumptionPerDay * hours / 24;
        state.Mech.Food = Math.Max(0, state.Mech.Food - consumption);
        return consumption;
    }

    public static FoodSummary Measure(GameState state)
    {
        var production = state.Entities
            .Where(entity => entity.HasComponent<FoodProducerComponent>())
            .Sum(entity => entity.GetComponent<FoodProducerComponent>().CurrentOutput);

        return new FoodSummary(production, CrewConsumptionPerDay);
    }
}

public sealed record FoodSummary(double Production, double Consumption);
