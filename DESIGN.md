---
# ──────────────────────────────────────────────────────────────────────
# Lügen — design system tokens
#
# A bluff-card-game UI that wears a dark casino-felt costume: deep
# emerald gradients for the table, slate panels with backlit blur for
# floating surfaces, and a hot yellow as the singular call-to-action
# accent. Information density is high (multi-player snapshot, hand,
# log, jokers), so the system leans on tiny uppercase eyebrow labels,
# bold numerics, and color-coded ring outlines to identify card affixes.
# ──────────────────────────────────────────────────────────────────────

name: "Lügen"
description: >-
  Roguelike-flavored multiplayer bluff card game. The interface is a
  digital green felt table with floating slate-glass panels, jewel-tone
  affix rings, and high-contrast typography for fast reads under
  challenge-window time pressure.

# ───── Color tokens ───────────────────────────────────────────────────
colors:
  # — Surface (table) —
  surface:
    # The body background: a green-felt gradient that feels like looking
    # down at a casino table from above.
    table-base:        "#022c22"   # deepest emerald (corner shadows)
    table-mid:         "#166534"   # mid-table green
    table-deep:        "#011a14"   # near-black emerald (vignette)
    table-gradient:    "linear-gradient(135deg, #022c22 0%, #166534 50%, #011a14 100%)"

    # — Floating panels (modals, lobby cards, fork sheet) —
    panel-base:        "#1e293b"   # slate-800 — primary panel fill
    panel-deep:        "#0f172a"   # slate-900 — secondary panel fill
    panel-gradient:    "linear-gradient(135deg, #1e293b 0%, #0f172a 100%)"

    # — Backdrops & scrim —
    overlay-scrim:     "rgba(0,0,0,0.80)"
    panel-glass:       "rgba(0,0,0,0.40)"   # 40% black + backdrop-blur
    panel-glass-deep:  "rgba(0,0,0,0.60)"
    divider:           "rgba(255,255,255,0.10)"

  # — Brand accent —
  brand:
    # Lügen yellow is the single global "ACT NOW" color: primary CTAs,
    # selected cards, host badges, the target-rank glyph.
    primary:           "#eab308"   # yellow-500
    primary-hover:     "#facc15"   # yellow-400
    primary-deep:      "#a16207"   # yellow-700 (depressed state)
    on-primary:        "#000000"   # text-on-yellow is true black

  # — Functional / semantic —
  semantic:
    success:           "#10b981"   # emerald-500
    success-soft:      "#34d399"   # emerald-400
    success-text:      "#a7f3d0"   # emerald-200
    info:              "#06b6d4"   # cyan-500
    info-text:         "#67e8f9"   # cyan-300
    warning:           "#f59e0b"   # amber-500
    warning-text:      "#fcd34d"   # amber-300
    danger:            "#e11d48"   # rose-600
    danger-soft:       "#f43f5e"   # rose-500
    danger-text:       "#fda4af"   # rose-300
    boss:              "#7e22ce"   # purple-700 (Lugen / final-boss seat)
    boss-text:         "#d8b4fe"   # purple-300
    treasure:          "#db2777"   # pink-600 (treasure / relic)
    treasure-text:     "#f9a8d4"   # pink-300

  # — Affix palette (eight ring colors that mark card mechanics) —
  # Each affix paints a 2px solid ring around the card. Order matters
  # only for the rainbow Mirage animation (see motion.rainbow.stops).
  affix:
    gilded:            "#facc15"   # yellow-400 — gold income while held
    glass:             "#22d3ee"   # cyan-400  — burns on reveal
    spiked:            "#f87171"   # red-400   — pickup draws +1
    cursed:            "#a855f7"   # purple-500 — locks LIAR / 2-turn hold
    steel:             "#d1d5db"   # gray-300  — immune to mods
    mirage:            "#f472b6"   # pink-400  — wildcard (rainbow border)
    hollow:            "#818cf8"   # indigo-400 — counts as zero
    echo:              "#e879f9"   # fuchsia-400 — peek next play

  # — Rarity color (joker tiles) —
  rarity:
    common:            "#374151"   # gray-700 chip / gray-100 ink
    uncommon:          "#047857"   # emerald-700 chip
    rare:              "#1d4ed8"   # blue-700 chip
    legendary:         "#b45309"   # amber-700 chip
    mythic:            "#be123c"   # rose-700 chip — the "red" tier

  # — Card faces (the only persistently light surface in the app) —
  card:
    face-bg:           "#ffffff"
    face-border:       "#d1d5db"   # gray-300
    face-text:         "#000000"
    back-stripe-a:     "#1e3a8a"   # blue-900
    back-stripe-b:     "#4338ca"   # indigo-700
    back-border:       "#ffffff"
    selected-ring:     "#facc15"   # yellow-400, 3px halo + 14px lift

  # — Text scale on dark surfaces —
  text:
    primary:           "#ffffff"
    secondary:         "rgba(255,255,255,0.70)"
    tertiary:          "rgba(255,255,255,0.60)"
    muted:             "rgba(255,255,255,0.40)"
    disabled-bg:       "rgba(255,255,255,0.10)"
    placeholder:       "rgba(255,255,255,0.50)"
    inverse:           "#0f172a"   # for the rare light surface (card faces)

