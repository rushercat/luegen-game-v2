// Lügen — DebugGameUI.cs
// IMGUI debug interface that drives a full run end-to-end:
// character select → round → round-end → floor-end → fork node →
// shop / reward / event / cleanse / treasure → boss relic → next floor →
// run-end.
//
// It's a single MonoBehaviour state-machine. Phases are tracked by the
// `phase` enum; each phase has a Draw method. Press Play and the IMGUI
// renders the current phase.
//
// All visuals are intentionally minimal — IMGUI buttons, text, and
// scroll views. The point is to validate the rules engine across the
// whole run, not to look pretty.

#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using Lugen.Affixes;
using Lugen.AI;
using Lugen.Cards;
using Lugen.Characters;
using Lugen.Consumables;
using Lugen.Core;
using Lugen.Floor;
using Lugen.Jokers;
using Lugen.Relics;
using Lugen.Round;
using Lugen.Run;
using UnityEngine;

namespace Lugen.UnityHooks
{
    public class DebugGameUI : MonoBehaviour
    {
        // ---------------- State ----------------

        private enum Phase
        {
            Intro,        // Welcome screen, "Start"
            CharSelect,   // Pick a character
            Round,        // Active round — play / call liar
            RoundEnd,     // Round resolved, show outcome + Continue
            FloorEnd,     // Floor cleared (or floor lost) — settle hearts
            BossRelic,    // After boss, pick from 2 relics
            Fork,         // 3-way (or 2-way after boss) fork node
            Shop,         // Buy consumables / jokers / relic
            Reward,       // 2 jokers or 75g
            Event,        // Random event
            Cleanse,      // Remove a Cursed card / strip affix
            Treasure,     // Act III: rare relic pool
            RunEnd,       // Run over (win or lose)
        }

        private Phase phase = Phase.Intro;
        private RunManager run;
        private RoundController controller;
        private readonly HashSet<string> selectedCardIds = new HashSet<string>();

        // Bot scheduling.
        private float botTurnDelay = 0.5f;
        private float challengeWindowSec = 5f;
        private float botActionTimer;
        private float challengeTimer;
        private bool challengeWindowOpen;
        // Who's allowed to challenge the most-recent play (the seat right after
        // the player who just played). When this is 0 the human gets the
        // Call LIAR / Let it go buttons; otherwise a bot decides automatically
        // after a short readable pause.
        private int currentChallengerSeat = -1;
        private float botChallengeDecideTimer;

        private List<string> uiLog = new List<string>();
        private Vector2 logScroll;
        private Vector2 mainScroll;

        // Per-phase scratch state.
        private ForkOptions currentFork;
        private Lugen.Shop.Shop currentShop;
        private List<string> rewardJokerOffers;        // 2 jokers
        private List<string> bossRelicOffers;          // 2 relics
        private List<string> treasureRelicOffers;       // 2 relics from treasure pool
        private string lastEventTitle, lastEventText, lastEventResult;
        private int? lastWinnerIdx;
        private int lastRoundGoldGained;

        // ---------------- Lifecycle ----------------

        private void Update()
        {
            if (phase != Phase.Round || controller?.State == null) return;
            var s = controller.State;
            if (s.gameOver) { GoToRoundEnd(); return; }

            if (!challengeWindowOpen && s.currentTurn != 0)
            {
                botActionTimer -= Time.deltaTime;
                if (botActionTimer <= 0f)
                {
                    PumpBotTurn();
                    botActionTimer = botTurnDelay;
                }
            }
            if (challengeWindowOpen)
            {
                if (currentChallengerSeat == 0)
                {
                    challengeTimer -= Time.deltaTime;
                    if (challengeTimer <= 0f) CloseChallenge();
                }
                else
                {
                    // Bot is "thinking" — short delay before they roll their
                    // challenge rate so the user can read what just happened.
                    botChallengeDecideTimer -= Time.deltaTime;
                    if (botChallengeDecideTimer <= 0f) BotChallengeDecide();
                }
            }
        }

        private void OnGUI()
        {
            GUI.skin.label.wordWrap = true;
            switch (phase)
            {
                case Phase.Intro:      DrawIntro();      break;
                case Phase.CharSelect: DrawCharSelect(); break;
                case Phase.Round:      DrawRound();      break;
                case Phase.RoundEnd:   DrawRoundEnd();   break;
                case Phase.FloorEnd:   DrawFloorEnd();   break;
                case Phase.BossRelic:  DrawBossRelic();  break;
                case Phase.Fork:       DrawFork();       break;
                case Phase.Shop:       DrawShop();       break;
                case Phase.Reward:     DrawReward();     break;
                case Phase.Event:      DrawEvent();      break;
                case Phase.Cleanse:    DrawCleanse();    break;
                case Phase.Treasure:   DrawTreasure();   break;
                case Phase.RunEnd:     DrawRunEnd();     break;
            }
        }

