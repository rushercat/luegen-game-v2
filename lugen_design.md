# LUGEN — Design Doc (v0.1)

A solo roguelike built on Liar's Bar bones. One human against AI seats, climbing floors, building a run.

---

## Design pillars

1. **Pressure is constant.** A round of Lugen should feel like the deck is closing in on you. Small deck, fast Jack accumulation, tight tempo.
2. **Affixes matter every round, not on a coinflip.** If a card is special, you should feel it whether or not it gets revealed.
3. **Runs have identity.** Characters, joker synergies, and modifiers should make a Banker run feel different from a Sharp run.
4. **The Jack-curse is the centerpiece.** Every system should orbit "how do I avoid drowning in Jacks while making opponents drown in theirs."

---

## The run

A run is **9 floors across 3 acts**.

- **Act I (floors 1–3):** 4 seats (you + 3 AI). Standard rules. Floor 3 is a mini-boss.
- **Act II (floors 4–6):** 3 seats. Modifiers introduced. Floor 6 is a boss.
- **Act III (floors 7–9):** 2 seats (duel). Boss-only floors. Floor 9 is the final.

### Between-floor nodes

After every non-boss floor, you face a **3-way fork**. **Shop is always one of the 3 options**; the other 2 slots rotate from a pool. After every boss floor, you pick a **relic** and then face a smaller **2-way fork** (Shop or Reward).

