using Mechahouse.Core.Domain;

namespace Mechahouse.Cli;

public sealed class ModuleListRenderer
{
    public void PrintModules(IReadOnlyList<ModuleInstance> modules)
    {
        if (modules.Count == 0)
        {
            Console.WriteLine("No modules installed.");
            return;
        }

        foreach (var module in modules.OrderBy(module => module.OccupiedSlots.Min(slot => slot.Number)))
        {
            var powerState = module.Definition.PowerConsumption > 0 ? (module.Powered ? "powered" : "no power") : "n/a";
            var slots = string.Join(",", module.OccupiedSlots);
            Console.WriteLine($"{module.DefinitionId,-22} slots [{slots}], {(module.Enabled ? "enabled" : "disabled")}, {powerState}, durability {module.Durability:0.#}%");
        }
    }

    public void PrintParts(IEnumerable<ModuleDefinition> definitions)
    {
        Console.WriteLine("Available parts:");
        foreach (var definition in definitions.OrderBy(definition => definition.Id))
        {
            Console.WriteLine($"{definition.Id,-22} {definition.Name,-22} slots {definition.SlotsRequired}, cost {definition.BuildCost}, weight {definition.Weight:0.##}");
        }
    }
}
