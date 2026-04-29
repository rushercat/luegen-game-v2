// Lügen — TurnRotation.cs
// Translated from findNextActiveIdx() in server.js and advanceTurn() in
// public/beta.js. Skips eliminated, finished, and out-of-turns seats.
//
// The "next active" rotation is a tight loop because in late rounds you
// can have many disabled seats and only one or two players still active.

namespace Lugen.Round
{
    public static class TurnRotation
    {
        // Find the next seat that can still play. Returns -1 if there's
        // nobody (which means the round is over and the caller should
        // call EndRound).
        public static int FindNextActive(RoundState s, int fromIdx)
        {
            int n = s.NumPlayers;
            for (int step = 1; step <= n; step++)
            {
                int idx = (fromIdx + step) % n;
                if (!s.eliminated[idx] && !s.finished[idx] && !s.outOfTurns[idx]
                    && s.hands[idx].Count > 0)
                {
                    return idx;
                }
            }
            return -1;
        }

        public static void AdvanceTurn(RoundState s, int fromIdx)
        {
            int next = FindNextActive(s, fromIdx);
            if (next < 0)
            {
                s.gameOver = true;
                return;
            }
            s.currentTurn = next;
        }
    }
}
