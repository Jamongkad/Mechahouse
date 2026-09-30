namespace Mechahouse.Cli;

public sealed class ConsolePanelRenderer
{
    public void PrintFrame(string title, IReadOnlyList<string> lines)
    {
        var contentWidth = Math.Max(title.Length, lines.Count == 0 ? 0 : lines.Max(line => line.Length));
        var border = new string('=', contentWidth + 2);

        Console.WriteLine($"+{border}+");
        PrintFrameLine(title, contentWidth);
        Console.WriteLine($"+{new string('-', contentWidth + 2)}+");
        foreach (var line in lines)
        {
            PrintFrameLine(line, contentWidth);
        }

        Console.WriteLine($"+{border}+");
    }

    public void PrintTable(string title, IReadOnlyList<string> headers, IReadOnlyList<string[]> rows)
    {
        var widths = headers
            .Select((header, index) => Math.Max(header.Length, rows.Count == 0 ? 0 : rows.Max(row => row[index].Length)))
            .ToArray();
        var topDivider = FormatTableDivider(widths, '=');
        var tableDivider = FormatTableDivider(widths);
        var titleWidth = topDivider.Length - 4;

        Console.WriteLine(topDivider);
        Console.WriteLine($"| {title.PadRight(titleWidth)} |");
        Console.WriteLine(tableDivider);
        Console.WriteLine(FormatTableRow(headers, widths));
        Console.WriteLine(tableDivider);

        foreach (var row in rows)
        {
            Console.WriteLine(FormatTableRow(row, widths));
        }

        Console.WriteLine(tableDivider);
    }

    private static void PrintFrameLine(string line, int contentWidth)
    {
        Console.WriteLine($"| {line.PadRight(contentWidth)} |");
    }

    private static string FormatTableDivider(IReadOnlyList<int> widths, char fill = '-')
    {
        return $"+{string.Join("+", widths.Select(width => new string(fill, width + 2)))}+";
    }

    private static string FormatTableRow(IReadOnlyList<string> values, IReadOnlyList<int> widths)
    {
        return $"| {string.Join(" | ", values.Select((value, index) => value.PadRight(widths[index])))} |";
    }
}
