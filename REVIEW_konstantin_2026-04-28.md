# Lügen — Independent Beta Review

**Reviewer:** Claude (independent — no prior reviews consulted)
**Date:** 2026-04-28
**Scope:** Beta tab — `server-beta-rooms.js`, `public/beta.js` (solo bots), `public/beta-mp.js` (PvP client), and `lugen_design.md` cross-reference.
**Method:** read the design doc front-to-back first, then traced the code paths from deal → play → challenge → resolve → fork → boss → run end. The design doc is treated as the source of truth for intended behavior; deviations are flagged as bugs even when the code works internally.

---

## TL;DR

The beta is structurally impressive — a 9-floor roguelike with shared-run multiplayer, ~14 jokers, ~6 affixes, ~10 shop items, 6 floor modifiers, 3 bosses, 6 + 4 relics, and a fork/event/cleanse/treasure node system. The bones are solid.

But under the hood there are several **dead jokers** (purchasable, do nothing), a **fundamental mechanic divergence** between solo and PvP (Glass triggers on different events), a **seat-1 bias** that makes turn order asymmetric every round, and a handful of **off-by-one validation gaps** that let players exploit shop prices or get stuck on Tattletale charges. None are crashy, but cumulatively they erode the bluff-pressure-economy loop the design pillars are aiming for.

The single most important fix: kill the dead jokers (Doubletalk, Sleight of Hand) before another player buys one and feels cheated.

---

## 1. Critical bugs (fix before next playtest)

### 1.1 Doubletalk joker is dead code
**File:** `server-beta-rooms.js` `handlePlay`, lines 1048–1050.

```js
if (!Array.isArray(cardIds) || cardIds.length === 0 || cardIds.length > 3) {
    return { error: 'Play 1–3 cards.' };
}
```

The play validator hard-rejects >3 cards. `room.doubletalkArmed` is set/reset in lifecycle hooks but **never read** here. A player can buy Doubletalk for 250g in the shop and the joker has zero effect. Same bug in solo when Doubletalk activates outside the `dtArmed` branch.

**Fix:** read `room.doubletalkArmed[playerId]` and bump max to 4 when armed; consume the arm when used.

### 1.2 Sleight of Hand joker is dead code
**File:** `server-beta-rooms.js` — no socket handler exists for it.

`room.sleightUsedRound` is reset in `startRound` but never gated on by any action. There is no `useSleightOfHand` server function. The Ace character starts with this joker, so **the Ace's signature joker does nothing**.

**Fix:** add `useSleightOfHand(room, playerId)` that draws 1 from the top of the draw pile on the player's turn (once per round); add a corresponding socket event and a UI button.

### 1.3 Glass triggers on pickup in PvP, on reveal in solo — design says reveal
**File:** `server-beta-rooms.js` `applyPickupAffixes` (line 524) vs. `public/beta.js` `callLiar` Glass block (line 2598).

Design doc § Cards & affixes: *"Glass | On-reveal | The card and 2 random pile cards are burned…"*

Solo correctly burns on every successful LIAR-reveal. PvP only burns when somebody picks up a pile (i.e. only after a caught lie or a wrong call — not after a passed challenge that never reveals). This **silently halves** Glass's usefulness in PvP and breaks the Glass-build identity. A "Brittle floor" is dramatically less punishing in PvP than in solo.

**Fix:** in `_handleLiarInner`, after the reveal but before pile distribution, run the Glass burn pass over the revealed cards regardless of whether the pile gets picked up. Track burned cards on `room.burnedCards` exactly the same way solo does.

### 1.4 Same player leads every round
**File:** `server-beta-rooms.js` `startRound`, lines 968–971.

```js
room.currentTurnIdx = 0;
for (let i = 0; i < room.players.length; i++) {
  if (!room.players[i].eliminated) { room.currentTurnIdx = i; break; }
}
```

Player at seat 0 always leads round 1, round 2, round 3, every floor. In a 4-player run that's a sustained advantage for one seat across 27+ rounds.

