// Lügen — CharacterCatalog.cs
// Translated from CHARACTER_CATALOG in beta.js (around lines 42-170).

using System.Collections.Generic;

namespace Lugen.Characters
{
    [System.Serializable]
    public class CharacterData
    {
        public string id;
        public string name;
        public string flavor;
        public string passive;

        // Bonuses / flags. Only one or two of these should be set per character.
        public int? challengeBonusMs;     // Sharp: +1000 ms.
        public int? handSizeBonus;        // Hoarder: +1.
        public int? jackLimitBonus;       // Hoarder: +1.
        public int? startingGold;         // Banker: 0g, but starts with Gilded A. Default = 0.
        public bool startingGildedA;      // Banker.
        public bool engineerStartingAffix;
        public bool startingGlassCard;    // Witch.
        public bool peekAtRoundStart;     // Bait.
        public bool whisperPeek;          // The Whisper.
        public bool transformPerRound;    // Magician.
        public float? affixDiscount;      // Engineer: 0.25.
        public float? cardDiscount;       // RANDOM.EXE: 0.20.
        public float? goldMultiplier;     // Gambler: 1.5.
        public bool forcedCursedOnNewFloor; // Gambler.
        public bool witchUncappedGlass;   // Witch.
        public bool apostateReroll;       // RANDOM.EXE.

        public string startingJokerId;

        // Unlock gates. Default = always unlocked.
        public bool unlockAlways;
        public int unlockAtFloor;         // 0 = none.
        public bool unlockOnRunWin;
        public string unlockHint;
    }

    public static class CharacterCatalog
    {
        public static readonly Dictionary<string, CharacterData> All = new Dictionary<string, CharacterData>
        {
            { "rookie", new CharacterData {
                id = "rookie", name = "The Rookie",
                flavor = "Hands shake. The cards don't care. Your first journey.",
                passive = "No special abilities — pure mechanics.",
                unlockAlways = true,
            }},
            { "sharp", new CharacterData {
                id = "sharp", name = "The Sharp",
                flavor = "Reads the table before striking. Sees the lie before it lands.",
                passive = "Challenge window +1s.",
                challengeBonusMs = 1000,
                startingJokerId = "tattletale",
                unlockAtFloor = 2,
                unlockHint = "Reach Floor 2 in any run.",
            }},
            { "hoarder", new CharacterData {
                id = "hoarder", name = "The Hoarder",
                flavor = "Never met a card worth folding. Holds tight, dies last.",
                passive = "Hand size +1 (6 cards). Jack limit 5.",
                handSizeBonus = 1,
                jackLimitBonus = 1,
                startingJokerId = "safetyNet",
                unlockAtFloor = 4,
                unlockHint = "Reach Floor 4 in any run.",
            }},
            { "banker", new CharacterData {
                id = "banker", name = "The Banker",
                flavor = "Came in with capital. Every gilded card is just compounding.",
                passive = "Start with a Gilded Ace in your run deck.",
                startingGold = 0,
                startingGildedA = true,
                startingJokerId = "taxman",
                unlockAtFloor = 6,
                unlockHint = "Reach Floor 6 in any run.",
            }},
            { "bait", new CharacterData {
                id = "bait", name = "The Bait",
                flavor = "Looks like an easy mark. The trap snaps shut the moment you call.",
                passive = "Round start: see 1 random card from a random opponent.",
                peekAtRoundStart = true,
                startingJokerId = "spikedTrap",
                unlockAtFloor = 8,
                unlockHint = "Reach Floor 8 in any run.",
            }},
            { "gambler", new CharacterData {
                id = "gambler", name = "The Gambler",
                flavor = "All-in or nothing. The curse is the cost of admission.",
                passive = "+50% gold. Each new floor: 1 Cursed card forced into hand.",
                goldMultiplier = 1.5f,
                forcedCursedOnNewFloor = true,
                startingJokerId = "blackHole",
                unlockOnRunWin = true,
                unlockHint = "Beat Floor 9 (win a full run).",
            }},
            { "magician", new CharacterData {
                id = "magician", name = "The Magician",
                flavor = "Sleight is just slow magic. Once a round, the deck bends to me.",
                passive = "Once per round: transform a hand card to a different rank.",
                transformPerRound = true,
                startingJokerId = "sleightOfHand",
                unlockAtFloor = 3,
                unlockHint = "Reach Floor 3 in any run.",
            }},
            { "engineer", new CharacterData {
                id = "engineer", name = "The Engineer",
                flavor = "Affixes have grain — you just need to know where to apply pressure.",
                passive = "Run deck starts with 1 random affixed card. Affix services 25% off.",
                engineerStartingAffix = true,
                affixDiscount = 0.25f,
                startingJokerId = "forgeHand",
                unlockAtFloor = 5,
                unlockHint = "Reach Floor 5 in any run.",
            }},
            { "witch", new CharacterData {
                id = "witch", name = "The Witch",
                flavor = "Glass cuts both ways. Mine never reaches the cap.",
                passive = "Glass burns don't count toward the burn cap. Run deck starts with 1 Glass card.",
                witchUncappedGlass = true,
                startingGlassCard = true,
                unlockAtFloor = 7,
                unlockHint = "Reach Floor 7 in any run.",
            }},
            { "whisper", new CharacterData {
                id = "whisper", name = "The Whisper",
                flavor = "Half a word from the right neighbour is worth a hand of cards.",
                passive = "Round start: choose a neighbour and see one random card from their hand.",
                whisperPeek = true,
                startingJokerId = "eavesdropper",
                unlockAtFloor = 9,
                unlockHint = "Reach Floor 9 in any run.",
            }},
            { "randomExe", new CharacterData {
                id = "randomExe", name = "RANDOM.EXE",
                flavor = ">>> seed = roll(now); commit; reboot.",
                passive = "Every round: each run-deck card sheds its affix and gains a new random one.",
                apostateReroll = true,
                cardDiscount = 0.20f,
                unlockAtFloor = 6,
                unlockHint = "Reach Floor 6 in any run.",
            }},
        };

        public static CharacterData Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            All.TryGetValue(id, out var c);
            return c;
        }
    }
}