Node types:
- **Shop** — always available in non-boss forks. Spend gold on consumables, jokers, or affix-applications.
- **Event** — narrative or mechanical surprise. Free relic gambles, NPC encounters, mini-decisions with gold/card consequences.
- **Reward** — guaranteed pick from 2 jokers, or take 75g instead.
- **Cleanse** — remove a Cursed card from your hand permanently, or strip one affix from a run-deck card.
- **Treasure** (Act III only) — rare bonus node that sometimes replaces Reward; offers a free relic from the treasure pool (The Shroud, Crooked Cards, Black Market, Gambler's Mark).

A typical run path:

```
Floor 1 → Fork (Shop / Reward / Event)
Floor 2 → Fork (Shop / Reward / Cleanse)
Floor 3 BOSS → relic choice → mini-fork (Shop / Reward)
Floor 4 → Fork (Shop / Event / Reward)
Floor 5 → Fork (Shop / Event / Cleanse)
Floor 6 BOSS → relic choice → mini-fork (Shop / Reward)
Floor 7 → Fork (Shop / Treasure / Event)
Floor 8 → Fork (Shop / Cleanse / Event)
Floor 9 FINAL BOSS → end of run
```

You start the run with **3 Hearts**. Losing a floor costs 1 Heart. 0 Hearts = run over. Winning a floor on Heart 1 awards a **Heart shard** (3 shards = +1 Heart).

---

## The round (one floor)

### Setup
- **Round deck: 30 cards.** 6 each of A, K, Q, 10, plus 6 Jacks. Reshuffled and re-dealt each round inside a floor.
- Each player is dealt 5 cards.
- A **Target Rank** is rolled randomly from {A, K, Q, 10}. (Removing "first player picks" because it's exploitable in 2-player duels and gives nothing in 4-player.)
- The undealt cards form the **draw pile** for the round (typically 5–10 cards depending on player count). Effects that say "draw" or "draw extra" pull from the top of this pile.

### Draws and the draw pile
Players don't draw on their turn by default. Cards only come out of the draw pile via specific effects: the **Spiked** affix, the **Spiked Trap** joker, **Tracer** (consumable), or any future card/joker that calls for a draw.

**Fizzle rule:** if a draw effect needs more cards than are left in the pile, take what's there and the rest fizzles. If the pile is empty, the effect does nothing. Cards burned by **Glass** are removed from the round entirely and do not refill the pile — meaning Glass indirectly weakens future Spiked / Spiked Trap triggers, which is fine and intentional.

### Turn
- Play 1–3 cards face down, claim they match the Target.
- The **next player** can call "Liar" within a 5-second window. (Window can be extended or shortened by jokers.)
- Caught lying → liar takes the pile, challenger leads next round.
- Truth → challenger takes the pile, is **skipped**, next-next player leads.

### Win condition for the round
- First to empty their hand wins the round and earns gold based on cards left in opponents' hands.
- A floor is **best of 3 rounds**.

### The Jack curse
- Holding **4 Jacks at end of any turn** = you are eliminated from this floor (auto-lose). You lose 1 Heart.
- This is more forgiving than instant elimination on draw — it gives you a turn to dump a Jack via play, item, or joker.
- **Starting hand fairness rule:** no player can begin a round already at their Jack limit. After the initial deal (and after any forced start-of-round cards like The Gambler's Cursed card), if a player has Jacks equal to their limit, the dealer swaps Jacks down to **(limit − 1)**. Excess Jacks go to the bottom of the draw pile, replaced from the top with non-Jack cards. So at the standard 4-limit, you start with at most 3 Jacks. **Safety Net** (5-limit) caps starts at 4. **Greedy** modifier (3-limit) caps starts at 2. This rule applies only to the deal — once the round is in motion, you can absolutely be pushed to the limit by Spiked, pickups, or affix effects.

### Visibility — what's public, what's hidden

| Element | Public | Hidden |
|---|---|---|
| Hand size (count) of each player | ✓ | |
| Hand contents (ranks) | | ✓ — revealed only via Tattletale / Whisper / Eavesdropper |
| Card affixes in hand | | ✓ — only revealed when the affix triggers (challenge, pickup, play) or via a scouting joker (e.g., Surveyor reveals Gilded) |
| Played pile size | ✓ | |
| Played pile contents | | ✓ — revealed only on a Liar call |
| Draw pile size | ✓ | |
| Draw pile contents | | ✓ — revealed only via Tracer |
| Burned pile (Glass-removed) | ✓ | |
| Joker slots | ✓ | — opponents see what you're packing |
| Gold, Hearts, characters | ✓ | |
| Run deck size | ✓ | |
| Run deck contents | | ✓ — opponents know you have 8 personal cards, not which ones |
| Card ownership (whose run deck it's from) | ✓ — via colored borders on every run-deck card | |

The principle: **counts are public, contents are hidden.** Information only leaks through specific mechanics — affix triggers, scouting jokers, Liar calls. This keeps the bluffing tension intact while giving players enough scaffolding to make informed decisions.

---

## Run deck (your personal cards)

You build a personal **run deck of 8 cards** that travels with you across all 9 floors. At the start of each round, every player's run deck is shuffled into the round deck — your 8 + every opponent's 8 + the base 30-card round deck — and dealt out alongside it.

**Composition.** At run start, your run deck is 8 vanilla cards drawn from a starter pool (e.g., 2 each of A, K, Q, 10 — no Jacks). Affixes are added by spending gold in shops or earning rewards between floors. You can also swap cards out at certain reward nodes.

**Ownership and end-of-round.** Every run-deck card is tagged with its owner. When the round ends, all cards return to where they came from: round-deck cards reshuffle back into the round-deck pool, and run-deck cards return to their owner's run deck. Cards burned by Glass are gone for the rest of *that round* but return at end of round — Glass is a tempo punishment, not a permanent destruction.

**Visual: player colors and card borders.** Each player is assigned a unique color at the start of a run (or match in PvP) — say red, blue, green, gold, purple, teal for up to 6 players. Their run-deck cards have a thin colored **border** that's visible to all players at all times, on both faces of the card. The border shows ownership only — it does **not** reveal rank or affix.

What this gives you:
- At deal time, the **owner** sees their own hand fully (rank + affix). Other players see colored borders on each opponent's cards but not the contents.
- When a card is played face-down, the colored border on its back is visible. You know whose run deck it came from.
- When you pick up a pile, you can see the color distribution of what just landed in your hand before flipping anything.
- Vanilla round-deck cards have **no border** — neutral color, no owner.

This creates a strategic information layer without breaking the bluff. "The Banker is playing a lot of their own cards this round — probably loaded up on Gilded." "That pile has three of the Spiked-build player's cards — be careful picking it up." You get *signal* without *certainty*.

**Deck math at the table.** Round deck per round = 30 + (N × 8), where N is the player count.

| Players | Total deck per round | Hand × N | Draw pile |
|---|---|---|---|
| 2 (duel) | 46 | 10 | 36 |
| 4 (standard) | 62 | 20 | 42 |
| 6 (PvP max) | 78 | 30 | 48 |

Hand size stays 5 across all configurations. Bigger draw piles at higher player counts is intentional — more headroom for Spiked / Spiked Trap / Tracer effects.

**Why this matters.** Run decks give runs a clear identity. A "Glass build" loads up Glass-affixed cards to burn pressure. A "Spike build" stacks Spiked cards to punish pickups. A "Banker" run leans Gilded for gold income. Without run decks the only persistent customization was your 2 jokers — with them, the cards themselves become a build.

**Build limits — none for now.** No cap on duplicate ranks, no cap on affixed cards. You can run all 8 Gilded Aces if the run economy lets you build it. This is intentional for early testing — degenerate builds are *funny* and they teach us where the real balance pressure points are. If a specific build dominates, we cap it then. Likely future caps if needed: max copies per rank, max affixed cards, or a "rank diversity bonus" that softly incentivizes variety without banning it.

---

## Cards & affixes

Affixes are now a mix of **passive (while held)**, **on-play**, and **on-reveal**. Reveal-only triggers are rare because they happen rarely.

| Affix | Trigger | Effect |
|---|---|---|
| **Gilded** | Passive (held) | +3 gold at the start of each round it stays in your hand. Opponents can see this card is Gilded if they hold a Surveyor joker. |
| **Glass** | On-reveal | The card and 2 random pile cards are burned (removed from play for the rest of the round). Burned cards return to their owner at end of round. Subject to the burn cap (see below). |
| **Cursed** | Passive (held) | While held, you cannot call Liar. Picking up a Cursed card forces it into your hand for at least 2 turns. |
| **Steel** | Passive | Immune to all affix and joker effects that would move or destroy it. Counts double toward Jack curse if it's a Steel Jack. |
| **Mirage** | On-play | One-time wildcard: matches the Target. After it resolves, it's removed from the round deck. **Rare.** |
| **Spiked** | On-pickup | Whoever takes a pile containing a Spiked card draws +1 from the round deck immediately. |
| **Hollow** *(new)* | On-play | Counts as 0 cards toward your hand reduction — you played it but your hand size doesn't drop. Useful for stalling, dangerous if challenged. |
| **Echo** *(new)* | On-play | The next card played by the next player is also flipped face-up briefly to that player only (private read). |

Cards in your **run deck** can have **at most 1 affix**. Affixes are added in shops or as floor rewards.

### Burn cap

Each round has a **burn cap of 8 cards**. The burn pile is visible to all players with a live counter (e.g., "3 / 8 burned").

**What happens at the cap:** if a Glass trigger would push the burned total to 9+, the cap kicks in instead of the burn:

1. The Glass trigger still fires — the played Glass card and the 2 random pile cards leave the played pile as normal.
2. **All currently burned cards (including the 3 from this trigger) shuffle back into the draw pile.**
3. The burn counter resets to 0 for the rest of the round.

This means if the burn pile is sitting at 6–7, the next Glass trigger becomes a real decision: burn safely now (advancing toward the cap), or wait and try to time a deliberate overflow that recycles the burn pile back into circulation.

**Why this rule exists:**
- **Prevents deck destruction.** Without it, a Glass-spam build could burn 24+ cards in a round and leave nothing to draw or be dealt next round.
- **Creates strategic timing.** Glass becomes a *managed resource*, not a free trigger. Counter-Glass jokers and modifiers (if added later) can play with this dial.
- **Self-correcting.** The system fixes itself instead of needing a hard rule like "max 4 Glass cards in any run deck."

---

## Jokers

You start the run with **2 joker slots** and gain more as you clear acts:

- **Act I (floors 1–3):** 2 slots.
- **Act II (floors 4–6):** 3 slots (unlocked after beating Floor 3 boss).
- **Act III (floors 7–9):** 5 slots (unlocked after beating Floor 6 boss — jumps from 3 to 5 for the late-run crescendo).

Jokers persist across the run. Empty slots stay empty until you buy or earn a joker to fill them.

I dropped X-Ray Specs (suits don't matter in this design) and replaced/rebalanced a few others.

### Information
| Name | Rarity | Effect |
|---|---|---|
| The Surveyor | Common | See the top card of the round deck at all times. |
| Eavesdropper | Uncommon | Once every 2 rounds, when the player before you plays, see whether their hand contains **0**, **some** (1–2), or **many** (3+) cards matching the Target. Fuzzy read, not exact. |
| Tattletale | Rare | Once per floor, peek at a target player's full hand for 4 seconds. **Private** — only you see it, the target isn't notified. |
| Cold Read | Legendary | At the start of each round, secretly see one random card from each opponent's starting hand. |

### Aggression
| Name | Rarity | Effect |
|---|---|---|
| Spiked Trap | Rare | If you tell the truth and are challenged, the challenger draws 3 extra cards from the draw pile (in addition to picking up the played pile). Fizzles if the pile has fewer than 3 left. |
| The Taxman | Common | When an opponent picks up a pile of 5+ cards, you gain 10 gold. (Replaces the broken "4-of-a-kind" trigger.) |
| Vengeful Spirit | Legendary | If the Jack curse eliminates you, the player to your left also loses a Heart. |

### Jack management
| Name | Rarity | Effect |
|---|---|---|
| The Scapegoat | Uncommon | If you are caught lying with a Jack in the pile, that single Jack is forced into the challenger's hand instead of yours. The rest of the pile still goes to you. |
| Safety Net | Rare | Your Jack limit is 5 instead of 4. |
| Black Hole | Legendary | If you successfully bluff a Jack (no challenge), you may delete one **non-Jack** card from your hand at end of turn. Cannot delete Jacks (the played Jack already left your hand into the pile — this lets you thin a *different* card, not cycle two Jacks at once). |

### Tempo (new category)
| Name | Rarity | Effect |
|---|---|---|
| Slow Hand | Common | Your challenge window is 10 seconds instead of 5. |
| Hot Seat | Uncommon | Whoever is to your right has a 3-second challenge window instead of 5. |
| Doubletalk | Rare | Once per round, declare a turn as "double": you play 2–4 cards instead of 1–3. |
| Sleight of Hand | Uncommon | Once per round, on your turn (before playing), draw 1 card from the top of the draw pile. Your hand grows by 1. The drawn card is yours immediately and can be played that same turn. |

---

## Consumables (shop)

Rebalanced for the smaller deck and tighter tempo.

| Item | Cost | Effect |
|---|---|---|
| Smoke Bomb | 35g | Skip your turn. Play passes to the next player. You do not draw. |
| Counterfeit | 50g | At the start of any turn (yours or someone else's, before plays are committed), change the Target Rank. Once per floor. |
| Magnet | 75g | Give one card from your hand (your choice) to a random opponent. Cannot target Steel cards. *Was 60g and "most dangerous" — that was just a Jack-dump button.* |
| Glass Shard | 30g | Apply the Glass affix to one card in your hand (overwrites existing affix). |
| Tracer | 40g | See the top 3 cards of the draw pile and rearrange their order. The change persists for the rest of the round — whenever a Spiked or Spiked Trap effect triggers, draws come off your stacked order. |
| Devil's Bargain | 55g | Place 1 card from your hand on the bottom of the draw pile, then draw the top card. The drawn card gains the **Cursed** affix (overwrites any existing affix; Steel cards are immune and stay unaffixed). |
| Jack-be-Nimble | 90g | Discard up to 2 Jacks from your hand. Floor-locked (one purchase per floor). |
| Forger | 100g | Choose two run-deck cards currently in your hand. One is **permanently transformed** into a copy of the other (rank + affix), persisting for the rest of the run. Cannot copy *from* a Jack or *to* a Jack. Floor-locked (one purchase per floor). |

---

## Starting characters

Pick one at the start of a run. Each has a passive and a starting joker.

- **The Sharp** — Passive: challenge window +1 second. Starts with **Tattletale**.
- **The Hoarder** — Passive: hand size +1, Jack limit 5. Starts with **Safety Net**. Cannot start the run with a Jack-management joker as a second slot (forces variety).
- **The Banker** — Passive: starts the run with 150g and a **Gilded A** in the run deck. Starts with **The Taxman**.
- **The Whisper** *(unlock)* — Passive: at the start of each round, choose a direction via a toggle button — scout either the **previous player** (right neighbor) or the **next player** (left neighbor). See one random card from that player's hand. Starts with **Eavesdropper**.
- **The Gambler** *(unlock)* — Passive: +50% gold from rounds, but starts each floor with one random Cursed card forced into your hand. Starts with **Black Hole**.

---

## Floor modifiers (Act II+)

Rolled randomly when entering a non-boss floor. Visible before the player commits.

- **Foggy** — Target Rank is shown for 5 seconds at round start, then hidden. Players have to remember.
- **Greedy** — Gold rewards doubled, but Jack limit drops to 3.
- **Brittle** — All cards are temporarily Glass for this floor.
- **Echoing** — Every card played has a 20% chance to be flipped face-up to all players for 1 second.
- **Silent** — No challenge animations or sounds. You can challenge but you don't see other players' tells.
- **Tariff** — Each Liar call costs 5g. (Punishes spammy challenging.)

---

## Bosses & AI behavior

### Difficulty curve via "tells"

The roguelike difficulty escalates by stripping away your ability to read opponents:

- **Act I (floors 1–3).** Every AI has a personality and a **visible tell**. Examples: the Greedy seat hesitates before bluffing big plays. The Coward seat almost never calls Liar. The Eager seat calls Liar too often. Tells are deliberately legible — Act I is teaching the player how to read patterns. Even without scouting jokers you can win on observation alone.
- **Act II (floors 4–6).** Mixed roster. Some AI have tells, some don't. Tells get subtler — micro-pauses, gold reactions, change in play frequency. Floor modifiers also kick in here, adding new variables on top of the read.
- **Act III (floors 7–9).** **No tells.** AI play optimally with clean tempo. Pure math — your only edges are jokers, items, and your run deck. **Cold Read** becomes legitimately essential at this tier.

### Personality roster

Six AI personalities seed the non-boss seats across acts. Each one teaches the player to read a different *kind* of cue.

| Personality | Trait | Tell (Act I version) | What it teaches | First seen |
|---|---|---|---|---|
| **The Greedy** | Bluffs aggressively when ahead, plays safe when behind | Before a big bluff: ~1.5s extra pause, avatar's eyes flick to gold counter | **Timing reads** — long pauses before big plays = suspicious | Act I |
| **The Coward** | Almost never calls Liar (~10%) | When they *do* call: 2+ second hesitation, challenge button flashes red | **Frequency reads** — rare actions carry strong signal | Act I |
| **The Eager** | Challenges nearly everything (~80%) | Their challenge button buzzes/jitters during every opponent's play | **Signal-vs-noise** — opponents who challenge everything stop being readable | Act I |
| **The Methodical** | Only bluffs when math is safe (low Jacks, favorable Target) | Before any bluff: re-sorts hand visibly (card shuffle animation). No sort = truth | **Setup-action reads** — preparation animations reveal intent | Act II |
| **The Mimic** | Copies your behavior — bluffs when you bluff, plays safe when you play safe | Avatar glances *at the player* before each play (camera-on-player animation) | **Positional reads** — opponents react to your patterns | Act II |
| **The Wildcard** | Genuinely random behavior | "Tell" animation fires on both truths and lies — visually identical to a real tell | **Humility** — not every tell is real; some opponents need to be solved with math | Act II / III |

**Tell intensity scales by act:**
- **Act I:** ~1.5–2 second animations/pauses. Loud and obvious. Teaching tier.
- **Act II:** ~0.4–0.7 second. Subtler — players who learned the cues in Act I can still catch them. Mixed-tier rosters (some Act I personalities, some Act II).
- **Act III:** No tells at all. Pure math. Optional Wildcard appearance to remind players that some opponents are unreadable.

**Hard rule:** within a single tier, a personality's tell must be 100% consistent. A Greedy who *sometimes* doesn't pause on bluffs breaks the learning. The Wildcard is the only allowed exception — its inconsistency *is* the lesson.

### Bosses

Each boss is a 2-player duel with a unique rule.

- **Floor 3 — The Auditor.** Challenges every 3rd play, no exceptions. The predictability *is* the tell. Beating it teaches you to count plays.
- **Floor 6 — The Cheater.** Lies on 100% of plays. Has a visual tell on 1 in 4 lies (designed micro-animation). Without a scouting joker you have to count and find the pattern. Tattletale, Eavesdropper, and Whisper shine here.
- **Floor 9 — Lugen.** The final boss. Every card it plays is randomly affixed. Its Jack limit is 6. It can call Liar out-of-turn once per round. It starts with 7 cards. **No tells, optimal play.** Win this and the run ends with a victory.

Optional unlocked Floor 9 alts after first win:
- **The Mirror** — Plays whatever you played last turn. Beating it is about disrupting your own pattern.
- **The Hollow** — Has no hand visible to you (the count is hidden too). Pure paranoia run.

---

## Relics

Relics are permanent passive bonuses. They occupy a **separate slot system** from jokers — you don't lose joker space to gain a relic. One relic is awarded after each act boss, so a max of 3 per run.

After defeating a boss, you're offered a choice of **2 relics** from that boss's pool. Picking one closes the offer (no take-backs).

| Relic | Pool (boss) | Effect |
|---|---|---|
| Cracked Coin | Floor 3 (The Auditor) | Start of each round: gain **5g × Hearts remaining**. Rewards staying healthy. |
| Loaded Die | Floor 3 (The Auditor) | Once per floor, reroll the Target Rank for any round. |
| Pocket Watch | Floor 6 (The Cheater) | +5 seconds to your challenge window. Stacks with Slow Hand (15s total). |
| Hand Mirror | Floor 6 (The Cheater) | At the start of each round, see one random card from each opponent who currently holds **2 or more cards**. Opponents at 1 card are spared — you can never peek at someone's final card. |
| Iron Stomach | Floor 9 (Lugen, victory) | Glass-burned cards from your run deck return as **Steel** instead of vanilla at end of round. Permanent run-deck upgrade engine. |
| The Ledger | Floor 9 (Lugen, victory) | +25% gold from all sources (rounds, jokers, relics). |

Floor 9 relics activate on the next run if you're playing meta-progression — they don't help you on a run you've already won. (This gives victories tangible carryover and rewards repeated plays.)

**Why two pools per boss:** lets the player pick a direction. Cracked Coin is a "stay alive" build; Loaded Die is a "control luck" build. Pocket Watch is defensive; Hand Mirror is informational. Each boss becomes a fork rather than a fixed reward.

**Future relic ideas (treasure nodes, not boss drops):**
- **The Shroud** — Your run-deck card borders fade between rounds, harder to track at a glance.
- **Crooked Cards** — Once per floor, look at the entire draw pile and rearrange any 5 cards.
- **Black Market** — Shop prices are 25% lower, but Jack-be-Nimble is removed from your shop pool.
- **Gambler's Mark** — +1 joker slot, but you start each floor with one Cursed card forced into your hand.

These would come from random treasure nodes between floors (the 4-fork node system: Shop / Event / Reward / Cleanse). Treasure as a rare 5th node type that occasionally replaces a Reward.

---

## Multiplayer (PvP)

The roguelike framing scales to a competitive PvP mode where 2–6 humans replace the AI seats. The core systems carry over without modification — that's the design's biggest scalability win.

**What stays the same.**
- Round structure, Jack curse, affixes, jokers, consumables, visibility rules — all identical.
- Run decks shuffle in for everyone (so the deck math grows: 30 + N × 8, see Run Deck section).

**What changes.**
- **No floor progression.** A PvP match is a single multi-round contest, not a 9-floor run. Configurable: best-of-3 rounds (quick), best-of-5 (standard), or best-of-7 (long).
- **Hearts represent match life.** Each player has 3 Hearts; lose a Heart by losing a round. First to 0 Hearts is out. Last player standing wins the match.
- **Run decks are pre-built.** Players bring decks they've earned and customized in solo runs, OR pick from preset templates ("Glass Aggro," "Banker," "Spike Trap," etc.) for quick matches. **This gives solo progression a meaningful payoff in PvP** — the more you play solo, the more deck options you've unlocked.
- **Joker selection** is configurable per match: chosen by player from unlocked pool (competitive), randomized (variance), or banned/picked draft-style (high-skill).
- **Modifiers** can be enabled per match for spice. Foggy + Greedy + 6 players = chaos mode.

**Why PvP works without major redesign:** the AI was always just an opponent at the table. Replace the AI with a human and the rules don't shift. The only thing PvP loses is the run economy (gold, jokers earned over floors), which is replaced by pre-match build selection.

---

## Economy / pacing targets

- A floor takes **5–8 minutes** of play. A full run is **45–70 minutes**. This is on the long side for a roguelike but matches the social-game feel.
- Average gold per floor: 60–90g. Item prices are tuned so a player can afford ~1 consumable per floor and an occasional joker.
- Jokers appear in shops at **80g (Common), 150g (Uncommon), 250g (Rare), 400g (Legendary)**.

---

## Open questions I'm punting on

The big structural questions (best-of-3, visibility, tells, PvP, run deck) are now resolved and baked into the doc. What's left are tuning and UX questions that need playtesting:

1. ~~**Run-deck UI.**~~ Resolved — colored borders on cards (per-player color, visible on both faces, shows ownership only, not rank or affix). Owner sees their own hand contents at deal time; opponents only see border colors.
2. ~~**Mulligan rule.**~~ Resolved — no mulligan. Opening hand is pure deal (with the existing Jack-fairness rule preventing instant-loss starts). Players who want luck mitigation can build for it: **Sleight of Hand** (Uncommon joker) lets you draw 1 card per round on your turn. Mitigation becomes a build choice, not a free safety net.
3. ~~**Turn timer.**~~ Resolved — **30 seconds** per turn. Generous enough for deliberate play, tight enough to prevent stalling.
4. ~~**Run-deck card duplicates.**~~ Resolved — no soft cap, hard cap is the run deck size (8). Run all 8 of the same card if you want. Will revisit if a single-card build dominates testing.
5. ~~**Permanent run modifiers (relics).**~~ Resolved — relics added as a separate slot system, awarded one per act boss (3 max per run). See Relics section. Joker slots also expand per act: 2 → 3 → 5.
6. ~~**AI tell design specifics.**~~ Resolved — six personalities defined (Greedy, Coward, Eager, Methodical, Mimic, Wildcard), each teaching a different kind of read. See Personality roster table. Specific animation/audio tuning still needs playtest passes.
7. ~~**Cross-mode progression.**~~ Resolved — **both**. Solo runs unlock new PvP starter decks *and* cosmetics. See Cross-mode progression section below.

---

## Cross-mode progression

Solo runs feed PvP. Beating things in solo mode unlocks build options *and* cosmetic flair you can use in competitive matches.

### Decks (gameplay unlocks)

PvP players need pre-built run decks to bring to a match. Solo progression is how you earn them:

| Unlock | How to earn |
|---|---|
| **Vanilla starter** | Available from day 1. 2 each of A/K/Q/10. |
| **Character signature decks** (5 total — one per starting character) | Beat a full run with that character. Unlocks their themed build (e.g., The Banker's Gilded-stacked deck, The Hoarder's defensive deck). |
| **Boss-themed decks** (3 total) | Beat each Floor 9 alt boss (Lugen, The Mirror, The Hollow). Each unlocks a deck inspired by that boss's mechanics. |
| **Achievement decks** | Specific solo achievements unlock specialty builds. Examples: "Win without ever calling Liar" → Pacifist deck. "Win using only Glass-affixed cards" → Brittle deck. "Hit 4 Jacks and survive via Safety Net + Scapegoat" → Jack-Wrangler deck. |
| **Custom decks** | Any solo run-deck state you've ever ended a run with can be saved as a custom PvP preset (slot-limited — 5 saves). |

### Cosmetics (no gameplay impact)

Cosmetics are pure flavor. They never change rules or balance.

| Cosmetic type | Examples / how earned |
|---|---|
| **Card backs** | Earned by beating bosses, completing achievements, or hitting milestone wins (1st run, 10th run, etc.). |
| **Player border colors** | Default 6 colors at start; unlock 6 more "elite" tints (gold, neon, monochrome, etc.) by hitting cumulative win counts. |
| **Card art skins** | Alt art for ranks (e.g., gothic Aces, neon Jacks). Earned via specific achievements. |
| **Affix visual effects** | Different particle/glow looks for each affix. Glass shatters in stained-glass shards by default; unlock alt looks like rusting steel, smoke dissolution, etc. |
| **Joker portraits** | Each joker has a default portrait; unlock alts by using that joker in N winning runs. |
| **Table backgrounds** | Bar, casino, dive, void, neon-noir. Earned through general progression. |
| **Victory / elimination animations** | Custom flourishes for empty-hand wins, Jack-curse losses, etc. |

### Design principle

Gameplay unlocks (decks, character signatures) **expand build space** — they give PvP players more strategic options. Cosmetic unlocks **express identity** — you show what you've done in solo without changing the match. Keeping these separate prevents the "you can only beat me because you've grinded more" problem: a fresh PvP player has access to the vanilla starter and competitive presets from day one. Solo progression rewards variety and flair, not power.

---

## Why I made the changes I did

The biggest single change is **shrinking the deck to 30 cards** and making the round resettable. Two full decks at 6 players means the Jack curse is mostly noise. At 30 cards with 6 Jacks, every round you're tracking how many Jacks are out and who's holding them. That's the game.

The second biggest change is **affixes that work while held**. Reveal-only effects only fire when challenged, which is maybe 25% of plays. Passive affixes turn your hand into a thing you have to manage actively, not just a queue of stuff to dump.

Third, the **roguelike scaffolding** — characters, modifiers, bosses, deckbuilding nodes — is what separates a Liar's-Bar-clone with jokers from an actual run-based game. Without that structure the joker variety has nowhere to land.