        // ============================================================
        // Phase: Intro
        // ============================================================
        private void DrawIntro()
        {
            GUILayout.BeginArea(new Rect(40, 40, 500, 240), "Lügen — Debug UI", GUI.skin.window);
            GUILayout.Space(20);
            GUILayout.Label("A roguelike bluff card game.\n" +
                            "Pick a character, fight your way through 9 floors,\n" +
                            "build your run deck, and beat Lugen.\n");
            if (GUILayout.Button("Start a run", GUILayout.Height(40)))
            {
                phase = Phase.CharSelect;
            }
            GUILayout.EndArea();
        }

        // ============================================================
        // Phase: Character Select
        // ============================================================
        private void DrawCharSelect()
        {
            GUI.Box(new Rect(10, 10, Screen.width - 20, 30), "Pick a character");
            mainScroll = GUI.BeginScrollView(new Rect(10, 50, Screen.width - 20, Screen.height - 80),
                mainScroll, new Rect(0, 0, Screen.width - 60, CharacterCatalog.All.Count * 110));
            int y = 0;
            foreach (var ch in CharacterCatalog.All.Values)
            {
                bool unlocked = ch.unlockAlways || ch.unlockAtFloor == 0; // simplified: ignore floor unlocks for debug
                GUI.Box(new Rect(0, y, Screen.width - 60, 100), "");
                GUI.Label(new Rect(10, y + 5, 400, 20), $"{ch.name}");
                GUI.Label(new Rect(10, y + 25, Screen.width - 80, 40), $"\"{ch.flavor}\"\n{ch.passive}");
                if (unlocked && GUI.Button(new Rect(Screen.width - 200, y + 60, 120, 30), "Choose"))
                {
                    StartRun(ch.id);
                }
                else if (!unlocked)
                {
                    GUI.Label(new Rect(Screen.width - 240, y + 60, 200, 30), "(locked)");
                }
                y += 110;
            }
            GUI.EndScrollView();
        }

        private void StartRun(string charId)
        {
            run = new RunManager();
            run.StartRun(charId);
            controller = new RoundController(run.State);
            controller.Logs.OnLog += AppendLog;
            controller.Logs.OnPrivatePeek += AppendLog;
            // Roll floor modifier for floors 4-8 non-boss.
            RollFloorModifier();
            controller.StartRound();
            AppendLog($"=== Run started: {CharacterCatalog.Get(charId).name}, Floor 1 ===");
            phase = Phase.Round;
            botActionTimer = botTurnDelay;
        }

        private void RollFloorModifier()
        {
            run.State.currentFloorModifier = null;
            int f = run.State.currentFloor;
            if (f >= 4 && f != 6 && f != 9 && f != 3)
            {
                if (!string.IsNullOrEmpty(run.State.preRolledNextFloorMod))
                {
                    run.State.currentFloorModifier = run.State.preRolledNextFloorMod;
                    run.State.preRolledNextFloorMod = null;
                }
                else
                {
                    var pool = FloorModifiers.EligibleForFloor(f);
                    if (pool.Count > 0)
                        run.State.currentFloorModifier = Rng.Pick(pool);
                }
                if (run.State.currentFloorModifier != null)
                    AppendLog($"Floor modifier: {FloorModifiers.All[run.State.currentFloorModifier].name}.");
            }
        }

        // ============================================================
        // Phase: Round (the main gameplay screen)
        // ============================================================
        private void DrawRound()
        {
            DrawTopBar();
            DrawOpponents();
            DrawPile();
            DrawHand();
            DrawActions();
            DrawLog();
        }

        private void DrawTopBar()
        {
            var s = controller.State;
            var rs = run.State;
            string mod = string.IsNullOrEmpty(rs.currentFloorModifier) ? "" : $" · MOD: {rs.currentFloorModifier}";
            string txt = $"Floor {rs.currentFloor}/{Constants.TOTAL_FLOORS}    " +
                         $"♥ {rs.hearts}    💰 {rs.gold}g    " +
                         $"Target: {s.targetRank.ToShort()}    " +
                         $"Draw pile: {s.drawPile.Count}    " +
                         $"Played: {s.pile.Count}    " +
                         $"Turn: seat {s.currentTurn}{mod}    " +
                         $"Wins: {string.Join("/", rs.roundsWon)}";
            GUI.Box(new Rect(10, 10, Screen.width - 20, 30), txt);
        }

        private void DrawOpponents()
        {
            var s = controller.State;
            GUILayout.BeginArea(new Rect(10, 50, 360, 200), "Opponents", GUI.skin.box);
            GUILayout.Space(20);
            for (int i = 1; i < s.NumPlayers; i++)
            {
                string status = s.eliminated[i] ? "ELIM" : s.finished[i] ? "DONE" : $"{s.hands[i].Count} cards";
                string pers = run.State.botPersonalities[i] ?? "?";
                bool isCurrent = s.currentTurn == i && !challengeWindowOpen;
                GUILayout.Label((isCurrent ? "▶ " : "  ") + $"Seat {i} ({pers}) — {status}");
            }
            GUILayout.EndArea();
        }