Compounded by **1.5 below** (target rank biased toward leader's deck): the seat-0 player gets to lead with a deck-favorable target nearly every round.

**Fix:** rotate `room.currentTurnIdx` based on previous round's loser, or seed it from the round number, or have the previous round's last-place finisher lead next.

### 1.5 Target rank silently biased toward the round-starter's deck
**File:** `server-beta-rooms.js` `startRound`, lines 982–997.

70% of the time, the target is forced to the starter's most-stacked non-Jack rank. There is no mention of this in the design doc, no UI surfacing, and no opponent counter-play. Combined with **1.4**, the seat-0 player almost always opens with a friendly target.

**Either delete this or document and balance it.** The design doc's intent is *"a Target Rank is rolled randomly from {A, K, Q, 10}"* — the bias contradicts that.

### 1.6 Counterfeit's "lock" never engages
**File:** `server-beta-rooms.js` lines 2152–2156, 791–793.

`room.counterfeitLockedRanks[playerId] = true` is set on use. Reset on `startRound`. **Never read anywhere.** The shop description says *"Change target rank now and lock through next LIAR"*; players paying 35g get the rank change but **no lock**, meaning anyone can immediately Counterfeit it back or another effect can re-roll.

**Fix:** either remove the "locked" promise from the description, or have `_handleLiarInner` clear the flag on the next LIAR resolution and have anything that would change the target check the flag.

### 1.7 Sharp character's Tattletale has 0 charges on Floor 1
**File:** `server-beta-rooms.js` `startRun` vs. `maybeAdvanceFromFork` line 1629.

`applyCharacter` grants the starting Tattletale joker. `tattletaleChargesFloor` is only seeded inside `maybeAdvanceFromFork`, which doesn't fire on Floor 1 (you're already there). So a Sharp player picks the character whose entire identity is the rare Tattletale joker… and finds out on Floor 1 the charge counter is 0.

**Fix:** in `startRun`, after `applyCharacter`, walk every player and seed `room.tattletaleChargesFloor[p.id] = 1` if they have the joker.

### 1.8 Black Market discount missing on the services gold-check
**File:** `server-beta-rooms.js` `shopBuy` line 1843.

```js
if (item.type === 'service') {
  ...
  if ((p.gold || 0) < item.price) return { error: 'Not enough gold.' };  // ← uses ORIGINAL price
  p.gold -= (item._realPrice != null ? item._realPrice : item.price);
```

For Black Market relic owners (`_realPrice = ceil(item.price * 0.75)`), if their gold is between `realPrice` and `item.price`, the gold check rejects them despite affording the discounted service. Other types (joker/relic/consumable) check `realPrice` correctly.

**Fix:** use `realPrice` in the gold check. Same one-liner for all branches.

### 1.9 Spiked Trap doesn't fizzle as designed
**File:** `server-beta-rooms.js` `_handleLiarInner` lines 1313–1318.

Design: *"the challenger draws 3 extra cards from the draw pile … Fizzles if the pile has fewer than 3 left."*

Implementation draws whatever is available (1, 2, or 3) instead of fizzling completely. This means Spiked Trap is **stronger than designed in late-round** when the draw pile is shallow — a single draw still hurts.

**Fix:** if `room.drawPile.length < SPIKED_TRAP_DRAWS`, draw zero and log "Spiked Trap fizzles."

### 1.10 Cleanse `removeCursed` action is unreachable
**File:** `server-beta-rooms.js` `applyCleanse` lines 1782–1789.

The code looks for a Cursed card in the player's **run deck**. But Cursed only ever lands in run decks via Devil's Bargain (which Curses a freshly-drawn hand card, not a run-deck card) or via Gambler's forced Cursed (also a hand card). There is no path that puts Cursed into a run deck, so this branch always errors `'Cursed run-deck card not found.'`

The design intent is *"remove a Cursed card from your hand permanently"* — but that doesn't fit the fork phase, which happens between rounds when hands are empty.

