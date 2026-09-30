namespace Mechahouse.Core.Domain;

public sealed class ModuleInstance : Entity
{
    internal ModuleInstance(ModuleDefinition definition, IReadOnlyList<SlotId> occupiedSlots)
        : base(Guid.NewGuid(), definition.Name)
    {
        Definition = definition;
        OccupiedSlots = occupiedSlots.ToArray();
        AddComponent(new PositionComponent(OccupiedSlots));
        AddComponent(new OperationalComponent(definition.InitialDurability));
        AddComponent(new EntityConditionComponent());

        var capabilities = new InteractionCapabilitiesComponent(definition.Interactions ?? []);
        AddComponent(capabilities);
        if (capabilities.Supports(InteractionType.Examine) && definition.Examination is not null)
        {
            AddComponent(new ExaminableComponent(definition.Examination));
        }

        if (definition.Narration is not null)
        {
            AddComponent(new NarratableComponent(definition.Narration));
        }

        if (definition.Storage is not null)
        {
            AddComponent(new StorageComponent(definition.Storage));
        }

        if (capabilities.Supports(InteractionType.Install))
        {
            AddComponent(new OmniSlotComponent());
        }

        if (definition.PowerProduction > 0)
        {
            AddComponent(new PowerGeneratorComponent(definition.PowerProduction));
        }

        if (definition.PowerConsumption > 0)
        {
            AddComponent(new PowerConsumerComponent(definition.PowerConsumption));
        }

        if (definition.WaterProduction > 0)
        {
            AddComponent(new WaterProducerComponent(definition.WaterProduction));
        }

        if (definition.WaterConsumption > 0)
        {
            AddComponent(new WaterConsumerComponent(definition.WaterConsumption));
        }

        if (definition.FoodProduction > 0)
        {
            AddComponent(new FoodProducerComponent(definition.FoodProduction));
        }
    }

    public Guid InstanceId => Id;

    public ModuleDefinition Definition { get; }

    public string DefinitionId => Definition.Id;

    public IReadOnlyList<SlotId> OccupiedSlots { get; }

    public bool Enabled
    {
        get => GetComponent<OperationalComponent>().Enabled;
        internal set => GetComponent<OperationalComponent>().Enabled = value;
    }

    public double Durability
    {
        get => GetComponent<OperationalComponent>().Durability;
        internal set => GetComponent<OperationalComponent>().Durability = value;
    }

    public bool Powered
    {
        get => !TryGetComponent<PowerConsumerComponent>(out var power) || power is null || power.IsPowered;
        internal set
        {
            if (TryGetComponent<PowerConsumerComponent>(out var power) && power is not null)
            {
                power.IsPowered = value;
            }
        }
    }

    public bool IsOperational => GetComponent<OperationalComponent>().IsOperational;

    internal void MarkInstalledInOmniSlot()
    {
        AddComponent(new OmniSlotComponent());
        AddComponent(new InstalledInOmniSlotComponent());
        GetComponent<InteractionCapabilitiesComponent>().Add(InteractionType.Remove);
    }
}
