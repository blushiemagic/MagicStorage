using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MagicStorage.Items;
using Terraria.ID;

namespace MagicStorage.Components
{
    public class StorageUnit : StorageComponent
    {
        public override void ModifyObjectData()
        {
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.StyleMultiplier = 6;
            TileObjectData.newTile.StyleWrapLimit = 6;
        }

        public override ModTileEntity GetTileEntity()
        {
            return (ModTileEntity)TileEntity.manager.GetTileEntity<TEStorageUnit>(ModContent.TileEntityType<TEStorageUnit>());
        }

        public override void MouseOver(int i, int j)
        {
            Main.LocalPlayer.noThrow = 2;
        }

        public override int ItemType(int frameX, int frameY)
        {
            int style = frameY / 36;
			var type = style switch {
				1 => ModContent.ItemType<StorageUnitDemonite>(),
				2 => ModContent.ItemType<StorageUnitCrimtane>(),
				3 => ModContent.ItemType<StorageUnitHellstone>(),
				4 => ModContent.ItemType<StorageUnitHallowed>(),
				5 => ModContent.ItemType<StorageUnitBlueChlorophyte>(),
				6 => ModContent.ItemType<StorageUnitLuminite>(),
				7 => ModContent.ItemType<StorageUnitTerra>(),
				8 => ModContent.ItemType<StorageUnitTiny>(),
				_ => ModContent.ItemType<Items.StorageUnit>(),
			};
			return type;
        }

        public override bool CanKillTile(int i, int j, ref bool blockDamage)
        {
            return (Main.tile[i, j].frameX / 36) % 3 == 0;
        }

        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if ((Main.tile[i, j].frameX / 36) % 3 != 0)
            {
                fail = true;
            }
        }

        public override bool RightClick(int i, int j)
        {
            if (Main.tile[i, j].frameX % 36 == 18)
            {
                i--;
            }
            if (Main.tile[i, j].frameY % 36 == 18)
            {
                j--;
            }
            if (TryUpgrade(i, j))
            {
                return true;
            }
            TEStorageUnit storageUnit = (TEStorageUnit)TileEntity.ByPosition[new Point16(i, j)];
            Main.player[Main.myPlayer].tileInteractionHappened = true;
            string activeString = storageUnit.Inactive ? "Inactive" : "Active";
            string fullnessString = storageUnit.NumItems + " / " + storageUnit.Capacity + " Items";
            Main.NewText(activeString + ", " + fullnessString);
            return base.RightClick(i, j);
        }

        private static bool TryUpgrade(int i, int j)
        {
            Player player = Main.player[Main.myPlayer];
            Item item = player.inventory[player.selectedItem];
            int style = Main.tile[i, j].frameY / 36;
            bool success = false;
            if (style == 0 && item.type == ModContent.ItemType<UpgradeDemonite>())
            {
                SetStyle(i, j, 1);
                success = true;
            }
            else if (style == 0 && item.type == ModContent.ItemType<UpgradeCrimtane>())
            {
                SetStyle(i, j, 2);
                success = true;
            }
            else if ((style == 1 || style == 2) && item.type == ModContent.ItemType<UpgradeHellstone>())
            {
                SetStyle(i, j, 3);
                success = true;
            }
            else if (style == 3 && item.type == ModContent.ItemType<UpgradeHallowed>())
            {
                SetStyle(i, j, 4);
                success = true;
            }
            else if (style == 4 && item.type == ModContent.ItemType<UpgradeBlueChlorophyte>())
            {
                SetStyle(i, j, 5);
                success = true;
            }
            else if (style == 5 && item.type == ModContent.ItemType<UpgradeLuminite>())
            {
                SetStyle(i, j, 6);
                success = true;
            }
            else if (style == 6 && item.type == ModContent.ItemType<UpgradeTerra>())
            {
                SetStyle(i, j, 7);
                success = true;
            }
            if (success)
            {
                TEStorageUnit storageUnit = (TEStorageUnit)TileEntity.ByPosition[new Point16(i, j)];
                storageUnit.UpdateTileFrame();
                NetMessage.SendTileSquare(Main.myPlayer, i, j, 2, 2);
                TEStorageHeart heart = storageUnit.GetHeart();
                if (heart != null)
                {
                    if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        heart.ResetCompactStage();
                    }
                    else if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetHelper.SendResetCompactStage(heart.ID);
                    }
                }
                item.stack--;
                if (item.stack <= 0)
                {
                    item.SetDefaults(0);
                }
                if (player.selectedItem == 58)
                {
                    Main.mouseItem = item.Clone();
                }
            }
            return success;
        }

        private static void SetStyle(int i, int j, int style)
        {
            Main.tile[i, j].frameY = (short)(36 * style);
            Main.tile[i + 1, j].frameY = (short)(36 * style);
            Main.tile[i, j + 1].frameY = (short)(36 * style + 18);
            Main.tile[i + 1, j + 1].frameY = (short)(36 * style + 18);
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawPos = zero + 16f * new Vector2(i, j) - Main.screenPosition;
            Rectangle frame = new Rectangle(tile.frameX, tile.frameY, 16, 16);
            Color lightColor = Lighting.GetColor(i, j, Color.White);
            Color color = Color.Lerp(Color.White, lightColor, 0.5f);
            spriteBatch.Draw(Mod.Assets.Request<Texture2D>("Components/StorageUnit_Glow").Value, drawPos, frame, color);
        }
    }
}