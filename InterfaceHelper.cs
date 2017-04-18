using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using MagicStorage.Components;

namespace MagicStorage
{
	public static class InterfaceHelper
	{
		private static FieldInfo _itemIconCacheTimeInfo;

		public static void Initialize()
		{
			_itemIconCacheTimeInfo = typeof(Main).GetField("_itemIconCacheTime", BindingFlags.NonPublic | BindingFlags.Static);
		}

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
			if (modTile is CraftingAccess)
			{
				CraftingGUI.Draw(heart);
			}
			else
			{
				StorageGUI.Draw(heart);
			}
			return true;
		}

		public static void HideItemIconCache()
		{
			_itemIconCacheTimeInfo.SetValue(null, 0);
		}
	}
}