        private void DrawPile()
        {
            var s = controller.State;
            GUILayout.BeginArea(new Rect(380, 50, 360, 200), "Played pile", GUI.skin.box);
            GUILayout.Space(20);
            if (s.lastPlay != null)
                GUILayout.Label($"Seat {s.lastPlay.playerIdx} played {s.lastPlay.count} as {s.lastPlay.claim.ToShort()}");
            else
                GUILayout.Label("No plays yet this round.");
            GUILayout.Label($"Total in pile: {s.pile.Count}");
            // Show jokers / relics summary.
            var jokers = run.State.jokers.slots.Where(j => !string.IsNullOrEmpty(j)).ToList();
            GUILayout.Label($"Jokers: {(jokers.Count > 0 ? string.Join(", ", jokers) : "(none)")}");
            GUILayout.Label($"Relics: {(run.State.relics.Count > 0 ? string.Join(", ", run.State.relics) : "(none)")}");
            int invTotal = run.State.inventory.Values.Sum();
            GUILayout.Label($"Inventory: {invTotal}");
            GUILayout.EndArea();
        }

        private void DrawHand()
        {
            var s = controller.State;
            if (s.hands.Count == 0) return;
            var hand = s.hands[0];

            int top = 260;
            GUI.Box(new Rect(10, top, Screen.width - 20, 80), $"Your hand ({hand.Count})");
            float x = 20;
            float y = top + 22;
            foreach (var c in hand)
            {
                bool selected = selectedCardIds.Contains(c.id);
                GUI.color = selected ? Color.yellow : Color.white;
                string label = c.rank.ToShort();
                if (c.affix != Affix.None) label += $"\n[{c.affix.ToShort()}]";
                if (c.cursedLockTurns > 0) label += $"\nL:{c.cursedLockTurns}";
                if (GUI.Button(new Rect(x, y, 60, 50), label))
                {
                    if (selected) selectedCardIds.Remove(c.id);
                    else selectedCardIds.Add(c.id);
                }
                x += 65;
            }
            GUI.color = Color.white;
        }

        private void DrawActions()
        {
            var s = controller.State;
            int top = 350;

            if (s.currentTurn == 0 && !challengeWindowOpen && !s.gameOver)
            {
                GUI.Label(new Rect(10, top, 400, 20), $"Your turn. Select 1–3, Play as {s.targetRank.ToShort()}.");
                if (GUI.Button(new Rect(10, top + 22, 200, 30), $"Play {selectedCardIds.Count} cards"))
                {
                    DoHumanPlay();
                }
                if (GUI.Button(new Rect(220, top + 22, 100, 30), "Clear"))
                {
                    selectedCardIds.Clear();
                }
            }

            if (challengeWindowOpen)
            {
                GUI.color = Color.yellow;
                string lastPlayInfo = s.lastPlay == null ? "" :
                    $"seat {s.lastPlay.playerIdx} → {s.lastPlay.count} as {s.lastPlay.claim.ToShort()}";
                GUI.Box(new Rect(10, top, Screen.width - 20, 60),
                    $"Challenge — challenger: seat {currentChallengerSeat}   (last play: {lastPlayInfo})");
                GUI.color = Color.white;
                if (currentChallengerSeat == 0)
                {
                    GUI.Label(new Rect(15, top + 22, 200, 18), $"Your call — {challengeTimer:F1}s left");
                    if (GUI.Button(new Rect(15, top + 38, 200, 22), "Call LIAR"))
                        DoCallLiar();
                    if (GUI.Button(new Rect(225, top + 38, 200, 22), "Let it go"))
                        CloseChallenge();
                }
                else
                {
                    GUI.Label(new Rect(15, top + 30, 400, 22), $"Seat {currentChallengerSeat} is deciding...");
                }
            }
        }

        private void DrawLog()
        {
            int x = Screen.width - 410;
            GUI.Box(new Rect(x, 50, 400, Screen.height - 100), "Log");
            logScroll = GUI.BeginScrollView(new Rect(x + 10, 75, 380, Screen.height - 130),
                                             logScroll,
                                             new Rect(0, 0, 360, uiLog.Count * 18 + 10));
            for (int i = 0; i < uiLog.Count; i++)
                GUI.Label(new Rect(0, i * 18, 360, 20), uiLog[i]);
            GUI.EndScrollView();
        }

        // ============================================================
        // Round actions
        // ============================================================
        private void DoHumanPlay()
        {
            var s = controller.State;
            if (s.currentTurn != 0) return;
            if (selectedCardIds.Count < 1 || selectedCardIds.Count > 3) { AppendLog("Pick 1–3 cards."); return; }

            var ok = TurnResolver.TryPlay(s, 0, selectedCardIds.ToList(), 1, 3, controller.Logs);
            if (!ok) { AppendLog("Play rejected."); return; }
            selectedCardIds.Clear();

            if (s.hands[0].Count == 0)
            {
                JackCurse.MarkFinished(s, 0);
                AppendLog("You emptied your hand!");
            }

            OpenChallenge(0);
        }

