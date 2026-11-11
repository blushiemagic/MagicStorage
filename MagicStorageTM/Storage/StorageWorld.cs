using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using TerrariaModder.Core.Assets;

namespace MagicStorageTM.Storage
{
    /// <summary>An item that could not be resolved on load (its mod is missing). Kept verbatim so it is never lost.</summary>
    public sealed class RawItem
    {
        public string Id;
        public int Stack;
        public byte Prefix;
        public bool Favorited;
    }

    public sealed class UnitData
    {
        public bool Inactive;
        public readonly List<Item> Items = new List<Item>();
        public readonly List<RawItem> Unloaded = new List<RawItem>();
    }

    public sealed class HeartData
    {
        public string Name = "";
    }

    public sealed class CraftingAccessData
    {
        public const int MaxStations = 45; // MagicStorage: 3 rows x 15
        public readonly List<Item> Stations = new List<Item>();
        public readonly List<RawItem> Unloaded = new List<RawItem>();
    }

    public sealed class RemoteAccessData
    {
        public Point16 Heart = Point16.NegativeOne;
    }

    /// <summary>What a player's Locator / Portable Access is linked to in the current world.</summary>
    public sealed class PlayerBindings
    {
        public Point16 StorageHeart = Point16.NegativeOne;
        public Point16 CraftingAccess = Point16.NegativeOne;
    }

    /// <summary>
    /// All per-world Magic Storage state (the tile-entity replacement). Saved in the world sidecar.
    /// Every mutation happens on the main thread; <see cref="Sync"/> guards against the background
    /// autosave reading while we write.
    /// </summary>
    public static class StorageWorld
    {
        public static readonly object Sync = new object();
        private const int DataVersion = 1;

        public static readonly Dictionary<Point16, UnitData> Units = new Dictionary<Point16, UnitData>();
        public static readonly Dictionary<Point16, HeartData> Hearts = new Dictionary<Point16, HeartData>();
        public static readonly Dictionary<Point16, CraftingAccessData> CraftingAccesses = new Dictionary<Point16, CraftingAccessData>();
        public static readonly Dictionary<Point16, RemoteAccessData> RemoteAccesses = new Dictionary<Point16, RemoteAccessData>();
        public static readonly Dictionary<string, PlayerBindings> Bindings = new Dictionary<string, PlayerBindings>(StringComparer.Ordinal);

        /// <summary>Unit data whose tiles disappeared (e.g. a partial restore). Kept so items are never deleted.</summary>
        public static readonly List<UnitData> Orphans = new List<UnitData>();

        /// <summary>Bosses whose first-kill Shadow Diamond has already dropped in this world.</summary>
        public static readonly HashSet<int> DiamondBosses = new HashSet<int>();
        public static int RadiantJewelFailures;

        /// <summary>Bumped on every change so open UIs know to refresh.</summary>
        public static int Version { get; private set; }

        public static void MarkDirty() => Version++;

        public static PlayerBindings GetBindings(Player player)
        {
            string key = player?.name ?? "";
            lock (Sync)
            {
                if (!Bindings.TryGetValue(key, out var b)) Bindings[key] = b = new PlayerBindings();
                return b;
            }
        }

        public static UnitData GetUnit(Point16 pos, bool create = true)
        {
            lock (Sync)
            {
                if (Units.TryGetValue(pos, out var u)) return u;
                if (!create) return null;
                return Units[pos] = new UnitData();
            }
        }

        public static HeartData GetHeart(Point16 pos)
        {
            lock (Sync)
            {
                if (!Hearts.TryGetValue(pos, out var h)) Hearts[pos] = h = new HeartData();
                return h;
            }
        }

        public static CraftingAccessData GetCraftingAccess(Point16 pos)
        {
            lock (Sync)
            {
                if (!CraftingAccesses.TryGetValue(pos, out var c)) CraftingAccesses[pos] = c = new CraftingAccessData();
                return c;
            }
        }

        public static RemoteAccessData GetRemoteAccess(Point16 pos)
        {
            lock (Sync)
            {
                if (!RemoteAccesses.TryGetValue(pos, out var r)) RemoteAccesses[pos] = r = new RemoteAccessData();
                return r;
            }
        }

        // ───────────── persistence ─────────────

        public static void Reset()
        {
            lock (Sync)
            {
                Units.Clear();
                Hearts.Clear();
                CraftingAccesses.Clear();
                RemoteAccesses.Clear();
                Bindings.Clear();
                Orphans.Clear();
                DiamondBosses.Clear();
                RadiantJewelFailures = 0;
                Version++;
            }
        }

