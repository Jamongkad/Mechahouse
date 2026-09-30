using Mechahouse.Core;

namespace Mechahouse.Cli;

public sealed class CliApplication
{
    private readonly GameSimulation _simulation;
    private readonly CommandLineReader _commandLine;
    private readonly CliRenderer _renderer;
    private readonly CliCommandRouter _commandRouter;

    public CliApplication(GameSimulation simulation)
    {
        _simulation = simulation;
        _commandLine = new CommandLineReader();
        var playerAvatar = new AsciiPlayerAvatar("Player", '@');
        _renderer = new CliRenderer(playerAvatar);
        _commandRouter = new CliCommandRouter(_simulation, _commandLine, _renderer);
    }

    public void Run()
    {
        Console.WriteLine("MECHAHOUSE SIMULATION CLI");
        Console.WriteLine("Type 'help' for commands. Use Up/Down to browse command history.\n");
        _renderer.PrintDashboard(_simulation.State);

        while (true)
        {
            var line = _commandLine.ReadLine();
            if (line is null || _commandRouter.Handle(line))
            {
                break;
            }
        }
    }
}
