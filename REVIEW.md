# Lügen — Beta Review

A walkthrough of the current beta build of the Lügen roguelike, with attention to: bugs, design-doc vs. implementation gaps, UX rough edges, and concrete content suggestions (jokers, consumables, relics, achievements, modifiers, events). Reviewed against `lugen_design.md`, `public/beta.js`, `public/beta-mp.js`, `public/index.html`, and `server-beta-rooms.js`.

I tried to focus on things that matter for "feels right when you play" rather than nits. Where I'm calling something a bug I've put a file/line reference so you can jump straight to it.

---

## What's been fixed in this pass

The following items from the original review have been addressed in `public/beta.js` and `public/index.html`:

- **[FIXED] Personality tells fire only on bluffs** (Wildcard fires both ways, Mimic on copy moments). No more random tell-noise on truthful plays.
- **[FIXED] Mimic actually mimics** — bluffs when the human bluffed, plays safe when they were truthful.
- **[FIXED] Methodical is math-aware** — only bluffs when Jacks are low and matching count is high.
- **[FIXED] Wildcard is now genuinely random** — bluff rate is re-rolled every play.
- **[FIXED] Auditor cadence** — picks N ∈ {1..5} per floor, challenges every Nth chance.
- **[FIXED] Lugen specials** — 7-card start, Jack limit 6, every play randomly affixed, out-of-turn Liar once per round (probability scales with claimed count).
- **[FIXED] Boss-relic pickups wired** — Floor 3 and 6 wins open a 2-relic choice modal from `BOSS_RELIC_POOL`. Falls back to other unowned relics if the pool is exhausted.
- **[FIXED] Cleanse fork node** — replaces Event on floors 2, 5, 8. Lets you remove a Cursed run-deck card or strip an affix.
- **[FIXED] Reward node now offers "1 of 2 jokers"** — third option alongside 75g and Gilded upgrade. Rarity-weighted, free.
- **[FIXED] Hand Mirror `>= 2` guard** — doesn't peek at someone's last card.
- **[FIXED] Black Hole picker** — modal lets you choose which non-Jack to delete (or skip).
- **[FIXED] Vengeful Spirit reworked** — instead of round-eliminating the next active player, it preloads 2 forced Jacks into their hand at the start of the next round (the solo equivalent of "lose a Heart").
- **[FIXED] Forger and Jack-be-Nimble are floor-locked** — one purchase per floor, button greys to "Floor-locked" after use.
- **[FIXED] Steel Jacks double-count** — `jackCurseWeight()` gives Steel Jacks weight 2 toward the curse.
- **[FIXED] Scapegoat routes only one Jack** — not all of them.
- **[FIXED] Foggy obscures last-play claim** — when the target rank is hidden, the claim text is `?` too.
- **[FIXED] Private-peek toast** — Cold Read, Hand Mirror, Bait, Echo, Eavesdropper now show in a private toast (bottom-right, click to dismiss). Same code path will work in PvP without leaking to all players.
- **[FIXED] Resume modal** — the missing `resumeBetaRun()` function is now defined.
- **[FIXED] PvP UX hint** — outdated "MVP not wired" copy is gone.
- **[NEW] Floor 9 alt bosses** — `The Mirror` (plays whatever you played last turn) and `The Hollow` (hand size hidden from you) join Lugen at Floor 9. Mirror/Hollow appear randomly once you've won the run at least once; first-win is always Lugen.

---

## TL;DR (still-open items)

The bones are in great shape. Most of the design doc is implemented and the run loop holds together. The remaining notable gaps are:

1. Round-deck math diverges from the design's 30 + N×8 formula — about 38 cards in play vs. the design's 62 at 4 players. See section 1.16.
2. Run-deck size is 12 vs. the design's 8. See section 1.17.
3. Tariff modifier still only taxes the human (bots don't shop, so this is more of a polish issue).
4. The full UX section (2) and content sections (3) are still on the wishlist.

Detail and suggestions below.

---

## 1. Bugs and design/implementation gaps

### 1.1 Personality tells fire on truthful plays — [FIXED]

Was firing purely on a probability roll regardless of bluff. Now keyed to actual bluff status (`isBluff`) for personalities that should signal intent, with Wildcard firing on both truths and lies (its inconsistency is the lesson) and Mimic firing only when it's mimicking the human.

### 1.2 Methodical / Mimic / Wildcard are stubs — [FIXED]

- **Methodical:** branches on Jack count and matching count. Bluffs at 0.65 only when Jacks are low and matching ≥ 2; otherwise 0.10.
- **Mimic:** reads `state.lastHumanPlay.wasBluff` and mirrors. Falls back to 0.40 before the human's first play.
- **Wildcard:** `bluffRate = Math.random()` every play.

### 1.3 The Auditor cadence — [FIXED]

`runState.auditorEveryN` is rolled in [1..5] when the Auditor is at the table (always Floor 3, plus any future occurrence). `state.auditorChances` increments every time the Auditor is the natural challenger; they fire when `auditorChances % N === 0`. The cadence locks for the round.

### 1.4 Lugen — [FIXED]

- 7-card start: `deal()` pops 2 extra for the Lugen seat.
- Jack limit 6: `jackLimitFor(p)` returns 6 if `botPersonalities[p] === 'lugen'`.
- Random-affix plays: in `botTurn`, after card selection, every Lugen card has its affix overwritten to a random one of the 8.
- Out-of-turn Liar: in `openChallengeWindow`, before assigning the natural challenger, Lugen gets a probability roll (18% / 30% / 45% based on claim count). On success, `state.lugenLiarUsedThisRound` is set and `callLiar(lugenIdx)` runs immediately.

### 1.5 Boss-relic pickups — [FIXED]

In `endFloor`, after a boss-floor win, `showBossRelicPicker(bossId, ...)` opens a modal with two relic options from `BOSS_RELIC_POOL[bossId]`. Falls back to other unowned relics if the pool is exhausted; gracefully reports "you already own every relic" if everything is taken.

### 1.6 Cleanse node — [FIXED]

`isCleanseFloor()` returns true for floors 2, 5, 8. On those floors `showFork` swaps the Event button for a Cleanse button with a cyan tint and rewires the click handler. The Cleanse panel is built dynamically (`ensureCleansePanel`) and lets the player either remove a Cursed run-deck card or strip the affix off any affixed run-deck card.

### 1.7 Reward "2 jokers" — [FIXED]

`ensureRewardJokerButton()` injects a third Reward option. `startRewardJokerPick()` rolls 2 unowned jokers, rarity-weighted, into a sub-picker. The button greys out and relabels when both joker slots are full.

### 1.8 Hand Mirror `>= 2` guard — [FIXED]

`if (state.hands[i].length >= 2)` — opponents at 1 card are spared.

### 1.9 Black Hole picker — [FIXED]

`openBlackHolePicker(onClose)` builds an inline modal that lists every non-Jack hand card. Player picks one (deleted) or "Skip". `_continueAfterPass(lastPlayerIdx)` resumes turn flow after the choice.

### 1.10 Vengeful Spirit — [FIXED]

Rewritten. Instead of round-eliminating the targeted bot, the joker pushes that bot's index into `runState.vengefulNextRoundTargets`. At the start of the next round, the dealer swaps up to 2 non-Jacks out of their hand for Jacks from the draw pile (capped at `limit - 1` to avoid an instant Jack-curse). The joker description was updated to match.

### 1.11 Forger and Jack-be-Nimble floor-lock — [FIXED]

`runState.floorLockedBoughtThisFloor` map tracks per-floor purchases. Reset in `endFloor`. The shop row's button greys to "Floor-locked" once used.

### 1.12 Steel Jacks double-count — [FIXED]

New `jackCurseWeight(hand)` helper: each Jack contributes 1, Steel Jacks contribute 2. `applyJackFairness` and `checkJackCurse` use the weighted count. Eviction logic in fairness prefers Steel Jacks first.

### 1.13 Scapegoat single-Jack — [FIXED]

Now routes only `playedJackIds[0]` and the rest stays in the pile.

### 1.14 Eavesdropper trigger is brittle

Still hardcoded for the human-at-seat-0 case. Comment added to the code; full fix deferred since solo always has the human at seat 0.

### 1.15 Tariff modifier only taxes the human

Unchanged. Bots don't shop, so it's narratively fine. Could rename to "Stamp Duty" if you want the asymmetry to feel intentional.

### 1.16 Round deck math

Still diverges from the design (30 + N×8 = 62 at 4 players; impl produces ~38). Larger fix — would touch deck building. Suggested options unchanged from original review:

- Raise per-rank cap to 16, OR
- Add 24 base non-Jack cards on top of player run decks, OR
- Move run-deck size back to the design's 8.

### 1.17 Run deck size

Implementation uses 12 (3 per rank); design says 8 (2 per rank). Working in playtest but worth noting.

### 1.18 ~~Run-deck colored-border ownership not visible for opponents~~

Removed at user request — affixes carry more important information than ownership colors.

### 1.19 Foggy reveal text — [FIXED]

`betaLastPlay` now shows `claims N × ?` while Foggy is hiding the target rank.

### 1.20 Private-peek UI — [FIXED]

`privatePeek(msg)` renders a bottom-right cyan toast (and still appends to the round log for posterity). Cold Read, Hand Mirror, Bait, Echo, Eavesdropper all routed through it.

### 1.21 PvP UX hint — [FIXED]

Updated to "Jokers, consumables, shop and forks are wired up — bring your favorite build."

### 1.22 Tells in the log

Still shared with other log lines. Future polish — consider a per-opponent tell flash.

### 1.23 Resume modal — [FIXED]

`resumeBetaRun()` is now defined. If the round is live (`state` exists) it shows `betaGame` and re-renders, kicking the bot loop if needed. Otherwise it calls `startRound()`.

### 1.24 Counterfeit price discrepancy

Unchanged. Still 35g / once-per-round vs. the design's 50g / once-per-floor. Pick canonical.

### 1.25 Gambler gold scaling

Unchanged. Stacks correctly (Gambler ×1.5 × Ledger ×1.25 × Greedy ×2 = ×3.75).

---

## 2. UX and quality-of-life suggestions

(Unchanged — see the original list. Highlights still pending: pinned affix legend, end-of-run summary, run-seed sharing, color-blind mode, sound cues on truth/lie reveals.)

---

## 3. New content suggestions

(Unchanged. Pick what you like — full lists of jokers, consumables, relics, characters, modifiers, events, achievements are below.)

### 3.1 New jokers

| Name | Rarity | Effect |
|---|---|---|
| **The Magpie** | Common | When an opponent picks up the pile, gain 1g per affixed card in it. |
| **Forge Hand** | Common | Affix-applying shop services (Glass Shard, Spiked Wire, Steel Plating) cost 25% less. |
| **Last Word** | Uncommon | Once per floor: when a Liar call resolves against you, force a re-vote — challenger may withdraw. |
| **The Carouser** | Uncommon | Smoke Bombs, Counterfeits, and Jack-be-Nimble each get one free use per floor. |
| **The Memorizer** | Uncommon | Every revealed card on a Liar call is logged in a side panel for the rest of the round. |
| **Ricochet** | Uncommon | When you take a pile of 3+ Jacks, half (rounded down) bounce to a random opponent. |
| **The Confidant** | Rare | Round end: if you played truth on every turn this round, +25g. |
| **Diviner** | Rare | You see the burn counter increase per Glass played and the recycle threshold; the warning fires 1 turn before recycle. |
| **Hot Potato** | Rare | After picking up 5+ cards, your max play increases to 5 cards for the next turn only. |
| **The Saboteur** | Rare | Once per floor, force a target opponent to draw 1 random card from your hand. |
| **Doppelganger** | Legendary | Once per round, your next play exactly mimics the previous play (same count, same claim). Auto-truth or auto-lie inherited. |
| **Dead Hand** | Legendary | When you take the pile, the first 2 Jacks in it stay in the pile instead of joining your hand. |
| **The Patron** | Legendary | +1g per Gilded card in your hand on every turn. |

### 3.2 New consumables / services

| Item | Cost | Effect |
|---|---|---|
| **Whisper Network** | 30g | Hear how many Jacks each opponent currently holds (private, single read). |
| **Lucky Coin** | 20g | Re-roll the affix on one card in your hand to a random new affix (Steel-immune, Cursed cleared). |
| **Snake Eyes** | 45g | Force the next Target Rank rotation to reroll the SAME rank (one-shot rotation cancel). |
| **Empty Threat** | 40g | Once per floor: open a "fake" Liar window — you don't actually call, but the bot reacts as if you might have. |
| **Distillation** | 60g | Combine 2 same-rank hand cards into 1 with a random affix (Steel/Mirage-immune). |
| **Pickpocket** | 90g | Steal 1 random card from a target opponent's hand. Floor-locked. |
| **Dead Drop** | 70g | Discard 3 random cards from your hand, then draw 3 from the draw pile. |
| **Marked Deck** | 100g | Apply a chosen affix to a random draw-pile card. Floor-locked. |
| **The Joker's Mask** | 75g | One-shot: declare a non-Jack as a Jack for Jack-curse counting (lets you intentionally trip the curse, useful with Vengeful Spirit / Safety Net). |
| **Mirror Shard** | 45g | The next Liar call against you happens blind — challenger doesn't get the reveal animation, only the result. |

### 3.3 New relics

| Relic | Source | Effect |
|---|---|---|
| **The Hourglass** | Treasure | All challenge windows on you are +2 seconds. |
| **Seer's Eye** | Treasure | You see the affix ring (but not rank) on every card in every opponent's hand. |
| **Cracked Mirror** | Treasure | Once per floor, "rewind" your last turn — the play gets undone, your hand restored, but the bots' choices are NOT redone (pure information advantage). |
| **Dragon Scale** | Treasure | Steel cards in your hand grant +1 Jack-limit each. |
| **The Compass** | Boss reward (Cheater alt) | Bot tells become readable in Act III (they'd otherwise be silent). |
| **Tarnished Crown** | Boss reward (Auditor alt) | Win a floor with no Hearts lost = +50g bonus. |
| **Coward's Cloak** | Treasure | Pass actions never trigger Echo / Eavesdropper / Cold Read on your hand. |
| **The Bookmark** | Boss reward (Lugen alt) | At the end of each round, you may save 1 card from your hand into your run deck (replaces a chosen run-deck card). |
| **Steel Spine** | Treasure | Cursed cards block Liar for 1 turn instead of 2. |
| **Stacked Deck** | Treasure | Run deck cap raised from 24 to 32. |

### 3.4 New characters

- **The Magician** — Once per round, transform a hand card to a different rank (lossy: rank changes, affix wiped). Starts with **Sleight of Hand**.
- **The Engineer** — Run deck starts with 1 random affixed card; affix consumables 25% off. Starts with **Forge Hand**.
- **The Mute** — Cannot call Liar but immune to Cursed effects. +1 starting joker slot. Starts with **Surveyor**.
- **The Witch** — Glass burns don't count toward the burn cap. Starts with **Glass Shard** in inventory.

### 3.5 New floor modifiers

- **Inverted** — For one round, **Jacks ARE the target rank** and other ranks are bluffs.
- **Sticky** — Once a card is revealed, it stays face-up in the pile area for the rest of the round.
- **Rapid** — Challenge windows are 2s for everyone.
- **Open Books** — One round per floor: all hands visible. Pure-tactics test.
- **Drought** — No new draws from the draw pile this floor (Spiked / Spiked Trap / Tracer fizzle).
- **Rich Folk** — Gold awards halved, but jokers in shop are 50% off.

### 3.6 New events

- **Mysterious Stranger** — Trade 1 random card from your run deck for a random affixed card from a hidden pool.
- **Wandering Merchant** — A single rare consumable for sale at 60% price.
- **Card Sharp** — Pay 50g to peek at the next floor's modifier.
- **The Old Soldier** — Pay 25g to skip Jack-curse risk for the next round.
- **Lucky Find** — A random affix is added to a random card in your run deck (free, but uncontrolled).
- **Shrine of Hearts** — Pay 100g for a Heart shard.
- **Drunken Brawl** — Lose 30g but gain a free Counterfeit consumable.
- **The Auditor's Apprentice** — Pay 80g to see the next floor's bot personalities before committing to the fork.

### 3.7 New achievements

**Mastery (gameplay-related cosmetics)**
- **The Pacifist** — Win a run without ever calling Liar. *Unlocks:* "Pacifist" card back.
- **Truth Wins** — Survive 10 challenges where you told the truth in a single run. *Unlocks:* gold border tint.
- **Liar's Tongue** — Lie 10 times in a single round and never get caught. *Unlocks:* "Smirk" elimination animation.
- **Boss Slayer** — Beat all three Floor 9 alt bosses. *Unlocks:* "Crown" card back.
- **Untouched** — Beat Lugen without losing a single Heart. *Unlocks:* alt Lugen card art.

**Build identity**
- **Iron Will** — Win a run with at least 4 Steel-affixed cards in your run deck.
- **Glass Cannon** — Burn 100 cards across all runs.
- **Mass Forgery** — Make 4 of your run-deck cards be the same card via Forger.
- **The Pacifier** — Hold a Cursed card for 5 consecutive rounds.
- **Affix Connoisseur** — Have all 8 affixes appear simultaneously in your run deck.

**Economy / fluff**
- **The Wallet** — End a run with 1000+ gold.
- **Spendthrift** — Spend 2000g in a single run.
- **Speed Demon** — Win a floor in under 2 minutes.
- **Heart Surgeon** — Collect 10 Heart shards across all runs.
- **Empty Hand** — Empty your hand on the very first turn of a round.

**Run-defining**
- **Stripped Down** — Win a run with only 4 cards in your run deck.
- **Joker's Wild** — Equip 5 jokers in a single run.
- **Last Stand** — Win a round with 1 card in hand and 1 Heart remaining.
- **The Gambler's Hand** — Win Charlatan's Bet 5 times in a row in a single run.
- **Stoic** — Use no consumables in an entire run.

---

## 4. Multiplayer (PvP) — quick scan

PvP server already supports jokers / consumables / shop / forks. Issues from section 1 (Hand Mirror guard, etc.) likely apply equally to the PvP code path; that's a separate audit. The lobby copy is now correct.

---

## 5. Priority for what's still open

1. Round-deck math (1.16) — sizable fix, biggest pacing impact.
2. Eavesdropper trigger robustness (1.14).
3. Per-opponent tell flash UI (1.22).
4. Sound cues on Liar reveals.
5. Anything from section 3 that excites you.

---

Last thought: the bones are noticeably stronger after this pass. The personalities now read like personalities, the Auditor is a counting puzzle instead of a wall, Lugen actually feels like a final boss, and the run loop has all three fork types. Have fun.
