using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.ID;

namespace MagicStorageTM.Tiles
{
    /// <summary>Harmony patches that make vanilla treat our tile types as real multi-tile objects.</summary>
    internal static class TilePatches
    {
        private static bool _destroying;

        /// <summary>Raised on the main thread once custom tiles are fully set up (recipes hook in here).</summary>
        public static event Action AfterSetup;

        public static void Apply(Harmony h)
        {
            const BindingFlags A = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            Patch(h, typeof(Main).GetMethod("Initialize_AlmostEverything", A), postfix: nameof(Initialize_AlmostEverything_Postfix));
            Patch(h, typeof(WorldGen).GetMethod("TileFrameImportant", A), prefix: nameof(TileFrameImportant_Prefix));
            Patch(h, typeof(WorldGen).GetMethod("KillTile", A, null, new[] { typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(bool) }, null), prefix: nameof(KillTile_Prefix));
            Patch(h, typeof(WorldGen).GetMethod("CanKillTile", A, null, new[] { typeof(int), typeof(int), typeof(bool).MakeByRefType() }, null), prefix: nameof(CanKillTile_Prefix));
            Patch(h, typeof(Player).GetMethod("TileInteractionsUse", A), prefix: nameof(TileInteractionsUse_Prefix));
            Patch(h, typeof(Player).GetMethod("TileInteractionsMouseOver", A), prefix: nameof(TileInteractionsMouseOver_Prefix));
            Patch(h, typeof(Player).GetMethod("AdjTiles", A), prefix: nameof(AdjTiles_Prefix), postfix: nameof(AdjTiles_Postfix));
            Patch(h, typeof(Main).GetMethod("LoadTiles", A, null, new[] { typeof(int) }, null), prefix: nameof(LoadTiles_Prefix));
            Patch(h, typeof(SceneMetrics).GetMethod("ScanTiles", A), prefix: nameof(ScanTiles_Prefix));

            Type filter = typeof(NewCraftingUI).GetNestedType("CraftStationRecipeFilter", BindingFlags.Public | BindingFlags.NonPublic);
            Patch(h, filter?.GetMethod("AcceptTileType", A), prefix: nameof(AcceptTileType_Prefix));
        }

        private static void Patch(Harmony h, MethodBase target, string prefix = null, string postfix = null)
        {
            if (target == null)
            {
                Log.Error($"[Tiles] Patch target missing for {prefix ?? postfix}");
                return;
            }
            try
            {
                h.Patch(target,
                    prefix: prefix != null ? new HarmonyMethod(typeof(TilePatches).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic)) : null,
                    postfix: postfix != null ? new HarmonyMethod(typeof(TilePatches).GetMethod(postfix, BindingFlags.Static | BindingFlags.NonPublic)) : null);
            }
            catch (Exception ex) { Log.Error($"[Tiles] Failed to patch {target.DeclaringType?.Name}.{target.Name}", ex); }
        }

        // ── Setup ──

        private static void Initialize_AlmostEverything_Postfix()
        {
            CustomTiles.Setup();
            if (!CustomTiles.SetupDone) return;
            try { AfterSetup?.Invoke(); }
            catch (Exception ex) { Log.Error("[Tiles] AfterSetup handler threw", ex); }
        }

        // ── Integrity / breaking ──

        private static bool TileFrameImportant_Prefix(int i, int j, int type)
        {
            CustomTileDef def = CustomTiles.Get(type);
            if (def == null) return true;
            if (_destroying) return false;
            try
            {
                if (def.Width == 1 && def.Height == 1)
                {
                    def.CustomFrame?.Invoke(i, j);
                    return false;
                }
                CheckObject(def, i, j);
            }
            catch (Exception ex) { Log.Error($"[Tiles] Frame check failed at {i},{j}", ex); }
            return false;
        }

        private static void CheckObject(CustomTileDef def, int i, int j)
        {
            if (!CustomTiles.TryGetTopLeft(i, j, out int x, out int y, out _)) return;
            Tile here = Main.tile[i, j];
            CustomTiles.GetStyleOrigin(def, here.frameX, here.frameY, out int ofx, out int ofy);
            for (int dx = 0; dx < def.Width; dx++)
            {
                for (int dy = 0; dy < def.Height; dy++)
                {
                    if (!WorldGen.InWorld(x + dx, y + dy)) { DestroyObject(def, x, y, ofx, ofy, true); return; }
                    Tile t = Main.tile[x + dx, y + dy];
                    if (t == null || !t.active() || t.type != def.Type
                        || t.frameX != ofx + dx * 18 || t.frameY != ofy + def.RowOffset(dy))
                    {
                        DestroyObject(def, x, y, ofx, ofy, true);
                        return;
                    }
                }
            }
        }