# ───── Typography ─────────────────────────────────────────────────────
typography:
  font-family:
    base:              "ui-sans-serif, system-ui, -apple-system, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif"
    mono:              "ui-monospace, 'SFMono-Regular', Menlo, Consolas, monospace"
    # Used for: run seed pills, card-id codes, room-code badges.

  weight:
    regular:           400
    semibold:          600
    bold:              700
    extrabold:         800   # display + key numerics

  size:
    micro:             "9px"    # ascension star tags inside joker tiles
    eyebrow:           "10px"   # uppercase section labels
    caption:           "11px"   # micro hints, fork seed pill
    small:             "12px"   # body small / button labels
    body:              "14px"   # default body
    base:              "16px"   # form inputs
    h4:                "20px"   # section heads ("Players")
    h3:                "24px"   # modal heads, run-end totals
    h2:                "30px"   # screen titles
    h1:                "36px"   # run-result banner
    display:           "72px"   # the giant target-rank glyph

  line-height:
    tight:             1.1
    snug:              1.25
    normal:            1.5
    relaxed:           1.625

  letter-spacing:
    tight:             "-0.01em"
    normal:            "0"
    wide:              "0.04em"
    wider:             "0.08em"   # uppercase eyebrows ("Game Settings")
    widest:            "0.16em"   # affix labels, room-code input

  uppercase-eyebrow:
    size:              "10px"
    weight:            700
    letter-spacing:    "0.16em"
    color:             "rgba(255,255,255,0.70)"
    note: >-
      Tiny all-caps labels above sections ("PLAYERS", "JOKERS",
      "BURNED THIS ROUND"). One of the system's strongest tells —
      they appear above almost every grouped block.

# ───── Spacing scale (4px base) ───────────────────────────────────────
spacing:
  0:                   "0px"
  px:                  "1px"
  0.5:                 "2px"
  1:                   "4px"
  1.5:                 "6px"
  2:                   "8px"
  3:                   "12px"
  4:                   "16px"
  5:                   "20px"
  6:                   "24px"
  8:                   "32px"
  10:                  "40px"
  12:                  "48px"

# ───── Layout constants ───────────────────────────────────────────────
layout:
  max-width:
    auth-modal:        "384px"   # max-w-sm
    standard-modal:    "448px"   # max-w-md
    fork-panel:        "672px"   # max-w-2xl-ish
    game-shell:        "1024px"  # max-w-5xl
  player-tile-min:     "140px"
  card-size:
    width:             "64px"
    height:            "90px"
    burn-mini-w:       "22px"
    burn-mini-h:       "30px"
  challenge-bar-height:"8px"
  challenge-bar-radius:"9999px"

# ───── Radii ──────────────────────────────────────────────────────────
radii:
  none:                "0"
  sm:                  "4px"     # default rounded
  md:                  "8px"     # rounded-lg — buttons, inputs
  lg:                  "12px"    # rounded-xl — info pills, cards
  xl:                  "16px"    # rounded-2xl — modals, hero panels
  pill:                "9999px"  # rounded-full — auth bar, status chips

