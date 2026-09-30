using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Systems;

public static class ThermalSystem
{
    public const double AmbientTemperatureC = 21;

    public static void Initialize(GameState state)
    {
        var targets = GetTargets(state);
        state.Mech.OilTemperatureC = targets.OilTemperatureC;
        state.Mech.CoolantTemperatureC = targets.CoolantTemperatureC;
        state.Mech.OilPressureKPa = targets.OilPressureKPa;
        state.Mech.WaterTemperatureC = targets.WaterTemperatureC;
        state.Mech.CabinTemperatureC = targets.CabinTemperatureC;
    }

    public static ThermalSummary Run(GameState state, double hours)
    {
        if (!double.IsFinite(hours) || hours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hours), "Thermal time must be finite and non-negative.");
        }

        var targets = GetTargets(state);

        // Oil, coolant, and process water have different thermal inertia. A
        // zero-hour refresh leaves the current reading alone; simulation ticks
        // move the readings smoothly toward the conditions produced by the
        // systems.
        var oilResponse = 1 - Math.Exp(-hours / 4.0);
        var coolantResponse = 1 - Math.Exp(-hours / 1.5);
        var waterResponse = 1 - Math.Exp(-hours / 2.0);
        var cabinResponse = 1 - Math.Exp(-hours / 6.0);
        state.Mech.OilTemperatureC = Approach(state.Mech.OilTemperatureC, targets.OilTemperatureC, oilResponse);
        state.Mech.CoolantTemperatureC = Approach(state.Mech.CoolantTemperatureC, targets.CoolantTemperatureC, coolantResponse);
        state.Mech.OilPressureKPa = Approach(state.Mech.OilPressureKPa, targets.OilPressureKPa, coolantResponse);
        state.Mech.WaterTemperatureC = Approach(state.Mech.WaterTemperatureC, targets.WaterTemperatureC, waterResponse);
        state.Mech.CabinTemperatureC = Approach(state.Mech.CabinTemperatureC, targets.CabinTemperatureC, cabinResponse);

        return new ThermalSummary(
            state.Mech.OilTemperatureC,
            state.Mech.CoolantTemperatureC,
            state.Mech.OilPressureKPa,
            state.Mech.WaterTemperatureC,
            state.Mech.CabinTemperatureC);
    }

    private static ThermalTargets GetTargets(GameState state)
    {
        var engineLoad = state.Entities
            .Where(entity => entity.TryGetComponent<PowerGeneratorComponent>(out _))
            .Select(entity =>
            {
                var generator = entity.GetComponent<PowerGeneratorComponent>();
                return generator.Output <= 0 ? 0 : generator.CurrentOutput / generator.Output;
            })
            .DefaultIfEmpty(0)
            .Max();

        var waterProcessorLoad = state.Entities
            .Where(entity => entity.TryGetComponent<WaterProducerComponent>(out _))
            .Select(entity =>
            {
                var producer = entity.GetComponent<WaterProducerComponent>();
                return producer.Output <= 0 ? 0 : producer.CurrentOutput / producer.Output;
            })
            .DefaultIfEmpty(0)
            .Max();

        var normalizedEngineLoad = Math.Clamp(engineLoad, 0, 1);
        var normalizedWaterProcessorLoad = Math.Clamp(waterProcessorLoad, 0, 1);
        return new ThermalTargets(
            AmbientTemperatureC + 101 * normalizedEngineLoad,
            AmbientTemperatureC + 85 * normalizedEngineLoad,
            normalizedEngineLoad <= 0 ? 0 : 220 + 112 * normalizedEngineLoad,
            AmbientTemperatureC + 26 * normalizedWaterProcessorLoad,
            17 + 3 * normalizedEngineLoad);
    }

    private static double Approach(double current, double target, double response) =>
        current + (target - current) * Math.Clamp(response, 0, 1);
}

public sealed record ThermalSummary(
    double OilTemperatureC,
    double CoolantTemperatureC,
    double OilPressureKPa,
    double WaterTemperatureC,
    double CabinTemperatureC);

internal sealed record ThermalTargets(
    double OilTemperatureC,
    double CoolantTemperatureC,
    double OilPressureKPa,
    double WaterTemperatureC,
    double CabinTemperatureC);
