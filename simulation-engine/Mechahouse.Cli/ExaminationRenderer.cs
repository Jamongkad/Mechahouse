using Mechahouse.Core.Domain;

namespace Mechahouse.Cli;

public sealed class ExaminationRenderer
{
    public void Print(ExaminationReport report)
    {
        Console.WriteLine($"\n{report.EntityName.ToUpperInvariant()} — {report.Condition.ToString().ToUpperInvariant()}");
        Console.WriteLine(report.Observation);
        foreach (var fact in report.Facts)
        {
            Console.WriteLine($"  {fact.Label}: {fact.Value}");
        }

        if (report.AvailableInteractions.Count > 0)
        {
            Console.WriteLine($"  Actions: {string.Join(", ", report.AvailableInteractions.Select(FormatInteraction))}");
        }
    }

    private static string FormatInteraction(InteractionType interaction) => interaction.ToString().ToUpperInvariant();
}
