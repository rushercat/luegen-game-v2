// Lügen — Shop.cs
// Translated from showShop() / chooseShopItem() and the inventory math
// scattered through beta.js.
//
// Shops appear at every Shop fork node. They offer 3-4 random items from
// the consumable + joker + relic pools, plus a row of run-deck services.

using System.Collections.Generic;
using System.Linq;
using Lugen.Consumables;
using Lugen.Core;
using Lugen.Jokers;
using Lugen.Relics;
using Lugen.Run;

namespace Lugen.Shop
{
    public class Shop
    {
        public List<string> Inventory { get; private set; } = new List<string>();
        public List<string> JokerOffers { get; private set; } = new List<string>();
        public List<string> RelicOffers { get; private set; } = new List<string>();

        public RunState Run { get; private set; }

        public Shop(RunState run)
        {
            Run = run;
            Roll();
        }

        // Stock the shop. Called when entering the Shop fork node.
        public void Roll()
        {
            Inventory.Clear();
            JokerOffers.Clear();
            RelicOffers.Clear();

            // 4 random consumables.
            var consumables = ConsumableCatalog.All.Keys.ToList();
            Rng.ShuffleInPlace(consumables);
            for (int i = 0; i < 4 && i < consumables.Count; i++) Inventory.Add(consumables[i]);

            // 3 random jokers (filter to ones the player doesn't already own,
            // unless stackable and not at max).
            var jokers = JokerCatalog.All.Values
                .Where(j => !Run.jokers.Has(j.id) || (j.stackable && Run.jokers.Stack(j.id) < j.maxStack))
                .ToList();
            Rng.ShuffleInPlace(jokers);
            for (int i = 0; i < 3 && i < jokers.Count; i++) JokerOffers.Add(jokers[i].id);

            // 1 relic — only if player has fewer than 3.
            if (Run.relics.Count < 3)
            {
                var relics = RelicCatalog.All.Values
                    .Where(r => !Run.relics.Contains(r.id) && r.unlock == null)
                    .ToList();
                if (relics.Count > 0) RelicOffers.Add(Rng.Pick(relics).id);
            }
        }

        // Buy a consumable. Returns null on success, an error message on failure.
        public string BuyConsumable(string id)
        {
            if (!ConsumableCatalog.All.TryGetValue(id, out var data)) return "Unknown item.";
            int price = ApplyDiscounts(data.price, data);
            if (Run.gold < price) return "Not enough gold.";
            if (data.floorLocked && Run.floorLockedBoughtThisFloor.TryGetValue(id, out var bought) && bought)
                return "Already bought this floor.";

            Run.gold -= price;
            if (data.floorLocked) Run.floorLockedBoughtThisFloor[id] = true;

            switch (data.kind)
            {
                case ConsumableKind.Inventory:
                    int cap = Constants.INVENTORY_CAP_BASE + (Run.relics.Contains("brassRing") ? Constants.BRASS_RING_BONUS : 0);
                    int currentTotal = Run.inventory.Values.Sum();
                    if (currentTotal >= cap) { Run.gold += price; return "Inventory full."; }
                    Run.inventory[id] = (Run.inventory.TryGetValue(id, out var prev) ? prev : 0) + 1;
                    break;
                case ConsumableKind.Service:
                    // Caller handles the actual service application (e.g. Glass
                    // Shard prompts the user to pick a card). The shop just
                    // takes payment.
                    break;
            }
            return null;
        }

        // Buy a joker. Caller is responsible for equipping after success.
        public string BuyJoker(string id)
        {
            if (!JokerCatalog.All.TryGetValue(id, out var joker)) return "Unknown joker.";
            int price = ApplyDiscounts(joker.price, null);
            if (Run.gold < price) return "Not enough gold.";

            Run.gold -= price;
            if (!Run.jokers.TryEquip(joker))
            {
                Run.gold += price;
                return "All joker slots are full.";
            }
            return null;
        }

        public string BuyRelic(string id)
        {
            if (!RelicCatalog.All.TryGetValue(id, out var relic)) return "Unknown relic.";
            if (Run.relics.Contains(id)) return "Already owned.";
            if (Run.gold < relic.price) return "Not enough gold.";
            Run.gold -= relic.price;
            Run.relics.Add(id);
            return null;
        }

        // Apply standard price modifiers: Forge Hand on affix services,
        // RANDOM.EXE card discount, Engineer affix discount, Rich Folk modifier
        // halving joker prices, Mogul -10%.
        private int ApplyDiscounts(int basePrice, ConsumableData consumable)
        {
            float price = basePrice;

            // Forge Hand: Glass Shard / Spiked Wire / Steel Plating cost 25% less.
            if (consumable != null && Run.jokers.Has("forgeHand"))
            {
                if (consumable.id == "glassShard" || consumable.id == "spikedWire" || consumable.id == "steelPlating")
                    price *= 0.75f;
            }

            // Mogul: 10% off everything.
            if (Run.relics.Contains("mogul")) price *= 0.90f;

            return System.Math.Max(1, (int)System.Math.Round(price));
        }
    }
}
