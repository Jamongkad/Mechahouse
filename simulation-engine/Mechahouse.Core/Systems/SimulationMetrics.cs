using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Systems;

public static class SimulationMetrics
{
    public static MechMetrics Calculate(GameState state)
    {
        var power = PowerSystem.Measure(state);
        var water = WaterSystem.Measure(state);
        var food = FoodSystem.Measure(state);

        return new MechMetrics(
            state.Mech.UsedSlots,
            state.Mech.AvailableSlots,
            state.Mech.CurrentWeight,
            state.Mech.WeightCapacity,
            power.Production,
            power.Demand,
            power.PoweredDemand,
            power.Available,
            water.Capacity,
            water.Production,
            water.Consumption,
            food.Production,
            food.Consumption);
    }
}
