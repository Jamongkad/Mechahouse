namespace Mechahouse.Cli;

public sealed class AsciiPlayerAvatar
{
    public AsciiPlayerAvatar(string name, char glyph)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Avatar name is required.", nameof(name));
        }

        if (char.IsControl(glyph) || char.IsWhiteSpace(glyph))
        {
            throw new ArgumentException("Avatar glyph must be a visible character.", nameof(glyph));
        }

        Name = name;
        Glyph = glyph;
    }

    public string Name { get; }

    public char Glyph { get; }

    public string RenderInCell(string cellLabel)
    {
        if (cellLabel.Length != 3)
        {
            throw new ArgumentException("An interior cell label must be exactly three characters.", nameof(cellLabel));
        }

        var rendered = cellLabel.ToCharArray();
        rendered[1] = Glyph;
        return new string(rendered);
    }

    public string Legend(int slotNumber) => $"{Glyph} = {Name} (slot {slotNumber})";
}
