using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Systems;

public static class CleanupSystem
{
    public static void Run(GameState state)
    {
        state.Mech.Water = Math.Clamp(state.Mech.Water, 0, state.Mech.WaterCapacity);
        state.Mech.Food = Math.Max(0, state.Mech.Food);
        state.Metrics = SimulationMetrics.Calculate(state);
    }
}
