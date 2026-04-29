// Lügen — RunManager.cs
// Translated from startRun() / endRun() / advanceFloor() in beta.js.
// Owns the high-level run lifecycle:
//
//   StartRun(characterId)
//     → init runState with character bonuses
//     → assignBotPersonalities()
//     → StartRound() (delegated)
//
//   EndRound(winnerIdx)
//     → Update gold, hearts, place positions
//     → Either advance to next round or end the floor
//
//   EndFloor(humanWonFloor)
//     → Boss-floor relic offer / fork node
//     → If currentFloor == TOTAL_FLOORS, EndRun(victory)
//
// We don't drive the actual round here — that's the round controller. This
// is the meta loop only.

using System.Collections.Generic;
using Lugen.Cards;
using Lugen.Characters;
using Lugen.Core;
using Lugen.Deck;
using Lugen.Jokers;

namespace Lugen.Run
{
    public class RunManager
    {
        public RunState State { get; private set; }

        public void StartRun(string characterId)
        {
            var character = CharacterCatalog.Get(characterId);
            string seed = Rng.GenerateSeedCode();
            Rng.SeedFromString(seed);

            State = new RunState
            {
                seed = seed,
                characterId = characterId,
                hearts = Constants.STARTING_HEARTS,
                floorStartHearts = Constants.STARTING_HEARTS,
                gold = character?.startingGold ?? 0,
                runDeck = DeckBuilder.BuildInitialRunDeck(0),
                roundsWon = new int[Constants.NUM_PLAYERS],
            };

            // Default inventory tracking.
            State.inventory["smokeBomb"] = 0;
            State.inventory["counterfeit"] = 0;
            State.inventory["jackBeNimble"] = 0;

            ApplyCharacterBonuses(character);
            AssignBotPersonalities();
        }

        // Translated from beta.js startRun's character-effects block.
        private void ApplyCharacterBonuses(CharacterData character)
        {
            if (character == null) return;

            if (character.startingGildedA)
            {
                var aCard = State.runDeck.Find(c => c.rank == Rank.Ace && c.affix == Lugen.Affixes.Affix.None);
                if (aCard != null) aCard.affix = Lugen.Affixes.Affix.Gilded;
            }
            if (character.engineerStartingAffix)
            {
                var candidates = State.runDeck.FindAll(c => c.affix == Lugen.Affixes.Affix.None && c.rank != Rank.Jack);
                if (candidates.Count > 0)
                {
                    var card = Rng.Pick(candidates);
                    card.affix = Rng.Pick(new[] {
                        Lugen.Affixes.Affix.Gilded, Lugen.Affixes.Affix.Glass,
                        Lugen.Affixes.Affix.Spiked, Lugen.Affixes.Affix.Steel,
                        Lugen.Affixes.Affix.Mirage, Lugen.Affixes.Affix.Hollow,
                        Lugen.Affixes.Affix.Echo
                    });
                }
            }
            if (character.startingGlassCard)
            {
                var candidates = State.runDeck.FindAll(c => c.affix == Lugen.Affixes.Affix.None && c.rank != Rank.Jack);
                if (candidates.Count > 0)
                {
                    Rng.Pick(candidates).affix = Lugen.Affixes.Affix.Glass;
                }
            }
            if (!string.IsNullOrEmpty(character.startingJokerId))
            {
                if (JokerCatalog.All.TryGetValue(character.startingJokerId, out var joker))
                {
                    State.jokers.TryEquip(joker);
                    if (character.startingJokerId == "tattletale")
                    {
                        State.tattletaleChargesThisFloor = Constants.TATTLETALE_CHARGES_PER_FLOOR;
                    }
                }
            }
        }

        // Translated from assignBotPersonalities() in beta.js.
        // Floors 3, 6, 9 = boss floors with single boss seat.
        // Other floors get personalities from the regular pool.
        public void AssignBotPersonalities()
        {
            if (State == null) return;
            int floor = State.currentFloor;

            // Reset.
            for (int i = 0; i < State.botPersonalities.Length; i++) State.botPersonalities[i] = null;

            if (floor == 3) State.botPersonalities[1] = "auditor";
            else if (floor == 6) State.botPersonalities[1] = "cheater";
            else if (floor == 9)
            {
                if (string.IsNullOrEmpty(State.floor9BossId)) State.floor9BossId = "lugen";
                State.botPersonalities[1] = State.floor9BossId;
            }
            else
            {
                // Pull from the act-appropriate pool.
                var pool = ActPersonalityPool(GetCurrentAct());
                for (int i = 1; i < State.botPersonalities.Length; i++)
                {
                    State.botPersonalities[i] = Rng.Pick(pool);
                }
            }
        }

        // Returns 1, 2, or 3.
        public int GetCurrentAct()
        {
            if (State == null) return 1;
            if (State.currentFloor <= 3) return 1;
            if (State.currentFloor <= 6) return 2;
            return 3;
        }

        public bool ShouldShowTells()
        {
            if (State == null) return false;
            if (State.currentFloorModifier == "silent") return false;
            if (State.relics.Contains("compass")) return true;
            return GetCurrentAct() <= 2;
        }

        // Act I = teaching tier (loud tells), II = mixed, III = no tells.
        public static string[] ActPersonalityPool(int act)
        {
            switch (act)
            {
                case 1: return new[] { "greedy", "coward", "eager" };
                case 2: return new[] { "greedy", "coward", "eager", "methodical", "mimic", "wildcard" };
                case 3: return new[] { "methodical", "mimic", "wildcard", "prophet" };
            }
            return new[] { "greedy" };
        }

        public bool IsBossFloor(int f) => f == 3 || f == 6 || f == 9;

        // Move to the next floor (after winning the current floor's best-of-3).
        // In the JS this is interleaved with the fork-node UI; here we just
        // advance the counter — the caller routes through the fork.
        public void AdvanceFloor()
        {
            State.currentFloor++;
            State.floorStartHearts = State.hearts;
            State.tattletaleChargesThisFloor = Constants.TATTLETALE_CHARGES_PER_FLOOR;
            State.loadedDieUsedThisFloor = false;
            State.lastWordUsedThisFloor = false;
            State.saboteurUsedThisFloor = false;
            State.deadHandUsedThisFloor = false;
            State.pickpocketUsedThisFloor = false;
            State.markedDeckUsedThisFloor = false;
            State.emptyThreatUsedThisFloor = false;
            State.crackedMirrorUsedThisFloor = false;
            State.carouserUsedThisFloor.Clear();
            State.floorLockedBoughtThisFloor.Clear();
            State.roundsWon = new int[Constants.NUM_PLAYERS];

            AssignBotPersonalities();
        }

        public bool RunOver() => State == null || State.hearts <= 0 || State.currentFloor > Constants.TOTAL_FLOORS;
    }
}
