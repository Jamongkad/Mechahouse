using Mechahouse.Core.Domain;

namespace Mechahouse.Cli;

public sealed class ModuleGlyphRenderer
{
    public const int CellWidth = 9;
    public const int CellHeight = 3;

    public IReadOnlyList<string> RenderCell(
        GameState state,
        ModuleInstance? module,
        SlotId slot,
        char? playerGlyph = null)
    {
        var glyph = SelectGlyph(state, module, slot);
        var lines = glyph.Lines.Select(line => line.ToCharArray()).ToArray();

        if (module is not null)
        {
            lines[0][CellWidth - 1] = StatusMarker(module);
        }

        if (playerGlyph is not null)
        {
            lines[glyph.PlayerRow][glyph.PlayerColumn] = playerGlyph.Value;
        }

        return lines.Select(line => new string(line)).ToArray();
    }

    private static CellGlyph SelectGlyph(GameState state, ModuleInstance? module, SlotId slot)
    {
        if (module is null)
        {
            return new CellGlyph(
            [
                "         ",
                "    .    ",
                "         "
            ], 2, 4);
        }

        return module.DefinitionId switch
        {
            "cockpit" => new CellGlyph(
            [
                "   /\\    ",
                "  /__\\   ",
                "  |[]|   "
            ], 2, 7),
            "hydroponics_small" => new CellGlyph(
            [
                " \" \" \" \" ",
                "  |||||  ",
                "  |||||  "
            ], 0, 4),
            "bed" => new CellGlyph(
            [
                "  _____  ",
                " |_____| ",
                "         "
            ], 2, 4),
            "water_processing" => WaterTankGlyph(state),
            "engine_room" => EngineGlyph(state, module, slot),
            "omni_slot" => new CellGlyph(
            [
                "         ",
                "    .    ",
                "         "
            ], 2, 4),
            "storage" => new CellGlyph(
            [
                " .-----. ",
                " |[] []| ",
                " '-----' "
            ], 2, 8),
            _ => new CellGlyph(
            [
                "  [???]  ",
                "  |:::|  ",
                "  [___]  "
            ], 2, 4)
        };
    }

    private static CellGlyph WaterTankGlyph(GameState state)
    {
        var ratio = state.Mech.WaterCapacity <= 0
            ? 0
            : Math.Clamp(state.Mech.Water / state.Mech.WaterCapacity, 0, 1);
        var filledBands = (int)Math.Round(ratio * CellHeight, MidpointRounding.AwayFromZero);
        var lines = new string[CellHeight];

        for (var row = 0; row < CellHeight; row++)
        {
            var isFilled = row >= CellHeight - filledBands;
            lines[row] = isFilled ? " |~~~~~| " : " |     | ";
        }

        return new CellGlyph(lines, 0, 4);
    }

    private static CellGlyph EngineGlyph(GameState state, ModuleInstance module, SlotId slot)
    {
        var occupiedCoordinates = module.OccupiedSlots
            .Select(occupiedSlot => state.Mech.Layout.Find(occupiedSlot)?.Coordinate)
            .Where(coordinate => coordinate is not null)
            .Select(coordinate => coordinate!.Value)
            .ToArray();
        var currentCoordinate = state.Mech.Layout.Find(slot)?.Coordinate;
        var isLeftSegment = currentCoordinate is not null &&
            currentCoordinate.Value.Column == occupiedCoordinates.Min(coordinate => coordinate.Column);

        return isLeftSegment
            ? new CellGlyph(
            [
                "  [====] ",
                "  [=**=]-",
                "  [====] "
            ], 2, 8)
            : new CellGlyph(
            [
                "[====]   ",
                "[=**=]   ",
                "[====]   "
            ], 2, 8);
    }

    private static char StatusMarker(ModuleInstance module)
    {
        var operational = module.GetComponent<OperationalComponent>();
        if (!operational.IsOperational ||
            module.TryGetComponent<PowerConsumerComponent>(out var power) && power is not null && !power.IsPowered)
        {
            return 'x';
        }

        return module.TryGetComponent<EntityConditionComponent>(out var condition) && condition is not null &&
               condition.Condition is EntityCondition.Worn or EntityCondition.Critical
            ? '!'
            : ' ';
    }

    private sealed record CellGlyph(IReadOnlyList<string> Lines, int PlayerRow, int PlayerColumn);
}