# ───── Elevation / shadows ────────────────────────────────────────────
elevation:
  0:                   "none"
  1:                   "0 1px 2px rgba(0,0,0,0.10)"
  2-resting:           "0 4px 6px -1px rgba(0,0,0,0.10), 0 2px 4px -2px rgba(0,0,0,0.10)"   # shadow-md
  3-floating:          "0 10px 15px -3px rgba(0,0,0,0.20), 0 4px 6px -4px rgba(0,0,0,0.20)" # shadow-lg
  4-modal:             "0 20px 25px -5px rgba(0,0,0,0.30), 0 8px 10px -6px rgba(0,0,0,0.20)" # shadow-xl
  5-hero:              "0 25px 50px -12px rgba(0,0,0,0.50)"  # shadow-2xl

# ───── Borders / rings ────────────────────────────────────────────────
borders:
  width:
    hairline:          "1px"
    standard:          "2px"
    accent:            "3px"
  ring:
    affix:             "2px"     # solid ring around affixed cards
    selected:          "3px"     # yellow halo on a selected hand card
    selected-double:   "3px + 6px" # ring + outer ring for newly-drawn-and-selected
    pulse:             "0 → 12px outward fade" # turn-prompt animation

# ───── Backdrop / glass effect ────────────────────────────────────────
glass:
  blur:                "8px"     # default backdrop-blur
  blur-deep:           "12px"
  tint-light:          "rgba(0,0,0,0.30)"
  tint-medium:         "rgba(0,0,0,0.40)"
  tint-deep:           "rgba(0,0,0,0.60)"
  tint-scrim:          "rgba(0,0,0,0.80)"
  hairline:            "rgba(255,255,255,0.10)"

# ───── Motion ─────────────────────────────────────────────────────────
motion:
  duration:
    instant:           "0ms"
    quick:             "150ms"   # button hover/press
    settle:            "300ms"   # panel fade-in
    swap:              "600ms"   # round-end → next-deal pause
    deliberate:        "1500ms"  # bot/Lugen play delay
  easing:
    standard:          "cubic-bezier(0.4, 0, 0.2, 1)"   # default
    decelerate:        "cubic-bezier(0, 0, 0.2, 1)"
    accelerate:        "cubic-bezier(0.4, 0, 1, 1)"
    linear:            "linear"
  pulse-ring:
    name:              "pulse"
    duration:          "1.4s"
    iteration:         "infinite"
    keyframes:
      "0%":            "box-shadow: 0 0 0 0 rgba(250,204,21,0.7)"
      "50%":           "box-shadow: 0 0 0 12px rgba(250,204,21,0)"
      "100%":          "box-shadow: 0 0 0 0 rgba(250,204,21,0.7)"
    used-for:          "Active-turn indicator, your-action prompts"
  rainbow-border:
    name:              "rainbow-border"
    duration:          "3s"
    iteration:         "infinite"
    timing:            "linear"
    stops:
      "0%":            "#ff0080"   # hot pink
      "16%":           "#ff8c00"   # amber
      "33%":           "#ffd700"   # gold
      "50%":           "#00d26a"   # emerald
      "66%":           "#00bfff"   # sky
      "83%":           "#8a2be2"   # violet
      "100%":          "#ff0080"   # back to pink
    used-for:          "Mirage-affixed cards (the wildcard affix)"
  challenge-bar-tick:  "80ms refresh, fills shrink toward 0% as deadline nears"
  hover-lift:          "transform: translateY(-2px), 150ms"
  card-selected-lift:  "translateY(-14px) + 3px yellow ring, snap (no easing)"
  hover-scale:         "scale(1.05) on shop cards / pick-target buttons"

# ───── Iconography ────────────────────────────────────────────────────
iconography:
  style:               "inline emoji + monochrome SVG strokes"
  emoji-conventions:
    target-rank:       "🃏"
    boss:              "👑 / 😈"
    fortune:           "🎴 / 🍀"
    danger:            "🏳️ (forfeit)"
    information:       "🔍 (Surveyor) / 👁️ (Tattletale) / 🧮 (Card Counter)"
    sound:             "📢 (Screamer)"
    auth:              "👤"
    leaderboard:       "🏆"
    stats:             "📊"
  svg:
    settings-gear:     "stroke 2px, currentColor, 24×24"
    close:             "× character at 24px"

