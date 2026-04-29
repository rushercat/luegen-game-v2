// Lügen — Card.cs
// Translated from the JS card object shape used throughout beta.js / server.js:
//
//   { rank: 'A', id: 'p0_A_0', owner: 0, affix: 'gilded', ... }
//
// In Unity we want this to be a plain serializable class so JsonUtility
// can persist it (save system) and so MonoBehaviours don't accidentally
// take ownership of the data. Cards are pure data — anything visual
// belongs in a separate CardView MonoBehaviour.

using System;
using Lugen.Affixes;

namespace Lugen.Cards
{
    [Serializable]
    public class Card
    {
        // Rank — one of A / K / Q / 10 / J. See Rank.cs.
        public Rank rank;

        // Stable unique identifier. JS used strings like "p0_A_0" or
        // "rd_J_3" — we keep that convention so logs / save files match
        // the original game. CardIdFactory below mints them.
        public string id;

        // Which player "owns" this card. -1 = round-deck (unowned vanilla),
        // 0..N-1 = run-deck card belonging to that seat. Used by the
        // colored-border ownership UI hint and a handful of jokers/relics
        // (Iron Stomach, Magpie, Ricochet) that gate on ownership.
        public int owner = -1;

        // The affix carried by this card. None for vanilla.
        public Affix affix = Affix.None;

        // ---- Per-card transient state -------------------------------------------------

        // Mirage is technically a one-shot wildcard, but the original code
        // tracks 3 uses before removing it from the run deck. We mirror that.
        public int mirageUses = 0;

        // When a Cursed card is picked up it locks the holder out of LIAR
        // (and out of replaying it) for N turns. Steel Spine relic shortens
        // this from 2 to 1.
        public int cursedLockTurns = 0;

        // The "claim" attached to this card while it sits in the played
        // pile face-down. Equals the Target Rank at the moment of play.
        // Only meaningful for cards in `state.pile`, not in hands or decks.
        public Rank claim;

        // -------------------------------------------------------------------------------

        public Card() { }

        public Card(Rank rank, string id, int owner = -1, Affix affix = Affix.None)
        {
            this.rank = rank;
            this.id = id;
            this.owner = owner;
            this.affix = affix;
            this.claim = rank;
        }

        // Shallow clone — used everywhere the JS code did `{...card}` to
        // avoid mutating the source run-deck card when building a round
        // deck (see Deck.BuildDeck).
        public Card Clone()
        {
            return new Card
            {
                rank = rank,
                id = id,
                owner = owner,
                affix = affix,
                mirageUses = mirageUses,
                cursedLockTurns = cursedLockTurns,
                claim = claim,
            };
        }

        public override string ToString()
        {
            string a = affix == Affix.None ? "" : "(" + affix.ToShort() + ")";
            return rank.ToShort() + a;
        }
    }

    // ID minting helper. The JS code generates IDs in two patterns:
    //
    //   Round-deck base cards:  "rd_<RANK>_<i>"   (owner = -1)
    //   Run-deck cards:         "p<owner>_<RANK>_<i>"
    //
    // We keep both schemes so save data round-trips with old games.
    public static class CardIdFactory
    {
        public static string RoundDeck(Rank rank, int i) => $"rd_{rank.ToShort()}_{i}";
        public static string RunDeck(int owner, Rank rank, int i) => $"p{owner}_{rank.ToShort()}_{i}";

        // Used when a card is freshly minted mid-round (forced Cursed from
        // Gambler character, event-spawned, etc.).
        public static string Adhoc(string tag) =>
            tag + "_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
    }
}
