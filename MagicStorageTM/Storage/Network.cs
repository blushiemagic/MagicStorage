using System.Collections.Generic;
using System.Linq;
using MagicStorageTM.Content;
using MagicStorageTM.Tiles;
using Terraria;
using Terraria.DataStructures;

namespace MagicStorageTM.Storage
{
    public enum NetKind { None, Component, Heart, Unit, StorageAccess, CraftingAccess, RemoteAccess, Connector }

    /// <summary>Result of flood-filling one connected group of Magic Storage tiles.</summary>
    public sealed class NetworkScan
    {
        public readonly List<Point16> Members = new List<Point16>();
        public readonly List<Point16> Hearts = new List<Point16>();
        public readonly List<Point16> Remotes = new List<Point16>();
        public readonly List<Point16> Units = new List<Point16>();
        public readonly List<Point16> CraftingAccesses = new List<Point16>();

        public int CenterCount => Hearts.Count + Remotes.Count;
        public Point16 Center => Hearts.Count > 0 ? Hearts[0] : Remotes.Count > 0 ? Remotes[0] : Point16.NegativeOne;
    }

    /// <summary>
    /// Network topology, replacing MagicStorage's TileNetworkScanner. Two pieces are connected when their
    /// tiles touch edge-to-edge (corners don't count); Storage Connectors bridge gaps. Each network may hold
    /// at most one "center" (Storage Heart or Remote Storage Access).
    /// </summary>
    public static class Network
    {
        private static readonly Dictionary<Point16, NetworkScan> _cache = new Dictionary<Point16, NetworkScan>();

        public static void Invalidate()
        {
            _cache.Clear();
            StorageWorld.MarkDirty();
        }

        public static NetKind KindOf(int tileType) => MSTiles.KindOf(tileType);

        public static NetKind KindAt(int i, int j)
        {
            if (!WorldGen.InWorld(i, j)) return NetKind.None;
            Tile t = Main.tile[i, j];
            return t != null && t.active() ? KindOf(t.type) : NetKind.None;
        }

        /// <summary>Top-left of the network member covering (i, j), if any.</summary>
        public static bool TryGetMember(int i, int j, out Point16 topLeft, out NetKind kind)
        {
            topLeft = Point16.NegativeOne;
            kind = KindAt(i, j);
            if (kind == NetKind.None) return false;
            if (!CustomTiles.TryGetTopLeft(i, j, out int x, out int y, out _)) return false;
            topLeft = new Point16(x, y);
            return true;
        }

        public static NetworkScan Scan(Point16 start)
        {
            if (_cache.TryGetValue(start, out var cached)) return cached;
            var scan = new NetworkScan();
            if (!TryGetMember(start.X, start.Y, out Point16 first, out _)) return scan;

            var seen = new HashSet<Point16> { first };
            var queue = new Queue<Point16>();
            queue.Enqueue(first);
            while (queue.Count > 0)
            {
                Point16 p = queue.Dequeue();
                Tile t = Main.tile[p.X, p.Y];
                NetKind kind = KindOf(t.type);
                CustomTileDef def = CustomTiles.Get(t.type);
                scan.Members.Add(p);
                switch (kind)
                {
                    case NetKind.Heart: scan.Hearts.Add(p); break;
                    case NetKind.RemoteAccess: scan.Remotes.Add(p); break;
                    case NetKind.Unit: scan.Units.Add(p); break;
                    case NetKind.CraftingAccess: scan.CraftingAccesses.Add(p); break;
                }
                foreach (Point16 n in EdgeNeighbors(p.X, p.Y, def.Width, def.Height))
                {
                    if (!TryGetMember(n.X, n.Y, out Point16 member, out _)) continue;
                    if (seen.Add(member)) queue.Enqueue(member);
                }
            }
            foreach (Point16 m in scan.Members) _cache[m] = scan;
            return scan;
        }

        public static IEnumerable<Point16> EdgeNeighbors(int x, int y, int w, int h)
        {
            for (int dy = 0; dy < h; dy++)
            {
                yield return new Point16(x - 1, y + dy);
                yield return new Point16(x + w, y + dy);
            }
            for (int dx = 0; dx < w; dx++)
            {
                yield return new Point16(x + dx, y - 1);
                yield return new Point16(x + dx, y + h);
            }
        }

        /// <summary>
        /// How many centers would end up in one network if an object of size w×h were placed at (x, y).
        /// Used to refuse placements that would join two hearts.
        /// </summary>
        public static int CentersAfterPlacing(int x, int y, int w, int h, bool placingCenter)
        {
            var centers = new HashSet<Point16>();
            foreach (Point16 n in EdgeNeighbors(x, y, w, h))
            {
                if (!TryGetMember(n.X, n.Y, out Point16 member, out _)) continue;
                NetworkScan scan = Scan(member);
                foreach (var c in scan.Hearts) centers.Add(c);
                foreach (var c in scan.Remotes) centers.Add(c);
            }
            return centers.Count + (placingCenter ? 1 : 0);
        }

        /// <summary>The Storage Heart a member (access, unit, remote...) ultimately belongs to, or (-1,-1).</summary>
        public static Point16 ResolveHeart(Point16 member)
        {
            NetworkScan scan = Scan(member);
            if (scan.Hearts.Count > 0) return scan.Hearts[0];
            if (scan.Remotes.Count > 0)
            {
                var remote = StorageWorld.RemoteAccesses.TryGetValue(scan.Remotes[0], out var data) ? data : null;
                if (remote != null && IsHeart(remote.Heart)) return remote.Heart;
            }
            return Point16.NegativeOne;
        }

        public static bool IsHeart(Point16 p) => p.X >= 0 && KindAt(p.X, p.Y) == NetKind.Heart
            && CustomTiles.TryGetTopLeft(p.X, p.Y, out int x, out int y, out _) && x == p.X && y == p.Y;

        public static bool IsKind(Point16 p, NetKind kind) => p.X >= 0 && KindAt(p.X, p.Y) == kind
            && CustomTiles.TryGetTopLeft(p.X, p.Y, out int x, out int y, out _) && x == p.X && y == p.Y;

        /// <summary>Local networks served by a heart: its own plus every Remote Access linked to it.</summary>
        public static List<NetworkScan> NetworksOf(Point16 heart)
        {
            var result = new List<NetworkScan>();
            if (!IsHeart(heart)) return result;
            var own = Scan(heart);
            result.Add(own);
            foreach (var kv in StorageWorld.RemoteAccesses.ToList())
            {
                if (kv.Value.Heart != heart || !IsKind(kv.Key, NetKind.RemoteAccess)) continue;
                var scan = Scan(kv.Key);
                if (!result.Contains(scan)) result.Add(scan);
            }
            return result;
        }

        public static List<Point16> UnitsOf(Point16 heart) => NetworksOf(heart).SelectMany(s => s.Units).Distinct().ToList();

        public static List<Point16> CraftingAccessesOf(Point16 heart) => NetworksOf(heart).SelectMany(s => s.CraftingAccesses).Distinct().ToList();
    }
}
