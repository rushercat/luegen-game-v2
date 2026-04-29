// Lügen — JackCurse.cs
// Translated from checkJackCurse() / checkInstantLoss() / checkLastPlayerStanding().
//
// The Jack curse: holding 4 Jacks (or 5 with Safety Net, 3 with Greedy
// modifier, 6 if you're Lugen) at the END of any turn = elimination from
// this floor + lose 1 Heart.
//
// Steel Jacks count double toward the curse weight.

using Lugen.Cards;
using Lugen.Deck;

namespace Lugen.Round
{
    public static class JackCurse
    {
        // Returns true if the seat just got cursed (caller should announce
        // and end-floor logic must handle Heart loss / elimination).
        public static bool CheckCurse(RoundState s, int playerIdx, int jackLimit)
        {
            if (s.eliminated[playerIdx]) return false;
            int weight = JackFairness.JackCurseWeight(s.hands[playerIdx]);
            if (weight >= jackLimit)
            {
                s.eliminated[playerIdx] = true;
                return true;
            }
            return false;
        }

        // Mark a seat as having emptied their hand (round-scope, not floor-scope).
        public static void MarkFinished(RoundState s, int playerIdx)
        {
            if (s.finished[playerIdx]) return;
            s.finished[playerIdx] = true;
            s.placements.Add(playerIdx);
        }

        // Returns true if the round is over: only one (or zero) seats are
        // still active. Mirrors endRoundIfDone().
        public static bool ShouldEndRound(RoundState s)
        {
            int active = 0;
            for (int p = 0; p < s.NumPlayers; p++)
            {
                if (!s.eliminated[p] && !s.finished[p] && !s.outOfTurns[p]) active++;
            }
            return active <= 1;
        }
    }
}
