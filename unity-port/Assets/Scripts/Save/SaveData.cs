// Lügen — SaveData.cs
// Cross-run persistent player profile. Translated from the localStorage-
// backed helpers in beta.js (_STORAGE_MAX_FLOOR / _STORAGE_RUN_WON /
// _ACH_STORAGE_KEY / _ACH_PROGRESS_KEY).
//
// In Unity we serialize this with JsonUtility and write to:
//   Application.persistentDataPath/lugen_save.json
//
// JsonUtility is fine for value types but doesn't serialize Dictionary<,>,
// so anything dictionary-shaped becomes a list of (key, value) pairs.

using System.Collections.Generic;

namespace Lugen.Save
{
    [System.Serializable]
    public class StringIntPair { public string key; public int value; }
    [System.Serializable]
    public class StringPair    { public string key; public string value; }

    [System.Serializable]
    public class SaveData
    {
        public int maxFloorReached = 1;
        public bool runWon = false;

        // Cumulative achievement progress (cards burned, etc.).
        public List<StringIntPair> achievementProgress = new List<StringIntPair>();

        // Unlocked achievement IDs.
        public List<string> achievementsUnlocked = new List<string>();

        // Boss kills tracking (for Boss Slayer + Mogul unlock).
        public List<StringIntPair> bossKills = new List<StringIntPair>();

        // Run history — last N completed runs, oldest-first for chronological display.
        public List<RunHistoryEntry> runHistory = new List<RunHistoryEntry>();

        // PvP / cosmetic unlocks (decks, card backs, border tints).
        public List<string> unlockedDecks = new List<string> { "vanilla" };
        public List<string> unlockedCardBacks = new List<string> { "default" };
        public List<string> unlockedBorderTints = new List<string> { "default" };

        public int GetProgress(string id)
        {
            foreach (var p in achievementProgress) if (p.key == id) return p.value;
            return 0;
        }
        public void AddProgress(string id, int delta)
        {
            foreach (var p in achievementProgress) if (p.key == id) { p.value += delta; return; }
            achievementProgress.Add(new StringIntPair { key = id, value = delta });
        }

        public bool IsUnlocked(string id) => achievementsUnlocked.Contains(id);
        public void Unlock(string id) { if (!IsUnlocked(id)) achievementsUnlocked.Add(id); }
    }

    [System.Serializable]
    public class RunHistoryEntry
    {
        public string characterId;
        public string characterName;
        public string result;       // "won" / "lost"
        public int maxFloor;
        public int hearts;
        public int gold;
        public string seed;
        public long timestamp;       // Unix ms.
    }
}
