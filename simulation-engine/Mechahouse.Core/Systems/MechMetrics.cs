namespace Mechahouse.Core.Systems;

public sealed record MechMetrics(
    int UsedSlots,
    int AvailableSlots,
    double CurrentWeight,
    double WeightCapacity,
    double PowerProduction,
    double PowerDemand,
    double PoweredDemand,
    double PowerAvailable,
    double WaterCapacity,
    double WaterProduction,
    double WaterConsumption,
    double FoodProduction,
    double FoodConsumption)
{
    public static MechMetrics Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}
