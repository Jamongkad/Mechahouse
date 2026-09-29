using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Systems;

public static class ExamineSystem
{
    public static ExaminationReport Examine(GameState state, Entity entity)
    {
        if (!state.Entities.Any(candidate => candidate.Id == entity.Id))
        {
            throw new ArgumentException("The examined entity must belong to the current game state.", nameof(entity));
        }

        var examinable = entity.GetComponent<ExaminableComponent>();
        var condition = entity.GetComponent<EntityConditionComponent>().Condition;
        var observation = condition switch
        {
            EntityCondition.Critical => examinable.Profile.CriticalObservation,
            EntityCondition.Worn => examinable.Profile.WornObservation,
            _ => examinable.Profile.NormalObservation
        };

        return new ExaminationReport(
            entity.Name,
            condition,
            observation,
            GetMechanicalFacts(entity),
            InteractionSystem.GetAvailableInteractions(entity));
    }

    private static IReadOnlyList<ExaminationFact> GetMechanicalFacts(Entity entity)
    {
        var facts = new List<ExaminationFact>();
        if (entity.TryGetComponent<OperationalComponent>(out var operational) && operational is not null)
        {
            facts.Add(new ExaminationFact("Integrity", $"{operational.Durability:0.#}%"));
        }

        if (entity.TryGetComponent<PowerGeneratorComponent>(out var generator) && generator is not null)
        {
            var ratedOutput = generator.Output > 0 ? generator.CurrentOutput / generator.Output * 100 : 0;
            facts.Add(new ExaminationFact("Power output", $"{generator.CurrentOutput:0.##} kW ({ratedOutput:0.#}% rated)"));
        }

        if (entity.TryGetComponent<PowerConsumerComponent>(out var power) && power is not null)
        {
            facts.Add(new ExaminationFact("Power", power.IsPowered ? "Online" : "Offline"));
        }

        if (entity.TryGetComponent<WaterProducerComponent>(out var waterProducer) && waterProducer is not null)
        {
            facts.Add(new ExaminationFact("Water output", $"{waterProducer.CurrentOutput:0.##} L/day"));
        }

        if (entity.TryGetComponent<WaterConsumerComponent>(out var waterConsumer) && waterConsumer is not null)
        {
            facts.Add(new ExaminationFact("Water supply", waterConsumer.HasWater ? "Available" : "Insufficient"));
        }

        if (entity.TryGetComponent<FoodProducerComponent>(out var food) && food is not null)
        {
            facts.Add(new ExaminationFact("Food output", $"{food.CurrentOutput:0.##} / {food.Output:0.##} per day"));
        }

        return facts;
    }
}
