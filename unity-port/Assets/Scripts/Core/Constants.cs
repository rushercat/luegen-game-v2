// Lügen — Constants.cs
// All the "magic number" tunables from public/beta.js, gathered into one
// place so they're easy to balance without hunting through 8000-line files.
//
// Source comments preserved next to each constant where helpful.

namespace Lugen.Core
{
    public static class Constants
    {
        // ---- Deck / hand sizing ----
        public const int HAND_SIZE = 5;
        public const int NUM_PLAYERS = 4;        // Solo run = 1 human + 3 bots. Boss floors override to 2.
        public const int JACK_LIMIT = 4;
        public const int RUN_DECK_SIZE = 8;      // Cards in personal run deck (before shop additions).
        public const int RUN_DECK_PER_RANK = 2;  // Default starter: 2 each of A/K/Q/10.
        public const int ROUND_DECK_RANK_CAP = 16; // Max cards of any one rank in a single round's deck.
        public const int BASE_JACKS_PER_ROUND = 6;
        public const int BASE_NON_JACK_PER_ROUND = 6;

        // ---- Run progression ----
        public const int STARTING_HEARTS = 3;
        public const int TOTAL_FLOORS = 9;
        public const int ROUNDS_TO_WIN_FLOOR = 2;   // Best-of-3 rounds per floor.

        // ---- Timing ----
        public const int CHALLENGE_MS = 5000;       // Default LIAR-call window.
        public const int SLOW_HAND_WINDOW_MS = 10000;
        public const int BOT_TURN_DELAY_MS = 400;   // Pacing — purely cosmetic (gives the human time to read).
        public const int BOT_CHALLENGE_DELAY_MIN_MS = 500;
        public const int BOT_CHALLENGE_DELAY_RAND_MS = 700;
        public const int REVEAL_HOLD_MS = 1500;
        public const int TATTLETALE_PEEK_MS = 4000;

        // ---- Gold / economy ----
        public const int GOLD_PLACE_1 = 20;
        public const int GOLD_PLACE_2 = 10;
        public const int GOLD_PER_FLOOR_WIN = 30;
        public const int REWARD_NODE_GOLD = 75;
        public const float LEDGER_GOLD_MULT = 1.25f;

        // ---- Affix tuning ----
        public const int GOLD_PER_GILDED_PER_TURN = 2;
        public const int SPIKED_DRAWS_ON_PICKUP = 1;
        public const int GLASS_BURN_RANDOM = 2;
        public const int BURN_CAP = 8;

        // ---- Joker tuning ----
        public const int SPIKED_TRAP_DRAWS = 3;
        public const int TATTLETALE_CHARGES_PER_FLOOR = 1;

        // ---- Run / Floor odds ----
        public const int HEART_SHARDS_REQUIRED = 3;            // 3 shards = +1 Heart
        public const float TREASURE_CHANCE_ACT_III = 0.33f;
        public const int LAST_CALL_TURN_LIMIT = 5;
        public const float LAST_CALL_GOLD_PENALTY = 0.30f;
        public const float TARGET_BIAS_CHANCE = 0.70f;        // Chance Target Rank biases toward your stacked rank.
        public const float OWN_DECK_MIN_FRACTION_BASE = 0.30f;

        // ---- Inventory ----
        public const int INVENTORY_CAP_BASE = 3;
        public const int BRASS_RING_BONUS = 2;

        // ---- Joker price tiers (from JOKER_CATALOG) ----
        public const int PRICE_COMMON     = 80;
        public const int PRICE_UNCOMMON   = 150;
        public const int PRICE_RARE       = 250;
        public const int PRICE_LEGENDARY  = 400;
    }
}
