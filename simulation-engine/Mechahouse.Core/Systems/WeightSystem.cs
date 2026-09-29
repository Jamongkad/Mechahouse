using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Systems;

public static class WeightSystem
{
    public static double Calculate(MechState state) => state.CurrentWeight;
}
