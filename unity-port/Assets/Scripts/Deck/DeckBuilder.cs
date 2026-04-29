// Lügen — DeckBuilder.cs
// Translated from buildDeck() / buildInitialRunDeck() / deal() / shuffle() in
// public/beta.js (around lines 1916-2050).
//
// The round deck is constructed fresh each round from THREE sources:
//
//   1. Base 30 vanilla cards    — 6 each of A/K/Q/10 + 6 Jacks (no owner).
//   2. Each player's run deck   — 8+ owned cards each (their build).
//   3. Per-rank cap             — at most 16 cards of each rank may end
//                                 up in a single round, with affixed cards
//                                 prioritized over plain.
//
// Then we shuffle and deal 5 to each player. Hoarder character gets +1.
// Lugen boss gets +2.

using System.Collections.Generic;
using System.Linq;
using Lugen.Affixes;
using Lugen.Cards;
using Lugen.Core;

namespace Lugen.Deck
{
    public static class DeckBuilder
    {
        // Build the per-round shuffled deck. `runDecks[p]` is the personal
        // deck of seat p (0 = human). For non-human seats in solo we use a
        // synthetic vanilla starter deck (matches buildInitialRunDeck in JS).
        public static List<Card> BuildDeck(IList<List<Card>> runDecks)
        {
            var deck = new List<Card>();

            // 1) Base 30: 6 Jacks + 6 of each non-Jack target rank.
            for (int i = 0; i < Constants.BASE_JACKS_PER_ROUND; i++)
            {
                deck.Add(new Card(Rank.Jack, CardIdFactory.RoundDeck(Rank.Jack, i), owner: -1));
            }
            foreach (var r in RankExtensions.TargetRanks)
            {
                for (int i = 0; i < Constants.BASE_NON_JACK_PER_ROUND; i++)
                {
                    deck.Add(new Card(r, CardIdFactory.RoundDeck(r, i), owner: -1));
                }
            }

            // 2) Bucket every player's run-deck cards by rank. Clone so we
            //    don't mutate the source decks (JS uses {...card}).
            var buckets = new Dictionary<Rank, List<Card>>();
            foreach (var r in RankExtensions.AllRanks) buckets[r] = new List<Card>();

            for (int p = 0; p < runDecks.Count; p++)
            {
                if (runDecks[p] == null) continue;
                foreach (var card in runDecks[p])
                {
                    if (buckets.ContainsKey(card.rank))
                    {
                        var clone = card.Clone();
                        clone.owner = p; // re-stamp ownership in case decks were swapped
                        buckets[card.rank].Add(clone);
                    }
                }
            }

            // 3) For each rank: count what's already in the deck (the base
            //    cards). Player-deck cards fill up to ROUND_DECK_RANK_CAP
            //    total per rank. Affixed cards are prioritized so a player's
            //    investment isn't silently dropped on degenerate over-cap builds.
            foreach (var rank in buckets.Keys.ToList())
            {
                var cards = buckets[rank];
                int alreadyInDeck = deck.Count(c => c.rank == rank);
                int remainingSlots = System.Math.Max(0, Constants.ROUND_DECK_RANK_CAP - alreadyInDeck);
                if (remainingSlots == 0) continue;

                if (cards.Count <= remainingSlots)
                {
                    deck.AddRange(cards);
                    continue;
                }

                var affixed = Rng.Shuffled(cards.Where(c => c.affix != Affix.None));
                var plain   = Rng.Shuffled(cards.Where(c => c.affix == Affix.None));
                var ordered = affixed.Concat(plain).ToList();
                for (int i = 0; i < remainingSlots; i++) deck.Add(ordered[i]);
            }

            Rng.ShuffleInPlace(deck);
            return deck;
        }

        // Vanilla starter run deck: 2 each of A/K/Q/10. Matches buildInitialRunDeck.
        public static List<Card> BuildInitialRunDeck(int playerIdx)
        {
            var deck = new List<Card>();
            foreach (var r in RankExtensions.TargetRanks)
            {
                for (int i = 0; i < Constants.RUN_DECK_PER_RANK; i++)
                {
                    deck.Add(new Card(r, CardIdFactory.RunDeck(playerIdx, r, i), owner: playerIdx));
                }
            }
            return deck;
        }

        // Result type for Deal() so callers don't have to juggle out-params.
        public struct DealResult
        {
            public List<List<Card>> hands;   // hands[p] = that seat's starting hand
            public List<Card>       drawPile; // remainder of the deck (top of pile = last index)
        }

        // Deal HAND_SIZE cards to each seat. Hoarder (handSizeBonus = 1) and
        // Lugen (lugenStartingBonus) are passed in by the caller.
        public static DealResult Deal(List<Card> deck, int numPlayers, int humanHandBonus = 0, bool[] lugenSeats = null)
        {
            var hands = new List<List<Card>>();
            for (int p = 0; p < numPlayers; p++) hands.Add(new List<Card>());

            for (int i = 0; i < Constants.HAND_SIZE; i++)
            {
                for (int p = 0; p < numPlayers; p++)
                {
                    if (deck.Count == 0) break;
                    hands[p].Add(PopTop(deck));
                }
            }

            // Hoarder bonus (extra cards for the human).
            for (int i = 0; i < humanHandBonus; i++)
            {
                if (deck.Count == 0) break;
                hands[0].Add(PopTop(deck));
            }

            // Lugen specials: starts with 7 cards.
            if (lugenSeats != null)
            {
                for (int p = 1; p < numPlayers && p < lugenSeats.Length; p++)
                {
                    if (lugenSeats[p])
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            if (deck.Count == 0) break;
                            hands[p].Add(PopTop(deck));
                        }
                    }
                }
            }

            return new DealResult { hands = hands, drawPile = deck };
        }

        public static Card PopTop(List<Card> deck)
        {
            // JS uses .pop() (off the end) — keep that convention.
            var c = deck[deck.Count - 1];
            deck.RemoveAt(deck.Count - 1);
            return c;
        }
    }
}
