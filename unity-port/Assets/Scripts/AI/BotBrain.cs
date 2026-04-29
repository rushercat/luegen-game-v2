// Lügen — BotBrain.cs
// Translated from botTurn() / botChallenge() in beta.js.
//
// The brain decides two things:
//
//   1. ChooseCardsToPlay — how many cards, and whether to bluff or play truth.
//   2. ShouldCallLiar     — whether to challenge an opponent's last play.
//
// Behavior is shaped by the seat's personality. Each personality overrides
// the default "roll bluffRate" / "roll challengeRate" behavior with hand-tuned
// math so seats actually feel different at the table.

using System.Collections.Generic;
using System.Linq;
using Lugen.Affixes;
using Lugen.Cards;
using Lugen.Core;
using Lugen.Round;

namespace Lugen.AI
{
    public class BotPlayDecision
    {
        public List<Card> cardsToPlay;     // The actual chosen cards from the bot's hand.
        public bool isBluff;               // True if the bot is lying about the claim.
    }

    public static class BotBrain
    {
        // Choose what cards this bot will play this turn. Returns null only
        // if hand is empty (caller should mark the seat as finished).
        public static BotPlayDecision ChoosePlay(
            RoundState s,
            int botIdx,
            PersonalityData personality,
            int auditorEveryN,
            HumanProfile humanProfile)
        {
            var hand = s.hands[botIdx];
            if (hand.Count == 0) return null;

            Rank target = s.targetRank;
            var matching = hand.Where(c => c.rank == target).ToList();
            var nonMatching = hand.Where(c => c.rank != target).ToList();
            int myJacks = Lugen.Deck.JackFairness.CountJacks(hand);
            int limit = JackLimitFor(botIdx, personality);
            float bluffRate = personality?.bluffRate ?? 0.30f;

            string id = personality?.id;

            // Personality-specific overrides.
            switch (id)
            {
                case "methodical":
                    {
                        bool safeJacks = myJacks <= System.Math.Max(0, limit - 3);
                        bool goodHand = matching.Count >= 2;
                        bluffRate = (safeJacks && goodHand) ? 0.65f : 0.10f;
                        break;
                    }
                case "mimic":
                    if (s.lastHumanPlay != null)
                        bluffRate = s.lastHumanPlay.wasBluff ? 0.80f : 0.10f;
                    else
                        bluffRate = 0.40f;
                    break;
                case "wildcard":
                    bluffRate = (float)Rng.NextDouble();
                    break;
                case "lugen":
                case "prophet":
                    {
                        double challengeRate = humanProfile?.PredictChallengeRate() ?? 0.30;
                        // Linear: 0% challenge → bluff 0.85, 50% → 0.45, 100% → 0.10.
                        bluffRate = (float)System.Math.Max(0.05, System.Math.Min(0.95, 0.85 - 0.75 * challengeRate));
                        break;
                    }
            }

            if (s.emptyThreatPending) bluffRate = System.Math.Max(0.05f, bluffRate - 0.40f);

            // The Mirror boss: copy the human's last play size + claim.
            if (id == "mirror" && s.lastHumanPlay != null)
            {
                int wantCount = System.Math.Min(System.Math.Min(s.lastHumanPlay.count, hand.Count), 3);
                var honestPicks = matching.Take(wantCount).ToList();
                int padCount = wantCount - honestPicks.Count;
                var padPool = Rng.Shuffled(nonMatching).Take(padCount);
                var picks = honestPicks.Concat(padPool).ToList();
                if (picks.Count == 0) picks = Rng.Shuffled(hand).Take(1).ToList();
                bool _isBluff = !picks.All(c => c.rank == target || c.affix == Affix.Mirage);
                return new BotPlayDecision { cardsToPlay = picks, isBluff = _isBluff };
            }

            // Cheater boss: lies on every play.
            if (id == "cheater") bluffRate = 1.0f;

            bool willBluff = matching.Count == 0 || Rng.Chance(bluffRate);
            bool truthful = !willBluff && matching.Count > 0;

            List<Card> chosen;
            if (truthful)
            {
                int max = System.Math.Min(3, matching.Count);
                int count = 1 + Rng.Range(0, max);
                chosen = matching.Take(count).ToList();
            }
            else
            {
                int max = System.Math.Min(3, hand.Count);
                int count = 1 + Rng.Range(0, max);
                var pool = nonMatching.Count >= count ? Rng.Shuffled(nonMatching) : Rng.Shuffled(hand);
                chosen = pool.Take(count).ToList();
            }

            // Lugen specials: every card is randomly affixed (overwriting affixes).
            if (id == "lugen")
            {
                foreach (var c in chosen) c.affix = Rng.Pick(AffixExtensions.AllRandomable);
            }

            bool isBluff = !chosen.All(c => c.rank == target || c.affix == Affix.Mirage);
            return new BotPlayDecision { cardsToPlay = chosen, isBluff = isBluff };
        }

        // Decide whether this bot will call LIAR on the most recent play.
        // Auditor uses a deterministic counter; everyone else rolls challengeRate
        // with personality / Cheater modifications.
        public static bool ShouldCallLiar(
            RoundState s,
            int botIdx,
            PersonalityData personality,
            int auditorEveryN)
        {
            if (s.lastPlay == null) return false;
            if (s.lastPlay.playerIdx == botIdx) return false; // Can't call yourself.

            string id = personality?.id;

            // Auditor: challenges every Nth play.
            if (id == "auditor")
            {
                s.auditorChances++;
                return (s.auditorChances % System.Math.Max(1, auditorEveryN)) == 0;
            }

            // Eager: very high challenge rate.
            // Coward: very low.
            // Mimic: mirrors the human's recent challenge fire/no-fire.
            float rate = personality?.challengeRate ?? 0.30f;
            return Rng.Chance(rate);
        }

        // Jack limit per seat. Includes Lugen's elevated 6, otherwise default.
        public static int JackLimitFor(int seat, PersonalityData personality)
        {
            if (personality?.id == "lugen") return 6;
            return Constants.JACK_LIMIT;
        }
    }
}
