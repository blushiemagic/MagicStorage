using System;
using System.Collections.Generic;
using System.Linq;
using MagicStorageTM.Content;
using MagicStorageTM.Tiles;
using Terraria;
using Terraria.DataStructures;

namespace MagicStorageTM.Storage
{
    public enum Fullness { Empty = 0, Partial = 1, Full = 2 }

    /// <summary>One entry in the storage view: all stacks of the same item type + prefix, combined.</summary>
    public sealed class StoredEntry
    {
        public int Type;
        public int Prefix;
        public long Count;
        public Item Sample;   // representative item (stack 1) used for name, tooltip, sorting
        public bool Favorited;
    }

    /// <summary>Deposit / withdraw / count operations on a heart's storage (replaces TEStorageHeart).</summary>
    public static class StorageOps
    {
        public struct UnitRef
        {
            public Point16 Pos;
            public UnitData Data;
            public UnitTier Tier;
            public int Capacity => Tier?.Capacity ?? 0;
        }

        public static UnitTier TierAt(Point16 unitPos)
        {
            Tile t = Main.tile[unitPos.X, unitPos.Y];
            if (t == null || !t.active() || t.type != MSTiles.Unit.Type) return null;
            return UnitTier.ByRow(t.frameY / MSTiles.Unit.FrameHeight);
        }

        public static List<UnitRef> Units(Point16 heart, bool includeInactive = false)
        {
            var list = new List<UnitRef>();
            foreach (Point16 pos in Network.UnitsOf(heart))
            {
                UnitTier tier = TierAt(pos);
                if (tier == null) continue;
                UnitData data = StorageWorld.GetUnit(pos);
                if (data.Inactive && !includeInactive) continue;
                list.Add(new UnitRef { Pos = pos, Data = data, Tier = tier });
            }
            return list;
        }

        public static void GetCapacity(Point16 heart, out int used, out int total)
        {
            used = 0; total = 0;
            foreach (var u in Units(heart))
            {
                used += u.Data.Items.Count;
                total += u.Capacity;
            }
        }

        /// <summary>All stored items combined by (type, prefix).</summary>
        public static List<StoredEntry> Aggregate(Point16 heart)
        {
            var map = new Dictionary<long, StoredEntry>();
            lock (StorageWorld.Sync)
            {
                foreach (var u in Units(heart))
                {
                    foreach (Item item in u.Data.Items)
                    {
                        if (item == null || item.IsAir) continue;
                        long key = ((long)item.type << 16) | (uint)(item.prefix & 0xFFFF);
                        if (!map.TryGetValue(key, out var e))
                        {
                            var sample = item.Clone();
                            sample.stack = 1;
                            map[key] = e = new StoredEntry { Type = item.type, Prefix = item.prefix, Sample = sample };
                        }
                        e.Count += item.stack;
                        e.Favorited |= item.favorited;
                    }
                }
            }
            return map.Values.ToList();
        }

        /// <summary>Total of an item type across all prefixes (for crafting).</summary>
        public static Dictionary<int, long> CountByType(Point16 heart)
        {
            var counts = new Dictionary<int, long>();
            lock (StorageWorld.Sync)
            {
                foreach (var u in Units(heart))
                    foreach (Item item in u.Data.Items)
                        if (item != null && !item.IsAir)
                        {
                            counts.TryGetValue(item.type, out long c);
                            counts[item.type] = c + item.stack;
                        }
            }
            return counts;
        }

        private static bool SameStack(Item a, Item b) => a.type == b.type && a.prefix == b.prefix && a.maxStack > 1;