        /// <summary>Remove every tile of the object at (x, y), drop its item and notify the owner.</summary>
        internal static void DestroyObject(CustomTileDef def, int x, int y, int originFrameX, int originFrameY, bool dropItem)
        {
            _destroying = true;
            try
            {
                for (int dx = 0; dx < def.Width; dx++)
                {
                    for (int dy = 0; dy < def.Height; dy++)
                    {
                        if (!WorldGen.InWorld(x + dx, y + dy)) continue;
                        Tile t = Main.tile[x + dx, y + dy];
                        if (t == null || !t.active() || t.type != def.Type) continue;
                        if (t.frameX - dx * 18 != originFrameX || t.frameY - def.RowOffset(dy) != originFrameY) continue;
                        t.ClearTile();
                        t.type = 0;
                        t.frameX = 0;
                        t.frameY = 0;
                    }
                }

                bool drops = dropItem && !WorldGen.isGeneratingOrLoadingWorld && Main.netMode != 1;
                int item = drops ? (def.GetDropItem?.Invoke(originFrameX, originFrameY) ?? 0) : 0;
                if (item > 0)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(x, y), new Vector2(x * 16 + def.Width * 8, y * 16 + def.Height * 8), item);

                try { def.OnKilled?.Invoke(x, y, originFrameX, originFrameY); }
                catch (Exception ex) { Log.Error($"[Tiles] OnKilled({def.Name}) threw", ex); }
            }
            finally { _destroying = false; }

