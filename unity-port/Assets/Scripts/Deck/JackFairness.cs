// Lügen — JackFairness.cs
// Translated from applyJackFairness() and enforceOwnDeckMinimum() in
// public/beta.js. The rules:
//
// 1. Starting-hand fairness: no player can begin a round already AT their
//    Jack limit. After the deal, if a player has Jacks >= limit, swap Jacks
//    down to (limit - 1). Excess Jacks go to the bottom of the draw pile,
//    and we replace from the top with non-Jack cards.
//
//    At the standard 4-limit, you start with at most 3 Jacks.
//    Safety Net (5-limit) caps starts at 4. Greedy modifier (3-limit) caps at 2.
//
// 2. Own-deck minimum: at least 30% of the human's starting hand must be
//    drawn from their own run deck. Hometown Hero pushes that to 50%, and
//    Stacked Hand consumable adds another +20%.
//
// These two rules are both deck shaping that happens AFTER the initial deal
// but BEFORE the round starts.

using System;
using System.Collections.Generic;
using System.Linq;
using Lugen.Cards;
using Lugen.Core;

namespace Lugen.Deck
{
    public static class JackFairness
    {
        // Returns a count of "Jack-curse weight". A normal Jack counts 1; a
        // Steel Jack counts 2 (per design doc — "Steel Jack counts double
        // toward Jack curse"). Mirrors jackCurseWeight() in beta.js.
        public static int JackCurseWeight(IList<Card> hand)
        {
            int w = 0;
            foreach (var c in hand)
            {
                if (c.rank != Rank.Jack) continue;
                w += (c.affix == Lugen.Affixes.Affix.Steel) ? 2 : 1;
            }
            return w;
        }

        public static int CountJacks(IList<Card> hand) => hand.Count(c => c.rank == Rank.Jack);

        // For each seat, swap Jacks above (limit - 1) back into the draw pile.
        // jackLimitFor(p) is provided by the caller — it depends on character,
        // jokers, and floor modifiers, all of which live outside this helper.
        public static void ApplyJackFairness(
            IList<List<Card>> hands,
            List<Card> drawPile,
            Func<int, int> jackLimitFor)
        {
            for (int p = 0; p < hands.Count; p++)
            {
                int limit = jackLimitFor(p);
                int safeCap = Math.Max(0, limit - 1);
                while (JackCurseWeight(hands[p]) > safeCap)
                {
                    int jackIdx = hands[p].FindIndex(c => c.rank == Rank.Jack);
                    if (jackIdx < 0) break;
                    int swapIdx = drawPile.FindIndex(c => c.rank != Rank.Jack);
                    if (swapIdx < 0) break;
                    var jack    = hands[p][jackIdx];
                    var nonJack = drawPile[swapIdx];
                    hands[p][jackIdx]   = nonJack;
                    drawPile[swapIdx]   = jack;
                }
            }
        }

        // Ensure the human (seat 0) has at least `minFraction` of their hand
        // drawn from their own run deck. Swap from drawPile when not.
        // Steel cards aren't swapped out (they're rare and an investment).
        public static void EnforceOwnDeckMinimum(
            IList<List<Card>> hands,
            List<Card> drawPile,
            float minFraction)
        {
            if (hands == null || hands.Count == 0) return;
            var hand = hands[0];
            int targetCount = (int)Math.Ceiling(hand.Count * minFraction);
            int ownCount    = hand.Count(c => c.owner == 0);
            int needed = targetCount - ownCount;
            while (needed > 0)
            {
                // Find an "own-deck" card in the draw pile to swap in.
                int ownInDraw = drawPile.FindIndex(c => c.owner == 0);
                if (ownInDraw < 0) break;
                // Find a non-own, non-Steel card in the hand to swap out.
                int swapOut = hand.FindIndex(c => c.owner != 0 && c.affix != Lugen.Affixes.Affix.Steel);
                if (swapOut < 0) break;

                var into = drawPile[ownInDraw];
                var outc = hand[swapOut];
                hand[swapOut]      = into;
                drawPile[ownInDraw] = outc;
                needed--;
            }
        }
    }
}
