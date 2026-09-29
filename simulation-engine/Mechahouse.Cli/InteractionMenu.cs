using Mechahouse.Core.Domain;

namespace Mechahouse.Cli;

public static class InteractionMenu
{
    public static InteractionType? Select(string entityName, IReadOnlyList<InteractionType> interactions)
    {
        var options = interactions
            .Select(interaction => new MenuOption(interaction.ToString(), interaction))
            .Append(new MenuOption("Cancel", null))
            .ToArray();

        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            Console.WriteLine($"{entityName}:");
            foreach (var option in options)
            {
                Console.WriteLine($"  {option.Label}");
            }

            Console.WriteLine("Interactive menus require a terminal. Use 'interact <target> <action>' when input is redirected.");
            return null;
        }

        var selectedIndex = 0;
        var lineCount = options.Length + 2;
        Console.Write("\u001b[?25l");
        Render(entityName, options, selectedIndex);

        while (true)
        {
            var key = Console.ReadKey(intercept: true).Key;
            switch (key)
            {
                case ConsoleKey.UpArrow:
                    selectedIndex = (selectedIndex - 1 + options.Length) % options.Length;
                    Redraw(lineCount, entityName, options, selectedIndex);
                    break;
                case ConsoleKey.DownArrow:
                    selectedIndex = (selectedIndex + 1) % options.Length;
                    Redraw(lineCount, entityName, options, selectedIndex);
                    break;
                case ConsoleKey.Enter:
                    ClearMenu(lineCount);
                    return options[selectedIndex].Interaction;
                case ConsoleKey.Escape:
                    ClearMenu(lineCount);
                    return null;
            }
        }
    }

    private static void Redraw(
        int lineCount,
        string entityName,
        IReadOnlyList<MenuOption> options,
        int selectedIndex)
    {
        Console.Write($"\u001b[{lineCount}A");
        Render(entityName, options, selectedIndex);
    }

    private static void ClearMenu(int lineCount)
    {
        Console.Write($"\u001b[{lineCount}A");
        for (var index = 0; index < lineCount; index++)
        {
            ClearCurrentLine();
            if (index < lineCount - 1)
            {
                Console.Write("\u001b[1B");
            }
        }

        Console.Write("\u001b[1B\r\u001b[?25h");
    }

    private static void Render(string entityName, IReadOnlyList<MenuOption> options, int selectedIndex)
    {
        ClearCurrentLine();
        Console.WriteLine($"{entityName}:");
        for (var index = 0; index < options.Count; index++)
        {
            ClearCurrentLine();
            var marker = index == selectedIndex ? ">" : " ";
            Console.WriteLine($"{marker} {options[index].Label}");
        }

        ClearCurrentLine();
        Console.WriteLine("Use Up/Down to choose, Enter to select, Esc to cancel.");
    }

    private static void ClearCurrentLine() => Console.Write("\u001b[2K\r");

    private sealed record MenuOption(string Label, InteractionType? Interaction);
}
