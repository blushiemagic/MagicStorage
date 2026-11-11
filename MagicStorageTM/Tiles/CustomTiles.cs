using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Map;
using Terraria.ObjectData;

namespace MagicStorageTM.Tiles
{
    /// <summary>
    /// Minimal custom-tile system for vanilla Terraria 1.4.5.8.
    ///
    /// Types are assigned at TileID.Count and above. TileID.Count itself is never changed
    /// (vanilla loops over 0..Count-1 load textures and write world/map headers); instead every
    /// array sized TileID.Count is grown so our types index safely. World files stay vanilla:
    /// see <see cref="WorldSaveLayer"/>.
    /// </summary>
    public static class CustomTiles
    {
        private static readonly List<CustomTileDef> _defs = new List<CustomTileDef>();
        private static readonly Dictionary<string, CustomTileDef> _byName = new Dictionary<string, CustomTileDef>(StringComparer.Ordinal);

        public static int VanillaCount { get; private set; } = -1;
        public static int ExtendedCount => VanillaCount + _defs.Count;
        public static bool SetupDone { get; private set; }
        public static IReadOnlyList<CustomTileDef> All => _defs;

        /// <summary>False when the world save layer failed to install: placing our tiles would corrupt the .wld.</summary>
        public static bool PlacementAllowed { get; set; } = true;

        internal static string ModFolder;

        public static CustomTileDef Register(CustomTileDef def)
        {
            if (SetupDone) throw new InvalidOperationException("Custom tiles must be registered before setup.");
            if (string.IsNullOrEmpty(def.Name)) throw new ArgumentException("Tile needs a Name");
            if (VanillaCount < 0) VanillaCount = TileID.Count;
            def.Type = VanillaCount + _defs.Count;
            _defs.Add(def);
            _byName[def.Name] = def;
            return def;
        }

        public static bool IsCustom(int type) => VanillaCount >= 0 && type >= VanillaCount && type < VanillaCount + _defs.Count;

        public static CustomTileDef Get(int type) => IsCustom(type) ? _defs[type - VanillaCount] : null;

        public static CustomTileDef GetByName(string name) => name != null && _byName.TryGetValue(name, out var d) ? d : null;

        /// <summary>Resolve the top-left tile of the custom object covering (i, j).</summary>
        public static bool TryGetTopLeft(int i, int j, out int x, out int y, out CustomTileDef def)
        {
            x = i; y = j; def = null;
            if (!WorldGen.InWorld(i, j)) return false;
            Tile tile = Main.tile[i, j];
            if (tile == null || !tile.active()) return false;
            def = Get(tile.type);
            if (def == null) return false;
            int localX = tile.frameX % def.FrameWidth;
            int localY = tile.frameY % def.FrameHeight;
            x = i - localX / 18;
            y = j - def.RowFromFrameY(localY);
            return true;
        }

        /// <summary>Frame of the top-left tile of the style that contains (frameX, frameY).</summary>
        public static void GetStyleOrigin(CustomTileDef def, int frameX, int frameY, out int originFrameX, out int originFrameY)
        {
            originFrameX = frameX - frameX % def.FrameWidth;
            originFrameY = frameY - frameY % def.FrameHeight;
        }

        /// <summary>Rewrite all tiles of an object to use the style whose top-left frame is (frameX, frameY).</summary>
        public static void SetObjectFrame(int x, int y, CustomTileDef def, int frameX, int frameY)
        {
            for (int dx = 0; dx < def.Width; dx++)
            {
                for (int dy = 0; dy < def.Height; dy++)
                {
                    Tile t = Main.tile[x + dx, y + dy];
                    if (t == null || !t.active() || t.type != def.Type) continue;
                    t.frameX = (short)(frameX + dx * 18);
                    t.frameY = (short)(frameY + def.RowOffset(dy));
                }
            }
        }

        // ───────────────────────────── Setup ─────────────────────────────

