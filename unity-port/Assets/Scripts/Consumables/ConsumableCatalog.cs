// Lügen — ConsumableCatalog.cs
// Translated from CONSUMABLE_INFO / SHOP_ITEMS in beta.js (around line 533+).
//
// "Consumables" in Lügen are inventory items you spend gold on at the
// Shop. Some are floor-locked (one purchase per floor). Some are services
// (one-time, applied to a run-deck card and gone).

using System.Collections.Generic;

namespace Lugen.Consumables
{
    public enum ConsumableKind
    {
        Inventory,   // Goes into inventory; consumes on use.
        Service,     // Immediately applied to run deck (no inventory entry).
        Joker,       // Equips a joker.
        Relic,       // Adds to relics.
    }

    [System.Serializable]
    public class ConsumableData
    {
        public string id;
        public string name;
        public int price;
        public string desc;
        public ConsumableKind kind;
        public bool floorLocked;

        public ConsumableData(string id, string name, int price, string desc, ConsumableKind kind = ConsumableKind.Inventory, bool floorLocked = false)
        {
            this.id = id; this.name = name; this.price = price; this.desc = desc; this.kind = kind; this.floorLocked = floorLocked;
        }
    }

    public static class ConsumableCatalog
    {
        public static readonly Dictionary<string, ConsumableData> All = new Dictionary<string, ConsumableData>
        {
            // Inventory consumables
            { "smokeBomb",      new ConsumableData("smokeBomb",      "Smoke Bomb",      35,  "Skip your turn.") },
            { "counterfeit",    new ConsumableData("counterfeit",    "Counterfeit",     50,  "Change the target rank for the rest of the round.") },
            { "jackBeNimble",   new ConsumableData("jackBeNimble",   "Jack-be-Nimble",  90,  "Discard up to 2 Jacks from your hand.", floorLocked: true) },
            { "whisperNetwork", new ConsumableData("whisperNetwork", "Whisper Network", 30,  "Hear how many Jacks each opponent currently holds.") },
            { "luckyCoin",      new ConsumableData("luckyCoin",      "Lucky Coin",      20,  "Re-roll the affix on one hand card.") },
            { "snakeEyes",      new ConsumableData("snakeEyes",      "Snake Eyes",      45,  "Cancel the next Target Rank rotation.") },
            { "emptyThreat",    new ConsumableData("emptyThreat",    "Empty Threat",    40,  "Feign a Liar call against the next bot.", floorLocked: true) },
            { "distillation",   new ConsumableData("distillation",   "Distillation",    60,  "Merge 2 same-rank hand cards into 1 with a random affix.") },
            { "pickpocket",     new ConsumableData("pickpocket",     "Pickpocket",      90,  "Steal a random non-Jack from an opponent.", floorLocked: true) },
            { "deadDrop",       new ConsumableData("deadDrop",       "Dead Drop",       70,  "Discard 3 random hand cards, then draw 3 from the draw pile.") },
            { "markedDeck",     new ConsumableData("markedDeck",     "Marked Deck",     100, "Apply a chosen affix to a random draw-pile card.", floorLocked: true) },
            { "jokersMask",     new ConsumableData("jokersMask",     "The Joker's Mask",75,  "Tag a non-Jack so it counts as a Jack for the curse.") },
            { "mirrorShard",    new ConsumableData("mirrorShard",    "Mirror Shard",    45,  "Arm: the next Liar call against you reveals only the result.") },
            { "stackedHand",    new ConsumableData("stackedHand",    "Stacked Hand",    100, "Arm: next round, +20% extra of starting hand from your run deck.") },
            { "crookedDie",     new ConsumableData("crookedDie",     "Crooked Die",     50,  "Re-roll the Target Rank for this round only.", floorLocked: true) },
            { "lieDetector",    new ConsumableData("lieDetector",    "Lie Detector",    60,  "Arm: peek truth/lie privately before deciding to call.") },
            { "tracer",         new ConsumableData("tracer",         "Tracer",          40,  "See top 3 cards of the draw pile and rearrange them.") },

            // Services (applied immediately; no inventory entry)
            { "glassShard",     new ConsumableData("glassShard",     "Glass Shard",     30,  "Apply Glass to a run-deck card.", kind: ConsumableKind.Service) },
            { "spikedWire",     new ConsumableData("spikedWire",     "Spiked Wire",     30,  "Apply Spiked to a run-deck card.", kind: ConsumableKind.Service) },
            { "steelPlating",   new ConsumableData("steelPlating",   "Steel Plating",   50,  "Apply Steel to a run-deck card.",  kind: ConsumableKind.Service) },
            { "mirageLens",     new ConsumableData("mirageLens",     "Mirage Lens",     200, "Apply Mirage to a run-deck card.", kind: ConsumableKind.Service) },
            { "stripper",       new ConsumableData("stripper",       "Stripper",        60,  "Permanently remove one card from your run deck.", kind: ConsumableKind.Service) },
            { "engraver",       new ConsumableData("engraver",       "Engraver",        80,  "Add one new vanilla card to your run deck.", kind: ConsumableKind.Service) },
            { "forger",         new ConsumableData("forger",         "Forger",          100, "Clone one run-deck card onto another.", kind: ConsumableKind.Service) },
            { "devilsBargain",  new ConsumableData("devilsBargain",  "Devil's Bargain", 55,  "Drop a hand card to bottom of draw pile; draw top with Cursed.", kind: ConsumableKind.Service) },
            { "magnet",         new ConsumableData("magnet",         "Magnet",          75,  "Give one hand card to a random opponent.", kind: ConsumableKind.Service) },
        };
    }
}
