using Mechahouse.Core.Commands;
using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Systems;

public static class ActionTimeCosts
{
    public const double MoveMinutes = 10;
    public const double ToggleMinutes = 10;
    public const double BuildMinutes = 30;
    public const double RepairMinutes = 30;
    public const double RemoveMinutes = 10;

    public static double For(SimulationCommand command) => command switch
    {
        MovePlayerCommand => MoveMinutes,
        ToggleModuleCommand => ToggleMinutes,
        BuildModuleCommand => BuildMinutes,
        RepairModuleCommand => RepairMinutes,
        InteractionIntent { Interaction: InteractionType.Examine or InteractionType.Use or InteractionType.Sleep } => 0,
        InteractionIntent { Interaction: InteractionType.Toggle } => ToggleMinutes,
        InteractionIntent { Interaction: InteractionType.Remove } => RemoveMinutes,
        InteractionIntent { Interaction: InteractionType.Repair } => RepairMinutes,
        InteractionIntent { Interaction: InteractionType.Install } => BuildMinutes,
        _ => 0
    };
}