**Fix:** either (a) re-scope Cleanse to operate on hand mid-round (hard refactor), or (b) repurpose `removeCursed` to strip Cursed from any run-deck card that has it (and accept that's currently impossible until you add a Cursed-applying service), or (c) drop the branch and only ship "strip affix."

---

## 2. High-priority bugs

### 2.1 Vengeful Spirit is mis-scoped in PvP
Joker description (`server-beta-rooms.js` line 62): *"If a Jack curse eliminates you, the next active player is also eliminated."* Design doc § Jokers: *"the player to your left also loses a Heart."*

The implementation eliminates the next player **from the round** — much stronger than the designed 1-Heart penalty. In a best-of-3 floor, round-eliminating an opponent costs them ~33% of the floor. In a 2-player late-floor duel, it's a guaranteed round loss → guaranteed Heart for them anyway, but with worse pacing because they sit out an entire round. Either rewrite to deduct a Heart, or rewrite the description and accept the buffed version (and balance the joker price).

### 2.2 Cheater boss force-play bypasses Cursed locks
**File:** `server-beta-rooms.js` line 1004–1019. Direct hand mutation skips the `cursedTurnsLeft` check that `handlePlay` enforces. A player whose hand is all-Cursed-locked at round-start gets a Cursed card auto-played on their behalf, which is fine for them — but it's inconsistent rule enforcement. If you ever add a "Cursed cards revealed when played" effect, this path will leak it.

### 2.3 Run-deck size mismatch (12 vs. 8)
Design: *"You build a personal run deck of 8 cards."* — `RUN_DECK_PER_RANK = 3` produces 12 (3 each of A/K/Q/10). The shop card cap is 24 (also undocumented). Either update the design doc or shrink the starter to 2/rank. The deck-math table in the design (30 + N×8 = 62 at 4 players) is **wrong** in current code — it's 30 + N×12 = 78 at 4 players, with a much fatter draw pile than the design tunes for. Spiked, Tracer, Spiked Trap balance is all calibrated against the 8-card assumption.

### 2.4 Auditor boss is a different mechanic in PvP than in solo
Solo: `botDecideChallenge` literally challenges every Nth play (faithful to design).
PvP: `_handleLiarInner` halves challenge windows (line 1158). These are not the same mechanic, not even close. PvP players who beat solo Auditor will be confused by what they fight on Floor 3.

**Pick one and ship it everywhere.** I'd port solo's "auto-challenge every Nth play" up — it's the more interesting mechanic and the design doc's "predictability is the tell" only works that way.

### 2.5 Fork system delivers ONE option, not three
Design: *"After every non-boss floor, you face a 3-way fork. Shop is always one of the 3 options; the other 2 slots rotate from a pool."*

`enterForkPhase` (line 1582) seeds an RNG and picks **exactly one** option from {shop, reward, event, cleanse, +treasure}. The player can take it or skip. That's a 1-or-pass node, not a fork.

Either rewrite the design doc to match the simpler model, or have `enterForkPhase` build an offer with `hasShop=true` plus 2 random others. The current single-pick + skip is honestly fine UX — but call it a "node" not a "fork", because "fork" implies branching choice.

### 2.6 `endFloor` `survivors` array is dead code
**File:** lines 1474–1500. Built up but never used (the function reads `aliveCount` from `room.players` instead). Harmless, but it suggests the survivor-aware logic was abandoned partway. Worth a refactor pass.

### 2.7 Race window between LIAR resolution and round-end broadcast
`_handleLiarInner` ends with `if (activeCount(room) <= 1) return endRoundIfDone(room);` which calls `endRound`, which sets a `setTimeout(1500)` to start the next round. The state broadcast happens immediately after, so the client sees a 1.5s "round just ended" state before the new round materializes. Players sometimes see stale hand counts or a "you finished" message disappear and reappear. Not fatal, but it's the kind of jank that piles up.

**Fix:** either send an explicit "round ending in 1.5s" event so the UI can show a banner, or shorten the delay to ~600ms.

### 2.8 `removePlayer` mid-challenge can leave dangling indices
**File:** line 2013. If the challenger leaves the room while `challengeOpen=true`, `room.challengerIdx` may now point past `room.players.length` after the splice. The setTimeout fallback in `openChallengeWindow` will `findNextActiveIdx` from a bad index. It works because `findNextActiveIdx` modulos by `room.players.length`, but it's fragile.

**Fix:** in `removePlayer`, if `runStarted` and `challengeOpen`, clamp `room.challengerIdx` and consider firing `handlePassNoChallengeInternal` immediately.

---

## 3. Medium / cosmetic bugs

| # | Where | Issue |
|---|---|---|
| 3.1 | `shopBuy` lines 1823, 1830, 1836, 1846 | Logs `-${item.price}g` instead of `-${realPrice}g` for Black Market buyers. Cosmetic but misleading. |
| 3.2 | Whisper char `applyCharacter`+`startRound` line 942 | `room.players.length * room.players.length` (squared) used to ensure positive modulus. `+ room.players.length` is sufficient. |
| 3.3 | `regenerateShopOffer` line 638 | Always regenerates shop AND card offers in `enterForkPhase` even when the fork pick isn't shop. Wasteful but cheap. |
| 3.4 | `shopBuyCard` line 684 | `newId` shadows the imported `newId` helper from line 193. Subtle but confusing for future readers. |
| 3.5 | `Counterfeit` shop description line 109 | Promises a lock the implementation doesn't deliver (see 1.6). |
| 3.6 | `applyService` engraver | Uses `Date.now()` for IDs — two engravers in the same millisecond on the same player would collide. Use the random ID helper. |
| 3.7 | `floorRng` | Seeded RNG used for fork picks but live `Math.random()` is used for shop card rolls, joker rarity rolls, modifier rolls, and event payouts. Reproducible-seed runs aren't actually reproducible. |
| 3.8 | `useConsumable` `tracer` | The first call sends the peek, the second call applies the perm — but if a player tabs away between calls, `room.pendingPeeks` is drained on broadcast and the second call has no peek to validate against. Works because the perm is sent freshly, but the UX of "I lost my Tracer" can happen on a refresh. |
| 3.9 | `applyFloorAffixesToDrawPile` | Adds N affixes per round (where N = floor #). At Floor 9 that's 9 affixes per round × 3 rounds = 27 affixed cards per round, on top of player-built run-deck affixes. Heavy floor-static late-game; check that this matches your tuning intent. |
| 3.10 | Solo `beta.js` Glass burn | `Math.floor(Math.random() * burnable.length)` — fine, but uses index-replacement instead of array filtering. Multiple Glass cards in the same play burn with biased pile selection (each burn shifts indices). Subtle but probably not visible. |
| 3.11 | `applyJackFairness` | Two-pointer loop with `let newIdx = -1; for (let i = …)` — the second scan re-walks the whole `drawPile`. Fine for 50-card piles; if you ever scale up, vectorize. |
| 3.12 | `startRound` infusion runs every round | The "Floor N adds N affixes" infusion fires per round, not per floor (drawPile is rebuilt each round). The design wording implies per-floor; the code implements per-round. Minor balance tilt. |
| 3.13 | `pickClosestActivePlayer` | If multiple actives have the same min hand size, it picks the first by index — a deterministic tie-break that always favors lower seats. Combine with seat-0 always-leads (1.4) and you've got a third seat-bias. |

---

## 4. Balance & tuning concerns

### 4.1 Magnet is currently a Jack-execution button
A player at Jack-limit-1 + the room having anyone with a Jack in hand + 75g = forced round elimination on a coinflip target. That's enormous value for 75g compared to Smoke Bomb (35g) or Counterfeit (35g). Either:
- raise Magnet's price to ~120g, OR
- add a "the recipient cannot be at Jack-limit-1" rule, OR
- let the magnet-target see what's coming and choose to block it (consume gold or a relic).

### 4.2 The Banker is a snowball trap
150g start + Gilded Ace (+2g/turn while held) + Taxman (+10g per opponent's 5+ pickup). At 4 players × 3 rounds × ~6 turns/round, that's potentially 50–80g/floor passive. Banker also ignores the Glass meta because Gilded is the safest affix. Currently the strongest start. Consider:
- nerf Banker starting gold to 100g, OR
- give Banker a downside (e.g. -1 challenge-window second, or a starting Cursed card).

### 4.3 RANDOM.EXE's 20% card discount + reroll-affix-each-round = strictly upside
The "compensation" for affix volatility is a 20% discount. But the volatility is **also upside** — you get to spin for Steel/Mirage every round on every run-deck card. Design pillar #3 says runs should have identity; RANDOM.EXE has the most identity by getting *all* of them. Either remove the discount or remove the reroll's ability to land Steel/Mirage.

### 4.4 Joker prices vs. shop refresh
With shop refreshing every fork (and forks now offering a single random pick of which shop is one option), a 400g Legendary joker rarely shows up at all — and when it does, you've usually already spent on cheaper things. The design's "Common 80 / Uncommon 150 / Rare 250 / Legendary 400" assumes regular shop access. Since forks now gate the shop, consider:
- guarantee shop on every odd floor (1, 3, 5, 7), OR
- when a non-shop fork is rolled, display a peek of "Shop will be available next floor", OR
- lower Legendary to 350g and Rare to 220g.

### 4.5 Floor 9 Lugen — "pure-Jack plays count as a lie"
The implemented Lugen rule (line 1265–1268) only fires when the played cards are **entirely Jacks**. But mixed plays (e.g. 1 Jack + 1 Ace) fall back to the normal lie check, which treats Jacks as wildcards. That makes Lugen's signature mechanic trivially gameable — never play pure-Jack. Either Lugen counts ANY Jack in a play as a lie-component, or the rule should be removed.

### 4.6 Burn cap is per-round, not per-floor
On a Brittle modifier floor, every round resets the burn pile and a Glass-heavy build can burn 8 cards × 3 rounds = 24 cards across the floor. Iron Stomach's Steel-restoration is also per-round, so cards endlessly cycle Steel→Glass→Steel for a build that's hard to interrupt. Consider letting the burn cap persist across rounds in the same floor.

### 4.7 Heart shard: only awarded at 1 Heart, only to floor winner
A 4-player room where two players make it to the last round at 1 Heart each — only the winner gets a shard. The runner-up *also* survived on 1 Heart but gets nothing. Consider awarding a shard to anyone who survives a floor at exactly 1 Heart, regardless of who won.

---

## 5. New feature ideas (ranked by ROI)

### 5.1 Persistent Wins / Loadout system (high ROI)
The design doc § Cross-mode progression specs an unlock tree (deck unlocks, cosmetics, achievements). The code reports run results to `/api/beta/run-history` and `/api/beta/progression` — but I see no path back from those endpoints to any unlocked content. Players currently grind without seeing the carrot.

Build a profile screen showing: total runs, wins per character, jokers used, achievements (with names and progress bars), and a "next unlock at X wins" hint. This is the single best way to make the game feel like a roguelike instead of a series of disconnected matches.

### 5.2 Spectator mode for eliminated players
When a player is eliminated from a round mid-floor, they go silent — the run continues for 2-3 more rounds and they have nothing to do. Add a spectator overlay: show all players' real hands, the draw pile, and a "predict the next play" mini-game where eliminated players can quietly score points by guessing right. Solves the dead-time UX without affecting balance.

### 5.3 Replay seeds and "challenge a friend" runs
The seed system already exists (`room.seed = _generateRunSeed()`). What's missing:
- a "Replay this seed" button on the run-end screen,
- a copyable seed link `/?seed=XXXX-XXXX` that prefills the lobby,
- a leaderboard for "best floor reached" per seed.

This unlocks community content (streamers compare runs on the same seed, friends race the same run) at minimal dev cost. Note the seed isn't actually deterministic right now (see 3.7) — fix that first.

### 5.4 Joker synergy hints in the shop
When the shop offers a joker that synergizes with one you already own (e.g. you have Tattletale, shop offers Cold Read — both Information jokers), highlight it with a "synergy" badge. The same goes for relics. Helps new players see builds emerge.

### 5.5 Hand-strength heatmap during your turn
Show a small "X% chance the table has a copy of the target rank still in someone's hand" indicator based on what you can derive from hand size + draw pile + burned pile + your hand. This is information players can compute manually but rarely do, and it sharpens decisions without giving away anything hidden.

### 5.6 "Last Stand" — auto-trigger on 1 Heart
When you're at 1 Heart entering a floor, the game gives you one of: a free relic re-roll, a free Smoke Bomb, or +50g. Cheap to implement, makes the "scrappy comeback" feel real. Tweak gold so this never kicks in cumulatively > 100g/run.

### 5.7 Daily challenge run
A specific seed, fixed character, fixed floor 1 modifier — same for every player every day. Leaderboard for max floor + final gold. Gives players a reason to log in tomorrow. Server-side: one cron job that rolls a seed at midnight UTC. Client-side: a tab in the lobby.

### 5.8 Joker upgrade ("ascended") variants
After winning a run with a particular joker equipped at run end, that joker unlocks an "ascended" variant in future runs (slightly buffed, e.g. Surveyor → Surveyor+ which sees the top 2 cards). Makes repeated runs interesting. Aligns with design § Cosmetics § Joker portraits but adds gameplay.

### 5.9 Affix preview on run-deck shop cards
The shop offers run-deck cards with a 50% chance of an affix (line 647). The buyer sees the affix before buying. Consider offering "mystery cards" — pay 70g, get a random rank + random affix, blind. Cheaper than the 100g visible cards. Adds a fun gambling layer to the Banker fantasy.

### 5.10 In-game tutorial via the bot personalities
Solo Floor 1 already has personality tells. Add an opt-in "tutorial mode" where the game pauses on the first tell and explains it — "see how Bot Bob hesitated before that play? That's The Greedy's tell. Long pauses = bluffs." Onboards new players without a separate tutorial flow.

### 5.11 PvP run forfeit / surrender
Currently if you're 0:2 down and clearly losing, you have to grind out a final round you know you'll lose. Add a "forfeit run" button (host-confirmed in PvP) that ends the run with last-place credit. Saves 5+ minutes of unfun play.

### 5.12 Run history "story mode" log
At run end, show a 3-paragraph narrative summary: "On Floor 4 you beat the Coward seat with a triple-Jack bluff. On Floor 6 The Cheater forced your first play into a Glass card — you survived on 1 Heart. Floor 9: you ran out of gold before the boss." Pulled directly from the game log + key events. Players love sharing these.

---

## 6. Architecture / code health (longer-term)

### 6.1 Solo and PvP have diverged in mechanics
Glass timing, Auditor mechanic, joker availability (Magpie/Dead Hand/Ricochet/Hot Potato/Memorizer/Trickster/Sixth Sense exist in solo but not in PvP — confirm by searching `JOKER_CATALOG` and comparing to solo's catalog), modifier names (`lastCall`, `rapid` exist in solo but not in PvP). The design doc treats them as the same game. Either:
- promote the PvP server to authoritative game state and have solo run the same rules locally with bots, OR
- explicitly fork the design doc into "solo rules" and "PvP rules" sections.

The current "drift" is the worst of both worlds: the PvP server is a stale subset of solo, and players can't tell which rules apply.

### 6.2 The design doc is stale
beta.js has at least 8 jokers (Magpie, Dead Hand, Ricochet, Hot Potato, Memorizer, Trickster, Sixth Sense, Caller's Mark), 2 relics (Steel Spine, Hourglass, Cracked Mirror), and 2 modifiers (Last Call, Rapid) that aren't in the design doc. Some are in PvP, some aren't. New players reading the doc will be surprised.

Schedule a doc-update sweep: pull every joker/relic/affix/modifier into a single canonical table, mark each "solo only / PvP only / both," and tag the file version.

### 6.3 No tests
There are no tests in the repo (verified via `Glob: **/*test*.js` returning only node_modules). Even one happy-path harness (start run, deal 4 hands, play 1 turn, call LIAR, assert state) would catch the dead-joker bugs above. Recommend a `tests/` directory with one file per game phase.

### 6.4 `server-beta-rooms.js` is 2362 lines in one file
At this size every change risks merging conflicts. Split into:
- `room.js` (state shape, addPlayer/removePlayer/lobby)
- `dealing.js` (buildRoundDeck, applyJackFairness, applyFloorAffixesToDrawPile)
- `play.js` (handlePlay, handleLiar, handlePass, openChallengeWindow)
- `affix.js` (applyPickupAffixes, markCursedOnEntry, etc.)
- `fork.js` (enterForkPhase, pickFork, rewardPick, applyCleanse)
- `shop.js` (regenerateShopOffer, shopBuy, shopBuyCard, applyService)
- `consumable.js` (useConsumable, useTattletale, useLoadedDie)
- `index.js` (broadcast, publicBetaState, exports)

### 6.5 Magic numbers
`SPIKED_DRAWS_ON_PICKUP = 1`, `GLASS_BURN_RANDOM = 2`, `BURN_CAP = 8`, `GOLD_PER_GILDED_PER_TURN = 2`, `TREASURE_CHANCE_ACT_III = 0.33`, `CURSED_TURN_LOCK = 2`, etc. all live in different parts of the file. Lift all balance numbers into a single `BALANCE` const at the top so a designer can tune without grepping.

### 6.6 Client `prompt()` for Tracer
**File:** `beta-mp.js` line 458. Using `window.prompt()` to ask for a comma-separated permutation is brittle (mobile keyboards struggle, no validation feedback). Replace with a proper drag-to-reorder UI. Tracer is already a 40g consumable — its UX deserves better.

### 6.7 No rate-limiting on socket events
A malicious client can spam `beta:play` at 1000 reqs/sec. The server validates on each one (turn check, etc.), but it's still a DoS vector. Add a per-socket rate limit (e.g. 20 events/sec max) at the socket.io middleware level.

---

## 7. UX papercuts

- The MP shop lacks a "synergy with my current jokers" hint (see 5.4).
- The "Run seed" pill in the fork is `<code>` styled but not click-to-copy.
- Tattletale's countdown is in seconds — the "(4)" charges counter is ambiguous (charges? remaining seconds?).
- The "Empty" joker slot tile says "Empty" but doesn't say "Buy or earn one in shops/rewards." First-run players don't always know.
- The challenge window has a visual bar in solo (`betaChallengeBar`) but **not in PvP** (search `betaMpChallengeBar` — no result). PvP players have to mentally count seconds.
- The "Burn 5/8" counter in PvP doesn't show *what* was burned, just the count. Solo shows the burned cards as faces. Port the visualization.
- Whisper character has a "left/right toggle" passive but I see no UI button to toggle direction in either client. Confirm `setWhisperDirection` is wired to a clickable element.
- The fork's "Skip & continue" button text is the same regardless of the fork option offered. A player who doesn't notice the offered option may skip without realizing they had a choice.

---

## 8. Quick-win priorities (one sprint)

If you can only do five things this week:

1. **Fix Doubletalk and Sleight of Hand** (1.1, 1.2) — players are losing gold on dead jokers right now.
2. **Rotate round leadership** (1.4) and **kill the target-rank bias** (1.5) — fixes seat-0 dominance in one PR.
3. **Port Glass-on-reveal to PvP** (1.3) — single-mechanic divergence is hurting the PvP meta.
4. **Seed Tattletale charges in `startRun`** (1.7) — Sharp character is currently broken on Floor 1.
5. **Fix the Black Market services gold-check** (1.8) — small change, makes the relic actually consistent.

These five are all <50 lines each, all server-side, all unblockable by other refactors.

---

## 9. Things I'm not sure about

- I didn't read the other reviews (per the user's request). If those identified the same items, treat as triangulation; if they identified different ones, treat both lists as a superset.
- I didn't run the game live — every claim above is from static analysis. Some "bugs" might be intentional design that the doc doesn't mention.
- The progression API endpoints (`/api/beta/run-history`, `/api/beta/progression`) live in a file I didn't read (likely `auth.js` or `server.js`) — I assumed they work as described in the client.
- I didn't audit `server.js` (the parent server) — only `server-beta-rooms.js`. There may be additional event-routing bugs in the socket.io wiring layer.

---

## Appendix: file map

| File | Purpose | LOC | Notes |
|---|---|---|---|
| `server-beta-rooms.js` | Authoritative PvP server logic | 2362 | Recommend split (6.4). |
| `public/beta.js` | Solo client + bot AI + UI | 7515 | Has more features than PvP server. |
| `public/beta-mp.js` | PvP client renderer | 1320 | Uses `prompt()` in places (6.6). |
| `public/index.html` | Shell + tabs + DOM | 1200 | Beta tab here. |
| `lugen_design.md` | Design doc | 416 | Stale vs. code (6.2). |
| `auth.js` | Auth + progression endpoints (assumed) | 18378 chars | Not reviewed. |
| `server.js` | Parent app server (assumed) | 67263 chars | Not reviewed. |