        private void DoCallLiar() => DoCallLiarFor(0);

        private void DoCallLiarFor(int challenger)
        {
            var outcome = controller.CallLiar(challenger);
            challengeWindowOpen = false;
            currentChallengerSeat = -1;
            AppendLog(outcome.truthTold
                ? $"Truth told. Seat {challenger} takes the pile and is skipped."
                : $"LIAR! Seat {controller.State.lastPlay.playerIdx} takes the pile.");
            ResolvePostChallenge(outcome);
        }

        // Bot challenger rolled their challengeRate. If they call LIAR, resolve;
        // otherwise close the window and advance.
        private void BotChallengeDecide()
        {
            var s = controller.State;
            int bot = currentChallengerSeat;
            var pers = PersonalityCatalog.Get(run.State.botPersonalities[bot]);
            int auditorEveryN = 1 + Rng.Range(0, 5);
            bool willCall = BotBrain.ShouldCallLiar(s, bot, pers, auditorEveryN);
            if (willCall)
            {
                AppendLog($"Seat {bot} ({pers?.name ?? "?"}) calls LIAR!");
                DoCallLiarFor(bot);
            }
            else
            {
                AppendLog($"Seat {bot} passes.");
                CloseChallenge();
            }
        }

        private void CloseChallenge()
        {
            challengeWindowOpen = false;
            currentChallengerSeat = -1;
            // Clear the engine-side flag too — without this, TurnResolver.TryPlay
            // and RoundController.RunBotTurn both bail early on the next turn,
            // every play silently fails, and the round wedges.
            if (controller?.State != null) controller.State.challengeOpen = false;
            AdvanceAfterPlay();
        }

        private void OpenChallenge(int playerJustPlayed)
        {
            // Find the next active seat after the player who just played —
            // they are the one who can call LIAR. If there's nobody (everyone
            // else is eliminated/finished), close immediately.
            int challenger = TurnRotation.FindNextActive(controller.State, playerJustPlayed);
            if (challenger < 0)
            {
                if (controller?.State != null) controller.State.challengeOpen = false;
                AdvanceAfterPlay();
                return;
            }
            currentChallengerSeat = challenger;
            challengeWindowOpen = true;
            challengeTimer = challengeWindowSec;
            botChallengeDecideTimer = 0.7f;     // brief readability pause for bot decisions
            controller.OpenChallengeWindow(playerJustPlayed);
        }

        private void PumpBotTurn()
        {
            var s = controller.State;
            int bot = s.currentTurn;
            if (s.eliminated[bot] || s.finished[bot] || s.outOfTurns[bot] || s.hands[bot].Count == 0)
            {
                if (s.hands[bot].Count == 0) JackCurse.MarkFinished(s, bot);
                controller.AdvanceTurn();
                return;
            }
            // Snapshot pile size; if the bot didn't actually play (e.g. legal-move
            // exhaustion, AdvanceTurn was called inside RunBotTurn) we shouldn't
            // open a phantom challenge window.
            int pileBefore = s.pile.Count;
            controller.RunBotTurn(bot);
            if (s.pile.Count <= pileBefore) return;
            OpenChallenge(bot);
        }

        private void AdvanceAfterPlay()
        {
            controller.AdvanceTurn();
            CheckRoundEnd();
        }

        private void ResolvePostChallenge(LiarOutcome outcome)
        {
            var s = controller.State;
            int picker = outcome.pickerUpper;

            if (JackCurse.CheckCurse(s, picker, controller.JackLimitFor(picker)))
            {
                AppendLog($"Jack curse: seat {picker} eliminated.");
                if (picker == 0) run.State.hearts = Mathf.Max(0, run.State.hearts - 1);
            }

            for (int p = 0; p < s.NumPlayers; p++)
            {
                if (!s.finished[p] && !s.eliminated[p] && s.hands[p].Count == 0)
                {
                    JackCurse.MarkFinished(s, p);
                    AppendLog($"Seat {p} emptied their hand.");
                }
            }

            CheckRoundEnd();

            if (!s.gameOver)
            {
                int next = TurnRotation.FindNextActive(s, picker);
                if (next < 0) s.gameOver = true;
                else s.currentTurn = next;
            }
        }

        private void CheckRoundEnd()
        {
            var s = controller.State;
            if (JackCurse.ShouldEndRound(s))
            {
                s.gameOver = true;
                AppendLog($"=== Round over. Placements: {string.Join(", ", s.placements)} ===");
            }
        }

