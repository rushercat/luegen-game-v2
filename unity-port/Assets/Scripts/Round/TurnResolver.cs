// Lügen — TurnResolver.cs
// Translated from playCards() in public/beta.js (around lines 2173-2620).
//
// This is the single most-touched function in the JS code. The C# port
// breaks it up into smaller stages so each phase is testable in isolation.
//
// The play pipeline:
//
//   1. Validate (count, ownership, Cursed-lock).
//   2. Move cards from hand → pile.
//   3. Echo trigger (peek for whoever was previously Echo-armed).
//   4. Doppelganger override (force claim to match previous play).
//   5. Stamp lastPlay / lastHumanPlay.
//   6. Last Call accounting.
//   7. Mirage usage tracking.
//   8. Hollow replacement draws.
//   9. Echo arm (if any played card was Echo).
//  10. Eavesdropper trigger.
//  11. Open the challenge window.
//
// Side-effect-free where possible — we mutate the RoundState/Player passed
// in, but emit messages through ILogSink rather than writing to a global.

using System.Collections.Generic;
using System.Linq;
using Lugen.Affixes;
using Lugen.Cards;
using Lugen.Core;
using Lugen.Players;

namespace Lugen.Round
{
    public interface ILogSink { void Log(string message); void PrivatePeek(string message); }

    public class NullLogSink : ILogSink
    {
        public void Log(string m) { }
        public void PrivatePeek(string m) { }
    }

    public static class TurnResolver
    {
        // Returns false if the play was rejected (illegal count, Cursed-locked, etc).
        // Caller is responsible for opening the challenge window after a successful play.
        public static bool TryPlay(
            RoundState s,
            int playerIdx,
            List<string> cardIds,
            int minCards,
            int maxCards,
            ILogSink logSink)
        {
            if (s.gameOver || s.challengeOpen) return false;
            if (cardIds == null || cardIds.Count < minCards || cardIds.Count > maxCards) return false;

            var hand = s.hands[playerIdx];
            var picked = new List<Card>();
            foreach (var id in cardIds)
            {
                var c = hand.FirstOrDefault(card => card.id == id);
                if (c != null) picked.Add(c);
            }
            if (picked.Count != cardIds.Count) return false;

            // Cursed-lock check (HUMAN only — bots respect this implicitly via AI).
            if (playerIdx == 0)
            {
                if (picked.Any(c => c.cursedLockTurns > 0))
                {
                    logSink.Log("A Cursed card is still locked.");
                    return false;
                }
            }

            // Stage 2: move cards hand → pile.
            s.hands[playerIdx] = hand.Where(c => !cardIds.Contains(c.id)).ToList();
            foreach (var c in picked)
            {
                s.pile.Add(new PlayedCard
                {
                    card = c,
                    claim = s.targetRank,
                    playedBy = playerIdx,
                });
            }

            // Stage 3: Echo trigger (peek for armed player).
            if (s.echoArmedFor >= 0 && s.echoArmedFor != playerIdx && picked.Count > 0)
            {
                int peeker = s.echoArmedFor;
                var peeked = picked[0];
                s.echoArmedFor = -1;
                if (peeker == 0)
                {
                    logSink.PrivatePeek($"Echo's eye: seat {playerIdx}'s first card is a {peeked.rank.ToShort()}"
                        + (peeked.affix != Affix.None ? $" ({peeked.affix.ToShort()})" : "") + ".");
                }
            }

            // Stage 4: Doppelganger override.
            Rank claim = s.targetRank;
            if (playerIdx == 0 && s.doppelArmed && s.lastPlay != null)
            {
                claim = s.lastPlay.claim;
                int start = s.pile.Count - picked.Count;
                for (int i = start; i < s.pile.Count; i++)
                {
                    if (i >= 0) s.pile[i].claim = claim;
                }
                s.doppelArmed = false;
                s.doppelUsedThisRound = true;
                logSink.Log($"Doppelganger: your play mimics the previous ({picked.Count} x {claim.ToShort()}).");
            }

            // Hot Potato consume.
            if (playerIdx == 0 && s.hotPotatoArmed) s.hotPotatoArmed = false;

            // Stage 5: stamp lastPlay.
            s.lastPlay = new LastPlay
            {
                playerIdx = playerIdx,
                count = picked.Count,
                claim = claim,
            };

            // Stage 6: Last Call accounting.
            s.turnsTaken[playerIdx]++;
            // Caller has access to the floor-modifier; if Last Call is active and the
            // player just hit LAST_CALL_TURN_LIMIT, mark them out-of-turns. The exact
            // conditional lives outside this resolver because we don't have the
            // run state here.

            // Track human bluffs/lies on the human's plays.
            if (playerIdx == 0)
            {
                bool wasBluff = !picked.All(c => c.rank == s.targetRank || c.affix == Affix.Mirage);
                s.lastHumanPlay = new LastPlay
                {
                    playerIdx = 0,
                    count = picked.Count,
                    claim = s.targetRank,
                    wasBluff = wasBluff,
                };
                if (wasBluff) s.humanLiesThisRound++;
                s.humanPlaysThisRound++;
                if (!wasBluff) s.foxStreakAlive = false;
            }

            logSink.Log($"Seat {playerIdx} plays {picked.Count} card(s) as {s.targetRank.ToShort()}.");

            // Stage 7: Mirage usage. Mirage on human-owned run-deck cards
            // tracks 3 plays before the card is consumed.
            // (The actual run-deck mutation lives on RunState; the play
            // resolver just exposes the trigger flag for the caller.)

            // Stage 8: Hollow draws — for each Hollow played, draw a replacement.
            int hollowCount = picked.Count(c => c.affix == Affix.Hollow);
            for (int i = 0; i < hollowCount; i++)
            {
                if (s.drawPile.Count == 0) break;
                s.hands[playerIdx].Add(Lugen.Deck.DeckBuilder.PopTop(s.drawPile));
            }
            if (hollowCount > 0)
                logSink.Log($"Seat {playerIdx} draws {hollowCount} from draw pile (Hollow).");

            // Stage 9: Echo arming.
            if (picked.Any(c => c.affix == Affix.Echo))
            {
                s.echoArmedFor = playerIdx;
            }

            // Stage 11: caller is responsible for OpenChallengeWindow().
            return true;
        }
    }
}
