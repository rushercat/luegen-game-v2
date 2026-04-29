// Lügen — Rng.cs
// Centralized random number generator. The JS port uses Math.random()
// everywhere, which is fine for casual play but kills reproducibility.
// Centralizing here lets us:
//
//   1. Seed runs (the JS already mints a seed code like "4F2K-9A7B" —
//      we honor it).
//   2. Swap in a deterministic PRNG later for replays / share-codes
//      without touching every call site.
//
// In Unity you'd typically use UnityEngine.Random, but that's a global
// singleton and a pain to seed for a single subsystem. System.Random is
// good enough.

using System;
using System.Collections.Generic;

namespace Lugen.Core
{
    public static class Rng
    {
        private static Random _random = new Random();

        /// <summary>Reseed the RNG. Call once per run from the seed code.</summary>
        public static void Seed(int seed) { _random = new Random(seed); }

        /// <summary>Reseed from a string seed code (e.g. "4F2K-9A7B").</summary>
        public static void SeedFromString(string code)
        {
            // FNV-ish hash. Same input → same int seed.
            // The 2166136261 / 16777619 constants are the standard FNV-1a
            // 32-bit basis + prime; the basis is > int.MaxValue so we
            // compute in uint and cast at the end.
            unchecked
            {
                uint h = 2166136261u;
                for (int i = 0; i < code.Length; i++)
                {
                    h = (h ^ code[i]) * 16777619u;
                }
                Seed((int)h);
            }
        }

        /// <summary>Inclusive lo, exclusive hi. Mirrors `Math.floor(Math.random() * n)` when called as Range(0, n).</summary>
        public static int Range(int loInclusive, int hiExclusive) =>
            _random.Next(loInclusive, hiExclusive);

        /// <summary>0.0 .. 1.0 — direct equivalent of Math.random().</summary>
        public static double NextDouble() => _random.NextDouble();

        /// <summary>True with the given probability (0..1). `Chance(0.55)` == `Math.random() < 0.55`.</summary>
        public static bool Chance(double p) => _random.NextDouble() < p;

        /// <summary>Pick a uniformly random element. Throws on empty.</summary>
        public static T Pick<T>(IList<T> list)
        {
            if (list == null || list.Count == 0) throw new InvalidOperationException("Rng.Pick on empty list");
            return list[_random.Next(list.Count)];
        }

        /// <summary>Fisher–Yates in place. Mirrors the JS shuffle() helper.</summary>
        public static void ShuffleInPlace<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>Returns a shuffled copy. Direct port of JS shuffle().</summary>
        public static List<T> Shuffled<T>(IEnumerable<T> source)
        {
            var copy = new List<T>(source);
            ShuffleInPlace(copy);
            return copy;
        }

        // Generates a seed code like "4F2K-9A7B" — same shape as the JS
        // _generateRunSeed() helper in beta.js.
        public static string GenerateSeedCode()
        {
            const string ALPHABET = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
            char[] s = new char[9];
            for (int i = 0; i < 9; i++)
            {
                if (i == 4) s[i] = '-';
                else s[i] = ALPHABET[_random.Next(ALPHABET.Length)];
            }
            return new string(s);
        }
    }
}
