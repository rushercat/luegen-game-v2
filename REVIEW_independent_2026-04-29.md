# Lügen / Jacks Gambit — Independent Beta Review

**Reviewer:** Claude (independent pass — did not consult prior REVIEW\*.md files)
**Date:** 2026-04-29
**Scope:** the Beta tab — `public/beta.js`, `server-beta-rooms.js`, `public/beta-mp.js`, `public/index.html`, `public/beta-table-preview.html`, plus `DESIGN.md` and `lugen_design.md`.
**Method:** static read of the implementation against the design doc. No playtest.

---

## TL;DR

The build is dense, ambitious, and surprisingly close to the design vision. The bones are great — Jack curse, affixes-while-held, run-deck identity, fork progression, distinct boss personalities. What it suffers from is **scope drift creeping faster than balance audits**: the catalogs (28 jokers, 30+ shop items, 14 floor modifiers, 18 relics, 22 achievements, 11 characters) have outpaced the rules and UI checks that should keep them honest. Most of the bugs below are silent UI gates that quietly break expensive purchases, and most of the balance issues come from new toys layered on top of older toys without re-checking the multiplications.

The single most damaging bug: **the play button hard-caps at 3 cards selected**, which silently breaks Doubletalk and Hot Potato — two purchasable jokers that promise 4- and 5-card plays.

---

## 1. Bugs (verified against the source)

### 1a. Critical

**B1. The Play button cap silently breaks Doubletalk and Hot Potato.**
The submit handler and the disabled-state check both hardcode a 3-card maximum:

- `public/beta.js:4423` — `playBtn.disabled = selected.size < 1 || selected.size > 3;`
- `public/beta.js:5164` — `if (selected.size < 1 || selected.size > 3) return;`

Meanwhile, `playCards()` at line 2178–2180 correctly raises `maxCards` to 4 (Doubletalk) or 5 (Hot Potato). So the rules engine accepts the play, the UI never lets you submit it. Players who buy Doubletalk (Rare, 250g) or Hot Potato (Rare, 250g) lose value with zero feedback. Fix: replace `3` with the same `maxCards` calculation `playCards()` uses, or just compute it in `renderActionButtons()` and gate on that.

**B2. Shop discount cache is sticky.**
In `_buildShopRow()` (lines 5856–5864):

```js
if (forgeDiscount > 0 && !item._origPrice) {
  item._origPrice = item.price;
  item.price = Math.floor(item.price * (1 - forgeDiscount));
} else if (forgeDiscount === 0 && item._origPrice) {
  item.price = item._origPrice;
  delete item._origPrice;
}
```

