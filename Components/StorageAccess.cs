using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MagicStorage.Components
{
	public class StorageAccess : StorageComponent
	{
		public override int ItemType(int frameX, int frameY)
		{
			return mod.ItemType("StorageAccess");
		}

		public override bool HasSmartInteract()
		{
			return true;
		}

		public virtual TEStorageHeart GetHeart(int i, int j)
		{
			Point16 point = TEStorageComponent.FindStorageCenter(new Point16(i, j));
			if (point.X < 0 || point.Y < 0 || !TileEntity.ByPosition.ContainsKey(point))
			{
				return null;
			}
			TileEntity heart = TileEntity.ByPosition[point];
			if (!(heart is TEStorageCenter))
			{
				return null;
			}
			return ((TEStorageCenter)heart).GetHeart();
		}

		public override void MouseOver(int i, int j)
		{
			Player player = Main.LocalPlayer;
			Tile tile = Main.tile[i, j];
			player.showItemIcon = true;
			player.showItemIcon2 = ItemType(tile.frameX, tile.frameY);
			player.noThrow = 2;
		}

		public override bool NewRightClick(int i, int j)
		{
			if (Main.tile[i, j].frameX % 36 == 18)
			{
				i--;
			}
			if (Main.tile[i, j].frameY % 36 == 18)
			{
				j--;
			}
			Player player = Main.player[Main.myPlayer];
			if (GetHeart(i, j) == null)
			{
				Main.NewText("This access is not connected to a Storage Heart!");
				return true;
			}
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
			Main.mouseRightRelease = false;
			if (player.sign > -1)
			{
				Main.PlaySound(11, -1, -1, 1);
				player.sign = -1;
				Main.editSign = false;
				Main.npcChatText = string.Empty;
			}
			if (Main.editChest)
			{
				Main.PlaySound(12, -1, -1, 1);
				Main.editChest = false;
				Main.npcChatText = string.Empty;
			}
			if (player.editedChestName)
			{
				NetMessage.SendData(MessageID.SyncPlayerChest, -1, -1, NetworkText.FromLiteral(Main.chest[player.chest].name), player.chest, 1f, 0f, 0f, 0, 0, 0);
				player.editedChestName = false;
			}
			if (player.talkNPC > -1)
			{
				player.talkNPC = -1;
				Main.npcChatCornerItem = 0;
				Main.npcChatText = string.Empty;
			}
			bool hadChestOpen = player.chest != -1;
			player.chest = -1;
			Main.stackSplit = 600;
			Point16 toOpen = new Point16(i, j);
			Point16 prevOpen = modPlayer.ViewingStorage();
			if (prevOpen == toOpen)
			{
				modPlayer.CloseStorage();
				Main.PlaySound(11, -1, -1, 1);
				Recipe.FindRecipes();
			}
			else
			{
				bool hadOtherOpen = prevOpen.X >= 0 && prevOpen.Y >= 0;
				modPlayer.OpenStorage(toOpen);
				modPlayer.timeSinceOpen = 0;
				Main.playerInventory = true;
				if (PlayerInput.GrappleAndInteractAreShared)
				{
					PlayerInput.Triggers.JustPressed.Grapple = false;
				}
				Main.recBigList = false;
				Main.PlaySound(hadChestOpen || hadOtherOpen ? 12 : 10, -1, -1, 1);
				Recipe.FindRecipes();
			}
            return true;
		}

		public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
		{
			Tile tile = Main.tile[i, j];
			Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
			Vector2 drawPos = zero + 16f * new Vector2(i, j) - Main.screenPosition;
			Rectangle frame = new Rectangle(tile.frameX, tile.frameY, 16, 16);
			Color lightColor = Lighting.GetColor(i, j, Color.White);
			Color color = Color.Lerp(lightColor, Color.White, Main.essScale);
			spriteBatch.Draw(mod.GetTexture("Components/" + Name + "_Glow"), drawPos, frame, color);
		}
	}
}