# ───── Form controls ──────────────────────────────────────────────────
forms:
  input:
    bg:                "rgba(255,255,255,0.10)"
    border:            "rgba(255,255,255,0.20)"
    border-focus:      "#facc15"
    placeholder:       "rgba(255,255,255,0.50)"
    radius:            "8px"
    padding-y:         "8px"
    padding-x:         "16px"
    focus-ring:        "2px solid #facc15"
  select:
    bg:                "rgba(0,0,0,0.40)"
    border:            "rgba(255,255,255,0.10)"
    text-size:         "11px"
  checkbox-toggle:
    on:                "#10b981"
    off:               "rgba(255,255,255,0.20)"

# ───── Components (key recipes) ───────────────────────────────────────
components:
  primary-cta:
    bg:                "#eab308"
    bg-hover:          "#facc15"
    text:              "#000000"
    weight:            700
    radius:            "8px"
    padding:           "8px 24px"
    shadow:            "none"
    transition:        "background-color 150ms ease"

  secondary-cta:
    bg:                "rgba(255,255,255,0.10)"
    bg-hover:          "rgba(255,255,255,0.20)"
    text:              "#ffffff"
    weight:            700
    radius:            "8px"
    padding:           "8px 16px"

  destructive-cta:
    bg:                "#e11d48"   # rose-600
    bg-hover:          "#f43f5e"   # rose-500
    text:              "#ffffff"
    weight:            700
    radius:            "8px"

  pill-status:
    radius:            "9999px"
    padding:           "4px 12px"
    text-size:         "11px"
    weight:            700
    variants:
      gold:            "bg #facc15 / text #000"
      red:             "bg #f43f5e / text #fff"
      synergy:         "bg #a21caf66 / text #f5d0fe (10px uppercase ★ Synergy ×N)"
      ascend-1:        "bg #047857 / text #fff / ★1"
      ascend-2:        "bg #1d4ed8 / text #fff / ★2"
      ascend-3:        "bg #d97706 / text #fff / ★3"
      ascend-4:        "bg #e11d48 / text #fff / ★4"

  modal-shell:
    bg:                "linear-gradient(135deg, #1e293b 0%, #0f172a 100%)"
    border:            "1px solid {accent}/40"   # accent = yellow-400 or emerald-400
    radius:            "16px"
    padding:           "32px"
    shadow:            "0 25px 50px -12px rgba(0,0,0,0.50)"
    overlay:           "rgba(0,0,0,0.80) + backdrop-blur 8px"

  panel-glass:
    bg:                "rgba(0,0,0,0.40)"
    radius:            "16px"
    padding:           "24px"
    border:            "1px solid rgba(255,255,255,0.10)"
    backdrop-blur:     "8px"
    shadow:            "0 10px 15px -3px rgba(0,0,0,0.20)"

  player-tile:
    bg:                "rgba(0,0,0,0.40)"
    radius:            "8px"
    padding:           "8px 12px"
    min-width:         "140px"
    text-align:        "center"
    states:
      active-turn:     "ring 2px #facc15"
      open-challenger: "ring 2px #fb7185"
      eliminated:      "opacity 0.5"
      lugen-boss:      "bg #581c87/0.6, border 1px #a855f7"

  card-tile:
    width:             "64px"
    height:            "90px"
    radius:            "4px"
    face-bg:           "#ffffff"
    face-border:       "1px solid #d1d5db"
    face-text-size:    "24px"
    face-text-weight:  700
    face-text-color:   "#000000"
    back:              "repeating-linear-gradient(45deg, #1e3a8a, #1e3a8a 6px, #4338ca 6px, #4338ca 12px)"
    back-border:       "2px solid #ffffff"
    selected:          "translateY(-14px), 3px ring #facc15"
    affix-ring:        "2px ring (color from colors.affix.*)"
    rainbow-border:    "animation rainbow-border 3s linear infinite (Mirage only)"

  scrollbar-thin:
    width:             "6px"
    thumb:             "rgba(255,255,255,0.30)"
    thumb-radius:      "3px"
    track:             "transparent"

  challenge-bar:
    track-bg:          "rgba(0,0,0,0.40)"
    track-radius:      "9999px"
    height:            "8px"
    fill-bg:           "#f43f5e"   # rose-500
    fill-tween:        "linear width fade as deadline approaches"

# ───── Sound design (light footprint) ─────────────────────────────────
sound:
  catalog:
    challenge-deal:    "short percussive hit on round-deal"
    play-card:         "soft thud / card-snap"
    liar-call:         "alarm sting"
    glass-burn:        "shatter"
    win-floor:         "fanfare ascending"
    run-end:           "deeper resolution chord"
  volume-default:      "0.6"

