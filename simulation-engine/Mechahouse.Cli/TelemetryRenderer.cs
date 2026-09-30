using System.Globalization;
using Mechahouse.Core.Domain;
using Mechahouse.Core.Systems;

namespace Mechahouse.Cli;

public sealed class TelemetryRenderer
{
    private readonly ConsolePanelRenderer _consolePanels;

    public TelemetryRenderer(ConsolePanelRenderer consolePanels)
    {
        _consolePanels = consolePanels;
    }

    public void Print(GameState state)
    {
        var mech = state.Mech;
        var metrics = state.Metrics;
        var load = metrics.WeightCapacity <= 0 ? 0 : metrics.CurrentWeight / metrics.WeightCapacity * 100;
        var waterNet = metrics.WaterProduction - metrics.WaterConsumption;
        var lines = new List<string>
        {
            FormatTitle(state),
            string.Empty,
            FormatSectionHeaders("PROPULSION", "POWER"),
            FormatPair("Gross Weight", $"{metrics.CurrentWeight:0.0} t", "Generator", $"{metrics.PowerProduction:0.0} kW"),
            FormatPair("Rated Load", $"{metrics.WeightCapacity:0.0} t", "Demand", $"{metrics.PowerDemand:0.0} kW"),
            FormatPair("Load", $"{load:0.0}%", "Reserve", FormatSigned(metrics.PowerAvailable, "0.0") + " kW"),
            string.Empty,
            FormatSectionHeaders("ENGINE", "WATER"),
            FormatPair("Oil Temp", $"{metrics.OilTemperatureC:0} C", "Fresh Tank", $"{mech.Water:0} L"),
            FormatPair("Coolant Temp", $"{metrics.CoolantTemperatureC:0} C", "Consumption", $"{metrics.WaterConsumption:0.##} L/day"),
            FormatPair("Oil Pressure", $"{metrics.OilPressureKPa:0} kPa", "Purifier", FormatSigned(metrics.WaterProduction, "0.##") + " L/day"),
            FormatPair("", "", "Net", FormatSigned(waterNet, "0.##") + " L/day"),
            string.Empty,
            FormatSectionHeaders("HABITAT", "STORES"),
            FormatPair("Cabin Temp", $"{metrics.CabinTemperatureC:0} C", "Food", "???"),
            FormatPair("Air Quality", FormatAirQuality(state), "Scrap", "???"),
            FormatPair("Water Temp", $"{metrics.WaterTemperatureC:0} C", "Parts", "???"),
            string.Empty,
            "RANGE ESTIMATE",
            FormatPair("Water", FormatWaterRange(mech, metrics), null, null),
            FormatPair("Food", "MANUAL INVENTORY REQUIRED", null, null),
            string.Empty,
            new string('-', 72)
        };

        lines.AddRange(GetWarnings(metrics, load));
        Console.WriteLine();
        _consolePanels.PrintFrame("MECH CONSOLE // TELEMETRY", lines);
    }

    private static string FormatTitle(GameState state)
    {
        var title = "# M-07 \"HOME\"";
        var clock = $"DAY {state.Clock.DayNumber:000}  {state.Clock.HourOfDay:00}:{state.Clock.MinuteOfHour:00}";
        return title.PadRight(58) + clock;
    }

    private static string FormatSectionHeaders(string left, string right) => $"{left,-38}{right}";

    private static string FormatPair(string leftLabel, string? leftValue, string? rightLabel, string? rightValue)
    {
        var left = string.IsNullOrEmpty(leftLabel) ? string.Empty : FormatMetric(leftLabel, leftValue ?? string.Empty);
        var right = string.IsNullOrEmpty(rightLabel) ? string.Empty : FormatMetric(rightLabel, rightValue ?? string.Empty);
        return $"{left,-38}{right}";
    }

    private static string FormatMetric(string label, string value) => $"{label,-16} .... {value,8}";

    private static string FormatSigned(double value, string format) =>
        (value >= 0 ? "+" : "") + value.ToString(format, CultureInfo.InvariantCulture);

    private static string FormatAirQuality(GameState state) =>
        state.Metrics.PowerAvailable >= 0 && state.Mech.Water > 0 ? "NOMINAL" : "DEGRADED";

    private static string FormatWaterRange(MechState mech, MechMetrics metrics)
    {
        var net = metrics.WaterProduction - metrics.WaterConsumption;
        if (net > 0.001)
        {
            return $"STABLE ({FormatSigned(net, "0.##")} L/day)";
        }

        var dailyDraw = Math.Abs(net) > 0.001 ? Math.Abs(net) : metrics.WaterConsumption;
        return dailyDraw > 0.001
            ? $"{mech.Water / dailyDraw:0.0} days"
            : "NO CONSUMPTION";
    }

    private static IEnumerable<string> GetWarnings(MechMetrics metrics, double load)
    {
        var warnings = new List<string>();
        if (load >= 90)
        {
            warnings.Add("WARNING: VEHICLE LOAD APPROACHING CHASSIS LIMIT");
        }

        if (metrics.PowerAvailable < 0)
        {
            warnings.Add("WARNING: POWER RESERVE NEGATIVE");
        }

        if (metrics.OilTemperatureC >= 110 || metrics.CoolantTemperatureC >= 100)
        {
            warnings.Add("WARNING: ENGINE TEMPERATURE HIGH");
        }

        if (metrics.WaterTemperatureC >= 60)
        {
            warnings.Add("WARNING: FRESH WATER TEMPERATURE HIGH");
        }

        if (warnings.Count == 0)
        {
            warnings.Add("STATUS: SYSTEMS NOMINAL");
        }

        return warnings;
    }
}
