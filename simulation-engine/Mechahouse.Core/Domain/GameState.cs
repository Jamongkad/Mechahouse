using Mechahouse.Core.Systems;

namespace Mechahouse.Core.Domain;

public sealed class GameState
{
    private readonly List<string> _events = [];

    internal GameState(MechState mech, PlayerState player)
    {
        Mech = mech;
        Player = player;
    }

    public MechState Mech { get; }

    public PlayerState Player { get; }

    public IReadOnlyList<Entity> Entities => Mech.Entities;

    public GameClock Clock { get; } = new();

    public int Turn { get; internal set; }

    public IReadOnlyList<string> Events => _events;

    public MechMetrics Metrics { get; internal set; } = MechMetrics.Empty;

    internal void RecordEvent(string message) => _events.Add(message);
}