# ───── Breakpoints ────────────────────────────────────────────────────
breakpoints:
  sm:                  "640px"
  md:                  "768px"
  lg:                  "1024px"
  xl:                  "1280px"

# ───── Z-index scale ─────────────────────────────────────────────────
z-index:
  base:                0
  raised:              10
  sticky:              20
  fixed-bar:           40   # auth bar, settings cog
  modal:               50   # all overlays
  toast:               60
---

# Lügen — Visual Identity & Look-and-Feel

## Stage one: the table is the room

The whole product opens onto a **dark emerald felt gradient** —
top-left bright, bottom-right deep — that reads as "you are looking
down at a casino table from above." It never fades to neutral gray;
even the most minimal screens (auth, profile) keep the same green
behind a scrim. The green is the brand.

Everything else floats above the felt. Floating surfaces are slate
panels (a 135° gradient from `#1e293b` to `#0f172a`) edged by a
**hairline accent border** in either the brand yellow or a contextual
color (emerald for stats, yellow for actions, pink for treasure,
purple for boss). The combination — saturated dark green table,
lavender-cool slate cards on top, single hot yellow accent — is the
core palette tension.

Modal overlays sit on a `rgba(0,0,0,0.80)` scrim with an 8px
**backdrop-blur**. The blur isn't decorative; it's how the system
signals "the table has paused for you." Anywhere you see the blur,
you can assume the round clock is also paused.

## Stage two: yellow is the only verb

Lügen has many decorative colors but **exactly one CTA color**: the
brand yellow (`#eab308` resting / `#facc15` hover, with true black
ink). It's used for:

- The primary action button on every screen ("Sign up", "Submit",
  "Start run", "Play").
- The **selected card** halo — a 3px yellow ring around any card the
  player has lifted out of their hand.
- The **active-turn ring** around a player tile.
- The pulse animation that fades from solid yellow to transparent
  every 1.4 seconds when it's your move.
- The host badge in the lobby.
- The target rank chip below the giant target-rank glyph.

Because yellow always means "act here," the eye finds the next move
without reading. Every other color exists to *describe state* (gold,
hearts, hand size) rather than ask for input. The discipline is the
system: when in doubt, the answer is "yellow if you should click it,
anything else if it's just information."

## Stage three: the affix language

Cards carry a state called an *affix* — a one-word mechanical effect
like Glass, Spiked, Cursed, Mirage. The visual language for affixes
is a **2px solid ring** around the card face in a fixed color:

- **Yellow** (Gilded — gold income).
- **Cyan** (Glass — burns on reveal).
- **Red** (Spiked — punishes pickups).
- **Purple** (Cursed — locks LIAR calls).
- **Gray** (Steel — immune to mods).
- **Pink** (Mirage — wildcard, plus the rainbow animation).
- **Indigo** (Hollow — counts as zero).
- **Fuchsia** (Echo — peeks the next play).

Cyan and red look superficially close, but in context the cyan reads
as "fragile" and the red as "danger" — and that's the design's job.
Mirage gets the only animated border in the system, a 6-stop hot
rainbow that cycles every 3 seconds. It's loud on purpose: Mirage is
rare, and if it lands face-up the player should *feel* it.

The sole ring shape is consistent across hand cards, shop card
previews, the burn-pile mini-tiles, the run-deck inspector, and the
cleanse panel. A player who has learned the eight ring colors can
read any unlabeled card in any context.

## Stage four: typography pulls double duty

Body sits at 14px, the comfortable density target. Numbers go
**heavy** — `font-bold` (700) and `font-extrabold` (800) — because
hearts, gold, hand sizes, round wins, and the giant 72px target-rank
glyph all need to be readable at a glance during the 8-second
challenge window.

Labels go small and quiet. The system uses a recurring **uppercase
eyebrow**: 10px, weight 700, letter-spacing 0.16em, white at 70%
opacity. It sits above almost every grouped section ("PLAYERS",
"JOKERS", "CONSUMABLES", "BURNED THIS ROUND"). Without it the table
would feel cluttered; with it the eye knows exactly where each
information cluster begins and ends.

Mono is reserved for **machine-shaped strings**: run seeds, room
codes, card IDs, system-generated identifiers. Anything the player
might want to copy/paste lives in mono.

## Stage five: the card itself

