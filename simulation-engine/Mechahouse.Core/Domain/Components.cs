namespace Mechahouse.Core.Domain;

public sealed class PositionComponent
{
    public PositionComponent(IReadOnlyCollection<SlotId> quadrants)
    {
        if (quadrants.Count == 0)
        {
            throw new ArgumentException("An entity must occupy at least one quadrant.", nameof(quadrants));
        }

        Quadrants = quadrants.Distinct().ToArray();
    }

    public IReadOnlyList<SlotId> Quadrants { get; }

    public SlotId Quadrant => Quadrants[0];
}

public sealed class OperationalComponent
{
    public OperationalComponent(double durability = 100)
    {
        if (!double.IsFinite(durability) || durability < 0 || durability > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(durability), "Durability must be between 0 and 100.");
        }

        Durability = durability;
    }

    public bool Enabled { get; internal set; } = true;

    public double Durability { get; internal set; }

    public bool IsOperational => Enabled && Durability > 0;
}

public sealed class PowerGeneratorComponent
{
    public PowerGeneratorComponent(double output)
    {
        if (output < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(output));
        }

        Output = output;
    }

    public double Output { get; }

    public double CurrentOutput { get; internal set; }
}

public sealed class PowerConsumerComponent
{
    public PowerConsumerComponent(double requiredPower)
    {
        if (requiredPower < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredPower));
        }

        RequiredPower = requiredPower;
    }

    public double RequiredPower { get; }

    public bool IsPowered { get; internal set; }
}

public sealed class WaterProducerComponent
{
    public WaterProducerComponent(double output)
    {
        if (output < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(output));
        }

        Output = output;
    }

    public double Output { get; }

    public double CurrentOutput { get; internal set; }
}

public sealed class WaterConsumerComponent
{
    public WaterConsumerComponent(double requiredWater)
    {
        if (requiredWater < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredWater));
        }

        RequiredWater = requiredWater;
    }

    public double RequiredWater { get; }

    public bool HasWater { get; internal set; }
}

public sealed class FoodProducerComponent
{
    public FoodProducerComponent(double output)
    {
        if (output < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(output));
        }

        Output = output;
    }

    public double Output { get; }

    public double CurrentOutput { get; internal set; }
}
