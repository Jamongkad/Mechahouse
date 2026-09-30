using Mechahouse.Core.Domain;

namespace Mechahouse.Cli;

public sealed record EntityReferenceResult(Entity? Entity, string? Error)
{
    public bool Succeeded => Entity is not null;
}

public sealed class EntityReferenceResolver
{
    public EntityReferenceResult Resolve(GameState state, string reference)
    {
        if (int.TryParse(reference, out var slotNumber))
        {
            var target = state.Entities.SingleOrDefault(entity =>
                entity.GetComponent<PositionComponent>().Quadrants.Any(slot => slot.Number == slotNumber));
            return target is null
                ? new EntityReferenceResult(null, $"No entity occupies quadrant {slotNumber}.")
                : new EntityReferenceResult(target, null);
        }

        var matches = state.Entities.Where(entity =>
                string.Equals(entity.Id.ToString(), reference, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(NormalizeReference(entity.Name), NormalizeReference(reference), StringComparison.OrdinalIgnoreCase) ||
                entity is ModuleInstance module && string.Equals(module.DefinitionId, reference, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length == 1)
        {
            return new EntityReferenceResult(matches[0], null);
        }

        return matches.Length > 1
            ? new EntityReferenceResult(null, $"'{reference}' matches multiple entities. Use a quadrant number instead.")
            : new EntityReferenceResult(null, $"No entity matches '{reference}'. Use 'interior' to see the quadrants.");
    }

    private static string NormalizeReference(string value) => new(value.Where(char.IsLetterOrDigit).ToArray());
}
