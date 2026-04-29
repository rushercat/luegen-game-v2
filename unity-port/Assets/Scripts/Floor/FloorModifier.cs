// Lügen — FloorModifier.cs
// Translated from FLOOR_MODIFIERS in beta.js (around line 344).
//
// Floor modifiers roll on Act II+ non-boss floors. They twist a single
// rule for that floor only.

using System.Collections.Generic;

namespace Lugen.Floor
{
    [System.Serializable]
    public class FloorModifierData
    {
        public string id;
        public string name;
        public string desc;
        public int minFloor; // 0 = available everywhere; >0 = gated.

        public FloorModifierData(string id, string name, string desc, int minFloor = 0)
        {
            this.id = id; this.name = name; this.desc = desc; this.minFloor = minFloor;
        }
    }

    public static class FloorModifiers
    {
        public static readonly Dictionary<string, FloorModifierData> All = new Dictionary<string, FloorModifierData>
        {
            { "foggy",    new FloorModifierData("foggy",    "Foggy",     "Target rank fades after 5 seconds.") },
            { "greedy",   new FloorModifierData("greedy",   "Greedy",    "+100% gold, but Jack limit drops to 3.") },
            { "brittle",  new FloorModifierData("brittle",  "Brittle",   "Every card is temporarily Glass for this floor.") },
            { "echoing",  new FloorModifierData("echoing",  "Echoing",   "Each play: 20% chance the first card is flashed to all.") },
            { "silent",   new FloorModifierData("silent",   "Silent",    "No bot tells are visible this floor.") },
            { "tariff",   new FloorModifierData("tariff",   "Tariff",    "Each Liar call you make costs 5g.") },
            { "inverted", new FloorModifierData("inverted", "Inverted",  "Target rank is locked to J this floor.", minFloor: 7) },
            { "sticky",   new FloorModifierData("sticky",   "Sticky",    "Once revealed, cards stay face-up for the round.") },
            { "rapid",    new FloorModifierData("rapid",    "Rapid",     "Challenge windows are 2 seconds for everyone.") },
            { "richFolk", new FloorModifierData("richFolk", "Rich Folk", "Gold halved, but joker prices 50% off.") },
            { "lastCall", new FloorModifierData("lastCall", "Last Call", "Each player gets 5 plays per round.") },
        };

        public static List<string> EligibleForFloor(int floor)
        {
            var list = new List<string>();
            foreach (var kv in All)
            {
                if (kv.Value.minFloor > 0 && floor < kv.Value.minFloor) continue;
                list.Add(kv.Key);
            }
            return list;
        }
    }
}
