using System.Text;

namespace Mechahouse.Cli;

public sealed class CommandLineReader
{
    private readonly List<string> _history = [];

    public string? ReadLine(string prompt = "> ")
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            return Console.ReadLine();
        }

        Console.Write(prompt);
        var buffer = new StringBuilder();
        var cursor = 0;
        var historyIndex = _history.Count;
        var renderedLength = prompt.Length;

        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    var line = buffer.ToString();
                    if (!string.IsNullOrWhiteSpace(line) && (_history.Count == 0 || _history[^1] != line))
                    {
                        _history.Add(line);
                    }

                    return line;
                case ConsoleKey.UpArrow:
                    if (historyIndex > 0)
                    {
                        historyIndex--;
                        ReplaceBuffer(buffer, _history[historyIndex], ref cursor);
                        Render(prompt, buffer, cursor, ref renderedLength);
                    }

                    break;
                case ConsoleKey.DownArrow:
                    if (historyIndex < _history.Count - 1)
                    {
                        historyIndex++;
                        ReplaceBuffer(buffer, _history[historyIndex], ref cursor);
                        Render(prompt, buffer, cursor, ref renderedLength);
                    }
                    else if (historyIndex == _history.Count - 1)
                    {
                        historyIndex = _history.Count;
                        ReplaceBuffer(buffer, string.Empty, ref cursor);
                        Render(prompt, buffer, cursor, ref renderedLength);
                    }

                    break;
                case ConsoleKey.LeftArrow:
                    if (cursor > 0)
                    {
                        cursor--;
                        Console.Write('\b');
                    }

                    break;
                case ConsoleKey.RightArrow:
                    if (cursor < buffer.Length)
                    {
                        Console.Write(buffer[cursor]);
                        cursor++;
                    }

                    break;
                case ConsoleKey.Home:
                    while (cursor > 0)
                    {
                        Console.Write('\b');
                        cursor--;
                    }

                    break;
                case ConsoleKey.End:
                    while (cursor < buffer.Length)
                    {
                        Console.Write(buffer[cursor]);
                        cursor++;
                    }

                    break;
                case ConsoleKey.Backspace:
                    if (cursor > 0)
                    {
                        buffer.Remove(cursor - 1, 1);
                        cursor--;
                        Render(prompt, buffer, cursor, ref renderedLength);
                    }

                    break;
                case ConsoleKey.Delete:
                    if (cursor < buffer.Length)
                    {
                        buffer.Remove(cursor, 1);
                        Render(prompt, buffer, cursor, ref renderedLength);
                    }

                    break;
                case ConsoleKey.Escape:
                    ReplaceBuffer(buffer, string.Empty, ref cursor);
                    Render(prompt, buffer, cursor, ref renderedLength);
                    break;
                case ConsoleKey.C:
                    if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        Console.WriteLine("^C");
                        return string.Empty;
                    }

                    Insert(buffer, key.KeyChar, ref cursor);
                    Render(prompt, buffer, cursor, ref renderedLength);
                    break;
                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        Insert(buffer, key.KeyChar, ref cursor);
                        Render(prompt, buffer, cursor, ref renderedLength);
                    }

                    break;
            }
        }
    }

    private static void Insert(StringBuilder buffer, char character, ref int cursor)
    {
        buffer.Insert(cursor, character);
        cursor++;
    }

    private static void ReplaceBuffer(StringBuilder buffer, string value, ref int cursor)
    {
        buffer.Clear();
        buffer.Append(value);
        cursor = buffer.Length;
    }

    private static void Render(string prompt, StringBuilder buffer, int cursor, ref int renderedLength)
    {
        Console.Write('\r');
        Console.Write(new string(' ', renderedLength));
        Console.Write('\r');
        Console.Write(prompt);
        Console.Write(buffer);

        var endPosition = prompt.Length + buffer.Length;
        var cursorPosition = prompt.Length + cursor;
        if (endPosition > cursorPosition)
        {
            Console.Write(new string('\b', endPosition - cursorPosition));
        }

        renderedLength = Math.Max(renderedLength, endPosition);
    }
}
