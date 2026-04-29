// Lügen — Rank.cs
// Translated from public/beta.js (RANKS / ALL_RANKS constants).
//
// In the JS code ranks are bare strings: "A", "K", "Q", "10", "J".
// In C# we want a strongly-typed enum so logic like "is a Jack" or
// "matches the target" compiles into integer compares instead of string
// equality. The canonical scoring/sort order from the design doc is:
//
//     J  <  10  <  Q  <  K  <  A
//
// (Used by the Trickster joker for +/-1 wildcard matches; everywhere else
// rank ordering doesn't matter — it's pure equality.)

namespace Lugen.Cards
{
    public enum Rank
    {
        Jack    = 0,   // "J"
        Ten     = 1,   // "10"
        Queen   = 2,   // "Q"
        King    = 3,   // "K"
        Ace     = 4,   // "A"
    }

    public static class RankExtensions
    {
        // The four ranks the Target can be rolled from. Jack is excluded —
        // Jacks are the "curse" rank, never the goal of a round. (The
        // Inverted floor modifier overrides this and forces Target = J.)
        public static readonly Rank[] TargetRanks = { Rank.Ace, Rank.King, Rank.Queen, Rank.Ten };
        public static readonly Rank[] AllRanks    = { Rank.Ace, Rank.King, Rank.Queen, Rank.Ten, Rank.Jack };

        // Pretty-print a rank back into its single-character / "10" form
        // matching the JS strings, for log lines / UI labels.
        public static string ToShort(this Rank r)
        {
            switch (r)
            {
                case Rank.Ace:   return "A";
                case Rank.King:  return "K";
                case Rank.Queen: return "Q";
                case Rank.Ten:   return "10";
                case Rank.Jack:  return "J";
                default:         return r.ToString();
            }
        }

        public static bool IsJack(this Rank r) => r == Rank.Jack;
    }
}
