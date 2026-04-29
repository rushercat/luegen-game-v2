// Lügen — RunState.cs
// Translated from runState in beta.js (around startRun, lines 980-1100).
//
// One run = the persistent state across 9 floors, 3 acts. This is what
// you save to disk, what the result screen reads, and what you mutate
// when buying things in the shop.

using System.Collections.Generic;
using Lugen.Cards;
using Lugen.Jokers;

namespace Lugen.Run
{
    [System.Serializable]
    public class RunState
    {
        // Identity & seed
        public string seed;                     // "4F2K-9A7B" — for replay/share.
        public string characterId;              // "rookie", "sharp", etc. See Characters/CharacterCatalog.cs.
        public string floor9BossId;             // "lugen" / "mirror" / "hollow".

        // Progression
        public int currentFloor = 1;
        public int hearts;
        public int floorStartHearts;
        public int heartShards;
        public int gold;
        public int[] roundsWon;                 // Per seat in current floor — best-of-3.

        // Persistent build
        public List<Card> runDeck = new List<Card>();
        public JokerSlots jokers = new JokerSlots();
        public List<string> relics = new List<string>();
        public Dictionary<string, int> inventory = new Dictionary<string, int>();

        // Per-floor flags (reset every floor)
        public string currentFloorModifier;     // "foggy" / "greedy" / "brittle" / etc.
        public int tattletaleChargesThisFloor;
        public bool loadedDieUsedThisFloor;
        public bool lastWordUsedThisFloor;
        public bool saboteurUsedThisFloor;
        public bool deadHandUsedThisFloor;
        public bool pickpocketUsedThisFloor;
        public bool markedDeckUsedThisFloor;
        public bool emptyThreatUsedThisFloor;
        public bool crackedMirrorUsedThisFloor;
        public Dictionary<string, bool> carouserUsedThisFloor = new Dictionary<string, bool>();
        public Dictionary<string, bool> floorLockedBoughtThisFloor = new Dictionary<string, bool>();

        // Per-round flags
        public bool bookmarkUsedThisRound;
        public int eavesdropperLastFiredRound = -99;

        // Pre-rolled previews from events (Card Sharp, Auditor's Apprentice).
        public string preRolledNextFloorMod;
        public List<string> preRolledNextFloorPersonalities = new List<string>();

        // Bot personalities for current floor (1..N-1; index 0 is unused).
        public string[] botPersonalities = new string[Lugen.Core.Constants.NUM_PLAYERS];

        // Vengeful Spirit: bots owed forced-Jack penalties next round.
        public List<int> vengefulNextRoundTargets = new List<int>();

        // Misc one-shots
        public bool oldSoldierImmuneNextRound;
        public bool stackedHandPending;
        public string whisperDirection = "left";   // for The Whisper character.

        // Cross-run / achievement progress is in Achievements/AchievementsState.cs.
    }
}
