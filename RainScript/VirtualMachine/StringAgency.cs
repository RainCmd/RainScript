using System;

namespace RainScript.VirtualMachine
{
    internal class StringAgency : IDisposable
    {
        private static readonly uint[] primes =
            {
            3, 7, 11, 0x11, 0x17, 0x1d, 0x25, 0x2f, 0x3b, 0x47, 0x59, 0x6b, 0x83, 0xa3, 0xc5, 0xef,
            0x125, 0x161, 0x1af, 0x209, 0x277, 0x2f9, 0x397, 0x44f, 0x52f, 0x63d, 0x78b, 0x91d, 0xaf1, 0xd2b, 0xfd1, 0x12fd,
            0x16cf, 0x1b65, 0x20e3, 0x2777, 0x2f6f, 0x38ff, 0x446f, 0x521f, 0x628d, 0x7655, 0x8e01, 0xaa6b, 0xcc89, 0xf583, 0x126a7, 0x1619b,
            0x1a857, 0x1fd3b, 0x26315, 0x2dd67, 0x3701b, 0x42023, 0x4f361, 0x5f0ed, 0x72125, 0x88e31, 0xa443b, 0xc51eb, 0xec8c1, 0x11bdbf, 0x154a3f, 0x198c4f,
            0x1ea867, 0x24ca19, 0x2c25c1, 0x34fa1b, 0x3f928f, 0x4c4987, 0x5b8b6f, 0x6dda89
            };
        private static uint GetPrimes(uint min)
        {
            for (uint i = 0; i < 72; i++) if (primes[i] > min) return primes[i];
            return min;
        }
        private struct Slot
        {
            public string value;
            public uint refernce;
            public uint next;
        }
        private uint[] buckets;
        private uint slotTop, freeSlot;
        private Slot[] slots;
        public StringAgency()
        {
            slotTop = 1;
            freeSlot = 0;
            slots = new Slot[32];
            TryResize();
        }
        private bool TryResize()
        {
            var nbs = GetPrimes(slotTop * 2);
            if (buckets == null || buckets.Length < nbs)
            {
                buckets = new uint[nbs];
                for (uint i = 1; i < slotTop; i++)
                {
                    var idx = (uint)slots[i].value.GetHashCode() % nbs;
                    slots[i].next = buckets[idx];
                    buckets[idx] = i;
                }
                return true;
            }
            return false;
        }
        public uint Add(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            var bidx = (uint)value.GetHashCode() % buckets.Length;
            var sidx = buckets[bidx];
            while (sidx > 0)
            {
                var slot = slots[sidx];
                if (slot.value == value) return sidx;
                else sidx = slot.next;
            }
            if (freeSlot > 0)
            {
                sidx = freeSlot;
                freeSlot = slots[sidx].next;
            }
            else
            {
                if (TryResize()) bidx = (uint)value.GetHashCode() % buckets.Length;
                if (slotTop == slots.Length)
                {
                    var newSlots = new Slot[slotTop << 1];
                    Array.Copy(slots, 0, newSlots, 0, slotTop);
                    slots = newSlots;
                }
                sidx = slotTop++;
            }
            slots[sidx].value = value;
            slots[sidx].refernce = 0;
            slots[sidx].next = buckets[bidx];
            buckets[bidx] = sidx;
            return sidx;
        }
        public void Reference(uint value)
        {
            if (value > 0 && value < slotTop) slots[value].refernce++;
        }
        public void Release(uint value)
        {
            if (value > 0 && value < slotTop)
            {
                slots[value].refernce--;
                if (slots[value].refernce == 0)
                {
                    var idx = (uint)slots[value].value.GetHashCode() % buckets.Length;
                    if (buckets[idx] == value) buckets[idx] = slots[value].next;
                    else
                    {
                        var sidx = buckets[idx];
                        while (slots[sidx].next != value) sidx = slots[sidx].next;
                        slots[sidx].next = slots[value].next;
                    }
                    slots[value].next = freeSlot;
                    slots[value].value = "";
                    freeSlot = value;
                }
            }
        }
        public string Get(uint value)
        {
            if (value > 0 && value < slotTop) return slots[value].value;
            return "";
        }

        public uint GetStringCount()
        {
            var count = slotTop;
            var index = freeSlot;
            while (index > 0)
            {
                count--;
                index = slots[index].next;
            }
            return count;
        }
        public bool IsEquals(uint a, uint b)
        {
            if (a == b) return true;
            else if (a >= slotTop && b >= slotTop) return true;
            else return slots[a].value == slots[b].value;
        }
        public void Dispose()
        {

        }
    }
}
