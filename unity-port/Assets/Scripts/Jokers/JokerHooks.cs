// Lügen — JokerHooks.cs
// Hand-translated from the joker triggers scattered through callLiar(),
// playCards(), and the pile-distribute closure in public/beta.js. These
// are the per-joker side-effects the rules engine fires.
//
// Each hook is a static method that takes the current run/round state and
// applies its effect. They're idempotent / side-effect-only — the caller
// owns sequencing.
//
// Where a joker's effect is purely a tunable (Slow Hand: window = 10s),
// it's read directly from JokerSlots.Has() inline at the call site rather
// than going through here.

using System.Collections.Generic;
using System.Linq;
using Lugen.Affixes;
using Lugen.Cards;
using Lugen.Core;
using Lugen.Round;

namespace Lugen.Jokers
{
    public static class JokerHooks
    {
        // ---- Round-start hooks --------------------------------------------------------

        // Cold Read: see one random card from each opponent's starting hand.
        // Returns a list of "<seat>: <rank>" strings for the UI to peek-display.
        public static List<string> TriggerColdRead(RoundState s)
        {
            var peeks = new List<string>();
            for (int i = 1; i < s.NumPlayers; i++)
            {
                if (s.hands[i].Count > 0)
                {
                    var c = Rng.Pick(s.hands[i]);
                    peeks.Add($"seat {i}: {c.rank.ToShort()}");
                }
            }
            return peeks;
        }

        // ---- After-play hooks ---------------------------------------------------------

        // Eavesdropper: when the player BEFORE you plays, fuzzy-count their
        // matching cards. Fires every 2 rounds (caller should track `lastFiredRound`).
        public static string TriggerEavesdropper(RoundState s, int playerJustPlayed, int humanSeat)
        {
            int prevSeat = (humanSeat - 1 + s.NumPlayers) % s.NumPlayers;
            if (playerJustPlayed != prevSeat) return null;
            int matches = s.hands[playerJustPlayed].Count(c => c.rank == s.targetRank);
            string bucket = matches == 0 ? "NONE" : matches <= 2 ? "SOME (1-2)" : "MANY (3+)";
            return $"Eavesdropper: seat {playerJustPlayed} has {bucket} matches for {s.targetRank.ToShort()}.";
        }

        // Sixth Sense: 15% per stack chance to learn whether the play was a
        // bluff. Returns null if the roll missed.
        public static string TriggerSixthSense(RoundState s, int playerWhoPlayed, int stacks, IEnumerable<Card> playedCards)
        {
            double chance = 0.15 * stacks;
            if (!Rng.Chance(chance)) return null;
            bool isBluff = !playedCards.All(c =>
                c.rank == s.targetRank ||
                c.affix == Affix.Mirage);
            return $"Sixth Sense ({(int)(chance * 100)}%): seat {playerWhoPlayed}'s play was " +
                (isBluff ? "a BLUFF" : "truth") + ".";
        }

        // ---- Pickup hooks (LIAR resolved, pile assigned) -----------------------------

        // Magpie: when an opponent picks up the pile, gain 1g per affixed card.
        public static int TriggerMagpie(IEnumerable<PlayedCard> pile, int taker, int humanSeat)
        {
            if (taker == humanSeat) return 0; // Doesn't fire when you take the pile.
            return pile.Count(p => p.card.affix != Affix.None);
        }

        // Taxman: when an opponent takes a pile of 5+ cards, gain 10g.
        public static int TriggerTaxman(int pileSize, int taker, int humanSeat)
        {
            if (taker == humanSeat) return 0;
            return pileSize >= 5 ? 10 : 0;
        }

        // Dead Hand: keep the first 2 Jacks in a pile out of YOUR hand.
        // Returns the IDs of cards to redirect to the bottom of the draw pile.
        public static HashSet<string> TriggerDeadHand(IEnumerable<PlayedCard> pile, int taker, int humanSeat, ref bool deadHandUsedThisFloor)
        {
            var ids = new HashSet<string>();
            if (taker != humanSeat) return ids;
            if (deadHandUsedThisFloor) return ids;
            int kept = 0;
            foreach (var p in pile)
            {
                if (p.card.rank != Rank.Jack) continue;
                if (kept >= 2) break;
                ids.Add(p.card.id);
                kept++;
            }
            if (kept > 0) deadHandUsedThisFloor = true;
            return ids;
        }

        // Ricochet: 3+ Jacks in a pile = half (rounded down) bounce to a
        // random opponent. Returns (bouncedIds, target) — empty set if not triggered.
        public static (HashSet<string> ids, int target) TriggerRicochet(
            List<PlayedCard> pile, int taker, int humanSeat, RoundState s, HashSet<string> deadHandIds)
        {
            var ids = new HashSet<string>();
            int target = -1;
            if (taker != humanSeat) return (ids, -1);

            var eligible = pile.Where(p => p.card.rank == Rank.Jack && !deadHandIds.Contains(p.card.id)).ToList();
            if (eligible.Count < 3) return (ids, -1);

            int bounceN = eligible.Count / 2;
            var targets = new List<int>();
            for (int i = 1; i < s.NumPlayers; i++)
            {
                if (!s.eliminated[i] && !s.finished[i]) targets.Add(i);
            }
            if (targets.Count == 0) return (ids, -1);

            target = Rng.Pick(targets);
            for (int i = 0; i < bounceN; i++) ids.Add(eligible[i].card.id);
            return (ids, target);
        }

        // ---- Caught-lying hooks ------------------------------------------------------

        // Scapegoat: caught lying with at least 1 Jack in the pile? One Jack
        // gets forced into the challenger's hand (the rest still goes to you).
        public static string TriggerScapegoat(RoundState s, IEnumerable<PlayedCard> revealed, int liar, int challenger)
        {
            if (liar != 0) return null; // Human-only trigger in solo.
            var jackEntry = revealed.FirstOrDefault(p => p.card.rank == Rank.Jack);
            if (jackEntry == null) return null;
            // Caller will need to physically move the Jack — the hook just
            // surfaces the trigger string + identifies the card via the entry.
            return $"Scapegoat: a Jack is forced into challenger seat {challenger}.";
        }

        // ---- Caller's Mark gold delta on first LIAR call of the round ----------------

        // Returns the gold delta (positive = gain, negative = lose). Fires
        // once per round; caller should set state.callersMarkFiredThisRound = true.
        public static int TriggerCallersMark(bool truthTold)
        {
            return truthTold ? -15 : +20;
        }

        // ---- Spiked Trap: truth + challenged = challenger draws +3 -------------------

        public static int TriggerSpikedTrap(RoundState s, int liar, int challenger, bool truthTold)
        {
            if (!truthTold) return 0;
            if (liar != 0) return 0; // Human-only joker.
            if (challenger == 0) return 0;
            int drawn = 0;
            for (int i = 0; i < Constants.SPIKED_TRAP_DRAWS; i++)
            {
                if (s.drawPile.Count == 0) break;
                s.hands[challenger].Add(Lugen.Deck.DeckBuilder.PopTop(s.drawPile));
                drawn++;
            }
            return drawn;
        }
    }
}
