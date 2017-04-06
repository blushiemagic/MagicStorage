using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using MagicStorage.Components;

namespace MagicStorage
{
	public class StoragePlayer : ModPlayer
	{
		private Point16 storageAccess = new Point16(-1, -1);

		public override void UpdateDead()
		{
			storageAccess = new Point16(-1, -1);
		}

		public override void ResetEffects()
		{
			if (player.chest != -1 || !Main.playerInventory || player.sign > -1 || player.talkNPC > -1)
			{
				CloseStorage();
				Recipe.FindRecipes();
			}
			else if (storageAccess.X >= 0 && storageAccess.Y >= 0)
			{
				int playerX = (int)(player.Center.X / 16f);
				int playerY = (int)(player.Center.Y / 16f);
				if (playerX < storageAccess.X - Player.tileRangeX || playerX > storageAccess.X + Player.tileRangeX + 1 || playerY < storageAccess.Y - Player.tileRangeY || playerY > storageAccess.Y + Player.tileRangeY + 1)
				{
					Main.PlaySound(11, -1, -1, 1);
					CloseStorage();
					Recipe.FindRecipes();
				}
				else if (!(TileLoader.GetTile(Main.tile[storageAccess.X, storageAccess.Y].type) is StorageAccess))
				{
					Main.PlaySound(11, -1, -1, 1);
					CloseStorage();
					Recipe.FindRecipes();
				}
			}
		}

		public void OpenStorage(Point16 point)
		{
			storageAccess = point;
			StorageGUI.RefreshItems();
		}

		public void CloseStorage()
		{
			storageAccess = new Point16(-1, -1);
			Main.blockInput = false;
			StorageGUI.searchBar.Reset();
		}

		public Point16 ViewingStorage()
		{
			return storageAccess;
		}

		public void FindRecipesFromStorage()
		{
			
		}
	}
}