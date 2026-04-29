// Lügen — Player.cs
// In the JS code "player" is implicit — there's a single human at seat 0
// and bots at 1..N-1, and most state lives on the global runState/state
// objects. For the Unity port we want a real Player object so AI seats
// and the human can share an interface, and so PvP would be a drop-in
// later.

using System.Collections.Generic;
using Lugen.Cards;

namespace Lugen.Players
{
    public enum PlayerKind
    {
        Human,    // The local player.
        Bot,      // AI-controlled.
        Remote,   // Reserved for future PvP — not used in solo.
    }

    [System.Serializable]
    public class Player
    {
        public int seatIndex;       // 0 = human in solo. Wraparound modulo NUM_PLAYERS.
        public string displayName;
        public PlayerKind kind;

        // Per-round state — the cards currently in this seat's hand.
        public List<Card> hand = new List<Card>();

        // Round-end flags. `eliminated` = removed by Jack curse for the
        // current FLOOR (loses 1 Heart). `finished` = emptied hand for the
        // current ROUND (placed). Both reset on round/floor boundaries.
        public bool eliminated;
        public bool finished;

        // Last Call modifier: tracks plays per player (5 max each).
        public int turnsTaken;
        public bool outOfTurns;

        // The human-only tracking flags lived directly on `state` in JS;
        // for the port they're modeled on the round state, not the player.

        public Player(int seat, string name, PlayerKind kind)
        {
            this.seatIndex = seat;
            this.displayName = name;
            this.kind = kind;
        }

        public int CountJacks() => Lugen.Deck.JackFairness.CountJacks(hand);
        public int JackCurseWeight() => Lugen.Deck.JackFairness.JackCurseWeight(hand);

        public override string ToString() => $"Player#{seatIndex}({displayName})";
    }
}
