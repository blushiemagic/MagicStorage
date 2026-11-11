using System;
using System.Linq;
using MagicStorageTM.Storage;
using MagicStorageTM.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;

namespace MagicStorageTM.Content
{
    /// <summary>Tile and item behaviour for the storage network (replaces the ModTile/ModItem overrides).</summary>
    public static class Interactions
    {
        public static void Notify(string text) => Main.NewText(text, 255, 240, 20);

        // ── Lifecycle ──

        public static void OnPlaced(NetKind kind, Point16 pos)
        {
            switch (kind)
            {
                case NetKind.Heart: StorageWorld.GetHeart(pos); break;
                case NetKind.Unit: StorageWorld.GetUnit(pos); break;
                case NetKind.CraftingAccess: StorageWorld.GetCraftingAccess(pos); break;
                case NetKind.RemoteAccess: StorageWorld.GetRemoteAccess(pos); break;
            }
            Network.Invalidate();
            MSTiles.ReframeConnectorsAround(pos.X, pos.Y, 2, 2);
            if (kind == NetKind.Unit) StorageOps.RefreshUnitFrame(pos);
        }

        public static bool CanBreak(NetKind kind, Point16 pos)
        {
            switch (kind)
            {
                case NetKind.Unit:
                {
                    var u = StorageWorld.GetUnit(pos, create: false);
                    return u == null || (u.Items.Count == 0 && u.Unloaded.Count == 0);
                }
                case NetKind.CraftingAccess:
                {
                    var c = StorageWorld.CraftingAccesses.TryGetValue(pos, out var d) ? d : null;
                    return c == null || (c.Stations.Count == 0 && c.Unloaded.Count == 0);
                }
                default:
                    return true;
            }
        }

        public static void OnKilled(NetKind kind, Point16 pos)
        {
            lock (StorageWorld.Sync)
            {
                switch (kind)
                {
                    case NetKind.Unit:
                        if (StorageWorld.Units.TryGetValue(pos, out var u))
                        {
                            // Should be empty (CanBreak), but never delete items: drop whatever is left.
                            foreach (Item item in u.Items) DropAt(pos, item);
                            if (u.Unloaded.Count > 0)
                            {
                                var orphan = new UnitData { Inactive = true };
                                orphan.Unloaded.AddRange(u.Unloaded);
                                StorageWorld.Orphans.Add(orphan);
                            }
                            StorageWorld.Units.Remove(pos);
                        }
                        break;
                    case NetKind.Heart: StorageWorld.Hearts.Remove(pos); break;
                    case NetKind.CraftingAccess:
                        if (StorageWorld.CraftingAccesses.TryGetValue(pos, out var c))
                        {
                            foreach (Item item in c.Stations) DropAt(pos, item);
                            StorageWorld.CraftingAccesses.Remove(pos);
                        }
                        break;
                    case NetKind.RemoteAccess: StorageWorld.RemoteAccesses.Remove(pos); break;
                }
            }
            Network.Invalidate();
            MSTiles.ReframeConnectorsAround(pos.X, pos.Y, 2, 2);
            MagicUI.OnTileRemoved(pos);
        }

        private static void DropAt(Point16 pos, Item item)
        {
            if (item == null || item.IsAir) return;
            Item.NewItem(WorldGen.GetItemSource_FromTileBreak(pos.X, pos.Y), new Vector2(pos.X * 16 + 16, pos.Y * 16 + 16), item.type, item.stack, item.prefix);
        }

        // ── Right click ──

