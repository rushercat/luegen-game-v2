// Lügen — JokerSlots.cs
// Translated from runState.jokers / runState.jokerStacks plus equipJoker()
// in beta.js.
//
// 5 slots from run start. Sixth Sense (the only stackable joker) tracks an
// extra count instead of consuming additional slots — so you can have it
// at stack 1, 2, or 3 in a single slot.

using System.Collections.Generic;

namespace Lugen.Jokers
{
    [System.Serializable]
    public class JokerSlots
    {
        public const int SLOT_COUNT = 5;

        // The slot array. Length is always SLOT_COUNT; null entries = empty.
        public string[] slots = new string[SLOT_COUNT];

        // Stack count for stackable jokers (Sixth Sense). Default == 1 once
        // equipped.
        public Dictionary<string, int> stacks = new Dictionary<string, int>();

        // True if any slot holds the given joker.
        public bool Has(string jokerId)
        {
            if (string.IsNullOrEmpty(jokerId)) return false;
            for (int i = 0; i < slots.Length; i++) if (slots[i] == jokerId) return true;
            return false;
        }

        public int Stack(string jokerId)
        {
            if (!Has(jokerId)) return 0;
            return stacks.TryGetValue(jokerId, out var n) ? n : 1;
        }

        // Equip a joker. If stackable and already equipped, bump stack instead
        // of consuming a fresh slot. Returns true on success, false if all
        // slots are full and the joker isn't stackable-already.
        public bool TryEquip(JokerData joker)
        {
            if (joker == null) return false;
            if (joker.stackable && Has(joker.id))
            {
                int s = Stack(joker.id);
                if (s >= joker.maxStack) return false;
                stacks[joker.id] = s + 1;
                return true;
            }
            for (int i = 0; i < slots.Length; i++)
            {
                if (string.IsNullOrEmpty(slots[i]))
                {
                    slots[i] = joker.id;
                    if (joker.stackable) stacks[joker.id] = 1;
                    return true;
                }
            }
            return false;
        }

        public bool Remove(string jokerId)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == jokerId)
                {
                    slots[i] = null;
                    stacks.Remove(jokerId);
                    return true;
                }
            }
            return false;
        }

        public int FilledSlots()
        {
            int n = 0;
            for (int i = 0; i < slots.Length; i++) if (!string.IsNullOrEmpty(slots[i])) n++;
            return n;
        }
    }
}