        // ============================================================
        // Phase: Round End — settle gold, advance roundsWon, route to next phase
        // ============================================================
        private void GoToRoundEnd()
        {
            var s = controller.State;
            lastWinnerIdx = s.placements.Count > 0 ? s.placements[0] : (int?)null;
            lastRoundGoldGained = 0;

            // Settle gold for the human (rough): 1st place +20g, 2nd place +10g.
            if (s.placements.Count > 0 && s.placements[0] == 0)
                lastRoundGoldGained = GoldEconomy.AddGold(run.State, Constants.GOLD_PLACE_1);
            else if (s.placements.Count > 1 && s.placements[1] == 0)
                lastRoundGoldGained = GoldEconomy.AddGold(run.State, Constants.GOLD_PLACE_2);

            // Increment roundsWon for the round winner.
            if (lastWinnerIdx.HasValue) run.State.roundsWon[lastWinnerIdx.Value]++;

            phase = Phase.RoundEnd;
        }

        private void DrawRoundEnd()
        {
            var rs = run.State;
            GUILayout.BeginArea(new Rect(40, 40, 600, 400), "Round over", GUI.skin.window);
            GUILayout.Space(20);
            string winnerStr = lastWinnerIdx.HasValue ? $"seat {lastWinnerIdx.Value}" : "(no winner — all eliminated)";
            GUILayout.Label($"Round winner: {winnerStr}");
            GUILayout.Label($"Floor wins so far: {string.Join(" / ", rs.roundsWon)}");
            GUILayout.Label($"Gold: {rs.gold}g  ({(lastRoundGoldGained > 0 ? "+" + lastRoundGoldGained : "0")}g this round)");
            GUILayout.Label($"Hearts: {rs.hearts}");
            GUILayout.Space(20);

            // Decide next phase.
            int floorWinner = -1;
            for (int p = 0; p < rs.roundsWon.Length; p++)
                if (rs.roundsWon[p] >= Constants.ROUNDS_TO_WIN_FLOOR) floorWinner = p;

            if (floorWinner >= 0)
            {
                if (GUILayout.Button("Continue to floor end", GUILayout.Height(40)))
                    GoToFloorEnd(floorWinner);
            }
            else
            {
                if (GUILayout.Button("Next round", GUILayout.Height(40)))
                    NextRound();
            }
            GUILayout.EndArea();
        }

        private void NextRound()
        {
            selectedCardIds.Clear();
            controller.StartRound();
            phase = Phase.Round;
            botActionTimer = botTurnDelay;
        }

        // ============================================================
        // Phase: Floor End
        // ============================================================
        private void GoToFloorEnd(int floorWinner)
        {
            var rs = run.State;
            bool humanWon = floorWinner == 0;
            if (humanWon)
            {
                int floorBonus = GoldEconomy.AddGold(rs, Constants.GOLD_PER_FLOOR_WIN);
                AppendLog($"=== Floor {rs.currentFloor} cleared. +{floorBonus}g bonus. ===");
            }
            else
            {
                rs.hearts = Mathf.Max(0, rs.hearts - 1);
                AppendLog($"=== Lost floor {rs.currentFloor}. -1 Heart. ({rs.hearts} left) ===");
            }
            phase = Phase.FloorEnd;
        }

        private void DrawFloorEnd()
        {
            var rs = run.State;
            GUILayout.BeginArea(new Rect(40, 40, 600, 400), "Floor end", GUI.skin.window);
            GUILayout.Space(20);
            GUILayout.Label($"Floor {rs.currentFloor} resolved.");
            GUILayout.Label($"Hearts: {rs.hearts}  ·  Gold: {rs.gold}g");
            GUILayout.Space(20);

            if (rs.hearts <= 0)
            {
                if (GUILayout.Button("Run over", GUILayout.Height(40)))
                {
                    phase = Phase.RunEnd;
                }
                GUILayout.EndArea();
                return;
            }

            // Human cleared the floor → relic offer (boss) or fork.
            bool humanWonFloor = rs.roundsWon.Length > 0 && rs.roundsWon[0] >= Constants.ROUNDS_TO_WIN_FLOOR;
            if (humanWonFloor && rs.currentFloor == Constants.TOTAL_FLOORS)
            {
                if (GUILayout.Button("YOU BEAT THE RUN. Continue to victory screen.", GUILayout.Height(40)))
                    phase = Phase.RunEnd;
                GUILayout.EndArea();
                return;
            }

            if (humanWonFloor)
            {
                if (run.IsBossFloor(rs.currentFloor))
                {
                    if (GUILayout.Button("Pick a relic", GUILayout.Height(40))) GoToBossRelic();
                }
                else
                {
                    if (GUILayout.Button("Continue to fork", GUILayout.Height(40))) GoToFork(boss: false);
                }
            }
            else
            {
                // Human lost the floor; replay it (rounds reset in AdvanceFloor isn't right — we just zero rounds).
                if (GUILayout.Button("Retry this floor", GUILayout.Height(40)))
                {
                    rs.roundsWon = new int[Constants.NUM_PLAYERS];
                    NextRound();
                }
            }
            GUILayout.EndArea();
        }

