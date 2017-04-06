using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using MagicStorage.Components;

namespace MagicStorage
{
	public static class InterfaceHelper
	{
		public static void ModifyInterfaceLayers(List<MethodSequenceListItem> layers)
		{
			for (int k = 0; k < layers.Count; k++)
			{
				if (layers[k].Name == "Vanilla: Inventory")
				{
					layers.Insert(k + 1, new MethodSequenceListItem("MagicStorage: StorageAccess", DrawStorageGUI, layers[k]));
					k++;
				}
			}
		}

		public static bool DrawStorageGUI()
		{
			Player player = Main.player[Main.myPlayer];
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>(MagicStorage.Instance);
			Point16 storageAccess = modPlayer.ViewingStorage();
			if (!Main.playerInventory || storageAccess.X < 0 || storageAccess.Y < 0)
			{
				return true;
			}
			ModTile modTile = TileLoader.GetTile(Main.tile[storageAccess.X, storageAccess.Y].type);
			if (modTile == null || !(modTile is StorageAccess))
			{
				return true;
			}
			TEStorageHeart heart = ((StorageAccess)modTile).GetHeart(storageAccess.X, storageAccess.Y);
			if (heart == null)
			{
				return true;
			}
			StorageGUI.Draw(heart);
			return true;
		}
	}
}