// Lügen — AffixHooks.cs
// All affix triggers gathered into one file so the rules are easy to read
// alongside each other. Each method is a pure function that mutates the
// round state passed in — Unity-side code can call them from coroutines,
// timeline events, etc.
//
// Trigger map (one method per family):
//
//   PASSIVE (per-turn-start)
//     Gilded   →  TriggerGildedTurn()
//     Cursed   →  TickCursedLocks() lives in LiarResolver (per-turn tick)
//
//   ON-PLAY
//     Mirage   →  ConsumeMirageUse()       (Decrement uses, removes at 3)
//     Hollow   →  Handled inline in TurnResolver (draw replacement)
//     Echo     →  Handled inline in TurnResolver (set echoArmedFor)
//
//   ON-REVEAL
//     Glass    →  Handled inline in LiarResolver (burn pile cards)
//
//   ON-PICKUP
//     Spiked   →  Handled inline in LiarResolver (extra draws)
//
// (Steel is a tag, not a trigger — it's just "immune to mutation".)

using System.Collections.Generic;
using Lugen.Affixes;
using Lugen.Cards;
using Lugen.Core;
using Lugen.Round;

namespace Lugen.Affixes
{
    public static class AffixHooks
    {
        // Gilded: at the start of every turn, the seat that holds Gilded
        // cards earns +2g per Gilded card in hand. (Was a round-start trigger
        // in early prototypes — moved to per-turn so building Gilded keeps
        // paying you while you stall.)
        //
        // The Patron joker stacks on top: +1g per Gilded per turn (held by HUMAN).
        public static int CalculateGildedIncome(IList<Card> hand, bool patronEquipped)
        {
            int gilded = 0;
            foreach (var c in hand) if (c.affix == Affix.Gilded) gilded++;
            int gold = gilded * Constants.GOLD_PER_GILDED_PER_TURN;
            if (patronEquipped) gold += gilded; // +1g/Gilded
            return gold;
        }

        // Mirage: track per-card use count. The card is consumed on the 3rd
        // play. The JS code only does this for human-owned (run-deck) Mirages —
        // bots' Mirages just trigger and disappear without persistent tracking.
        // Returns true if the card was consumed (removed from the run deck).
        public static bool ConsumeMirageUse(Card card, IList<Card> humanRunDeck)
        {
            if (card.affix != Affix.Mirage) return false;
            if (card.owner != 0) return false;

            // Find the canonical run-deck card by id (the one in `cards` is
            // a clone from the round deck).
            for (int i = 0; i < humanRunDeck.Count; i++)
            {
                if (humanRunDeck[i].id == card.id)
                {
                    humanRunDeck[i].mirageUses++;
                    if (humanRunDeck[i].mirageUses >= 3)
                    {
                        humanRunDeck.RemoveAt(i);
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        // Apply random affixes to N cards in the draw pile. Mirrors the per-
        // floor static-affix infusion that runs after deal (see beta.js
        // "Per-floor random-affix infusion"). Skips Brittle floors — they
        // glass everything separately.
        public static int InfuseDrawPileWithRandomAffixes(IList<Card> drawPile, int floor)
        {
            // Number of cards to randomly affix scales with floor.
            int target = System.Math.Min(drawPile.Count, 1 + (floor / 3));
            int infused = 0;
            for (int i = 0; i < target; i++)
            {
                int idx = Rng.Range(0, drawPile.Count);
                var c = drawPile[idx];
                if (c.affix != Affix.None) continue;       // Don't overwrite.
                if (c.rank == Rank.Jack) continue;          // Don't affix Jacks.
                c.affix = Rng.Pick(AffixExtensions.AllRandomable);
                infused++;
            }
            return infused;
        }

        // Brittle modifier: every card becomes Glass for the round.
        public static void ApplyBrittleFloor(IList<List<Card>> hands, IList<Card> drawPile)
        {
            foreach (var hand in hands)
                foreach (var c in hand) c.affix = Affix.Glass;
            foreach (var c in drawPile) c.affix = Affix.Glass;
        }
    }
}