        // ============================================================
        // Phase: Boss relic offer (after Floors 3, 6, 9 wins)
        // ============================================================
        private void GoToBossRelic()
        {
            string bossId = run.State.botPersonalities[1]; // boss seat is 1.
            if (string.IsNullOrEmpty(bossId) || !RelicCatalog.BossPool.ContainsKey(bossId))
            {
                // No boss pool defined (e.g. mirror/hollow alts) — skip to fork.
                AdvanceToNextFloor();
                return;
            }
            var pool = RelicCatalog.BossPool[bossId].Where(r => !run.State.relics.Contains(r)).ToList();
            Rng.ShuffleInPlace(pool);
            bossRelicOffers = pool.Take(2).ToList();
            phase = Phase.BossRelic;
        }

        private void DrawBossRelic()
        {
            GUILayout.BeginArea(new Rect(40, 40, 700, 400), "Boss relic", GUI.skin.window);
            GUILayout.Space(20);
            GUILayout.Label("Pick one relic:");
            GUILayout.Space(10);
            foreach (var rid in bossRelicOffers)
            {
                if (!RelicCatalog.All.TryGetValue(rid, out var data)) continue;
                GUILayout.Box($"{data.name} — {data.desc}");
                if (GUILayout.Button($"Take {data.name}", GUILayout.Height(30)))
                {
                    run.State.relics.Add(rid);
                    AppendLog($"Relic acquired: {data.name}.");
                    AdvanceToNextFloor();
                }
            }
            if (GUILayout.Button("Skip relic", GUILayout.Height(30)))
            {
                AdvanceToNextFloor();
            }
            GUILayout.EndArea();
        }

        // ============================================================
        // Phase: Fork
        // ============================================================
        private void GoToFork(bool boss)
        {
            currentFork = ForkNode.RollFork(run.State.currentFloor, boss);
            phase = Phase.Fork;
        }

        private void DrawFork()
        {
            GUILayout.BeginArea(new Rect(40, 40, 600, 400), "Fork — pick a node", GUI.skin.window);
            GUILayout.Space(20);
            foreach (var opt in currentFork.options)
            {
                if (GUILayout.Button(opt.ToString(), GUILayout.Height(40)))
                {
                    EnterForkNode(opt);
                    break;
                }
            }
            GUILayout.EndArea();
        }

        private void EnterForkNode(ForkNodeType node)
        {
            switch (node)
            {
                case ForkNodeType.Shop:
                    currentShop = new Lugen.Shop.Shop(run.State);
                    phase = Phase.Shop;
                    break;
                case ForkNodeType.Reward:
                    var pool = JokerCatalog.All.Values
                        .Where(j => !run.State.jokers.Has(j.id) || (j.stackable && run.State.jokers.Stack(j.id) < j.maxStack))
                        .ToList();
                    Rng.ShuffleInPlace(pool);
                    rewardJokerOffers = pool.Take(2).Select(j => j.id).ToList();
                    phase = Phase.Reward;
                    break;
                case ForkNodeType.Event:
                    RollRandomEvent();
                    phase = Phase.Event;
                    break;
                case ForkNodeType.Cleanse:
                    phase = Phase.Cleanse;
                    break;
                case ForkNodeType.Treasure:
                    var tpool = RelicCatalog.TreasurePool
                        .Where(r => !run.State.relics.Contains(r) && (RelicCatalog.All[r].unlock == null))
                        .ToList();
                    Rng.ShuffleInPlace(tpool);
                    treasureRelicOffers = tpool.Take(2).ToList();
                    phase = Phase.Treasure;
                    break;
            }
        }

        // ============================================================
        // Phase: Shop
        // ============================================================
        private Vector2 shopScroll;
        private void DrawShop()
        {
            GUI.Box(new Rect(10, 10, Screen.width - 20, 30), $"Shop  —  Gold: {run.State.gold}g");
            shopScroll = GUI.BeginScrollView(new Rect(10, 50, Screen.width - 20, Screen.height - 100),
                shopScroll, new Rect(0, 0, Screen.width - 60, 1000));
            int y = 0;
            GUI.Label(new Rect(0, y, 200, 20), "Consumables"); y += 22;
            foreach (var id in currentShop.Inventory)
            {
                if (!ConsumableCatalog.All.TryGetValue(id, out var c)) continue;
                GUI.Label(new Rect(0, y, Screen.width - 200, 20), $"{c.name} — {c.desc}");
                if (GUI.Button(new Rect(Screen.width - 180, y, 120, 22), $"Buy ({c.price}g)"))
                {
                    var err = currentShop.BuyConsumable(id);
                    AppendLog(err ?? $"Bought {c.name} (-{c.price}g).");
                }
                y += 26;
            }
            y += 10;
            GUI.Label(new Rect(0, y, 200, 20), "Jokers"); y += 22;
            foreach (var id in currentShop.JokerOffers)
            {
                if (!JokerCatalog.All.TryGetValue(id, out var j)) continue;
                GUI.Label(new Rect(0, y, Screen.width - 200, 40), $"{j.name} [{j.rarity}] — {j.desc}");
                if (GUI.Button(new Rect(Screen.width - 180, y, 120, 22), $"Buy ({j.price}g)"))
                {
                    var err = currentShop.BuyJoker(id);
                    AppendLog(err ?? $"Bought {j.name} (-{j.price}g).");
                }
                y += 46;
            }
            if (currentShop.RelicOffers.Count > 0)
            {
                y += 10;
                GUI.Label(new Rect(0, y, 200, 20), "Relic"); y += 22;
                foreach (var id in currentShop.RelicOffers)
                {
                    if (!RelicCatalog.All.TryGetValue(id, out var r)) continue;
                    GUI.Label(new Rect(0, y, Screen.width - 200, 40), $"{r.name} — {r.desc}");
                    if (GUI.Button(new Rect(Screen.width - 180, y, 120, 22), $"Buy ({r.price}g)"))
                    {
                        var err = currentShop.BuyRelic(id);
                        AppendLog(err ?? $"Bought {r.name} (-{r.price}g).");
                    }
                    y += 46;
                }
            }
            GUI.EndScrollView();
            if (GUI.Button(new Rect(Screen.width - 200, Screen.height - 50, 180, 30), "Leave shop"))
            {
                AdvanceToNextFloor();
            }
        }