        /// <summary>
        /// Runs once, right after vanilla's Main.Initialize_AlmostEverything (TileObjectData, MapHelper
        /// and all tile sets are initialized by then). Must run on the main thread (creates textures).
        /// </summary>
        public static void Setup()
        {
            if (SetupDone || _defs.Count == 0) return;
            try
            {
                Log.Info($"[Tiles] Setting up {_defs.Count} custom tiles (types {VanillaCount}..{ExtendedCount - 1})");
                var map = new Dictionary<Array, Array>(ReferenceComparer.Instance);
                int statics = ArrayResizer.ResizeStaticArrays(typeof(Main).Assembly, VanillaCount, ExtendedCount, map);
                int instances = ArrayResizer.ResizeKnownInstances(VanillaCount, ExtendedCount, map);
                Log.Info($"[Tiles] Resized {statics} static and {instances} instance arrays");

                ApplyProperties();
                RegisterObjectData();
                SetupMap();
                LoadTextures();
                SetupDone = true;
                Log.Info("[Tiles] Custom tile setup complete");
            }
            catch (Exception ex)
            {
                Log.Error("[Tiles] Custom tile setup FAILED — Magic Storage blocks will not work", ex);
            }
        }

        private static void ApplyProperties()
        {
            foreach (var def in _defs)
            {
                int t = def.Type;
                Main.tileFrameImportant[t] = true;
                Main.tileSolid[t] = false;
                Main.tileSolidTop[t] = def.SolidTop;
                Main.tileTable[t] = false;
                Main.tileLavaDeath[t] = def.LavaDeath;
                Main.tileWaterDeath[t] = false;
                Main.tileNoAttach[t] = true;
                Main.tileLighted[t] = false;
                Main.tileBlockLight[t] = false;
                Main.tileCut[t] = false;
                Main.tileGlowMask[t] = -1;
                Main.tileContainer[t] = false;
                Main.tileSign[t] = false;
                Main.tileShine[t] = 0;
                Main.tileFrame[t] = 0;
                TileID.Sets.DisableSmartCursor[t] = true;
                TileID.Sets.HasOutlines[t] = false;
                TileID.Sets.CountsAsWaterForCrafting[t] = def.CountsAsWater;
                if (Main.tileMerge[t] == null || Main.tileMerge[t].Length < ExtendedCount)
                    Main.tileMerge[t] = new bool[ExtendedCount];

                Recipe.TileCountsAs[t] = def.CountsAsTiles != null && def.CountsAsTiles.Length > 0
                    ? new List<int>(def.CountsAsTiles)
                    : null;
            }
        }

        private static void RegisterObjectData()
        {
            const BindingFlags S = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            Type tod = typeof(TileObjectData);
            FieldInfo fReadOnly = tod.GetField("readOnlyData", S);
            FieldInfo fNewTile = tod.GetField("newTile", S);
            FieldInfo fNewAlternate = tod.GetField("newAlternate", S);
            FieldInfo fData = tod.GetField("_data", S);
            MethodInfo mAddTile = tod.GetMethod("addTile", S, null, new[] { typeof(int) }, null);
            MethodInfo mAddAlternate = tod.GetMethod("addAlternate", S, null, new[] { typeof(int) }, null);
            if (fReadOnly == null || fNewTile == null || fNewAlternate == null || fData == null || mAddTile == null || mAddAlternate == null)
                throw new MissingMemberException("TileObjectData internals changed; cannot register custom tiles");

            var data = (List<TileObjectData>)fData.GetValue(null);
            while (data.Count < ExtendedCount) data.Add(null);

            TileObjectData style1x1 = (TileObjectData)tod.GetField("Style1x1", S).GetValue(null);
            TileObjectData style2x2 = (TileObjectData)tod.GetField("Style2x2", S).GetValue(null);
            TileObjectData style3x3 = (TileObjectData)tod.GetField("Style3x3", S).GetValue(null);

            bool wasReadOnly = (bool)fReadOnly.GetValue(null);
            fReadOnly.SetValue(null, false);
            try
            {
                foreach (var def in _defs)
                {
                    var nt = (TileObjectData)fNewTile.GetValue(null);
                    TileObjectData baseStyle = def.Width == 1 && def.Height == 1 ? style1x1
                        : def.Width == 3 && def.Height == 3 ? style3x3 : style2x2;
                    nt.CopyFrom(baseStyle);
                    nt.Width = def.Width;
                    nt.Height = def.Height;
                    nt.Origin = new Point16(def.OriginX, def.OriginY);
                    nt.CoordinateWidth = 16;
                    nt.CoordinatePadding = 2;
                    nt.CoordinateHeights = def.CoordinateHeights ?? Enumerable.Repeat(16, def.Height).ToArray();
                    nt.StyleHorizontal = def.StyleHorizontal;
                    nt.StyleMultiplier = Math.Max(1, def.StyleMultiplier);
                    nt.StyleWrapLimit = def.StyleWrapLimit;
                    nt.LavaDeath = def.LavaDeath;
                    nt.UsesCustomCanPlace = true;
                    nt.AnchorTop = AnchorData.Empty;
                    nt.AnchorLeft = AnchorData.Empty;
                    nt.AnchorRight = AnchorData.Empty;
                    nt.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide | AnchorType.Table, def.Width, 0);
                    nt.HookCheckIfCanPlace = new PlacementHook(CanPlaceHook, -1, 0, false);
                    nt.HookPostPlaceMyPlayer = new PlacementHook(PostPlaceHook, -1, 0, true);

                    if (!def.RequiresFloor)
                    {
                        var na = (TileObjectData)fNewAlternate.GetValue(null);
                        na.CopyFrom(nt);
                        na.AnchorBottom = AnchorData.Empty;
                        mAddAlternate.Invoke(null, new object[] { 0 });
                    }

                    mAddTile.Invoke(null, new object[] { def.Type });
                }
            }
            finally
            {
                fReadOnly.SetValue(null, wasReadOnly);
            }
        }

