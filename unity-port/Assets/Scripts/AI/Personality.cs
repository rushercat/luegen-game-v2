// Lügen — Personality.cs
// Translated from PERSONALITY_CATALOG / BOSS_CATALOG in beta.js.

using System.Collections.Generic;

namespace Lugen.AI
{
    [System.Serializable]
    public class PersonalityData
    {
        public string id;
        public string name;
        public float bluffRate;          // Base probability of bluffing on any given turn.
        public float challengeRate;       // Base probability of calling LIAR.
        public string tell;               // What you see (or null = no tell).
        public bool isBoss;
        public int floor;                 // 0 = not floor-locked.
        public string desc;
    }

    public static class PersonalityCatalog
    {
        public static readonly Dictionary<string, PersonalityData> All = new Dictionary<string, PersonalityData>
        {
            // Regular personalities
            { "greedy",     new PersonalityData { id = "greedy",     name = "Greedy",     bluffRate = 0.55f, challengeRate = 0.20f, tell = "eyes the gold counter before this play" } },
            { "coward",     new PersonalityData { id = "coward",     name = "Coward",     bluffRate = 0.40f, challengeRate = 0.05f, tell = "hesitates uneasily" } },
            { "eager",      new PersonalityData { id = "eager",      name = "Eager",      bluffRate = 0.50f, challengeRate = 0.65f, tell = "fingers twitch over the LIAR button" } },
            { "methodical", new PersonalityData { id = "methodical", name = "Methodical", bluffRate = 0.25f, challengeRate = 0.20f, tell = "re-sorts their hand" } },
            { "mimic",      new PersonalityData { id = "mimic",      name = "Mimic",      bluffRate = 0.50f, challengeRate = 0.30f, tell = "glances at you" } },
            { "wildcard",   new PersonalityData { id = "wildcard",   name = "Wildcard",   bluffRate = 0.50f, challengeRate = 0.40f, tell = "shrugs (might mean anything)" } },
            { "prophet",    new PersonalityData { id = "prophet",    name = "Prophet",    bluffRate = 0.45f, challengeRate = 0.45f, tell = null } },

            // Bosses
            { "auditor", new PersonalityData {
                id = "auditor", name = "The Auditor", floor = 3, isBoss = true,
                bluffRate = 0.30f, challengeRate = 1.00f, tell = "snaps the ledger shut",
                desc = "Challenges every Nth play (N rolls 1-5 each round)." } },
            { "cheater", new PersonalityData {
                id = "cheater", name = "The Cheater", floor = 6, isBoss = true,
                bluffRate = 1.00f, challengeRate = 0.30f, tell = "a tiny smirk on 1-in-4 lies",
                desc = "Lies on every play." } },
            { "lugen",   new PersonalityData {
                id = "lugen", name = "Lugen", floor = 9, isBoss = true,
                bluffRate = 0.55f, challengeRate = 0.50f, tell = null,
                desc = "Starts with 7 cards, Jack limit 6, every play is randomly affixed." } },
            { "mirror",  new PersonalityData {
                id = "mirror", name = "The Mirror", floor = 9, isBoss = true,
                bluffRate = 0.50f, challengeRate = 0.50f, tell = null,
                desc = "Plays whatever you played last turn." } },
            { "hollow",  new PersonalityData {
                id = "hollow", name = "The Hollow", floor = 9, isBoss = true,
                bluffRate = 0.50f, challengeRate = 0.50f, tell = null,
                desc = "Hand size is hidden from you." } },
        };

        public static PersonalityData Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            // TryGetValue rather than GetValueOrDefault for compatibility with
            // older .NET Standard targets (Unity 2019/2020 may not have the
            // extension method on Dictionary).
            All.TryGetValue(id, out var p);
            return p;
        }
    }
}
