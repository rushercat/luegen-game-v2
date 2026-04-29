// Lügen — GoldEconomy.cs
// Centralized gold-add helper. Translated from addGold() in beta.js.
//
// Multiple multipliers stack:
//   - Gambler character: ×1.5
//   - The Ledger relic:  ×1.25
//   - Greedy floor mod:  ×2.0
//   - Rich Folk floor mod: ×0.5

using Lugen.Run;

namespace Lugen.Run
{
    public static class GoldEconomy
    {
        // Add gold respecting all stackable multipliers. Returns the
        // ACTUAL gold added (post-multipliers), so callers can log
        // "+25g" rather than the pre-multiplier "+20g".
        public static int AddGold(RunState run, int baseAmount)
        {
            float multiplier = 1f;

            // Character.
            var character = Lugen.Characters.CharacterCatalog.Get(run.characterId);
            if (character != null && character.goldMultiplier.HasValue)
                multiplier *= character.goldMultiplier.Value;

            // Ledger relic.
            if (run.relics.Contains("ledger")) multiplier *= Lugen.Core.Constants.LEDGER_GOLD_MULT;

            // Floor modifiers.
            if (run.currentFloorModifier == "greedy") multiplier *= 2f;
            if (run.currentFloorModifier == "richFolk") multiplier *= 0.5f;

            int total = (int)System.Math.Floor(baseAmount * multiplier);
            run.gold += total;
            return total;
        }
    }
}