            for (int dx = -1; dx <= def.Width; dx++)
                for (int dy = -1; dy <= def.Height; dy++)
                    if (WorldGen.InWorld(x + dx, y + dy, 1)) WorldGen.TileFrame(x + dx, y + dy);
            TileEvents.RaiseChanged(x, y);
        }

        private static bool KillTile_Prefix(int i, int j, ref bool fail, bool effectOnly, bool noItem)
        {
            if (!WorldGen.InWorld(i, j)) return true;
            Tile tile = Main.tile[i, j];
            if (tile == null || !tile.active()) return true;
            CustomTileDef def = CustomTiles.Get(tile.type);
            if (def == null) return true;
            if (fail || effectOnly) return true; // vanilla only plays the hit sound/dust here

            if (!CustomTiles.TryGetTopLeft(i, j, out int x, out int y, out _)) return true;
            bool canKill = true;
            try { canKill = def.CanKill == null || def.CanKill(x, y); }
            catch (Exception ex) { Log.Error($"[Tiles] CanKill({def.Name}) threw", ex); canKill = false; }
            if (!canKill)
            {
                fail = true;
                return true;
            }

            if (!Main.dedServ && !WorldGen.isGeneratingOrLoadingWorld && !Main.gameMenu)
                WorldGen.KillTile_PlaySounds(i, j, false, tile);
            if (!Main.dedServ)
                for (int n = 0; n < 8 * def.Width; n++)
                    Dust.NewDust(new Vector2(x * 16, y * 16), def.Width * 16, def.Height * 16, def.DustType);

            CustomTiles.GetStyleOrigin(def, tile.frameX, tile.frameY, out int ofx, out int ofy);
            DestroyObject(def, x, y, ofx, ofy, !noItem);
            return false;
        }

        private static bool CanKillTile_Prefix(int i, int j, ref bool blockDamaged, ref bool __result)
        {
            if (!WorldGen.InWorld(i, j)) return true;
            Tile tile = Main.tile[i, j];
            if (tile == null || !tile.active()) return true;
            CustomTileDef def = CustomTiles.Get(tile.type);
            if (def == null) return true;
            blockDamaged = false;
            if (!CustomTiles.TryGetTopLeft(i, j, out int x, out int y, out _)) { __result = true; return false; }
            try { __result = def.CanKill == null || def.CanKill(x, y); }
            catch (Exception ex) { Log.Error($"[Tiles] CanKill({def.Name}) threw", ex); __result = false; }
            return false;
        }

        // ── Interaction ──

        private static bool TileInteractionsUse_Prefix(Player __instance, int myX, int myY)
        {
            Tile tile = Main.tile[myX, myY];
            if (tile == null || !tile.active()) return true;
            CustomTileDef def = CustomTiles.Get(tile.type);
            if (def == null) return true;
            if (__instance.whoAmI != Main.myPlayer || !__instance.tileInteractAttempted || !__instance.releaseUseTile) return false;
            if (!CustomTiles.TryGetTopLeft(myX, myY, out int x, out int y, out _)) return false;
            try
            {
                if (def.OnRightClick != null && def.OnRightClick(x, y))
                {
                    __instance.tileInteractionHappened = true;
                    __instance.releaseUseTile = false;
                    Main.mouseRightRelease = false;
                }
            }
            catch (Exception ex) { Log.Error($"[Tiles] OnRightClick({def.Name}) threw", ex); }
            return false;
        }

        private static bool TileInteractionsMouseOver_Prefix(Player __instance, int myX, int myY)
        {
            if (myX != Player.tileTargetX || myY != Player.tileTargetY) return true;
            Tile tile = Main.tile[myX, myY];
            if (tile == null || !tile.active()) return true;
            CustomTileDef def = CustomTiles.Get(tile.type);
            if (def == null) return true;
            if (!CustomTiles.TryGetTopLeft(myX, myY, out int x, out int y, out _)) return false;
            try
            {
                int icon = def.HoverIcon?.Invoke(x, y) ?? 0;
                if (icon > 0)
                {
                    __instance.noThrow = 2;
                    __instance.cursorItemIconEnabled = true;
                    __instance.cursorItemIconID = icon;
                }
                string text = def.HoverText?.Invoke(x, y);
                if (!string.IsNullOrEmpty(text))
                {
                    __instance.cursorItemIconText = text;
                }
            }
            catch (Exception ex) { Log.Error($"[Tiles] Hover({def.Name}) threw", ex); }
            return false;
        }

        // ── Crafting-station flags ──

        private static void AdjTiles_Prefix(Player __instance)
        {
            if (CustomTiles.SetupDone && __instance.adjTile.Length < CustomTiles.ExtendedCount)
                Array.Resize(ref __instance.adjTile, CustomTiles.ExtendedCount);
        }

        private static void AdjTiles_Postfix(Player __instance)
        {
            if (!CustomTiles.SetupDone) return;
            foreach (CustomTileDef def in CustomTiles.All)
            {
                if (!__instance.adjTile[def.Type]) continue;
                if (def.CountsAsHoney) __instance.adjHoney = true;
                if (def.CountsAsLava) __instance.adjLava = true;
                if (def.CountsAsWater) __instance.adjWaterSource = true;
            }
        }

        // ── Size guards for instances created after setup ──

        private static bool LoadTiles_Prefix(int i) => !CustomTiles.IsCustom(i);

        private static FieldInfo _tileCountsField;

        private static void ScanTiles_Prefix(SceneMetrics __instance)
        {
            if (!CustomTiles.SetupDone) return;
            _tileCountsField ??= typeof(SceneMetrics).GetField("_tileCounts", BindingFlags.Instance | BindingFlags.NonPublic);
            if (_tileCountsField?.GetValue(__instance) is int[] counts && counts.Length < CustomTiles.ExtendedCount)
            {
                var bigger = new int[CustomTiles.ExtendedCount];
                Array.Copy(counts, bigger, counts.Length);
                _tileCountsField.SetValue(__instance, bigger);
            }
        }

        private static FieldInfo _acceptField;

        private static void AcceptTileType_Prefix(object __instance, int tileType)
        {
            if (!CustomTiles.SetupDone) return;
            _acceptField ??= __instance.GetType().GetField("acceptTileTypes", BindingFlags.Instance | BindingFlags.NonPublic);
            if (_acceptField?.GetValue(__instance) is bool[] arr && arr.Length <= tileType)
            {
                var bigger = new bool[CustomTiles.ExtendedCount];
                Array.Copy(arr, bigger, arr.Length);
                _acceptField.SetValue(__instance, bigger);
            }
        }
    }
}
