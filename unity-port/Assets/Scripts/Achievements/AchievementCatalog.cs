// Lügen — AchievementCatalog.cs
// Translated from ACHIEVEMENT_CATALOG in beta.js (around line 802).
//
// Achievements are local persistent flags. Each unlock grants a cosmetic
// reward (card back, border tint, etc.) — no gameplay impact, per the
// design doc's "gameplay vs cosmetic" split.

using System.Collections.Generic;

namespace Lugen.Achievements
{
    [System.Serializable]
    public class AchievementData
    {
        public string id;
        public string category;
        public string name;
        public string desc;
        public string unlocks;

        public AchievementData(string id, string cat, string name, string desc, string unlocks)
        {
            this.id = id; this.category = cat; this.name = name; this.desc = desc; this.unlocks = unlocks;
        }
    }

    public static class AchievementCatalog
    {
        public static readonly Dictionary<string, AchievementData> All = new Dictionary<string, AchievementData>
        {
            // Mastery
            { "pacifist",    new AchievementData("pacifist",    "Mastery",    "The Pacifist",      "Win a run without ever calling Liar.",                              "\"Pacifist\" card back") },
            { "truthWins",   new AchievementData("truthWins",   "Mastery",    "Truth Wins",        "Survive 10 challenges where you told the truth in a single run.",   "Gold border tint") },
            { "liarsTongue", new AchievementData("liarsTongue", "Mastery",    "Liar's Tongue",     "Lie 10 times in a single round and never get caught.",              "\"Smirk\" elimination animation") },
            { "bossSlayer",  new AchievementData("bossSlayer",  "Mastery",    "Boss Slayer",       "Beat all three Floor 9 alt bosses.",                                 "\"Crown\" card back") },
            { "untouched",   new AchievementData("untouched",   "Mastery",    "Untouched",         "Beat Lugen without losing a single Heart.",                          "Alt Lugen card art") },

            // Build identity
            { "ironWill",    new AchievementData("ironWill",    "Build",      "Iron Will",         "Win with at least 4 Steel-affixed cards in your run deck.",          "Steel border tint") },
            { "glassCannon", new AchievementData("glassCannon", "Build",      "Glass Cannon",      "Burn 100 cards across all runs.",                                    "Glass alt VFX") },
            { "massForgery", new AchievementData("massForgery", "Build",      "Mass Forgery",      "Make 7 of your run-deck cards the same card via Forger.",            "Forger alt portrait") },
            { "pacifier",    new AchievementData("pacifier",    "Build",      "The Pacifier",      "Hold a Cursed card for 5 consecutive rounds.",                       "Cursed alt VFX") },
            { "affixConn",   new AchievementData("affixConn",   "Build",      "Affix Connoisseur", "Have all 8 affixes appear simultaneously in your run deck.",         "Rainbow border tint") },

            // Economy / fluff
            { "wallet",      new AchievementData("wallet",      "Economy",    "The Wallet",        "End a run with 1000+ gold.",                                         "Banker alt portrait") },
            { "spendthrift", new AchievementData("spendthrift", "Economy",    "Spendthrift",       "Spend 2000g in a single run.",                                       "Coin shower victory animation") },
            { "speedDemon",  new AchievementData("speedDemon",  "Economy",    "Speed Demon",       "Win a floor in under 2 minutes.",                                    "Lightning elimination animation") },
            { "heartSurgeon",new AchievementData("heartSurgeon","Economy",    "Heart Surgeon",     "Collect 10 Heart shards across all runs.",                           "Heart card back") },
            { "emptyHand",   new AchievementData("emptyHand",   "Economy",    "Empty Hand",        "Empty your hand on the very first turn of a round.",                "Magician alt portrait") },
            { "stoic",       new AchievementData("stoic",       "Economy",    "Stoic",             "Win without using a single consumable.",                             "Stoic card back") },
            { "strippedDown",new AchievementData("strippedDown","Build",      "Stripped Down",     "Win with 4 or fewer cards in your run deck.",                        "Minimalist border tint") },

            // Reads / play
            { "mindReader",  new AchievementData("mindReader",  "Reads",      "Mind Reader",       "5 correct LIAR calls in a row on a single floor.",                   "Mind Reader card back") },
            { "theFox",      new AchievementData("theFox",      "Reads",      "The Fox",           "Win a round where every play was a bluff and you weren't caught.",   "Fox border tint") },
            { "gamblersHand",new AchievementData("gamblersHand","Reads",      "Gambler's Hand",    "Charlatan's Bet: 5 wins in a row.",                                  "Gambler card back") },
        };
    }
}