        public static byte[] Save()
        {
            lock (Sync)
            {
                using (var ms = new MemoryStream())
                using (var w = new BinaryWriter(ms))
                {
                    w.Write(DataVersion);

                    w.Write(Units.Count + Orphans.Count);
                    foreach (var kv in Units) WriteUnit(w, kv.Key, kv.Value);
                    foreach (var orphan in Orphans) WriteUnit(w, Point16.NegativeOne, orphan);

                    w.Write(Hearts.Count);
                    foreach (var kv in Hearts) { WritePoint(w, kv.Key); w.Write(kv.Value.Name ?? ""); }

                    w.Write(CraftingAccesses.Count);
                    foreach (var kv in CraftingAccesses)
                    {
                        WritePoint(w, kv.Key);
                        WriteItems(w, kv.Value.Stations, kv.Value.Unloaded);
                    }

                    w.Write(RemoteAccesses.Count);
                    foreach (var kv in RemoteAccesses) { WritePoint(w, kv.Key); WritePoint(w, kv.Value.Heart); }

                    w.Write(Bindings.Count);
                    foreach (var kv in Bindings)
                    {
                        w.Write(kv.Key);
                        WritePoint(w, kv.Value.StorageHeart);
                        WritePoint(w, kv.Value.CraftingAccess);
                    }

                    w.Write(DiamondBosses.Count);
                    foreach (int t in DiamondBosses) w.Write(t);
                    w.Write(RadiantJewelFailures);

                    w.Flush();
                    return ms.ToArray();
                }
            }
        }

        public static void Load(byte[] data)
        {
            lock (Sync)
            {
                using (var ms = new MemoryStream(data))
                using (var r = new BinaryReader(ms))
                {
                    int version = r.ReadInt32();
                    if (version > DataVersion) throw new InvalidDataException($"Storage data version {version} is newer than this mod");

                    int units = r.ReadInt32();
                    for (int k = 0; k < units; k++)
                    {
                        Point16 pos = ReadPoint(r);
                        var u = new UnitData { Inactive = r.ReadBoolean() };
                        ReadItems(r, u.Items, u.Unloaded);
                        if (pos.X < 0) Orphans.Add(u);
                        else Units[pos] = u;
                    }

                    int hearts = r.ReadInt32();
                    for (int k = 0; k < hearts; k++) Hearts[ReadPoint(r)] = new HeartData { Name = r.ReadString() };

                    int crafting = r.ReadInt32();
                    for (int k = 0; k < crafting; k++)
                    {
                        Point16 pos = ReadPoint(r);
                        var c = new CraftingAccessData();
                        ReadItems(r, c.Stations, c.Unloaded);
                        CraftingAccesses[pos] = c;
                    }

                    int remotes = r.ReadInt32();
                    for (int k = 0; k < remotes; k++) RemoteAccesses[ReadPoint(r)] = new RemoteAccessData { Heart = ReadPoint(r) };

                    int bindings = r.ReadInt32();
                    for (int k = 0; k < bindings; k++)
                    {
                        string name = r.ReadString();
                        Bindings[name] = new PlayerBindings { StorageHeart = ReadPoint(r), CraftingAccess = ReadPoint(r) };
                    }

                    int bosses = r.ReadInt32();
                    for (int k = 0; k < bosses; k++) DiamondBosses.Add(r.ReadInt32());
                    RadiantJewelFailures = r.ReadInt32();
                }
                Version++;
            }
        }

        private static void WriteUnit(BinaryWriter w, Point16 pos, UnitData u)
        {
            WritePoint(w, pos);
            w.Write(u.Inactive);
            WriteItems(w, u.Items, u.Unloaded);
        }

        private static void WriteItems(BinaryWriter w, List<Item> items, List<RawItem> unloaded)
        {
            var live = items.Where(i => i != null && !i.IsAir).ToList();
            int unloadedCount = unloaded?.Count ?? 0;
            w.Write(live.Count + unloadedCount);
            foreach (Item item in live)
            {
                w.Write(ItemRegistry.GetPersistentId(item.type) ?? ("vanilla:" + item.type));
                w.Write(item.stack);
                w.Write((byte)item.prefix);
                w.Write(item.favorited);
            }
            if (unloaded != null)
                foreach (RawItem raw in unloaded)
                {
                    w.Write(raw.Id);
                    w.Write(raw.Stack);
                    w.Write(raw.Prefix);
                    w.Write(raw.Favorited);
                }
        }

        private static void ReadItems(BinaryReader r, List<Item> into, List<RawItem> unloaded)
        {
            int n = r.ReadInt32();
            for (int k = 0; k < n; k++)
            {
                string id = r.ReadString();
                int stack = r.ReadInt32();
                byte prefix = r.ReadByte();
                bool fav = r.ReadBoolean();
                int type = ItemRegistry.ResolvePersistentId(id);
                if (type <= 0 || stack <= 0)
                {
                    if (stack > 0) unloaded?.Add(new RawItem { Id = id, Stack = stack, Prefix = prefix, Favorited = fav });
                    continue;
                }
                into.Add(MakeItem(type, stack, prefix, fav));
            }
        }

        public static Item MakeItem(int type, int stack, int prefix = 0, bool favorited = false)
        {
            var item = new Item();
            item.SetDefaults(type);
            item.stack = stack;
            if (prefix > 0) item.Prefix(prefix);
            item.favorited = favorited;
            return item;
        }

        private static void WritePoint(BinaryWriter w, Point16 p) { w.Write(p.X); w.Write(p.Y); }
        private static Point16 ReadPoint(BinaryReader r) => new Point16(r.ReadInt16(), r.ReadInt16());
    }
}