        public static bool RightClick(NetKind kind, Point16 pos)
        {
            Player player = Main.LocalPlayer;
            Item held = player.HeldItem;
            switch (kind)
            {
                case NetKind.Heart:
                    if (IsStorageLocator(held))
                    {
                        StorageWorld.GetBindings(player).StorageHeart = pos;
                        Notify($"Locator successfully set to: {DescribeLocation(pos)}");
                        return true;
                    }
                    MagicUI.ToggleStorage(pos, pos, remote: false);
                    return true;

                case NetKind.StorageAccess:
                    return OpenStorageFrom(pos);

                case NetKind.RemoteAccess:
                    if (MSItems.Is(held, "locator") || MSItems.Is(held, "locator-disk"))
                    {
                        LinkRemote(pos, held);
                        return true;
                    }
                    return OpenStorageFrom(pos);

                case NetKind.CraftingAccess:
                    if (IsCraftingLocator(held))
                    {
                        StorageWorld.GetBindings(player).CraftingAccess = pos;
                        Notify($"Locator successfully set to: {DescribeLocation(pos)}");
                        return true;
                    }
                    Point16 heart = Network.ResolveHeart(pos);
                    if (heart.X < 0) { Notify("This Crafting Interface is not connected to a Storage Heart."); return true; }
                    MagicUI.ToggleCrafting(pos, heart, remote: false);
                    return true;

                case NetKind.Unit:
                    return UnitRightClick(pos, held);
            }
            return false;
        }

        private static bool OpenStorageFrom(Point16 access)
        {
            Point16 heart = Network.ResolveHeart(access);
            if (heart.X < 0)
            {
                Notify(Network.KindAt(access.X, access.Y) == NetKind.RemoteAccess
                    ? "This Remote Storage Access is not linked to a Storage Heart. Right click it with a Locator."
                    : "This access is not connected to a Storage Heart.");
                return true;
            }
            MagicUI.ToggleStorage(access, heart, remote: false);
            return true;
        }

        private static void LinkRemote(Point16 remote, Item locator)
        {
            Point16 heart = StorageWorld.GetBindings(Main.LocalPlayer).StorageHeart;
            if (!Network.IsHeart(heart))
            {
                Notify("The Locator is not set to a Storage Heart.");
                return;
            }
            if (Network.Scan(remote).Hearts.Count > 0)
            {
                Notify("This Remote Storage Access is already connected to a Storage Heart directly.");
                return;
            }
            StorageWorld.GetRemoteAccess(remote).Heart = heart;
            Network.Invalidate();
            Notify("Remote Storage Access successfully linked to Storage Heart.");
            if (MSItems.Is(locator, "locator"))
            {
                locator.TurnToAir();
                StorageWorld.GetBindings(Main.LocalPlayer).StorageHeart = Point16.NegativeOne;
            }
            else
            {
                StorageWorld.GetBindings(Main.LocalPlayer).StorageHeart = Point16.NegativeOne;
            }
        }

        private static bool UnitRightClick(Point16 pos, Item held)
        {
            UnitTier tier = StorageOps.TierAt(pos);
            if (tier == null) return false;
            UnitData data = StorageWorld.GetUnit(pos);

            UnitTier target = UnitTier.All.FirstOrDefault(t => t.UpgradeItem != null && MSItems.Is(held, t.UpgradeItem));
            if (target != null)
            {
                if (!tier.CanUpgradeTo(target))
                {
                    Notify($"A {tier.DisplayName} cannot be upgraded with that.");
                    return true;
                }
                int frameX = (int)StorageOps.FullnessOf(data, target) * 36 + (data.Inactive ? 108 : 0);
                Tiles.CustomTiles.SetObjectFrame(pos.X, pos.Y, MSTiles.Unit, frameX, target.Row * 36);
                StorageOps.RefreshUnitFrame(pos);
                held.stack--;
                if (held.stack <= 0) held.TurnToAir();
                SoundEngine.PlaySound(SoundID.MaxMana, new Vector2(pos.X * 16 + 16, pos.Y * 16 + 16));
                for (int k = 0; k < 10; k++)
                    Dust.NewDustPerfect(new Vector2(pos.X * 16 + 16, pos.Y * 16 + 16), DustID.PureSpray, Main.rand.NextVector2Circular(2, 2), 0, Color.Green, 2f).noGravity = true;
                Network.Invalidate();
                return true;
            }

            string active = data.Inactive ? "Inactive" : "Active";
            Notify($"{active}, {data.Items.Count}/{tier.Capacity} Items");
            return true;
        }

