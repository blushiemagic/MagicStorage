﻿using System.Collections.Generic;
using MagicStorage.Components;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.Common.Systems;

public class MagicUI : ModSystem
{
	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int inventoryIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Inventory");
		if (inventoryIndex != -1)
			layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer("MagicStorage: StorageAccess", DrawStorageGUI, InterfaceScaleType.UI));
	}

	private static bool DrawStorageGUI()
	{
		Player player = Main.LocalPlayer;
		StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
		Point16 storageAccess = modPlayer.ViewingStorage();
		if (Main.playerInventory && storageAccess.X >= 0 && storageAccess.Y >= 0)
		{
			ModTile modTile = TileLoader.GetTile(Main.tile[storageAccess.X, storageAccess.Y].TileType);
			if (modTile is StorageAccess access)
			{
				TEStorageHeart heart = access.GetHeart(storageAccess.X, storageAccess.Y);
				if (heart is not null)
				{
					if (access is EnvironmentAccess)
						EnvironmentGUI.Draw();
					else if (access is CraftingAccess)
						CraftingGUI.Draw();
					else
						StorageGUI.Draw();
				}
			}
		}

		return true;
	}

	public override void PostUpdateInput()
	{
		if (!Main.instance.IsActive)
			return;

		if (!Main.gameMenu && StoragePlayer.LocalPlayer.ViewingStorage().X >= 0)
			Main.hidePlayerCraftingMenu = true;

		StorageGUI.Update(null);
		CraftingGUI.Update(null);
		EnvironmentGUI.Update(null);
	}
}