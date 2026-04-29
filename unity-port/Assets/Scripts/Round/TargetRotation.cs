// Lügen — TargetRotation.cs
// Translated from rotateTargetRank() / pickRandomTargetRank() / pickRotatedTarget()
// in server.js (lines 662-720) and the human-bias version in beta.js (around
// line 1280).
//
// After a truthful play, the target rotates — but with a twist: the round-
// start target is biased toward the player's stacked rank in their RUN DECK
// (70% chance), and rotations also avoid the same rank twice in a row.

using System.Collections.Generic;
using System.Linq;
using Lugen.Cards;
using Lugen.Core;

namespace Lugen.Round
{
    public static class TargetRotation
    {
        // Round-start: bias toward the rank you have the most of in your run deck.
        // If there's no clear winner (ties at the top), fall back to pure random.
        // Inverted floor modifier overrides everything to Jack.
        public static Rank PickInitialTarget(IList<Card> humanRunDeck, bool invertedFloor)
        {
            if (invertedFloor) return Rank.Jack;

            var counts = new Dictionary<Rank, int>();
            foreach (var r in RankExtensions.TargetRanks) counts[r] = 0;
            if (humanRunDeck != null)
            {
                foreach (var c in humanRunDeck)
                {
                    if (counts.ContainsKey(c.rank)) counts[c.rank]++;
                }
            }

            int maxCount = 0;
            foreach (var r in RankExtensions.TargetRanks) if (counts[r] > maxCount) maxCount = counts[r];
            var top = RankExtensions.TargetRanks.Where(r => counts[r] == maxCount).ToList();

            // No data, or tied at top → random.
            if (maxCount == 0 || top.Count > 1)
            {
                return Rng.Pick(RankExtensions.TargetRanks);
            }
            // Bias 70% toward the stacked rank, otherwise random.
            if (Rng.NextDouble() >= Constants.TARGET_BIAS_CHANCE)
            {
                return Rng.Pick(RankExtensions.TargetRanks);
            }
            return top[0];
        }

        // After a truth, rotate: pick a different rank from a randomized pool.
        // Snake Eyes consumable cancels the next rotation (handled by caller —
        // they should set state.snakeEyesLock and skip this call).
        public static Rank Rotate(Rank currentTarget)
        {
            var pool = RankExtensions.TargetRanks.Where(r => r != currentTarget).ToList();
            return Rng.Pick(pool);
        }
    }
}
