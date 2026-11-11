using System.Collections.Generic;
using MagicStorageTM.Storage;
using MagicStorageTM.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace MagicStorageTM.Content
{
    /// <summary>All Magic Storage tiles. Registered in Initialize (before items, which need the tile types).</summary>
    public static class MSTiles
    {
        public static CustomTileDef Component, Heart, Unit, StorageAccess, CraftingAccess, RemoteAccess, Connector;
        public static CustomTileDef Combined1, Combined2, Combined3, Combined4, Furniture1, Furniture2;

        private static readonly Dictionary<int, NetKind> _kinds = new Dictionary<int, NetKind>();

        public static NetKind KindOf(int type) => _kinds.TryGetValue(type, out var k) ? k : NetKind.None;

        public static void Register()
        {
            Component = Member("StorageComponent", "Assets/Tiles/StorageComponent.png", NetKind.Component, "storage-component");
            Heart = Member("StorageHeart", "Assets/Tiles/StorageHeart.png", NetKind.Heart, "storage-heart");
            Unit = Member("StorageUnit", "Assets/Tiles/StorageUnit.png", NetKind.Unit, null);
            Unit.StyleMultiplier = 6;
            Unit.StyleWrapLimit = 6;
            Unit.GetDropItem = (fx, fy) => MSItems.TypeOf(UnitTier.ByRow(fy / 36)?.UnitItem);
            StorageAccess = Member("StorageAccess", "Assets/Tiles/StorageAccess.png", NetKind.StorageAccess, "storage-access");
            CraftingAccess = Member("CraftingAccess", "Assets/Tiles/CraftingAccess.png", NetKind.CraftingAccess, "crafting-access");
            RemoteAccess = Member("RemoteAccess", "Assets/Tiles/RemoteAccess.png", NetKind.RemoteAccess, "remote-access");

            Connector = new CustomTileDef
            {
                Name = "StorageConnector",
                Texture = "Assets/Tiles/StorageConnector.png",
                Width = 1, Height = 1, OriginX = 0, OriginY = 0,
                SolidTop = false,
                GetDropItem = (fx, fy) => MSItems.TypeOf("storage-connector"),
                HoverIcon = (x, y) => 0,
            };
            Connector.CustomFrame = FrameConnector;
            Connector.CanPlace = (x, y) => Network.CentersAfterPlacing(x, y, 1, 1, false) <= 1;
            Connector.OnPlaced = (x, y) => { Network.Invalidate(); ReframeConnectorsAround(x, y, 1, 1); };
            Connector.OnKilled = (x, y, fx, fy) => Network.Invalidate();
            CustomTiles.Register(Connector);
            _kinds[Connector.Type] = NetKind.Connector;

            // Combined crafting stations (3x3, MagicStorage tile sheets are 54x56: rows 16,16,18 px)
            int[] tier1 = { TileID.WorkBenches, TileID.Furnaces, TileID.Anvils, TileID.Bottles, TileID.Sawmill, TileID.Sinks, TileID.Loom, TileID.Tables, TileID.Tables2 };
            int[] tier2 = Concat(tier1, TileID.Hellforge, TileID.AlchemyTable, TileID.CookingPots, TileID.TinkerersWorkbench, TileID.DyeVat, TileID.HeavyWorkBench, TileID.Kegs, TileID.TeaKettle);
            int[] tier3 = Concat(tier2, TileID.ImbuingStation, TileID.MythrilAnvil, TileID.AdamantiteForge, TileID.Bookcases, TileID.CrystalBall, TileID.Blendomatic);
            int[] furniture1 = { TileID.BoneWelder, TileID.GlassKiln, TileID.HoneyDispenser, TileID.IceMachine, TileID.LivingLoom, TileID.SkyMill, TileID.Solidifier };
            int[] furniture2 = Concat(furniture1, TileID.MeatGrinder, TileID.LesionStation, TileID.FleshCloningVat, TileID.SteampunkBoiler, TileID.LihzahrdFurnace);
            int[] tier4 = Concat(Concat(tier3, furniture2), TileID.Autohammer, TileID.LunarCraftingStation);

            Combined1 = Station("CombinedStations1", "CombinedStations1Tile", "combined-stations-1", tier1, water: true);
            Combined2 = Station("CombinedStations2", "CombinedStations2Tile", "combined-stations-2", tier2, water: true);
            Combined3 = Station("CombinedStations3", "CombinedStations3Tile", "combined-stations-3", tier3, water: true);
            Combined4 = Station("CombinedStations4", "CombinedStations4Tile", "combined-stations-4", tier4, water: true, lava: true, honey: true);
            Furniture1 = Station("CombinedFurnitureStations1", "CombinedFurnitureStations1Tile", "combined-furniture-stations-1", furniture1);
            Furniture2 = Station("CombinedFurnitureStations2", "CombinedFurnitureStations2Tile", "combined-furniture-stations-2", furniture2);
        }

        private static CustomTileDef Member(string name, string texture, NetKind kind, string dropItem)
        {
            bool isCenter = kind == NetKind.Heart || kind == NetKind.RemoteAccess;
            var def = new CustomTileDef
            {
                Name = name,
                Texture = texture,
                Width = 2, Height = 2, OriginX = 1, OriginY = 1,
                SolidTop = true,
                RequiresFloor = false,
                DustType = 7,
            };
            if (dropItem != null) def.GetDropItem = (fx, fy) => MSItems.TypeOf(dropItem);
            // Runs every frame during placement preview, so it must stay silent (the preview turns red instead).
            def.CanPlace = (x, y) => Network.CentersAfterPlacing(x, y, 2, 2, isCenter) <= 1;
            def.OnPlaced = (x, y) => Interactions.OnPlaced(kind, new Point16(x, y));
            def.CanKill = (x, y) => Interactions.CanBreak(kind, new Point16(x, y));
            def.OnKilled = (x, y, fx, fy) => Interactions.OnKilled(kind, new Point16(x, y));
            def.OnRightClick = (x, y) => Interactions.RightClick(kind, new Point16(x, y));
            def.HoverIcon = (x, y) =>
            {
                Tile t = Main.tile[x, y];
                return def.GetDropItem?.Invoke(t.frameX, t.frameY) ?? 0;
            };
            def.HoverText = (x, y) => Interactions.HoverText(kind, new Point16(x, y));
            CustomTiles.Register(def);
            _kinds[def.Type] = kind;
            return def;
        }

        private static CustomTileDef Station(string name, string texture, string item, int[] countsAs, bool water = false, bool lava = false, bool honey = false)
        {
            var def = new CustomTileDef
            {
                Name = name,
                Texture = "Assets/Tiles/" + texture + ".png",
                Width = 3, Height = 3, OriginX = 1, OriginY = 2,
                CoordinateHeights = new[] { 16, 16, 18 },
                SolidTop = false,
                RequiresFloor = true,
                StyleWrapLimit = 36,
                CountsAsTiles = countsAs,
                CountsAsWater = water,
                CountsAsLava = lava,
                CountsAsHoney = honey,
                MapColorFrom = TileID.WorkBenches,
                GetDropItem = (fx, fy) => MSItems.TypeOf(item),
            };
            def.HoverIcon = (x, y) => MSItems.TypeOf(item);
            return CustomTiles.Register(def);
        }

        private static int[] Concat(int[] a, params int[] b)
        {
            var list = new List<int>(a);
            foreach (int t in b) if (!list.Contains(t)) list.Add(t);
            return list.ToArray();
        }

        // ── Storage Connector framing (MagicStorage: frameX += 18 left, 36 right; frameY += 18 up, 36 down) ──

        private static bool ConnectsTo(int i, int j)
        {
            NetKind k = Network.KindAt(i, j);
            return k != NetKind.None;
        }

        private static void FrameConnector(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            int fx = 0, fy = 0;
            if (ConnectsTo(i - 1, j)) fx += 18;
            if (ConnectsTo(i + 1, j)) fx += 36;
            if (ConnectsTo(i, j - 1)) fy += 18;
            if (ConnectsTo(i, j + 1)) fy += 36;
            tile.frameX = (short)fx;
            tile.frameY = (short)fy;
        }

        public static void ReframeConnectorsAround(int x, int y, int w, int h)
        {
            foreach (Point16 n in Network.EdgeNeighbors(x, y, w, h))
            {
                if (!WorldGen.InWorld(n.X, n.Y)) continue;
                Tile t = Main.tile[n.X, n.Y];
                if (t != null && t.active() && t.type == Connector.Type) FrameConnector(n.X, n.Y);
            }
            if (w == 1 && h == 1 && WorldGen.InWorld(x, y) && Main.tile[x, y].active() && Main.tile[x, y].type == Connector.Type)
                FrameConnector(x, y);
        }
    }
}
