// Lügen — PlayedCard.cs
// Wrapper around a card that's been pushed into the played pile. Carries
// the claim (== the Target Rank at the moment of play) and the original
// owner so reveals can show "whose run-deck card was that".

using Lugen.Cards;

namespace Lugen.Round
{
    [System.Serializable]
    public class PlayedCard
    {
        public Card card;        // The actual card (rank, affix, owner, id).
        public Rank claim;       // What the player CLAIMED this card was.
        public int playedBy;     // Seat index that played it.
    }
}