        // ============================================================
        // Phase: Reward
        // ============================================================
        private void DrawReward()
        {
            GUILayout.BeginArea(new Rect(40, 40, 700, 400), "Reward — pick a joker", GUI.skin.window);
            GUILayout.Space(20);
            foreach (var id in rewardJokerOffers)
            {
                if (!JokerCatalog.All.TryGetValue(id, out var j)) continue;
                GUILayout.Box($"{j.name} [{j.rarity}] — {j.desc}");
                if (GUILayout.Button($"Take {j.name}", GUILayout.Height(30)))
                {
                    run.State.jokers.TryEquip(j);
                    AppendLog($"Joker equipped: {j.name}.");
                    AdvanceToNextFloor();
                }
            }
            GUILayout.Space(10);
            if (GUILayout.Button($"Skip — take {Constants.REWARD_NODE_GOLD}g instead", GUILayout.Height(30)))
            {
                int g = GoldEconomy.AddGold(run.State, Constants.REWARD_NODE_GOLD);
                AppendLog($"Skipped reward, +{g}g.");
                AdvanceToNextFloor();
            }
            GUILayout.EndArea();
        }

        // ============================================================
        // Phase: Event (small subset of beta.js EVENTS)
        // ============================================================
        private void RollRandomEvent()
        {
            // Pick from a tiny event pool. The full pool from beta.js has
            // ~14 events; we ship a representative few.
            var pool = new System.Action[]
            {
                () => { lastEventTitle = "Found Coins"; lastEventText = "You spot coins on the floor."; int g = GoldEconomy.AddGold(run.State, 30); lastEventResult = $"+{g}g"; },
                () => { lastEventTitle = "Generous Drunk"; lastEventText = "A patron buys you a drink and tips you."; int g = GoldEconomy.AddGold(run.State, 50); lastEventResult = $"+{g}g"; },
                () => { lastEventTitle = "Pickpocket"; lastEventText = "Someone bumps into you in the crowd."; int loss = Mathf.Min(run.State.gold, 20); run.State.gold -= loss; lastEventResult = $"-{loss}g"; },
                () => { lastEventTitle = "Lucky Find"; lastEventText = "A strange charm clings to one of your cards."; var cands = run.State.runDeck.Where(c => c.affix != Affix.Steel && c.rank != Rank.Jack).ToList(); if (cands.Count > 0) { var card = Rng.Pick(cands); var old = card.affix; card.affix = Rng.Pick(AffixExtensions.AllRandomable); lastEventResult = $"{card.rank.ToShort()}: {old.ToShort()} -> {card.affix.ToShort()}"; } else lastEventResult = "(no eligible card)"; },
                () => { lastEventTitle = "Shrine of Hearts"; lastEventText = "Donate 100g for a Heart shard."; if (run.State.gold >= 100) { run.State.gold -= 100; run.State.heartShards++; if (run.State.heartShards >= Constants.HEART_SHARDS_REQUIRED) { run.State.hearts++; run.State.heartShards = 0; lastEventResult = "+1 shard → +1 Heart!"; } else lastEventResult = $"+1 shard ({run.State.heartShards}/{Constants.HEART_SHARDS_REQUIRED})"; } else lastEventResult = "Not enough gold."; },
            };
            var pick = pool[Rng.Range(0, pool.Length)];
            pick();
            AppendLog($"Event: {lastEventTitle} — {lastEventResult}");
        }

        private void DrawEvent()
        {
            GUILayout.BeginArea(new Rect(40, 40, 600, 400), "Event", GUI.skin.window);
            GUILayout.Space(20);
            GUILayout.Label(lastEventTitle ?? "");
            GUILayout.Label(lastEventText ?? "");
            GUILayout.Space(10);
            GUILayout.Label($"Result: {lastEventResult}");
            GUILayout.Space(20);
            if (GUILayout.Button("Continue", GUILayout.Height(30)))
                AdvanceToNextFloor();
            GUILayout.EndArea();
        }