The middle case is missing: when `forgeDiscount` is *non-zero but different* than what was cached (e.g., player sells Forge Hand and now only Mogul's 10% applies, or Rich Folk modifier expires), neither branch fires and the price stays stuck at the previously-cached discount. A player who bought Forge Hand for one floor will keep getting 25% off shop services even after selling it. Fix: compute the correct price every render — `item.price = Math.floor(item._origPrice * (1 - forgeDiscount))` whenever the cache exists, and only delete the cache when `forgeDiscount === 0`.

**B3. Server (PvP) and client (solo) character rosters don't match.**
Solo (`public/beta.js:42`) defines `rookie, sharp, hoarder, banker, bait, gambler, magician, engineer, witch, whisper, randomExe`. PvP (`server-beta-rooms.js:52`) defines `ace, trickster, hoarder, banker, bait, gambler, sharp, whisper, randomExe`. There is **no character that appears in both** that has the same starting joker — Hoarder's starter is `safetyNet` in solo and `slowHand` in MP; Banker's is `taxman` in solo and `surveyor` in MP. A player who learns the Banker in solo will get a fundamentally different character in PvP. Either consolidate or label as different identities.

### 1b. Logic / state

**B4. Vengeful Spirit doesn't match its own description.**
`JOKER_CATALOG.vengefulSpirit` says "the next active player starts the next round with 2 forced Jacks." Comments at line 2982 acknowledge it's a substitute for the design doc's "loses a Heart." That's fine, except: when a *bot* gets 2 forced Jacks, it almost always survives — it just loses tempo. Compared to other Legendary jokers (Black Hole, Cold Read, Patron) this is much weaker than its rarity implies. Either restore the design's intent (track bot Hearts in solo) or buff the substitute (3 forced Jacks, or +2 Jacks AND -10g).

**B5. Mirage is silently a 3-use wildcard, but the doc and Mirage Lens flavor text say one-shot.**
Code at lines 2333–2349 stores `mirageUses` per run-deck card and only consumes it after the third trigger. Meanwhile `RELIC_CATALOG.mirageLens` description says "one-time wildcard, removed after play," and the design doc says "Rare," "after it resolves it's removed." At 200g for three "guaranteed truth" plays, Mirage Lens dwarfs every other affix service (30–80g for one-shot effects). Either restore one-shot semantics or update flavor text and increase the price (300g+ for three uses, or 200g for two).

**B6. Joker slots flat-5 from Floor 1.**
`SOLO_JOKER_SLOTS = 5` (line 4084) ignores the design doc's 2 → 3 → 5 act ramp. The comment "per-act ramp removed" doesn't say *why*. The ramp is one of the best progression beats in the doc — every act unlock feels like a power spike. Removing it flattens the curve and lets a Floor 1 lucky shop fill all 5 slots in 30 minutes. Recommend restoring the ramp; if you keep flat-5 there should at least be a slot-cap-as-modifier ("Lean Run: 2 slots all run for +50% gold").

**B7. Dead-code refactor leftover at line 2280.**

```js
const _jacksBefore = countJacks(hand) - cards.filter(c => c.rank === 'J').length + cards.filter(c => c.rank === 'J').length;
```

Subtracts `X` and adds `X`. Variable is never read. Just delete it.

**B8. Eavesdropper hardcoded to seat 0.**
Line 2372: `const EAVESDROPPER_OWNER_SEAT = 0; // human in solo; PvP: the local seat`. The TODO is in the comment, but the constant is a constant. Won't function correctly when ported to PvP without parameterization. Track this with a TODO/test.

**B9. `endRoundIfDone` ends at "≤ 2 active," not at first-finisher.**
The design doc says "First to empty their hand wins the round." The code waits until 2 or fewer players are still active (finished + eliminated count >= NUM_PLAYERS−2). A player who emptied their hand on turn 1 may have to watch 4 more turns of bots play out before the round resolves. Two issues: (a) it's confusing UX, (b) on Last Call modifier (5 plays each, then locked out) this can stretch a "no one finished" round to 20 plays. Recommend: end the round on the first `markFinished()`, give 2nd-place gold from whoever has the smallest remaining hand at that moment.

**B10. Shop discount stack only takes the *max* single discount.**
Line 5852–5855 uses `Math.max(baseDisc, ...)` on every discount source. The comment confirms "Stack: prefer the bigger discount available" — so Forge Hand 25% + Mogul 10% = 25% (not 32.5%). That's an OK design choice, but it should be communicated. New player will buy Mogul expecting "10% off everything stacks with my joker." Mention it in Mogul's flavor text.

**B11. `forgeDiscount` and `richFolk` mutate the SHOP_ITEMS array.**
Once the price has been mutated (line 5860), the original price is stored on `item._origPrice`. If the player navigates between fork screens or otherwise re-enters the shop without going through the `forgeDiscount === 0` reset path, the global `SHOP_ITEMS` array carries the discount across sessions. This works as long as the reset branch always fires, but combined with B2 it means saved runs may persist with stale `_origPrice` values forever. Move discount math into a derived getter, not a mutation.

**B12. Scapegoat keeps only the first played Jack from going to the player.**
Line 2922 hardcodes `playedJackIds[0]` — only the first of the played Jacks is routed. The design doc says "ONE Jack is forced into the challenger's hand" (singular), so this matches the doc. The flavor text is fine, but the joker description in the catalog (line 477) should say "**only one** Jack" explicitly so players don't think it dumps all of them.

**B13. Magpie counts the post-Glass-burn pile, not pre-burn.**
In `callLiar()`, Glass burn fires *before* `doSpikedDraws()`, which is where Magpie counts (`pileSnapshot = state.pile.slice()` at line 2719). So if a Magpie player picks up a pile that contained 3 Gilded + 3 Glass cards, Glass burns 3 + 6 random cards before pickup, and Magpie's gold-per-affixed counter sees only the surviving cards. This is probably not intended (Magpie's identity is "every affixed card you take pays you"). Fix: snapshot the pile *before* the Glass-burn block and pass it into `doSpikedDraws()`.

