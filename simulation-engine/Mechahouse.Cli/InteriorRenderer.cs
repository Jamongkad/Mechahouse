using Mechahouse.Core.Domain;

namespace Mechahouse.Cli;

public sealed class InteriorRenderer
{
    private const int GridWidth = 31;
    private readonly AsciiPlayerAvatar _playerAvatar;
    private readonly ConsolePanelRenderer _consolePanels;
    private readonly ModuleGlyphRenderer _glyphs;

    public InteriorRenderer(AsciiPlayerAvatar playerAvatar, ConsolePanelRenderer consolePanels)
    {
        _playerAvatar = playerAvatar;
        _consolePanels = consolePanels;
        _glyphs = new ModuleGlyphRenderer();
    }

    public void PrintLayout(GameState state)
    {
        foreach (var line in RenderLayout(state))
        {
            Console.WriteLine(line);
        }
    }

    public IReadOnlyList<string> RenderLayout(GameState state)
    {
        var moduleBySlot = new Dictionary<SlotId, ModuleInstance>();
        foreach (var module in state.Mech.Modules)
        {
            foreach (var slot in module.OccupiedSlots)
            {
                moduleBySlot[slot] = module;
            }
        }

        var renderedCells = state.Mech.Layout.Slots.ToDictionary(
            definition => definition.Id,
            definition => _glyphs.RenderCell(
                state,
                moduleBySlot.GetValueOrDefault(definition.Id),
                definition.Id,
                state.Player.CurrentSlot == definition.Id ? _playerAvatar.Glyph : null));

        var lines = new List<string>(15)
        {
            Center("M-07 INTERIOR"),
            GridDivider()
        };

        for (var row = 2; row >= 0; row--)
        {
            for (var glyphRow = 0; glyphRow < ModuleGlyphRenderer.CellHeight; glyphRow++)
            {
                var line = "|";
                for (var column = 0; column < 3; column++)
                {
                    var slot = FindSlot(state, column, row);
                    line += renderedCells[slot][glyphRow];
                    line += column == 2
                        ? "|"
                        : IsEngineConnector(moduleBySlot, slot, FindSlot(state, column + 1, row), glyphRow) ? "-" : "|";
                }

                lines.Add(line);
            }

            lines.Add(GridDivider());
        }

        lines.Add(Pad($"{_playerAvatar.Glyph} YOU Q{state.Player.CurrentSlot.Number}  ! WORN  x OFFLINE"));
        return lines;
    }

    public void PrintDetails(GameState state)
    {
        var headers = new[] { "Room", "Quadrant", "Condition", "Power", "Water", "Food" };
        var rows = state.Entities
            .OrderBy(entity => entity.GetComponent<PositionComponent>().Quadrants.Min(slot => slot.Number))
            .Select(entity => new[]
            {
                entity.Name,
                FormatSlots(entity.GetComponent<PositionComponent>().Quadrants),
                FormatCondition(entity),
                FormatPower(entity),
                FormatWater(entity),
                FormatFood(entity)
            })
            .ToArray();

        _consolePanels.PrintTable("MECH CONSOLE // INTERIOR SYSTEMS", headers, rows);
    }

    private static string FormatCondition(Entity entity)
    {
        var condition = entity.TryGetComponent<EntityConditionComponent>(out var entityCondition) && entityCondition is not null
            ? entityCondition.Condition.ToString().ToUpperInvariant()
            : "—";
        var integrity = entity.TryGetComponent<OperationalComponent>(out var operational) && operational is not null
            ? $"{operational.Durability:0.#}%"
            : "—";
        return $"{condition} ({integrity})";
    }

    private static string FormatPower(Entity entity)
    {
        var values = new List<string>();
        if (entity.TryGetComponent<PowerGeneratorComponent>(out var generator) && generator is not null)
        {
            values.Add($"OUT {generator.CurrentOutput:0.##}/{generator.Output:0.##} kW");
        }

        if (entity.TryGetComponent<PowerConsumerComponent>(out var consumer) && consumer is not null)
        {
            values.Add(consumer.IsPowered ? "ONLINE" : "OFFLINE");
        }

        return values.Count == 0 ? "—" : string.Join(" / ", values);
    }

    private static string FormatWater(Entity entity)
    {
        var values = new List<string>();
        if (entity.TryGetComponent<WaterProducerComponent>(out var producer) && producer is not null)
        {
            values.Add($"OUT {producer.CurrentOutput:0.##} L/day");
        }

        if (entity.TryGetComponent<WaterConsumerComponent>(out var consumer) && consumer is not null)
        {
            values.Add(consumer.HasWater ? "SUPPLIED" : "INSUFFICIENT");
        }

        return values.Count == 0 ? "—" : string.Join(" / ", values);
    }

    private static string FormatFood(Entity entity) =>
        entity.TryGetComponent<FoodProducerComponent>(out var producer) && producer is not null
            ? $"OUT {producer.CurrentOutput:0.##}/day"
            : "—";

    private static string FormatSlots(IEnumerable<SlotId> slots) => string.Join(",", slots.Select(slot => slot.Number));

    private static SlotId FindSlot(GameState state, int column, int row) => state.Mech.Layout.Slots
        .Single(definition => definition.Coordinate == new SlotCoordinate(column, row))
        .Id;

    private static bool IsEngineConnector(
        IReadOnlyDictionary<SlotId, ModuleInstance> moduleBySlot,
        SlotId leftSlot,
        SlotId rightSlot,
        int glyphRow)
    {
        if (glyphRow != 1 ||
            !moduleBySlot.TryGetValue(leftSlot, out var left) ||
            !moduleBySlot.TryGetValue(rightSlot, out var right))
        {
            return false;
        }

        return ReferenceEquals(left, right) && left.DefinitionId == "engine_room";
    }

    private static string GridDivider() => "+---------+---------+---------+";

    private static string Center(string text)
    {
        var leftPadding = (GridWidth - text.Length) / 2;
        return text.PadLeft(text.Length + leftPadding).PadRight(GridWidth);
    }

    private static string Pad(string text) => text.PadRight(GridWidth);
}
