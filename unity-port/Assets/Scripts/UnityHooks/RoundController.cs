// Lügen — RoundController.cs
// The "glue" that runs a single round end-to-end. This is what your Unity
// MonoBehaviours drive: they call StartRound(), wire up UI events to
// PlayCards() / CallLiar() / EndChallengeWindow(), and the controller
// pumps the rules engine / AI brain.
//
// Most of the rules logic lives in the static helpers in Round/, AI/,
// Affixes/, Jokers/. This class is the orchestrator.

using System.Collections.Generic;
using System.Linq;
using Lugen.Affixes;
using Lugen.AI;
using Lugen.Cards;
using Lugen.Characters;
using Lugen.Core;
using Lugen.Deck;
using Lugen.Jokers;
using Lugen.Round;
using Lugen.Run;

namespace Lugen.UnityHooks
{
    public class RoundController
    {
        public RunState Run { get; }
        public RoundState State { get; private set; }
        public HumanProfile HumanProfile { get; private set; }
        public LogSink Logs { get; } = new LogSink();

        public RoundController(RunState run)
        {
            Run = run;
            HumanProfile = new HumanProfile();
        }

        // Spin up the round: build deck, deal, apply fairness, set state.
        public void StartRound()
        {
            // 1. RANDOM.EXE: shed every run-deck affix and re-roll if active.
            var character = CharacterCatalog.Get(Run.characterId);
            if (character != null && character.apostateReroll)
            {
                foreach (var c in Run.runDeck)
                {
                    if (c.rank == Rank.Jack) continue;
                    c.affix = Rng.Pick(AffixExtensions.AllRandomable);
                    c.cursedLockTurns = 0;
                }
            }

            // 2. Build the round deck from base + every player's run deck.
            var allRunDecks = new List<List<Card>> { Run.runDeck };
            for (int p = 1; p < Constants.NUM_PLAYERS; p++)
            {
                allRunDecks.Add(DeckBuilder.BuildInitialRunDeck(p));
            }
            var deck = DeckBuilder.BuildDeck(allRunDecks);

            // 3. Deal — Hoarder bonus + Lugen-specials.
            var lugenSeats = new bool[Constants.NUM_PLAYERS];
            for (int p = 1; p < Constants.NUM_PLAYERS; p++)
                if (Run.botPersonalities[p] == "lugen") lugenSeats[p] = true;
            int handBonus = character?.handSizeBonus ?? 0;
            var dealt = DeckBuilder.Deal(deck, Constants.NUM_PLAYERS, handBonus, lugenSeats);

            // 4. Jack fairness + own-deck minimum.
            JackFairness.ApplyJackFairness(dealt.hands, dealt.drawPile, JackLimitFor);
            float minFraction = Constants.OWN_DECK_MIN_FRACTION_BASE;
            if (Run.jokers.Has("hometownHero")) minFraction = System.Math.Max(minFraction, 0.50f);
            if (Run.stackedHandPending) minFraction += 0.20f;
            JackFairness.EnforceOwnDeckMinimum(dealt.hands, dealt.drawPile, minFraction);

            // 5. Floor mods.
            if (Run.currentFloorModifier == "brittle")
                AffixHooks.ApplyBrittleFloor(dealt.hands, dealt.drawPile);
            else
                AffixHooks.InfuseDrawPileWithRandomAffixes(dealt.drawPile, Run.currentFloor);

            // 6. Initial Target Rank with bias.
            Rank target = TargetRotation.PickInitialTarget(Run.runDeck, Run.currentFloorModifier == "inverted");

            // 7. Build state.
            State = new RoundState
            {
                hands = dealt.hands,
                drawPile = dealt.drawPile,
                targetRank = target,
                eliminated = new bool[Constants.NUM_PLAYERS],
                finished = new bool[Constants.NUM_PLAYERS],
                turnsTaken = new int[Constants.NUM_PLAYERS],
                outOfTurns = new bool[Constants.NUM_PLAYERS],
                currentTurn = 0, // human leads round 1; rotation handles subsequent rounds.
                stackedHandActive = Run.stackedHandPending,
            };
            Run.stackedHandPending = false;

            // 8. Round-start joker / relic peeks (Cold Read, Hand Mirror, etc.)
            if (Run.jokers.Has("coldRead"))
            {
                foreach (var msg in JokerHooks.TriggerColdRead(State))
                    Logs.PrivatePeek("Cold Read — " + msg);
            }
            if (Run.relics.Contains("crackedCoin"))
            {
                int got = GoldEconomy.AddGold(Run, 5 * Run.hearts);
                Logs.Log($"Cracked Coin: +{got}g ({Run.hearts} hearts).");
            }
        }