        // ============================================================
        // Phase: Cleanse
        // ============================================================
        private void DrawCleanse()
        {
            GUILayout.BeginArea(new Rect(40, 40, 600, 500), "Cleanse — remove a Cursed card or strip an affix", GUI.skin.window);
            GUILayout.Space(20);
            // List Cursed cards in the run deck.
            var cursed = run.State.runDeck.Where(c => c.affix == Affix.Cursed).ToList();
            if (cursed.Count > 0)
            {
                GUILayout.Label("Cursed cards:");
                foreach (var c in cursed)
                {
                    if (GUILayout.Button($"Remove {c.rank.ToShort()} (Cursed)"))
                    {
                        run.State.runDeck.Remove(c);
                        AppendLog($"Cleansed Cursed {c.rank.ToShort()}.");
                        AdvanceToNextFloor();
                        GUILayout.EndArea(); return;
                    }
                }
            }
            // Strip an affix from any other run-deck card.
            GUILayout.Space(10);
            GUILayout.Label("Or strip the affix from a card:");
            foreach (var c in run.State.runDeck.Where(x => x.affix != Affix.None && x.affix != Affix.Cursed).Take(8))
            {
                if (GUILayout.Button($"Strip {c.affix.ToShort()} from {c.rank.ToShort()}"))
                {
                    var old = c.affix;
                    c.affix = Affix.None;
                    AppendLog($"Stripped {old.ToShort()} from {c.rank.ToShort()}.");
                    AdvanceToNextFloor();
                    GUILayout.EndArea(); return;
                }
            }
            GUILayout.Space(20);
            if (GUILayout.Button("Skip cleanse", GUILayout.Height(30)))
                AdvanceToNextFloor();
            GUILayout.EndArea();
        }

        // ============================================================
        // Phase: Treasure (Act III)
        // ============================================================
        private void DrawTreasure()
        {
            GUILayout.BeginArea(new Rect(40, 40, 700, 400), "Treasure — pick a relic", GUI.skin.window);
            GUILayout.Space(20);
            foreach (var rid in treasureRelicOffers)
            {
                if (!RelicCatalog.All.TryGetValue(rid, out var data)) continue;
                GUILayout.Box($"{data.name} — {data.desc}");
                if (GUILayout.Button($"Take {data.name}", GUILayout.Height(30)))
                {
                    run.State.relics.Add(rid);
                    AppendLog($"Treasure relic: {data.name}.");
                    AdvanceToNextFloor();
                }
            }
            if (GUILayout.Button("Skip", GUILayout.Height(30)))
                AdvanceToNextFloor();
            GUILayout.EndArea();
        }

        // ============================================================
        // Phase: Run End
        // ============================================================
        private void DrawRunEnd()
        {
            var rs = run.State;
            bool victory = rs.hearts > 0 && rs.currentFloor >= Constants.TOTAL_FLOORS && rs.roundsWon[0] >= Constants.ROUNDS_TO_WIN_FLOOR;
            GUILayout.BeginArea(new Rect(40, 40, 600, 400), victory ? "Victory!" : "Run over", GUI.skin.window);
            GUILayout.Space(20);
            GUILayout.Label(victory
                ? $"You cleared all {Constants.TOTAL_FLOORS} floors with {rs.hearts} ♥ remaining."
                : $"You ran out of Hearts on Floor {rs.currentFloor}.");
            GUILayout.Label($"Final gold: {rs.gold}g");
            GUILayout.Label($"Final run deck size: {rs.runDeck.Count}");
            GUILayout.Space(20);
            if (GUILayout.Button("Back to start", GUILayout.Height(40)))
            {
                run = null;
                controller = null;
                phase = Phase.Intro;
                uiLog.Clear();
            }
            GUILayout.EndArea();
        }

        // ============================================================
        // Floor advance
        // ============================================================
        private void AdvanceToNextFloor()
        {
            run.AdvanceFloor();
            RollFloorModifier();
            if (run.State.currentFloor > Constants.TOTAL_FLOORS)
            {
                phase = Phase.RunEnd;
                return;
            }
            // Reset round-scope state and start a new round.
            controller = new RoundController(run.State);
            controller.Logs.OnLog += AppendLog;
            controller.Logs.OnPrivatePeek += AppendLog;
            controller.StartRound();
            AppendLog($"=== Floor {run.State.currentFloor} starts. Target: {controller.State.targetRank.ToShort()} ===");
            phase = Phase.Round;
            botActionTimer = botTurnDelay;
            selectedCardIds.Clear();
        }

        // ============================================================
        // Helpers
        // ============================================================
        private void AppendLog(string msg)
        {
            uiLog.Add(msg);
            if (uiLog.Count > 200) uiLog.RemoveAt(0);
            logScroll.y = float.MaxValue;
        }
    }
}
#endif
