// Lügen — ForkNode.cs
// Translated from the fork-node logic in beta.js (showFork / chooseFork /
// rollForkOptions / etc.).
//
// After every non-boss floor you face a 3-way fork. After every boss
// floor you face a 2-way mini-fork (Shop or Reward).
//
// Node types:
//   Shop      — always available in non-boss forks.
//   Event     — narrative or mechanical surprise.
//   Reward    — pick from 2 jokers, or take 75g.
//   Cleanse   — remove a Cursed card permanently or strip 1 affix.
//   Treasure  — Act III only; 33% chance to swap into the Reward slot.

using System.Collections.Generic;

namespace Lugen.Floor
{
    public enum ForkNodeType { Shop, Event, Reward, Cleanse, Treasure }

    [System.Serializable]
    public class ForkOptions
    {
        public List<ForkNodeType> options = new List<ForkNodeType>();
    }

    public static class ForkNode
    {
        // Build a fork for the floor that was JUST cleared.
        // Mirrors beta.js rollForkOptions.
        public static ForkOptions RollFork(int floorJustCleared, bool isBossFloor)
        {
            var fork = new ForkOptions();
            if (isBossFloor)
            {
                // 2-way mini-fork after a boss: Shop + Reward.
                fork.options.Add(ForkNodeType.Shop);
                fork.options.Add(ForkNodeType.Reward);
                return fork;
            }

            // 3-way fork: Shop is always one of the 3.
            fork.options.Add(ForkNodeType.Shop);

            // Slot 2 + 3: rotate from a pool. Pool changes by act.
            int act = (floorJustCleared <= 3) ? 1 : (floorJustCleared <= 6) ? 2 : 3;
            var pool = new List<ForkNodeType> { ForkNodeType.Reward, ForkNodeType.Event, ForkNodeType.Cleanse };

            // Act III: 33% chance Reward becomes Treasure.
            if (act == 3 && Lugen.Core.Rng.Chance(Lugen.Core.Constants.TREASURE_CHANCE_ACT_III))
            {
                pool[0] = ForkNodeType.Treasure;
            }

            // Pick 2 distinct from pool.
            Lugen.Core.Rng.ShuffleInPlace(pool);
            fork.options.Add(pool[0]);
            fork.options.Add(pool[1]);
            return fork;
        }
    }
}
