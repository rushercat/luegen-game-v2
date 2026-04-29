// Lügen — JokerCatalog.cs
// Translated from JOKER_CATALOG in public/beta.js (lines 472-513).
//
// Each joker has:
//   - id        (string key, matches save data)
//   - name      (display)
//   - rarity    (drives shop price + drop pool)
//   - price     (gold cost in shops)
//   - desc      (UI tooltip)
//   - stackable / maxStack (only Sixth Sense is stackable)
//
// Behavior is split: this catalog is the data manifest, JokerHooks.cs has
// the actual triggers, and HookContext.cs is the parameter bag passed in.

using System.Collections.Generic;

namespace Lugen.Jokers
{
    public enum JokerRarity { Common, Uncommon, Rare, Legendary }

    [System.Serializable]
    public class JokerData
    {
        public string id;
        public string name;
        public JokerRarity rarity;
        public int price;
        public string desc;
        public bool stackable;
        public int maxStack;

        public JokerData(string id, string name, JokerRarity r, int price, string desc, bool stackable = false, int maxStack = 1)
        {
            this.id = id;
            this.name = name;
            this.rarity = r;
            this.price = price;
            this.desc = desc;
            this.stackable = stackable;
            this.maxStack = maxStack;
        }
    }

    public static class JokerCatalog
    {
        public static readonly Dictionary<string, JokerData> All = new Dictionary<string, JokerData>
        {
            // Information
            { "surveyor",      new JokerData("surveyor",      "The Surveyor",     JokerRarity.Common,    80,  "See the top card of the draw pile at all times.") },
            { "eavesdropper",  new JokerData("eavesdropper",  "Eavesdropper",     JokerRarity.Uncommon,  150, "Every 2 rounds: when the player before you plays, see whether they have NONE / SOME (1-2) / MANY (3+) matches.") },
            { "tattletale",    new JokerData("tattletale",    "Tattletale",       JokerRarity.Rare,      250, "Once per floor, peek at a target hand for 4 seconds.") },
            { "coldRead",      new JokerData("coldRead",      "Cold Read",        JokerRarity.Legendary, 400, "Round start: see one random card from each opponent.") },
            { "memorizer",     new JokerData("memorizer",     "The Memorizer",    JokerRarity.Uncommon,  150, "Every revealed card on a Liar call is logged for the rest of the round.") },
            { "sixthSense",    new JokerData("sixthSense",    "Sixth Sense",      JokerRarity.Uncommon,  150, "After each opponent play: 15% × stack chance to learn truth/lie. Stacks ×3.", stackable: true, maxStack: 3) },

            // Aggression
            { "spikedTrap",    new JokerData("spikedTrap",    "Spiked Trap",      JokerRarity.Rare,      250, "Truth told + challenged = challenger draws +3.") },
            { "taxman",        new JokerData("taxman",        "The Taxman",       JokerRarity.Common,    80,  "Opponent picks up 5+ pile = +10g.") },
            { "vengefulSpirit",new JokerData("vengefulSpirit","Vengeful Spirit",  JokerRarity.Legendary, 400, "Jack-cursed = next active player starts with 2 forced Jacks.") },
            { "saboteur",      new JokerData("saboteur",      "The Saboteur",     JokerRarity.Rare,      250, "Once per floor: dump a hand card into a target opponent.") },
            { "doppelganger",  new JokerData("doppelganger",  "Doppelganger",     JokerRarity.Legendary, 400, "Once per round: next play forced to match previous play.") },
            { "screamer",      new JokerData("screamer",      "The Screamer",     JokerRarity.Legendary, 400, "Once per floor: name a rank — every card of that rank is publicly revealed for the round.") },

            // Jack management
            { "scapegoat",     new JokerData("scapegoat",     "The Scapegoat",    JokerRarity.Uncommon,  150, "Caught lying with a Jack? One Jack goes to challenger.") },
            { "safetyNet",     new JokerData("safetyNet",     "Safety Net",       JokerRarity.Rare,      250, "Jack limit +1.") },
            { "blackHole",     new JokerData("blackHole",     "Black Hole",       JokerRarity.Legendary, 400, "Successful Jack bluff: delete a non-Jack from your hand.") },
            { "deadHand",      new JokerData("deadHand",      "Dead Hand",        JokerRarity.Legendary, 400, "Once per floor: 2 Jacks in a pile you take are sent under the draw pile instead.") },
            { "ricochet",      new JokerData("ricochet",      "Ricochet",         JokerRarity.Uncommon,  150, "Pile of 3+ Jacks taken = half bounce to a random opponent.") },

            // Tempo
            { "slowHand",      new JokerData("slowHand",      "Slow Hand",        JokerRarity.Common,    80,  "Your challenge window is 10s.") },
            { "hotSeat",       new JokerData("hotSeat",       "Hot Seat",         JokerRarity.Uncommon,  150, "Right neighbor's challenge window is 3s.") },
            { "doubletalk",    new JokerData("doubletalk",    "Doubletalk",       JokerRarity.Rare,      250, "Once per round: play 2-4 cards instead of 1-3.") },
            { "sleightOfHand", new JokerData("sleightOfHand", "Sleight of Hand",  JokerRarity.Uncommon,  150, "Once per round: draw 1 card from the top of the draw pile.") },
            { "hotPotato",     new JokerData("hotPotato",     "Hot Potato",       JokerRarity.Rare,      250, "After picking up 5+: max play = 5 next turn.") },

            // Economy / shape
            { "magpie",        new JokerData("magpie",        "The Magpie",       JokerRarity.Common,    80,  "Opponent pickup = +1g per affixed card.") },
            { "forgeHand",     new JokerData("forgeHand",     "Forge Hand",       JokerRarity.Common,    80,  "Glass Shard / Spiked Wire / Steel Plating cost 25% less.") },
            { "patron",        new JokerData("patron",        "The Patron",       JokerRarity.Legendary, 400, "+1g per Gilded card per turn (stacks with base).") },
            { "callersMark",   new JokerData("callersMark",   "Caller's Mark",    JokerRarity.Uncommon,  150, "First LIAR call each round: +20g if right, -15g if wrong.") },
            { "lastWord",      new JokerData("lastWord",      "Last Word",        JokerRarity.Uncommon,  150, "Once per floor: veto a Liar call against you.") },
            { "trickster",     new JokerData("trickster",     "The Trickster",    JokerRarity.Uncommon,  150, "Once per round: mark a hand card as a +/-1 wildcard.") },
            { "carouser",      new JokerData("carouser",      "The Carouser",     JokerRarity.Rare,      250, "Smoke / Counterfeit / Jack-be-Nimble: 1 free use each per floor.") },
            { "alchemist",     new JokerData("alchemist",     "The Alchemist",    JokerRarity.Rare,      250, "Once per round: transform a hand card with a random positive affix.") },
            { "hometownHero",  new JokerData("hometownHero",  "Hometown Hero",    JokerRarity.Uncommon,  150, "Starting hand draws 50%+ from your run deck (vs 30% base).") },
        };

        public static int PriceFor(JokerRarity r)
        {
            switch (r)
            {
                case JokerRarity.Common:    return Lugen.Core.Constants.PRICE_COMMON;
                case JokerRarity.Uncommon:  return Lugen.Core.Constants.PRICE_UNCOMMON;
                case JokerRarity.Rare:      return Lugen.Core.Constants.PRICE_RARE;
                case JokerRarity.Legendary: return Lugen.Core.Constants.PRICE_LEGENDARY;
            }
            return 0;
        }
    }
}