        public static bool IsStorageLocator(Item item) =>
            MSItems.Is(item, "locator") || MSItems.Is(item, "locator-disk")
            || MSItems.Is(item, "portable-access-prehm") || MSItems.Is(item, "portable-access-hm") || MSItems.Is(item, "portable-access");

        public static bool IsCraftingLocator(Item item) =>
            MSItems.Is(item, "portable-crafting-access-prehm") || MSItems.Is(item, "portable-crafting-access-hm") || MSItems.Is(item, "portable-crafting-access");

        public static string HoverText(NetKind kind, Point16 pos) => null;

        // ── Portable access ──

        /// <summary>Item use hook for every Portable (Crafting) Access tier. Returns true so the swing still plays.</summary>
        public static bool UsePortable(Player player, PortableKind kind, int playerRangeTiles, int pylonRangeTiles)
        {
            if (player == null || player.whoAmI != Main.myPlayer) return true;
            if (Main.netMode != 0) { Notify("Magic Storage only works in singleplayer."); return true; }

            var b = StorageWorld.GetBindings(player);
            Point16 target = kind == PortableKind.Storage ? b.StorageHeart : b.CraftingAccess;
            if (target.X < 0)
            {
                Notify(kind == PortableKind.Storage
                    ? "This Portable Access is not linked. Right click a Storage Heart while holding it."
                    : "This Portable Crafting Access is not linked. Right click a Storage Crafting Interface while holding it.");
                return true;
            }
            bool valid = kind == PortableKind.Storage ? Network.IsHeart(target) : Network.IsKind(target, NetKind.CraftingAccess);
            if (!valid)
            {
                Notify(kind == PortableKind.Storage ? "The linked Storage Heart is missing." : "The linked Storage Crafting Interface is missing.");
                return true;
            }
            if (!InPortableRange(player, target, playerRangeTiles, pylonRangeTiles, out string why))
            {
                Notify(why);
                return true;
            }

            if (kind == PortableKind.Storage)
                MagicUI.ToggleStorage(target, target, remote: true, playerRangeTiles, pylonRangeTiles);
            else
            {
                Point16 heart = Network.ResolveHeart(target);
                if (heart.X < 0) { Notify("The linked Crafting Interface is not connected to a Storage Heart."); return true; }
                MagicUI.ToggleCrafting(target, heart, remote: true, playerRangeTiles, pylonRangeTiles);
            }
            return true;
        }

        public static bool InPortableRange(Player player, Point16 target, int playerRangeTiles, int pylonRangeTiles, out string why)
        {
            why = null;
            if (playerRangeTiles < 0) return true;
            Vector2 targetWorld = new Vector2(target.X * 16 + 16, target.Y * 16 + 16);
            float range = playerRangeTiles * 16f;
            if (Vector2.Distance(player.Center, targetWorld) <= range) return true;

            var pylons = Main.PylonSystem?.Pylons;
            if (pylons == null || pylons.Count == 0) { why = "You are out of range of the storage system."; return false; }
            bool nearPylon = pylons.Any(p => Vector2.Distance(player.Center, p.PositionInTiles.ToWorldCoordinates()) <= range);
            if (!nearPylon) { why = "You are out of range of the storage system."; return false; }
            bool storageNearPylon = pylons.Any(p => Vector2.Distance(targetWorld, p.PositionInTiles.ToWorldCoordinates()) <= pylonRangeTiles * 16f);
            if (!storageNearPylon) { why = "The storage system is not close enough to any pylon."; return false; }
            return true;
        }

        /// <summary>Compass / depth-meter style description, like MagicStorage's "GPS coordinates".</summary>
        public static string DescribeLocation(Point16 p)
        {
            int east = p.X * 2 - Main.maxTilesX;
            string compass = east == 0 ? "Center" : east > 0 ? $"{east}' East" : $"{-east}' West";
            int depth = (int)(p.Y * 2 - Main.worldSurface * 2);
            string depthText = depth == 0 ? "Level" : depth > 0 ? $"{depth}' Below" : $"{-depth}' Above";
            return $"{compass}, {depthText}";
        }
    }
}
