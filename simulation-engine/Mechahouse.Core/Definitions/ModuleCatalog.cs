using Mechahouse.Core.Domain;

namespace Mechahouse.Core.Definitions;

public static class ModuleCatalog
{
    public static IReadOnlyDictionary<string, ModuleDefinition> CreateDefault()
    {
        var definitions = new[]
        {
            new ModuleDefinition(
                "cockpit", "Cockpit", 1, 0, 4,
                Interactions: [InteractionType.Examine, InteractionType.Repair],
                Examination: new ExaminationProfile(
                    "The cockpit is a former hauler cab fitted into the mech's frame. The seat has been patched with three different fabrics; a familiar hand has worn the paint smooth around the controls.",
                    "The patched seat sags further, and two dashboard lights only come on when the panel is tapped. The controls still answer.",
                    "The console is dark behind a web of old repairs. The seat is the least of the cockpit's problems now."),
                Narration: new NarrationProfile(
                    "You step into the cockpit, a former hauler cab fitted into the mech's frame. The seat is patched with different fabrics, and the control panel is worn smooth where someone's hand used to rest.")),
            new ModuleDefinition(
                "bed", "Bed", 1, 5, 2, PowerConsumption: 1,
                Interactions: [InteractionType.Examine, InteractionType.Sleep, InteractionType.Repair],
                Examination: new ExaminationProfile(
                    "A narrow bed is wedged against the inner wall. The blanket has been mended at the corners so many times that little of the original cloth remains.",
                    "The frame creaks when you sit. Someone added a brace under the near leg, cut from a piece of cargo shelving.",
                    "The frame has given way and the mattress is damp through. It needs attention before it is fit to sleep in."),
                Narration: new NarrationProfile(
                    "The bed is little more than a narrow bunk bolted to the wall. Mended blankets and a cargo-shelf brace make it feel less like furniture than something the mech learned to carry.")),
            new ModuleDefinition(
                "omni_slot", "Omni Slot", 1, 0, 0,
                Interactions: [InteractionType.Examine, InteractionType.Install, InteractionType.Repair],
                Examination: new ExaminationProfile(
                    "A reinforced mounting frame sits empty, ringed with old bolt marks. It was left deliberately adaptable; the handwritten labels beside it have faded away.",
                    "The mounting rails are bent and two bolt holes have been retapped. It can still hold a small module, but it will need a careful fit.",
                    "The frame is twisted out of square. Nothing should be mounted here until it is repaired."),
                Narration: new NarrationProfile(
                    "An empty reinforced frame waits here, its rails scored by old bolts. It is a place prepared for whatever the mech cannot do yet.")),
            new ModuleDefinition(
                "storage", "Storage Area", 1, 3, 1,
                Interactions: [InteractionType.Examine, InteractionType.Use, InteractionType.Repair],
                Examination: new ExaminationProfile(
                    "Dented lockers and shallow bins have been bolted into every usable gap. Chalk marks divide food from scrap, and every latch has been tied shut against the mech's gait.",
                    "Several locker doors sit crooked on their hinges. The tie-downs still hold, but loose hardware chatters inside the bins whenever the mech shifts its weight.",
                    "A mounting rail has torn loose and spilled the lockers against one another. Nothing here can be trusted to stay put until the frame is repaired."),
                Narration: new NarrationProfile(
                    "Dented lockers line the compartment, their doors labeled in chalk and grease pencil. This is where the mech keeps the small reserve of things that make another day possible."),
                Storage: new StorageProfile(
                    StoresScrap: true,
                    StoresFood: true,
                    InventoryNarration: "You undo the wire catches and count the lockers' contents by hand.")),
            new ModuleDefinition(
                "hydroponics_small", "Hydroponics", 2, 8, 5, WaterConsumption: 4, FoodProduction: 5,
                Interactions: [InteractionType.Examine, InteractionType.Repair],
                Examination: new ExaminationProfile(
                    "Grime has settled into the seams of the old hydroponics racks. Your father built most of this system years ago, joining fittings scavenged from other machines. Somehow, the trays are still growing food.",
                    "Grime fills the rack seams, and your father-built feed lines still carry mismatched fittings from old salvage. One pump stutters before it catches; his pencil mark for the last filter change is still there.",
                    "Several trays have gone dry. The pump barely turns, and the leaves nearest the feed line have curled in on themselves."),
                Narration: new NarrationProfile(
                    "The hydroponics racks fill the compartment with damp warmth and the smell of growing things. Mismatched pipes carry water between trays built from whatever your father could salvage."),
                InitialDurability: 82),
            new ModuleDefinition(
                "engine_room", "Engine Room", 2, 18, 8, PowerProduction: 12,
                Interactions: [InteractionType.Examine, InteractionType.Toggle, InteractionType.Repair],
                Examination: new ExaminationProfile(
                    "The engine settles into its familiar rhythm. A faint squeak rides beneath the steady thrum; your father's oil-change notes are tucked beside the panel. The sound was there before you can remember.",
                    "The familiar squeak has sharpened into a metallic scrape. Your father's notes track the sound through years of regular oil changes; now the rhythm suggests the cylinder bore itself may be scoring.",
                    "The scrape catches on every revolution, then rises to a hard shriek. The engine is still turning, but something inside is close to tearing itself apart."),
                Narration: new NarrationProfile(
                    "The engine room meets you with a low, uneven thrum. Oil, hot metal, and an old mechanical squeak cling to the air; the sound is part of the mech's rhythm now.",
                    OnEnabled: "You turn the ignition. The starter grinds, catches, and the creaking behemoth shudders awake. Its cylinders fire one after another, their uneven cadence gathering into the engine's familiar thrum.",
                    OnDisabled: "You pull the engine cutoff. Its heavy rhythm falters, each cylinder falling silent in turn. The hot block ticks more slowly as the last vibrations die away, and then there is only silence."),
                InitialDurability: 82),
            new ModuleDefinition(
                "water_processing", "Water Processing", 2, 10, 5, PowerConsumption: 3, WaterProduction: 12,
                Interactions: [InteractionType.Examine, InteractionType.Toggle, InteractionType.Repair],
                Examination: new ExaminationProfile(
                    "A filter stack and condenser occupy the whole compartment. The copper has gone green at the joints, but the pump's low, even note means clean water is still making it through.",
                    "The pump pulses unevenly through a film of mineral scale. A strip of newer tubing bridges an older repair; the output is holding, for now.",
                    "The pump knocks without drawing water. The outlet is dry, and the last filter change is long overdue."),
                Narration: new NarrationProfile(
                    "Filters, copper lines, and a small condenser crowd the water-processing bay. Mineral scale whitens the joints, but somewhere behind the panels a pump keeps a patient beat.",
                    OnEnabled: "You lift the pump switch. A relay snaps, the old motor coughs against its mineral scale, and water begins knocking through the copper lines until the condenser finds its patient rhythm.",
                    OnDisabled: "You lower the pump switch. The condenser's patient beat stutters out, water rattles back through the old copper lines, and the processing bay settles into stillness.",
                    OnEnabledWithoutPower: "You lift the pump switch. A relay clicks somewhere behind the panel, but the motor does not answer; without power, the copper lines remain dry and still."),
                InitialDurability: 82)
        };

        return definitions.ToDictionary(definition => definition.Id, StringComparer.OrdinalIgnoreCase);
    }
}
