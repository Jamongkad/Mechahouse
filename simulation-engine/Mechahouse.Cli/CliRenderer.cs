using Mechahouse.Core.Domain;

namespace Mechahouse.Cli;

public sealed class CliRenderer
{
    private readonly TelemetryRenderer _telemetryRenderer;
    private readonly InteriorRenderer _interiorRenderer;
    private readonly ModuleListRenderer _moduleListRenderer;
    private readonly ExaminationRenderer _examinationRenderer;

    public CliRenderer(AsciiPlayerAvatar playerAvatar)
    {
        var consolePanels = new ConsolePanelRenderer();
        _telemetryRenderer = new TelemetryRenderer(consolePanels);
        _interiorRenderer = new InteriorRenderer(playerAvatar, consolePanels);
        _moduleListRenderer = new ModuleListRenderer();
        _examinationRenderer = new ExaminationRenderer();
    }

    public void PrintMessage(string message) => Console.WriteLine(message);

    public void PrintResult(SimulationResult result, bool showClock = true)
    {
        Console.WriteLine(result.Message);
        if (showClock && result.Succeeded && result.TimeAdvancedMinutes > 0)
        {
            Console.WriteLine($"Time: {FormatGameTime(result.State.Clock)} (+{result.TimeAdvancedMinutes:0.##} min)");
        }

        if (result.Examination is not null)
        {
            _examinationRenderer.Print(result.Examination);
        }
    }

    public void PrintDashboard(GameState state)
    {
        _telemetryRenderer.Print(state);
        _interiorRenderer.PrintDetails(state);
    }

    public void PrintStatus(GameState state) => _telemetryRenderer.Print(state);

    public void PrintInterior(GameState state) => _interiorRenderer.PrintLayout(state);

    public void PrintInteriorDetails(GameState state) => _interiorRenderer.PrintDetails(state);

    public void PrintModules(IReadOnlyList<ModuleInstance> modules) => _moduleListRenderer.PrintModules(modules);

    public void PrintParts(IEnumerable<ModuleDefinition> definitions) => _moduleListRenderer.PrintParts(definitions);

    public void PrintHelp()
    {
        Console.WriteLine("status");
        Console.WriteLine("interior");
        Console.WriteLine("move <slot-number>");
        Console.WriteLine("modules");
        Console.WriteLine("parts");
        Console.WriteLine("build <definition-id> [slot-number ...]");
        Console.WriteLine("disable <module-id>");
        Console.WriteLine("enable <module-id>");
        Console.WriteLine("repair <module-id>");
        Console.WriteLine("examine <entity-name|definition-id|quadrant>");
        Console.WriteLine("interact [entity-name|definition-id|quadrant]");
        Console.WriteLine("interact <target> <examine|use|toggle|repair|sleep|install|remove> [part-id]");
        Console.WriteLine("  Examples: examine 8; interact storage use; interact engine_room repair; interact 4 install bed; interact 4 remove");
        Console.WriteLine("wait <hours> (or advance <hours>)");
        Console.WriteLine("Successful moves/toggles cost 10 minutes; builds/repairs cost 30.");
        Console.WriteLine("Interactions: examine/use are free; toggle/remove cost 10 minutes; repair/install cost 30; sleep advances 8 hours.");
        Console.WriteLine("Inspection commands are free.");
        Console.WriteLine("Up/Down: browse command history");
        Console.WriteLine("quit");
    }

    private static string FormatGameTime(GameClock clock) => $"Day {clock.DayNumber}, {clock.HourOfDay:00}:{clock.MinuteOfHour:00}";
}
