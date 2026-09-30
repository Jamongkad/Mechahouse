using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Systems;

public static class InteractionSystem
{
    public static IReadOnlyList<InteractionType> GetAvailableInteractions(Entity entity)
    {
        var interactions = entity.TryGetComponent<InteractionCapabilitiesComponent>(out var capabilities) && capabilities is not null
            ? capabilities.Interactions
                .Where(interaction => interaction switch
                {
                    InteractionType.Examine => entity.HasComponent<ExaminableComponent>(),
                    InteractionType.Repair => entity.TryGetComponent<OperationalComponent>(out var operational) &&
                                              operational is not null && operational.Durability < 100,
                    InteractionType.Sleep or InteractionType.Install or InteractionType.Use => !IsCritical(entity),
                    _ => true
                })
                .ToHashSet()
            : [];

        if (entity.HasComponent<ExaminableComponent>())
        {
            interactions.Add(InteractionType.Examine);
        }

        if (entity.HasComponent<OmniSlotComponent>())
        {
            if (entity.HasComponent<InstalledInOmniSlotComponent>())
            {
                interactions.Add(InteractionType.Remove);
            }
            else if (!IsCritical(entity))
            {
                interactions.Add(InteractionType.Install);
            }
        }

        return interactions.Order().ToArray();
    }

    public static bool IsWithinReach(GameState state, Entity entity)
    {
        return entity.TryGetComponent<PositionComponent>(out var position) &&
               position is not null &&
               position.Quadrants.Contains(state.Player.CurrentSlot);
    }

    private static bool IsCritical(Entity entity)
    {
        return entity.TryGetComponent<EntityConditionComponent>(out var condition) &&
               condition is not null &&
               condition.Condition == EntityCondition.Critical;
    }
}
