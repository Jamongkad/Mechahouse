namespace Mechahouse.Core.Domain;

public sealed record SimulationResult(
    bool Succeeded,
    string Message,
    GameState State,
    double TimeAdvancedMinutes = 0,
    ExaminationReport? Examination = null)
{
    public static SimulationResult Success(string message, GameState state) => new(true, message, state);

    public static SimulationResult Failure(string message, GameState state) => new(false, message, state);
}
