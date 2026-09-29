using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Commands;

public abstract record SimulationCommand;

public sealed record BuildModuleCommand(string DefinitionId, IReadOnlyList<SlotId>? SlotIds = null) : SimulationCommand;

public sealed record ToggleModuleCommand(string ModuleReference, bool Enabled) : SimulationCommand;

public sealed record RepairModuleCommand(string ModuleReference, double Amount = 100) : SimulationCommand;

public sealed record AdvanceTimeCommand(double Hours) : SimulationCommand;

public sealed record MovePlayerCommand(SlotId DestinationSlot) : SimulationCommand;

public sealed record InteractionIntent(
    Guid ActorId,
    Guid TargetEntityId,
    InteractionType Interaction,
    string? PartId = null) : SimulationCommand;