        // Computed Jack limit for any seat. Used by deck fairness + curse check.
        public int JackLimitFor(int seat)
        {
            int limit = Constants.JACK_LIMIT;
            var character = CharacterCatalog.Get(Run.characterId);
            int jackLimitBonus = character?.jackLimitBonus ?? 0;
            int jokerBonus = (seat == 0 && Run.jokers.Has("safetyNet")) ? 1 : 0;
            int greedyDrop = (Run.currentFloorModifier == "greedy") ? 1 : 0;
            if (seat == 0) limit += jackLimitBonus + jokerBonus;
            if (Run.botPersonalities[seat] == "lugen") limit = 6;
            return limit - greedyDrop;
        }

        // Advance turn after a play resolves.
        public void AdvanceTurn() => TurnRotation.AdvanceTurn(State, State.currentTurn);

        // Run a bot turn. The Unity layer should call this on a coroutine
        // so there's a brief delay (BOT_TURN_DELAY_MS) before the AI plays.
        public void RunBotTurn(int botIdx)
        {
            if (State.gameOver || State.challengeOpen) return;
            if (State.currentTurn != botIdx) return;
            var pers = PersonalityCatalog.Get(Run.botPersonalities[botIdx]);
            int auditorEveryN = 1 + Rng.Range(0, 5); // Auditor: rolls 1-5.
            var decision = BotBrain.ChoosePlay(State, botIdx, pers, auditorEveryN, HumanProfile);
            if (decision == null)
            {
                JackCurse.MarkFinished(State, botIdx);
                AdvanceTurn();
                return;
            }
            var ids = decision.cardsToPlay.Select(c => c.id).ToList();
            TurnResolver.TryPlay(State, botIdx, ids, 1, 3, Logs);
            // Caller opens challenge window via OpenChallengeWindow().
        }

        // Open the challenge window. Real Unity code wires this to a UI timer
        // and either fires CallLiar() or CloseChallengeWindow() when expired.
        public void OpenChallengeWindow(int playerJustPlayed)
        {
            State.challengeOpen = true;
            State.challengerIdx = (playerJustPlayed + 1) % Constants.NUM_PLAYERS;
        }

        public void CloseChallengeWindow()
        {
            State.challengeOpen = false;
            // Pass-the-claim move: turn advances to whoever could have called.
            AdvanceTurn();
        }

        public LiarOutcome CallLiar(int challengerIdx)
        {
            bool witchUncapped = (Run.characterId == "witch");
            bool ironOn = Run.relics.Contains("ironStomach");
            bool steelSpine = Run.relics.Contains("steelSpine");
            var outcome = LiarResolver.Resolve(State, challengerIdx, witchUncapped, ironOn, steelSpine);
            // Caller-Mark joker side-effect.
            if (challengerIdx == 0 && Run.jokers.Has("callersMark") && !State.callersMarkFiredThisRound)
            {
                State.callersMarkFiredThisRound = true;
                int delta = JokerHooks.TriggerCallersMark(outcome.truthTold);
                if (delta > 0) GoldEconomy.AddGold(Run, delta);
                else Run.gold = System.Math.Max(0, Run.gold + delta);
            }
            return outcome;
        }
    }

    // Tiny in-memory log sink. The Unity UI subscribes to OnLog / OnPrivatePeek.
    public class LogSink : ILogSink
    {
        public System.Action<string> OnLog;
        public System.Action<string> OnPrivatePeek;
        public List<string> Buffer = new List<string>();
        public void Log(string m) { Buffer.Add(m); OnLog?.Invoke(m); }
        public void PrivatePeek(string m) { OnPrivatePeek?.Invoke(m); }
    }
}