        /// <summary>
        /// Put <paramref name="item"/> into storage. Returns true if anything moved. The passed item's stack
        /// is reduced (and turned to air when fully deposited).
        /// </summary>
        public static bool Deposit(Point16 heart, Item item)
        {
            if (item == null || item.IsAir) return false;
            bool moved = false;
            var touched = new HashSet<Point16>();
            lock (StorageWorld.Sync)
            {
                var units = Units(heart);
                // 1) top up existing stacks
                if (item.maxStack > 1)
                {
                    foreach (var u in units)
                    {
                        foreach (Item s in u.Data.Items)
                        {
                            if (item.stack <= 0) break;
                            if (!SameStack(s, item) || s.stack >= s.maxStack) continue;
                            int take = Math.Min(item.stack, s.maxStack - s.stack);
                            s.stack += take;
                            item.stack -= take;
                            moved = true;
                            touched.Add(u.Pos);
                        }
                        if (item.stack <= 0) break;
                    }
                }
                // 2) new slots
                foreach (var u in units)
                {
                    while (item.stack > 0 && u.Data.Items.Count < u.Capacity)
                    {
                        Item copy = item.Clone();
                        copy.stack = Math.Min(item.stack, Math.Max(1, item.maxStack));
                        copy.favorited = false;
                        copy.newAndShiny = false;
                        u.Data.Items.Add(copy);
                        item.stack -= copy.stack;
                        moved = true;
                        touched.Add(u.Pos);
                    }
                    if (item.stack <= 0) break;
                }
                if (item.stack <= 0) item.TurnToAir();
            }
            foreach (var p in touched) RefreshUnitFrame(p);
            if (moved) StorageWorld.MarkDirty();
            return moved;
        }

        /// <summary>Take up to <paramref name="amount"/> of an item (exact prefix, or any prefix when prefix &lt; 0).</summary>
        public static Item Withdraw(Point16 heart, int type, int prefix, int amount, bool includeFavoritedLast = true)
        {
            var result = new Item();
            if (amount <= 0) return result;
            var touched = new HashSet<Point16>();
            lock (StorageWorld.Sync)
            {
                foreach (var u in Units(heart))
                {
                    var items = u.Data.Items;
                    for (int k = items.Count - 1; k >= 0 && amount > 0; k--)
                    {
                        Item s = items[k];
                        if (s == null || s.type != type || (prefix >= 0 && s.prefix != prefix)) continue;
                        if (result.IsAir)
                        {
                            result = s.Clone();
                            result.stack = 0;
                            result.favorited = false;
                        }
                        else if (result.prefix != s.prefix) continue;
                        int take = Math.Min(amount, s.stack);
                        s.stack -= take;
                        result.stack += take;
                        amount -= take;
                        if (s.stack <= 0) items.RemoveAt(k);
                        touched.Add(u.Pos);
                    }
                    if (amount <= 0) break;
                }
            }
            foreach (var p in touched) RefreshUnitFrame(p);
            if (touched.Count > 0) StorageWorld.MarkDirty();
            if (result.stack <= 0) result.TurnToAir();
            return result;
        }

        /// <summary>Remove <paramref name="amount"/> of a type (any prefix). Returns how many were actually removed.</summary>
        public static int Consume(Point16 heart, int type, int amount)
        {
            int removed = 0;
            while (amount > 0)
            {
                Item taken = Withdraw(heart, type, -1, amount);
                if (taken.IsAir) break;
                removed += taken.stack;
                amount -= taken.stack;
            }
            return removed;
        }

        public static Fullness FullnessOf(UnitData data, UnitTier tier)
        {
            if (data.Items.Count == 0) return Fullness.Empty;
            return data.Items.Count >= tier.Capacity ? Fullness.Full : Fullness.Partial;
        }

        /// <summary>Update the unit's sprite to show fullness / active state (MagicStorage frame layout).</summary>
        public static void RefreshUnitFrame(Point16 pos)
        {
            UnitTier tier = TierAt(pos);
            if (tier == null) return;
            UnitData data = StorageWorld.GetUnit(pos);
            int frameX = (int)FullnessOf(data, tier) * 36 + (data.Inactive ? 3 * 36 : 0);
            int frameY = tier.Row * 36;
            CustomTiles.SetObjectFrame(pos.X, pos.Y, MSTiles.Unit, frameX, frameY);
        }

        /// <summary>Move items from inactive units into active ones (MagicStorage "Storage Unit Wand" behaviour, run on toggle).</summary>
        public static void EvacuateInactive(Point16 heart)
        {
            foreach (var u in Units(heart, includeInactive: true).Where(u => u.Data.Inactive).ToList())
            {
                foreach (Item item in u.Data.Items.ToList())
                {
                    Item moving = item.Clone();
                    if (Deposit(heart, moving) && moving.IsAir)
                        u.Data.Items.Remove(item);
                    else if (!moving.IsAir)
                        item.stack = moving.stack;
                }
                RefreshUnitFrame(u.Pos);
            }
        }
    }
}
