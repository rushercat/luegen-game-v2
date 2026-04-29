// Lügen — RelicCatalog.cs
// Translated from RELIC_CATALOG / BOSS_RELIC_POOL / TREASURE_POOL in beta.js
// (around lines 430-465).
//
// Relics are permanent passive bonuses. Max 3 per run (one per act boss).
// Treasure-pool relics drop only at Treasure nodes (Act III).

using System.Collections.Generic;

namespace Lugen.Relics
{
    [System.Serializable]
    public class RelicData
    {
        public string id;
        public string name;
        public int price;
        public string desc;
        public RelicUnlock unlock;          // null = always unlocked.

        public RelicData(string id, string name, int price, string desc, RelicUnlock unlock = null)
        {
            this.id = id; this.name = name; this.price = price; this.desc = desc; this.unlock = unlock;
        }
    }

    [System.Serializable]
    public class RelicUnlock
    {
        public string type;     // "lugenKills" etc.
        public int count;
    }

    public static class RelicCatalog
    {
        public static readonly Dictionary<string, RelicData> All = new Dictionary<string, RelicData>
        {
            { "crackedCoin",    new RelicData("crackedCoin",    "Cracked Coin",    200, "Each round start: gain 5g × Hearts remaining.") },
            { "loadedDie",      new RelicData("loadedDie",      "Loaded Die",      200, "Once per floor, reroll the Target Rank for the current round.") },
            { "pocketWatch",    new RelicData("pocketWatch",    "Pocket Watch",    200, "Your challenge window is +5 seconds (stacks).") },
            { "handMirror",     new RelicData("handMirror",     "Hand Mirror",     250, "At round start, see one random card from each opponent.") },
            { "ironStomach",    new RelicData("ironStomach",    "Iron Stomach",    300, "Glass-burned run-deck cards return as Steel at end of round.") },
            { "ledger",         new RelicData("ledger",         "The Ledger",      300, "+25% gold from all sources (stacks with Gambler).") },
            { "hourglass",      new RelicData("hourglass",      "The Hourglass",   250, "Treasure. Your challenge window is +4s. Bots without it have their windows reduced 30%.") },
            { "seersEye",       new RelicData("seersEye",       "Seer's Eye",      250, "Treasure. See affix ring on every card in every opponent's hand.") },
            { "crackedMirror",  new RelicData("crackedMirror",  "Cracked Mirror",  300, "Treasure. Once per floor: rewind your last play.") },
            { "dragonScale",    new RelicData("dragonScale",    "Dragon Scale",    300, "Treasure. Steel cards in hand grant +1 Jack limit (max +1) and +10% gold per Steel.") },
            { "compass",        new RelicData("compass",        "The Compass",     300, "Boss reward. Bot tells become readable in Act III.") },
            { "tarnishedCrown", new RelicData("tarnishedCrown", "Tarnished Crown", 250, "Boss reward. Win a floor without losing any Hearts on it = +50g bonus.") },
            { "cowardsCloak",   new RelicData("cowardsCloak",   "Coward's Cloak",  200, "Treasure. Pass actions never trigger Echo / Eavesdropper / Cold Read peeks.") },
            { "bookmark",       new RelicData("bookmark",       "The Bookmark",    350, "Boss reward. End of each round: optionally save a hand card into your run deck.") },
            { "steelSpine",     new RelicData("steelSpine",     "Steel Spine",     200, "Treasure. Cursed cards block Liar for 1 turn instead of 2.") },
            { "stackedDeck",    new RelicData("stackedDeck",    "Stacked Deck",    250, "Treasure. Run deck cap raised from 24 to 32.") },
            { "mogul",          new RelicData("mogul",          "The Mogul",       400, "Treasure (Lugen-locked). Shop offers +1 of each. 10% off everything.",
                                  unlock: new RelicUnlock { type = "lugenKills", count = 5 }) },
            { "brassRing",      new RelicData("brassRing",      "Brass Ring",      200, "Treasure. Consumable inventory cap +2 (default 3 → 5).") },
        };

        public static readonly Dictionary<string, string[]> BossPool = new Dictionary<string, string[]>
        {
            { "auditor", new[] { "crackedCoin", "loadedDie", "tarnishedCrown" } },
            { "cheater", new[] { "pocketWatch", "handMirror", "compass" } },
            { "lugen",   new[] { "ironStomach", "ledger", "bookmark" } },
        };

        public static readonly string[] TreasurePool = new[]
        {
            "hourglass", "seersEye", "crackedMirror", "dragonScale",
            "cowardsCloak", "steelSpine", "stackedDeck", "mogul", "brassRing",
        };
    }
}
