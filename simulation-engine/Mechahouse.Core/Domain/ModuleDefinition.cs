namespace Mechahouse.Core.Domain;

public sealed record ModuleDefinition(
    string Id,
    string Name,
    int SlotsRequired,
    int BuildCost,
    double Weight,
    double PowerProduction = 0,
    double PowerConsumption = 0,
    double WaterProduction = 0,
    double WaterConsumption = 0,
    double FoodProduction = 0,
    double FoodConsumption = 0,
    double WaterCapacity = 0,
    IReadOnlyList<InteractionType>? Interactions = null,
    ExaminationProfile? Examination = null,
    NarrationProfile? Narration = null,
    double InitialDurability = 100,
    StorageProfile? Storage = null);