**B14. The Witch's "uncapped Glass" implementation may erase non-Witch Glass burns from the cap.**
Line 2705–2707: when the player is the Witch character, `state.burnedCards = []` after every Glass trigger. This means a *bot's* Glass card play also doesn't accrue toward the cap. That's probably fine in solo (only the human has affixed cards, mostly), but if floor modifiers or per-floor random affixes put a Glass card into a bot's hand, the Witch unintentionally also "absorbs" those burns. Document or scope to player-played Glass only.

**B15. `nextActivePlayer` returns -1 when only the played-from seat is active.**
Line 2139: if the only active player is `fromIdx`, the function returns -1, and `openChallengeWindow` calls `handlePassNoChallenge` immediately. This works, but `handlePassNoChallenge` then calls `_continueAfterPass` which calls `advanceTurn(fromIdx)` — which loops the same single-active player back to themselves. Could lead to an infinite loop if a 1-active state is reached without `endRoundIfDone()` firing. I don't think it's reachable today (because endRoundIfDone catches active≤2), but it's a fragile invariant. Add an assertion.

**B16. Inverted floor modifier targets Jacks but doesn't respect it everywhere.**
On Inverted floors, target = J. Bots' `botTurn` plays still use `matching = hand.filter(c => c.rank === target)` which works. But in `botDecideChallenge`, the predictor (`predictHumanBluffProb`) was trained on plays with non-J targets, so its prior is meaningless on Inverted floors. The Lugen / Prophet brain will misjudge bluff probability. Either skip predictor on Inverted, or train a separate Inverted prior.

**B17. Cracked Mirror snapshot only persists across one challenge.**
Line 2204–2210 snapshots state on every play. If the player plays, the challenge resolves, then the player plays again, the snapshot is overwritten — they can't rewind further than one play back. The button is gated by `state._mirrorSnapshot` so it disappears when overwritten. But a player who buys Cracked Mirror expecting "rewind any of my plays this floor" won't realize it's actually "rewind the most recent play, single-shot per floor." Update flavor text.

**B18. Foggy modifier hides the target rank in `betaTarget` but the rest of the UI still references it.**
Line 4419–4435 (renderActionButtons), the Counterfeit picker iterates `for (const r of RANKS) if (r === state.targetRank) continue;` — even if the player is on a Foggy floor and shouldn't see the target, the Counterfeit button still skips the *correct* rank. A motivated player could stockpile Counterfeit and reverse-engineer Foggy's hidden target by checking which rank is missing from the picker.

### 1c. Cosmetic / polish

**B19.** Doubletalk button doesn't auto-disarm on round end if the round ends before it's used. Players who arm Doubletalk and then get Jack-cursed will see it stuck "ON" in the next round's UI until they click cancel. Reset `state.doubletalkArmed` on `startRound`.

**B20.** Tattletale 4-second peek is fixed regardless of opponent count or hand size. Reading 7+ cards in 4 seconds is rough. Either scale by hand size, or add a "click to extend +2s" mechanic.

