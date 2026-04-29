# Lügen — Unity / C# Port (Logic Layer)

This folder contains a C# translation of the **rules engine, AI, and run/meta
systems** from the JavaScript beta of Lügen, restructured for use in a Unity
project (with an eye on a Steam release).

It is **not** a finished Unity game. There is no UI, no rendering, no
audio. What it gives you is a clean rules engine that drops into Unity as
plain C# (one MonoBehaviour at the top, the rest is engine-agnostic). You
build the visual layer on top of it.

The original JS sources are still where you left them (`server.js`,
`public/beta.js`, etc.) — nothing was deleted. This is a sibling folder.

## Folder layout

```
unity-port/
└── Assets/
    └── Scripts/
        ├── Achievements/      # Achievement catalog (cosmetic unlocks).
        ├── AI/                # Bot personalities, brain, human-profile predictor.
        ├── Affixes/           # 8 affixes: Gilded, Glass, Cursed, Steel, Mirage, Spiked, Hollow, Echo.
        ├── Bosses/            # (Empty — bosses live in AI/ as personalities.)
        ├── Cards/             # Card / Rank / id factory.
        ├── Characters/        # 11 starting characters (Rookie..RANDOM.EXE).
        ├── Consumables/       # Inventory items + services (shop pool).
        ├── Core/              # Constants, RNG.
        ├── Deck/              # DeckBuilder, JackFairness, deal logic.
        ├── Floor/             # FloorModifier, ForkNode (between-floor choices).
        ├── Jokers/            # 30+ jokers, 5-slot system, hooks.
        ├── Modifiers/         # (Empty — floor mods live in Floor/.)
        ├── Players/           # Player object (Human / Bot / Remote).
        ├── Relics/            # 18 relics (boss + treasure pools).
        ├── Round/             # RoundState, TurnResolver, LiarResolver, TargetRotation, JackCurse.
        ├── Run/               # RunState, RunManager, GoldEconomy.
        ├── Save/              # JsonUtility-based save data.
        ├── Shop/              # Shop offers, pricing, discounts.
        ├── Steam/             # Steamworks scaffolding (no-op default).
        └── UnityHooks/        # GameManager (MonoBehaviour) + RoundController glue.
```

## Namespace layout

Everything is under `Lugen.*`. Each folder is its own namespace
(`Lugen.Cards`, `Lugen.AI`, `Lugen.Round`, etc.). Only `UnityHooks/`
references `UnityEngine` — the rest is plain C# and runs in any host
(unit test runner, console app, server-side validator, etc.).

## What is translated

| System                         | Status                    |
|--------------------------------|---------------------------|
| Cards / Ranks / IDs            | Full                      |
| 8 affixes (logic, not VFX)     | Full                      |
| Round deck construction        | Full                      |
| Deal + Jack fairness rule      | Full                      |
| Own-deck minimum (Hometown Hero / Stacked Hand) | Full     |
| Target rank rotation + bias    | Full                      |
| Turn resolver (play 1–3 cards) | Full                      |
| Liar challenge resolver        | Full                      |
| Glass on-reveal + burn cap     | Full                      |
| Spiked on-pickup               | Full                      |
| Cursed locks + Steel Spine     | Full                      |
| Mirage 3-use tracking          | Full (run-deck owners)    |
| Echo / Hollow on-play          | Full                      |
| Gilded passive income          | Full                      |
| Jack curse (incl. Steel Jacks) | Full                      |
| 5-slot joker system + stacks   | Full                      |
| 30+ jokers (catalog)           | Full data, partial hooks  |
| 11 characters (catalog)        | Full data + bonuses       |
| 11 floor modifiers             | Catalog only              |
| 18 relics                      | Catalog + 6 hooked        |
| Shop with discounts            | Full                      |
| Consumables catalog            | Full data                 |
| AI bot personalities (7) + 5 bosses | Full                 |
| AI play decision               | Full                      |
| AI Liar decision               | Full                      |
| Human-behavior predictor (Lugen / Prophet) | Full          |
| Run / Floor / Fork progression | Full                      |
| Achievement catalog            | Full data, no triggers    |
| Save / Load (JsonUtility)      | Full                      |
| Steam hooks (interface)        | Scaffold only             |

## What is NOT translated (and why)

- **UI / rendering / animation.** This is a logic port. All the DOM
  manipulation, CSS animations, and DOM event wiring in `beta.js` is
  ignored. You will write your Unity UI from scratch — but you won't have
  to re-derive any rules.
