namespace Mechahouse.Core.Domain;

public enum InteractionType
{
    Examine,
    Use,
    Toggle,
    Repair,
    Sleep,
    Install,
    Remove
}

public enum EntityCondition
{
    Normal,
    Worn,
    Critical
}

public sealed class EntityConditionComponent(EntityCondition condition = EntityCondition.Normal)
{
    public EntityCondition Condition { get; internal set; } = condition;
}

public sealed record ExaminationProfile(
    string NormalObservation,
    string WornObservation,
    string CriticalObservation);

public sealed record NarrationProfile(
    string OnEnter,
    string? OnEnabled = null,
    string? OnDisabled = null,
    string? OnEnabledWithoutPower = null);

public sealed record StorageProfile(
    bool StoresScrap,
    bool StoresFood,
    string InventoryNarration);

public sealed class ExaminableComponent(ExaminationProfile profile)
{
    public ExaminationProfile Profile { get; } = profile;
}

public sealed class NarratableComponent(NarrationProfile profile)
{
    public NarrationProfile Profile { get; } = profile;
}

public sealed class StorageComponent(StorageProfile profile)
{
    public StorageProfile Profile { get; } = profile;
}

public sealed class InteractionCapabilitiesComponent(IEnumerable<InteractionType> interactions)
{
    private readonly HashSet<InteractionType> _interactions = interactions.ToHashSet();

    public IReadOnlyCollection<InteractionType> Interactions => _interactions.ToArray();

    public bool Supports(InteractionType interaction) => _interactions.Contains(interaction);

    internal void Add(InteractionType interaction) => _interactions.Add(interaction);
}

public sealed class OmniSlotComponent { }

public sealed class InstalledInOmniSlotComponent { }

public sealed record ExaminationFact(string Label, string Value);

public sealed record ExaminationReport(
    string EntityName,
    EntityCondition Condition,
    string Observation,
    IReadOnlyList<ExaminationFact> Facts,
    IReadOnlyList<InteractionType> AvailableInteractions);