**B21.** The opponent tile shows "🃏 Lugen" emoji, but Lugen's design specifies "👑 / 😈" (DESIGN.md iconography). Trivial.

**B22.** `runState.character.startingGold = 0` for Banker (line 75). Design doc says 150g. The current "Gilded Ace mines from turn 1" is a fine compensation but the doc clearly intended 150g + Gilded Ace.

---

## 2. Balance

### 2a. Gold compounds too cleanly

The gold pipeline in `addGold()` (line 4224–4234) multiplies through:

- Gambler character: ×1.5
- Ledger relic: ×1.25
- Greedy modifier: ×2
- Rich Folk modifier: ×0.5
- Dragon Scale: ×(1 + 0.10 × Steel-cards-in-hand)

A Gambler with Ledger and Dragon Scale + 5 Steel hand on a Greedy floor: **1.5 × 1.25 × 2 × 1.5 = ×5.625**. Combined with Cracked Coin (5g × Hearts × ~2 round-starts × 9 floors = up to 270g of free baseline), the Banker / Gambler late-act runs become trivially gold-rich, which collapses the shop's decision pressure (you can buy whatever you see). Recommend either capping the multiplier at 3.0 or making at least one tier *additive* rather than multiplicative.

### 2b. Outliers in the joker shop

| Joker | Rarity | Price | Verdict |
|---|---|---|---|
| **Carouser** | Rare | 250 | 1 free use / floor of Smoke / Counterfeit / Jack-be-Nimble = ~150g of value/floor across the run. Underpriced. → 350g |
| **Last Word** | Uncommon | 150 | Once-per-floor full veto of a caught lie. Effectively a free "I can't play, no penalty" button when your hand is dead. Should be Rare 250–300g. |
| **Black Hole** | Legendary | 400 | Lets you delete a non-Jack on every successful Jack bluff. With Hometown Hero + lots of Jack pressure, this thins your run deck for free. Very strong but priced fairly. |
| **Sixth Sense** (×3 stack) | Uncommon | 150 | Stacking three for 450g gives 45% per opponent play. That's almost half of opponent plays revealed, with no per-floor cap. Strongly underpriced — should scale (150 / 250 / 400 by stack). |
| **Patron** | Legendary | 400 | +1g per Gilded card per turn. Scales with build but is honest. Fine. |
| **The Screamer** | Legendary | 400 | "Public reveal of a chosen rank for the rest of the round" is a faction-defining ability. Even at 400g it's underpriced if you can buy it on Floor 1 with all 5 slots open (re B6). Should be unlock-gated, not just rarity-gated. |
| **Forge Hand** | Common | 80 | Pays for itself on a single Steel/Mirage purchase. Fair. |

### 2c. Outliers in the consumable shop

| Item | Price | Verdict |
|---|---|---|
| **Lie Detector** | 60g | Free truth/lie peek before deciding to challenge — that's strictly better than the 250g Tattletale rare-tier joker (which only fires once per floor). With a 5-slot inventory cap (or 5 with Brass Ring), you can stockpile permanent Lie Detector on every challenge for ~300g. Recommend: 100g, **floor-locked**, OR cap at 1 use/round. |
| **Empty Threat** | 40g, floor-locked | Fair. The "next bot bluffs cautiously once" effect is weak in isolation; floor-lock keeps it honest. |
| **Mirror Shard** | 45g | Blinds the next reveal against you. Cheap insurance. Fine. |
| **Devil's Bargain** | 55g | Cycles a hand card for a top-of-pile draw + Cursed. At 55g this is much weaker than Stripper (60g) or Engraver (80g). Either drop to 35g or guarantee the draw is non-Jack. |
| **Mirage Lens** | 200g | See B5. Re-spec Mirage to 1-use and 200g feels right. If you keep 3-use, raise to 350g. |
| **Forger** | 100g, floor-locked | Identity-defining for stacked builds. Fair. |
| **Crooked Die** | 50g, floor-locked | Solid tempo tool. Fair. |
| **Distillation** | 60g | Merge two same-rank cards. With Engraver (80g) you can stockpile rank diversity and then collapse it. Probably fine but track for stacked-rank builds. |