- **Networking / multiplayer.** `server.js`, `server-beta-rooms.js`, and
  `auth.js` aren't ported. You picked single-player. PvP can be layered
  on later — the engine is mode-agnostic.
- **Per-joker UI hooks.** A handful of jokers have catalog entries but
  not full triggers (Saboteur, Screamer, Doppelganger arming, Trickster
  marking, Alchemist transform). The rules-engine slots exist
  (`JokerSlots.Has(id)` / state flags on RoundState) — you wire the UI
  buttons to set those flags.
- **Per-relic active effects** (Cracked Mirror rewind, Bookmark save).
  Same — flags exist, UI completes them.
- **Floor modifier per-tick effects** (Foggy timer, Echoing 20% flash,
  Sticky reveals). The catalog flag is read; the UI / animation
  consequences are yours.
- **Deterministic replays from the seed code.** `Rng.SeedFromString()`
  exists, but full replay would require routing every random call through
  the seeded RNG. The bot brain still touches `System.Random` in places.

## Drop-in usage

1. Open or create a Unity 2021+ project (.NET Standard 2.1 recommended).
2. Copy the `Assets/Scripts/` folder into your project's `Assets/`.
3. Create a `GameManager` GameObject in your bootstrap scene and attach
   `Lugen.UnityHooks.GameManager`.
4. From your "Start Run" UI button, call:
   ```csharp
   GameManager.Instance.StartNewRun("rookie");
   ```
   The `RunManager` and `RoundController` are now live — read from
   `GameManager.Instance.CurrentRound.State` to render the table.
5. Wire UI events:
   ```csharp
   var c = GameManager.Instance.CurrentRound;
   c.RunBotTurn(c.State.currentTurn);             // when it's a bot's turn
   c.OpenChallengeWindow(playerJustPlayed);        // after every play
   c.CallLiar(0);                                  // when human clicks LIAR
   ```
6. Save data lives at `Application.persistentDataPath/lugen_save.json`
   and is read on `Awake()`, written on `OnApplicationQuit()`.

## Steam integration

`Steam/SteamScaffold.cs` defines an `ISteamHooks` interface with a
`NoOpSteamHooks` default. To go live:

1. Add **Steamworks.NET** to your project
   (https://steamworks.github.io/).
2. Implement `ISteamHooks` with calls to `SteamAPI.Init()`,
   `SteamUserStats.SetAchievement()`, `SteamUserStats.StoreStats()`,
   etc.
3. Map your local achievement IDs (e.g. `"pacifist"`) to Steam
   achievement API names (e.g. `"ACH_PACIFIST"`).
4. Register your impl on bootup:
   ```csharp
   Lugen.Steam.SteamHub.Hooks = new MySteamworksHooks();
   ```

Steam Cloud sync is automatic — Lügen saves to
`Application.persistentDataPath`, which Steam Cloud picks up if you
configure auto-cloud in the Steamworks partner site.

## Known caveats / gotchas

- **NUM_PLAYERS is fixed at 4.** Boss floors in the original game logically
  drop to 2 seats; in this port the array stays length 4 with non-boss
  seats marked `eliminated[i] = true` from the start. Your UI hides them.
- **Tuple syntax + `?.`** are used freely. Requires C# 7+ (Unity 2018+).
- **`Dictionary.GetValueOrDefault`** was avoided in favor of
  `TryGetValue` so the code compiles against .NET Standard 2.0 if you
  must support older Unity.
- **No live tests.** The sandbox where this was written had no
  C# compiler. The port has been read-checked but you may hit a typo —
  open the Unity Console and fix as Roslyn flags them.

## What to attack first inside Unity

A reasonable build order if you've never wired one of these up before:

1. Get a single round to play out in console output: deal a hand,
   render text-only, accept input, call `TryPlay` / `CallLiar`. Verify
   the rules engine works without UI.
2. Build a single floor's UI: hand, table, played pile, draw pile,
   target rank, LIAR button.
3. Layer on the affix VFX (Glass shatter, Spiked spike, Echo peek toast).
4. Add the run UI: floor counter, hearts, gold, jokers, relics.
5. Add the fork node screens: Shop, Reward, Cleanse, Event, Treasure.
6. Add character selection + intro screen.
7. Add the achievement toast and per-achievement triggers.
8. Wire Steam hooks.

Good luck shipping it.
