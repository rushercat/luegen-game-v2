// Lügen — Affix.cs
// Translated from public/beta.js (_EVENT_AFFIX_POOL + per-affix logic
// scattered through playCards / callLiar / the pile-distribute callback).
//
// Eight affixes drive most of the build identity in Lügen. They split
// into three trigger families:
//
//   PASSIVE (while held) — Gilded, Cursed, Steel
//   ON-PLAY              — Mirage, Hollow, Echo
//   ON-REVEAL            — Glass
//   ON-PICKUP            — Spiked
//
// The JS code stores `affix` as a string field on every card. We use an
// enum with an explicit "None" value so plain (no-affix) cards are still
// representable.

namespace Lugen.Affixes
{
    public enum Affix
    {
        None    = 0,
        Gilded  = 1,   // +2g per held card per turn (HUMAN only via bot trigger)
        Glass   = 2,   // On reveal: burns itself + 2 random pile cards
        Cursed  = 3,   // Held: blocks LIAR; on pickup locks for 2 (or 1 with Steel Spine) turns
        Steel   = 4,   // Immune to affix mutation / Glass burns
        Mirage  = 5,   // On play: counts as Target. 3 uses then consumed.
        Spiked  = 6,   // On pickup: taker draws +1 from draw pile
        Hollow  = 7,   // On play: counts as 0 toward hand reduction (you draw a replacement)
        Echo    = 8,   // On play: peek next player's first played card (private)
    }

    public static class AffixExtensions
    {
        // Used when rolling a random affix (events, RANDOM.EXE character,
        // Lugen boss). Mirrors _EVENT_AFFIX_POOL in beta.js exactly.
        public static readonly Affix[] AllRandomable =
        {
            Affix.Gilded, Affix.Glass, Affix.Spiked, Affix.Cursed,
            Affix.Steel,  Affix.Mirage, Affix.Hollow, Affix.Echo
        };

        // The "positive" affix pool used by The Alchemist joker — it
        // transforms a hand card and only ever applies a non-punishing
        // affix. (Source: JOKER_CATALOG.alchemist desc in beta.js.)
        public static readonly Affix[] PositiveAffixes =
        {
            Affix.Gilded, Affix.Mirage, Affix.Echo, Affix.Hollow
        };

        public static string ToShort(this Affix a)
        {
            switch (a)
            {
                case Affix.None:   return "";
                case Affix.Gilded: return "gilded";
                case Affix.Glass:  return "glass";
                case Affix.Cursed: return "cursed";
                case Affix.Steel:  return "steel";
                case Affix.Mirage: return "mirage";
                case Affix.Spiked: return "spiked";
                case Affix.Hollow: return "hollow";
                case Affix.Echo:   return "echo";
                default:           return a.ToString().ToLowerInvariant();
            }
        }
    }
}
