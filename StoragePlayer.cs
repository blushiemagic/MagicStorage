using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.UI;
using MagicStorage.Components;
using Terraria.ID;
using Terraria.Audio;

namespace MagicStorage
{
    public class StoragePlayer : ModPlayer
    {
        public int timeSinceOpen = 1;
        private Point16 storageAccess = new Point16(-1, -1);
        public bool remoteAccess = false;

        public override void UpdateDead()
        {
            if (Player.whoAmI == Main.myPlayer)
            {
                CloseStorage();
            }
        }

        public override void ResetEffects()
        {
            if (Player.whoAmI != Main.myPlayer)
            {
                return;
            }
            if (timeSinceOpen < 1)
            {
                Player.SetTalkNPC(-1);
                Main.playerInventory = true;
                timeSinceOpen++;
            }
            if (storageAccess.X >= 0 && storageAccess.Y >= 0 && (Player.chest != -1 || !Main.playerInventory || Player.sign > -1 || Player.talkNPC > -1))
            {
                CloseStorage();
                Recipe.FindRecipes();
            }
            else if (storageAccess.X >= 0 && storageAccess.Y >= 0)
            {
                int PlayerX = (int)(Player.Center.X / 16f);
                int PlayerY = (int)(Player.Center.Y / 16f);
                if (!remoteAccess && (PlayerX < storageAccess.X - Player.tileRangeX || PlayerX > storageAccess.X + Player.tileRangeX + 1 || PlayerY < storageAccess.Y - Player.tileRangeY || PlayerY > storageAccess.Y + Player.tileRangeY + 1))
                {
                    SoundEngine.PlaySound(11, -1, -1, 1);
                    CloseStorage();
                    Recipe.FindRecipes();
                }
                else if (!(TileLoader.GetTile(Main.tile[storageAccess.X, storageAccess.Y].type) is StorageAccess))
                {
                    SoundEngine.PlaySound(11, -1, -1, 1);
                    CloseStorage();
                    Recipe.FindRecipes();
                }
            }
        }

        public void OpenStorage(Point16 point, bool remote = false)
        {
            storageAccess = point;
            remoteAccess = remote;
            StorageGUI.RefreshItems();
        }

        public void CloseStorage()
        {
            storageAccess = new Point16(-1, -1);
            Main.blockInput = false;
            if (StorageGUI.searchBar != null)
            {
                StorageGUI.searchBar.Reset();
            }
            if (StorageGUI.searchBar2 != null)
            {
                StorageGUI.searchBar2.Reset();
            }
            if (CraftingGUI.searchBar != null)
            {
                CraftingGUI.searchBar.Reset();
            }
            if (CraftingGUI.searchBar2 != null)
            {
                CraftingGUI.searchBar2.Reset();
            }
        }

        public Point16 ViewingStorage()
        {
            return storageAccess;
        }

        public static void GetItem(Item item, bool toMouse)
        {
            Player Player = Main.player[Main.myPlayer];
            if (toMouse && Main.playerInventory && Main.mouseItem.IsAir)
            {
                Main.mouseItem = item;
                item = new Item();
            }
            else if (toMouse && Main.playerInventory && Main.mouseItem.type == item.type)
            {
                int total = Main.mouseItem.stack + item.stack;
                if (total > Main.mouseItem.maxStack)
                {
                    total = Main.mouseItem.maxStack;
                }
                int difference = total - Main.mouseItem.stack;
                Main.mouseItem.stack = total;
                item.stack -= difference;
            }
            if (!item.IsAir)
            {
                item = Player.GetItem(Main.myPlayer, item, GetItemSettings.InventoryEntityToPlayerInventorySettings);
                if (!item.IsAir)
                {
                    Player.QuickSpawnClonedItem(item, item.stack);
                }
            }
        }

        public override bool ShiftClickSlot(Item[] inventory, int context, int slot)
        {
            if (context != ItemSlot.Context.InventoryItem && context != ItemSlot.Context.InventoryCoin && context != ItemSlot.Context.InventoryAmmo)
            {
                return false;
            }
            if (storageAccess.X < 0 || storageAccess.Y < 0)
            {
                return false;
            }
            Item item = inventory[slot];
            if (item.favorited || item.IsAir)
            {
                return false;
            }
            int oldType = item.type;
            int oldStack = item.stack;
            if (StorageCrafting())
            {
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    GetCraftingAccess().TryDepositStation(item);
                }
                else
                {
                    NetHelper.SendDepositStation(GetCraftingAccess().ID, item);
                    item.SetDefaults(0, true);
                }
            }
            else
            {
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    GetStorageHeart().DepositItem(item);
                }
                else
                {
                    NetHelper.SendDeposit(GetStorageHeart().ID, item);
                    item.SetDefaults(0, true);
                }
            }
            if (item.type != oldType || item.stack != oldStack)
            {
                SoundEngine.PlaySound(7, -1, -1, 1, 1f, 0f);
                StorageGUI.RefreshItems();
            }
            return true;
        }

        public TEStorageHeart GetStorageHeart()
        {
            if (storageAccess.X < 0 || storageAccess.Y < 0)
            {
                return null;
            }
            Tile tile = Main.tile[storageAccess.X, storageAccess.Y];
            if (tile == null)
            {
                return null;
            }
            int tileType = tile.type;
            ModTile modTile = TileLoader.GetTile(tileType);
            if (modTile == null || !(modTile is StorageAccess))
            {
                return null;
            }
            return ((StorageAccess)modTile).GetHeart(storageAccess.X, storageAccess.Y);
        }

        public TECraftingAccess GetCraftingAccess()
        {
            if (storageAccess.X < 0 || storageAccess.Y < 0 || !TileEntity.ByPosition.ContainsKey(storageAccess))
            {
                return null;
            }
            return TileEntity.ByPosition[storageAccess] as TECraftingAccess;
        }

        public bool StorageCrafting()
        {
            if (storageAccess.X < 0 || storageAccess.Y < 0)
            {
                return false;
            }
            Tile tile = Main.tile[storageAccess.X, storageAccess.Y];
            return tile != null && tile.type == ModContent.TileType<CraftingAccess>();
        }

        public static bool IsStorageCrafting()    
        {
            return Main.player[Main.myPlayer].GetModPlayer<StoragePlayer>().StorageCrafting();
        }
    }
}