### 2d. Boss / personality balance

- **The Auditor's** N-roll 1–5 (`runState.auditorEveryN`) means *some* runs face an N=1 Auditor (challenges every play), which is unwinnable as a bluff player. Consider clamping N to [2, 5].
- **The Cheater** lies on 100% of plays. Without a scouting joker (Tattletale, Eavesdropper, Sixth Sense, Lie Detector) you can correctly call LIAR every time and win cleanly. With them you win even faster. The "1-in-4 lies has a tell" rule barely matters because you're already at 100% caught lies. Suggest: Cheater lies on 70% and the remaining 30% are honest plays that still reveal a tell — that creates the real "is this the 1-in-4 tell or the 30% truth?" mind game.
- **Lugen** at Floor 9 with Jack-limit 6 + 7-card start is appropriately hard. The predictor brain (Prophet/Lugen) is genuinely interesting. One concern: the brain reads `humanProfile` but only updates from human plays — bots' bluff data isn't used. So a player who *only ever bluffs* trains Lugen to expect bluffs and ramps challenge rate. Counterplay is to mix; that's the design intent. Fine.
- **Floor modifiers**: Greedy (Jack limit 3) is brutal in combination with Vengeful Spirit + 2 forced Jacks at start — could put you at 2/3 Jack-cursed before your first turn. Add a guard in `applyJackFairness` so post-Vengeful preload still respects the fairness rule.
- **The Wildcard personality** is meant to teach "humility — not every tell is real." But the implementation rolls `Math.random() < 0.5` for tells regardless of bluff state, which means the player gets a tell ~50% of the time on truths and ~50% on bluffs. Net: tell-firing carries zero signal. Either remove the tell display entirely on Wildcard (so the player learns "no animation = nothing learned") or make tells *correlate inversely* (tells fire on truths, no-tell on lies) so observant players are misled rather than uninformed.

### 2e. Run-deck cap math

- `RUN_DECK_PER_RANK = 2`, total 8 cards per design.
- `Stripper` (60g) removes one card. `Engraver` (80g) adds one.
- `Stacked Deck` relic raises run-deck cap to 32. With Engraver at 80g/card, building from 8 → 32 costs 1,920g over a 9-floor run — feasible with Banker/Gambler economy.
- Combined with Forger (clone any card), you can build all-Gilded all-A run decks and the round-deck cap (`ROUND_DECK_RANK_CAP = 16`) silently truncates your investment. Players will buy a 5th Gilded A only to find it doesn't appear in any round. Add a UI warning in the Engraver picker: "Your run deck has X cards of rank Y; only 16 of any rank can be in a round."

---

## 3. New features (ranked by ROI for the design's identity)

### 3a. The "Lügen does this best" tier

These deepen what already makes the game feel like Lügen:

1. **Card-counting overlay (already stubbed in server's `cardCounter` joker, 90g, Uncommon).** Wire it into solo. Show live count of seen-vs-unseen for the current target. This is *the* late-Act tool the design pillar of "math is the only edge in Act III" demands. Without it the late game is mostly luck for non-omniscient players.

2. **End-of-round recap modal.** After every round, a 5-second auto-dismissable card showing: Gilded gold earned, cards burned, jokers fired, biggest pile taken, longest bluff streak. Already partially logged in `state` — just needs the UI. This is the single highest-value UX add for run pacing.

3. **Bluff history micro-graph.** A small line chart in the run sidebar showing your bluff rate per round. Helps players *see* their pattern (which Lugen is reading) and lets them correct it. Nothing else gives this self-awareness.

4. **Run-replay on death / win.** Reuse the run seed (`_generateRunSeed`) to let players export a watch-only replay of their run. Already half-done since the seed exists; needs a serialized log of plays.

5. **Daily seed challenge.** Same seed for everyone, leaderboard for cleared floor + remaining hearts + run time. The seed system is already deterministic per floor; just expose a "today's seed" button and a server endpoint for scoring.

### 3b. Content gaps (low effort, high variety)

6. **Treasure-pool relics** for builds that don't currently have a synergy:
   - **The Misprint** (Common joker, 80g): Once per round, swap two cards in your run deck for the round only.
   - **Bookkeeper** (Uncommon, 150g): All Liar calls (yours and opponents') are added to the Memorizer panel.
   - **The Mole** (Rare, 250g): When an opponent calls LIAR on you and is wrong, you see one random card from their hand.
   - **The Janitor** (Legendary, 400g): At end of round, the burn pile becomes 1 random card in your hand instead of returning to the draw pile.

7. **More floor modifiers** to round out the 14:
   - **Twin Stars**: Two target ranks active simultaneously. Either matches truth.
   - **Auction House**: After each round end, players bid gold to influence the next round's target rank.
   - **House Cut**: All gold gains rounded down to nearest 10g; remainder goes to whoever wins the next round.
   - **Memo**: All claims are silently logged in a panel everyone can see.
   - **Hot Streak**: 3 consecutive uncaught bluffs = +50g; 3 consecutive failed calls = -50g.

8. **Boss-specific consumables**:
   - **Auditor's Pen** (75g, Floor 3 only): predicts the Auditor's next call play index.
   - **Cheater's Tell** (75g, Floor 6 only): the "1-in-4 lie tell" becomes 1-in-2 for one round.
   - **Lugen's Cipher** (150g, Floor 9 only): freezes the Prophet brain's bluff prediction for one round (it can't update from your plays).

### 3c. UX & pacing fixes

9. **First-run guided tutorial.** The current intro panel lists 5 bullets. With 28 jokers, 14 modifiers, 18 relics, etc., new players are lost. A 3-round guided round 1 (target rank, bluff, Jack curse) would massively reduce churn. The design's own pillars are easy to teach if walked through.

10. **Inventory category caps** instead of one global 3/5 cap. Today, buying Counterfeit eats your Smoke Bomb slot. Split into "Tactical (3)", "Service (1)", "Relic-as-consumable (1)" — players can build a real kit.

11. **Run goal HUD.** A persistent strip showing: floors cleared, hearts, gold, current run's "highest pile taken" / "longest bluff streak" / etc. — drives the achievement chase visibly.

12. **Joker reorder via drag-drop.** Now that there are 5 slots, ordering matters for some players' mental model. Trivial DOM work.

13. **Achievement progress chips in run.** When mid-run progress would unlock something, show a tiny chip near the run sidebar. The catalog has 22 achievements but no in-game progress tracker.

14. **Tells legend.** The bot personality tile shows "Greedy" / "Eager" etc. but new players don't know what those tells mean. Hover-reveal of the personality's exact tell-text would be invaluable in Act I (it's literally what Act I exists to teach).

15. **Spectator / replay seat in PvP.** The server (`server-beta-rooms.js`) tracks public state via `publicBetaState` already; add a spectator role.

### 3d. Long-tail / experimental

16. **Negative-rarity relics** ("Cursed treasures"): big upside + an offsetting constant penalty. E.g., **The Coin Eater**: All gold rewards ×2, but you start each floor with -25g. Lets players self-impose challenge runs.

17. **Synergy badge system.** When you equip 2+ jokers from the same theme (Information / Aggression / Tempo / Jack-management / Economy), display a small "★ Synergy ×2" badge. The design doc mentions this in `components.pill-status.synergy` but I can't find it wired in.

18. **Persistent "soft achievements"** for non-binary stats (cards burned across all runs, total gold earned, etc.). These already exist in `_achGetProgress` — surface them.

19. **End-of-run "you would have unlocked" hint.** When a run ends and you're 1 floor away from unlocking a character, show that prominently. Drives next-run motivation.

20. **Mute / SFX volume slider.** Sound files exist (`click.mp3`, `gunshot.mp3`) but I see no in-game UI control for volume.

---

## 4. Code-quality observations

- `beta.js` is **8,138 lines in a single file**. Splitting into modules — `engine.js`, `jokers.js`, `consumables.js`, `ui-render.js`, `bots.js`, `state.js` — would massively improve maintainability without touching behavior. The IIFE wrapper makes it harder than necessary.
- Many "// Phase N" comments. Some Phases reference work that's clearly already shipped. Worth a cleanup pass to remove stale phase tags.
- `runState` and `state` are both global-ish module-scoped lets. There are several places where `state` is mutated under `if (state)` guards — turning these into a single mutable object on `runState` (or a proper state machine) would catch a class of "round ended but a setTimeout fires" bugs.
- The shop discount mutation (B2 / B11) is the most concerning architectural smell. Mutating catalog data is a nasty source of bugs.
- Achievement progress stores in localStorage as raw JSON. If a user opens DevTools they can grant themselves anything. For competitive PvP cosmetics, server-side validation matters.
- Bot decision paths use `Math.random()` directly — non-seeded. Daily seeds + replay (idea #5) require seeded RNG per bot decision, not just per round.

---

## 5. Specific suggestions to act on first

If you can only do one batch this week:

1. **Fix B1 (play-button cap).** Doubletalk and Hot Potato are silently broken right now.
2. **Fix B2 (shop discount cache).** Players will complain about prices not updating.
3. **Reconcile B5 / B6 (Mirage uses, joker slot ramp).** Either update flavor text and the doc, or restore design intent — do not leave them silently divergent.
4. **Wire `cardCounter` joker (idea #1).** Single biggest win for late-Act gameplay feel.
5. **Add the end-of-round recap modal (idea #2).** Single biggest win for run pacing feel.
6. **Audit the gold-multiplier stack (§2a).** Decide once whether to additive-cap or accept the late-run blowup. Either is OK; the *both* of "infinite scaling AND tight item economy" is incoherent.

If you can do a second batch:

7. Fix the Witch's Glass cap interaction (B14) and the Magpie / Glass timing (B13).
8. Replace Wildcard's noise-tell with an inverse-correlation tell (§2d).
9. Pick five new jokers from §3b list and ship them with a "Floor 9 alt boss unlock" gate.
10. Add the seeded-replay system (idea #4) — it's the foundation for daily challenges *and* PvP spectating.

---

## 6. What the build does *really* well

- **The affix language is tight.** 8 affixes, distinct ring colors, distinct triggers (held vs. play vs. reveal vs. pickup). This is the strongest piece of design in the project.
- **The personality tells in Act I genuinely teach.** Greedy / Coward / Eager / Methodical / Mimic each demonstrate a different observation skill, and the tells are loud enough to learn.
- **The run-deck-as-ownership signal** (colored borders, "you have my Gilded A in your hand") is a quietly brilliant solution to the "how does build identity surface mid-round" problem.
- **The Lugen / Prophet predictor brain.** Real bot adaptation that's not just "harder math." That's the kind of system that creates emergent stories in roguelikes.
- **The fork node system + treasure node** has good rhythm. Cleanse on 2/5/8 is a lovely structural beat.
- **The visual design system** (DESIGN.md) is genuinely outstanding. The "yellow is the only verb" rule is a great example of using constraint to communicate.

The bones are all here. The surface needs a balance audit and a few UI fixes. Ship the joker-slot fix, the play-button fix, and the recap modal, and the build is closer to "polish phase" than "feature phase."

---

*— Independent review, no consultation of prior REVIEW files. Findings cite specific line numbers in `public/beta.js` and `server-beta-rooms.js` so each can be verified quickly.*
