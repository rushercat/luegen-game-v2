// Lügen — LiarResolver.cs
// Translated from callLiar() in public/beta.js (around lines 2625-3010).
//
// When a LIAR call fires, we have to:
//
//   1. Reveal the played cards (the "last play" — N cards from the end of the pile).
//   2. Decide truth vs. lie. Match conditions:
//        - rank == claim, OR
//        - affix == Mirage, OR
//        - Trickster-marked card +/-1 from claim on rank ladder.
//   3. Run Glass on-reveal (burn pile cards, possibly hit the burn cap).
//   4. Spiked draws on the player who picks up the pile.
//   5. Distribute the pile to challenger (truth) or liar (caught).
//   6. Cursed-lock the picked-up cards (2 turns, or 1 with Steel Spine).
//   7. Skip the challenger if they were wrong; otherwise they lead next turn.
//   8. Rotate target rank if a truth was told (next-next-player rule).
//
// This file resolves the rules of the call. Joker side-effects (Spiked Trap,
// Last Word, Caller's Mark, Magpie, Dead Hand, Ricochet, Memorizer) live in
// the joker hooks (see Jokers/JokerHooks.cs).

using System.Collections.Generic;
using System.Linq;
using Lugen.Affixes;
using Lugen.Cards;
using Lugen.Core;

namespace Lugen.Round
{
    public class LiarOutcome
    {
        public bool truthTold;          // True = challenger was wrong.
        public List<PlayedCard> revealed = new List<PlayedCard>();  // The cards that were "the last play".
        public List<Card> burnedFromGlass = new List<Card>();
        public bool burnCapTripped;
        public int recycledFromBurnCap; // Count of cards moved back into draw pile.

        public int pickerUpper;         // Whoever takes the pile (challenger or liar).
        public int spikedDrawsTriggered;
    }

    public static class LiarResolver
    {
        // Rank ladder used by Trickster's +/-1 wildcard match.
        private static readonly Rank[] RankLadder = { Rank.Jack, Rank.Ten, Rank.Queen, Rank.King, Rank.Ace };

        public static LiarOutcome Resolve(RoundState s, int challengerIdx, bool witchUncappedGlass, bool ironStomachActive, bool steelSpineActive)
        {
            var outcome = new LiarOutcome();
            if (!s.challengeOpen || s.lastPlay == null) return outcome;
            s.challengeOpen = false;

            var lp = s.lastPlay;
            int n = lp.count;
            var revealed = s.pile.Skip(s.pile.Count - n).ToList();
            outcome.revealed = revealed;

            bool allMatch = revealed.All(p =>
                p.card.rank == lp.claim ||
                p.card.affix == Affix.Mirage ||
                IsTricksterMatch(s, p.card, lp.claim));

            outcome.truthTold = allMatch;

            // ---- Glass on reveal ----
            int glassPlayed = revealed.Count(p => p.card.affix == Affix.Glass);
            if (glassPlayed > 0)
            {
                var burnedThisTrigger = new List<Card>();
                for (int g = 0; g < glassPlayed; g++)
                {
                    int glassIdx = s.pile.FindIndex(p => p.card.affix == Affix.Glass);
                    if (glassIdx >= 0)
                    {
                        var bc = s.pile[glassIdx].card;
                        if (ironStomachActive && bc.owner == 0) s.ironStomachBurned.Add(bc.id);
                        burnedThisTrigger.Add(bc);
                        s.pile.RemoveAt(glassIdx);
                    }
                    for (int i = 0; i < Constants.GLASS_BURN_RANDOM; i++)
                    {
                        var burnable = new List<int>();
                        for (int j = 0; j < s.pile.Count; j++)
                        {
                            if (s.pile[j].card.affix != Affix.Steel) burnable.Add(j);
                        }
                        if (burnable.Count == 0) break;
                        int pick = burnable[Rng.Range(0, burnable.Count)];
                        var bc2 = s.pile[pick].card;
                        if (ironStomachActive && bc2.owner == 0) s.ironStomachBurned.Add(bc2.id);
                        burnedThisTrigger.Add(bc2);
                        s.pile.RemoveAt(pick);
                    }
                }
                outcome.burnedFromGlass.AddRange(burnedThisTrigger);

                if (witchUncappedGlass)
                {
                    s.burnedCards.Clear();
                }
                else
                {
                    s.burnedCards.AddRange(burnedThisTrigger);
                    if (s.burnedCards.Count > Constants.BURN_CAP)
                    {
                        outcome.burnCapTripped = true;
                        outcome.recycledFromBurnCap = s.burnedCards.Count;
                        s.drawPile.AddRange(s.burnedCards);
                        Rng.ShuffleInPlace(s.drawPile);
                        s.burnedCards.Clear();
                    }
                }
            }

            // ---- Distribute the pile ----
            int taker = allMatch ? challengerIdx : lp.playerIdx;
            outcome.pickerUpper = taker;
            int spikedCount = s.pile.Count(p => p.card.affix == Affix.Spiked);
            int cursedLockTurns = steelSpineActive ? 1 : 2;

            foreach (var p in s.pile)
            {
                var card = p.card.Clone();
                card.owner = p.card.owner;
                if (card.affix == Affix.Cursed && taker == 0)
                {
                    card.cursedLockTurns = cursedLockTurns;
                }
                s.hands[taker].Add(card);
            }
            s.pile.Clear();

            // Spiked: +1 draw per Spiked picked up.
            int drawn = 0;
            for (int i = 0; i < spikedCount * Constants.SPIKED_DRAWS_ON_PICKUP; i++)
            {
                if (s.drawPile.Count == 0) break;
                s.hands[taker].Add(Lugen.Deck.DeckBuilder.PopTop(s.drawPile));
                drawn++;
            }
            outcome.spikedDrawsTriggered = drawn;

            return outcome;
        }

        private static bool IsTricksterMatch(RoundState s, Card card, Rank claim)
        {
            if (string.IsNullOrEmpty(s.tricksterMarkedId) || card.id != s.tricksterMarkedId) return false;
            int ci = System.Array.IndexOf(RankLadder, card.rank);
            int ti = System.Array.IndexOf(RankLadder, claim);
            if (ci < 0 || ti < 0) return false;
            return System.Math.Abs(ci - ti) == 1;
        }

        // Decrement the Cursed lock counters at the start of every turn — mirrors
        // the JS code's per-turn tick.
        public static void TickCursedLocks(RoundState s, int playerIdx)
        {
            foreach (var c in s.hands[playerIdx])
            {
                if (c.cursedLockTurns > 0) c.cursedLockTurns--;
            }
        }
    }
}