Cards are the only persistently *light* surface in the application.
Everything else — table, panels, inputs — sits in dark territory.
The face is pure white with a 1px gray border, the rank in 24px
black type. The back is a **repeating diagonal stripe** (45°, 6px on
6px) in two shades of indigo, hemmed by a 2px white border. The card
back is intentionally rich: it's the most-rendered element in the
game and reads as crisp at any distance.

Selected cards lift **14 pixels** off the hand row and gain a 3px
yellow ring. There is no in-between hover state — cards are either
seated or selected, and the snap is instantaneous. (Most other
elements in the system use 150ms transitions; the card lift skips
ease so the player feels the commitment.) Newly drawn cards add a
3px **black** ring on top, so a player can immediately see which
cards just arrived from a Spiked draw or a Hollow refill. When a
new card is also selected, both rings stack: black inner, yellow
outer.

## Stage six: motion is restrained

Three motions only:

- **Hover/press transitions** at 150ms — buttons brighten, secondary
  surfaces lighten by ~10%.
- **The pulse ring** — a 1.4s yellow halo fade on whichever element
  is asking for the player's input.
- **The rainbow border** — a 3s hue cycle reserved exclusively for
  Mirage cards (the wildcard affix).

The challenge bar is the only timer-driven animation: a thin (8px)
rose-tinted fill that shrinks left-to-right at ~80ms refresh. There
is **no scroll-jacking, no parallax, no decorative loop, no
scroll-triggered reveal**. The game loop already provides plenty of
movement (cards entering the pile, hand reorganization,
chip-counter increments); the chrome stays still on purpose.

## Stage seven: rarity & ascension

Joker tiles are rated on a five-tier rarity ladder (Common / Uncommon
/ Rare / Legendary / Mythic) using a chip color in the row header:
gray → green → blue → amber → rose. Mythic specifically reads as
**red**, signaling "this is the ceiling."

A separate progression mark — the **ascension star** — sits on
equipped jokers as a tiny 9px badge: ★1 emerald, ★2 blue, ★3 amber,
★4 rose. The star color intentionally mirrors the rarity ladder so
"Ascend 4" of any joker reads as "Mythic-strength" at a glance.

## Stage eight: the boss seat

When the final boss (Lugen) joins the table, its player tile breaks
the system on purpose:

- A **deep purple background** (`#581c87` at 60% opacity) instead of
  the standard 40% black.
- A 1px purple-500 border instead of the usual hairline.
- The hearts cell shows ♥∞ in purple-300 instead of a number.
- The character label says "Final Boss" in purple-200 instead of
  emerald.

The contrast is deliberate. Every other tile follows the system; the
boss tile is the only intentional outlier, and its outlier-ness is
the story.

## Voice & tone

Copy is short, present-tense, and slightly noir:
*"The deck is closing in."*
*"Lugen reseats."*
*"Burn cap reached — counter resets."*
The status bar narrates the round in second person ("Your turn. Play
1–3 cards as Q.") and falls back to third when watching others
("Waiting for Bot Bob to call or pass…"). System log lines append a
period and stay under ~80 characters whenever possible — they have
to fit a 15-line scrolling window without wrapping.

## Density and breakpoint behavior

The game shell caps at 1024px (`max-w-5xl`). Everything below that
breakpoint reflows: the players row wraps to 2-up, the action button
row stacks vertically, and the side panels (joker row, consumables
row, challenge bar) flatten into stacked rows. The system is dense
by intent — it has to fit 4 player tiles, a 5-card hand, a target,
a draw/pile/burn cluster, a 5-slot joker rack, a consumable rack,
and a scrolling log on a single screen — so the spacing scale stays
on a strict 4px grid (`p-1` … `p-12`) and the typography scale
collapses three steps below 640px to keep the same density readable
on a phone.

## Design intent in one paragraph

Lügen is a bluffing game; the UI must support **fast reads under
deadline pressure**. The design system delivers that with three
mechanics: the green felt + slate panels create a stable spatial
backdrop so the player's eyes always know where to land; the hot
yellow + uppercase eyebrows split the screen into "things you do"
and "things you read"; and the affix ring palette compresses an
eight-state mechanical system into a single shape repeated wherever
cards appear. Everything else — the rainbow Mirage, the boss tile,
the ascension stars — is a deliberate exception to those three
rules, and each exception means something specific.
