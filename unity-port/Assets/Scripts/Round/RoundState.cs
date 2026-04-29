// Lügen — RoundState.cs
// One Lügen round = a freshly-shuffled round deck, one Target Rank, and a
// turn loop that runs until somebody empties their hand, two seats are
// eliminated by the Jack curse, or the Last Call modifier fires.
//
// In the JS port this is the giant `state = { ... }` object. Splitting it
// into a real type lets save/load work cleanly and removes a large amount
// of "is this even on the state object" friction.

using System.Collections.Generic;
using Lugen.Cards;
using Lugen.Players;

namespace Lugen.Round
{
    [System.Serializable]
    public class RoundState
    {
        // Per-seat hands. Indexed by seat. (Player.hand is the canonical
        // source — these mirror it for serialization convenience.)
        public List<List<Card>> hands = new List<List<Card>>();

        public List<Card> drawPile = new List<Card>();
        public List<PlayedCard> pile = new List<PlayedCard>();
        public List<Card> burnedCards = new List<Card>(); // For burn-cap recycling.

        public Rank targetRank;
        public LastPlay lastPlay;       // Most recent play; null at round start.
        public LastPlay lastHumanPlay;  // For Mimic personality + Mirror boss.

        public int currentTurn;          // Seat whose turn it is.
        public bool[] eliminated;        // FLOOR-scope (Jack curse).
        public bool[] finished;          // ROUND-scope (emptied hand).
        public List<int> placements = new List<int>(); // Order of empty-handers.

        // Last Call modifier
        public int[] turnsTaken;
        public bool[] outOfTurns;

        // Per-round once-per flags. Each is reset every round in StartRound.
        public bool counterfeitUsed;
        public bool counterfeitLock;     // Suppresses next rotateTargetRank().
        public int echoArmedFor = -1;    // Player index armed by Echo (or -1).
        public bool doubletalkArmed;
        public bool doubletalkUsedThisRound;
        public bool sleightUsedThisRound;
        public List<string> ironStomachBurned = new List<string>(); // run-deck card IDs burned this round.
        public int auditorChances;       // Auditor boss: challenges every Nth chance.
        public bool lugenLiarUsedThisRound;
        public string tricksterMarkedId;
        public bool tricksterUsedThisRound;
        public bool doppelArmed;
        public bool doppelUsedThisRound;
        public bool hotPotatoArmed;
        public List<MemorizerEntry> memorizerLog = new List<MemorizerEntry>();
        public bool snakeEyesLock;
        public string jokersMaskCardId;
        public bool mirrorShardArmed;
        public bool emptyThreatPending;
        public bool magicianUsedThisRound;
        public bool alchemistUsedThisRound;
        public bool callersMarkFiredThisRound;
        public Rank? screamerRevealedRank; // null = nothing revealed.
        public bool lieDetectorArmed;
        public bool stackedHandActive;
        public bool foggyHidden;          // Foggy modifier.

        // Achievement / read tracking (per-round).
        public int humanPlaysThisRound;
        public bool foxStreakAlive = true;
        public int humanLiesThisRound;
        public bool humanCaughtThisRound;
        public bool humanFirstTurn = true;

        // Round-flow state.
        public bool gameOver;
        public bool challengeOpen;
        public int challengerIdx = -1;
        public List<string> log = new List<string>();

        public int NumPlayers => hands.Count;
    }

    [System.Serializable]
    public class LastPlay
    {
        public int playerIdx;
        public int count;
        public Rank claim;
        public bool wasBluff; // Only stamped on the human's plays (used by AI).
    }

    [System.Serializable]
    public class MemorizerEntry
    {
        public Rank rank;
        public Lugen.Affixes.Affix affix;
        public Rank claim;
    }
}