        private static int CanPlaceHook(int x, int y, int type, int style, int dir, int alternate)
        {
            var def = Get(type);
            if (def == null) return 0;
            // Singleplayer only: the server would not know our tile types.
            if (Main.netMode != 0 || !PlacementAllowed || !SetupDone) return -1;
            try
            {
                if (def.CanPlace != null && !def.CanPlace(x - def.OriginX, y - def.OriginY))
                    return -1;
            }
            catch (Exception ex) { Log.Error($"[Tiles] CanPlace({def.Name}) threw", ex); return -1; }
            return 0;
        }

        private static int PostPlaceHook(int x, int y, int type, int style, int dir, int alternate)
        {
            var def = Get(type);
            if (def == null) return 0;
            try
            {
                if (!TryGetTopLeft(x, y, out int tx, out int ty, out _)) { tx = x; ty = y; }
                def.OnPlaced?.Invoke(tx, ty);
                TileEvents.RaiseChanged(tx, ty);
            }
            catch (Exception ex) { Log.Error($"[Tiles] OnPlaced({def.Name}) threw", ex); }
            return 0;
        }

        private static void SetupMap()
        {
            if (MapHelper.tileLookup == null) return;
            foreach (var def in _defs)
            {
                if (def.Type < MapHelper.tileLookup.Length)
                    MapHelper.tileLookup[def.Type] = MapHelper.tileLookup[def.MapColorFrom];
                if (MapHelper.tileOptionCounts != null && def.Type < MapHelper.tileOptionCounts.Length)
                    MapHelper.tileOptionCounts[def.Type] = 1;
            }
        }

        private static void LoadTextures()
        {
            if (Main.dedServ) return;
            var gd = Main.instance?.GraphicsDevice;
            if (gd == null) { Log.Warn("[Tiles] No GraphicsDevice yet; tile textures not loaded"); return; }
            foreach (var def in _defs)
            {
                if (string.IsNullOrEmpty(def.Texture)) continue;
                string path = Path.Combine(ModFolder ?? "", def.Texture);
                Texture2D tex = TextureUtil.LoadPremultiplied(gd, path);
                if (tex == null) { Log.Warn($"[Tiles] Missing texture for {def.Name}: {path}"); continue; }
                var asset = TextureUtil.CreateLoadedAsset(tex, "MagicStorageTM/" + def.Name);
                if (asset != null) TextureAssets.Tile[def.Type] = asset;
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<Array>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();
            public bool Equals(Array a, Array b) => ReferenceEquals(a, b);
            public int GetHashCode(Array a) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(a);
        }
    }

    /// <summary>Notifies listeners (storage networks) that a custom tile was placed or removed.</summary>
    public static class TileEvents
    {
        public static event Action<int, int> Changed;
        public static void RaiseChanged(int x, int y)
        {
            try { Changed?.Invoke(x, y); }
            catch (Exception ex) { Log.Error("[Tiles] Changed handler threw", ex); }
        }
    }
}
