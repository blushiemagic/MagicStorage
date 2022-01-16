﻿using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using MagicStorage.Components;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class StorageDeactivator : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Storage Unit Wand");
			DisplayName.AddTranslation(GameCulture.Russian, "Жезл Ячейки Хранилища");
			DisplayName.AddTranslation(GameCulture.Polish, "Różdżka jednostki magazynującej");
			DisplayName.AddTranslation(GameCulture.French, "Baguette d'unité de stockage");
			DisplayName.AddTranslation(GameCulture.Spanish, "Varita de unidad de almacenamiento");
			DisplayName.AddTranslation(GameCulture.French, "Baguetter d'unité de stockage");
			DisplayName.AddTranslation(GameCulture.Chinese, "存储单元魔杖");

			Tooltip.SetDefault("<right> Storage Unit to toggle between Active/Inactive");
			Tooltip.AddTranslation(GameCulture.Russian, "<right> на Ячейке Хранилища что бы активировать/деактивировать ее");
			Tooltip.AddTranslation(GameCulture.Polish, "<right> aby przełączyć Jednostkę Magazynującą (wł./wył.)");
			Tooltip.AddTranslation(GameCulture.French, "<right> pour changer l'unité de stockage actif/inactif");
			Tooltip.AddTranslation(GameCulture.Spanish, "<right> para cambiar el unidad de almacenamiento activo/inactivo");
			Tooltip.AddTranslation(GameCulture.Chinese, "<right>存储单元使其切换启用/禁用");
		}

		public override void SetDefaults()
		{
			item.width = 24;
			item.height = 28;
			item.useTurn = true;
			item.autoReuse = true;
			item.useAnimation = 15;
			item.useTime = 15;
			item.useStyle = 1;
			item.tileBoost = 20;
			item.rare = 1;
			item.value = Item.sellPrice(0, 0, 40, 0);
		}

		public override bool UseItem(Player player)
		{
			if (player.whoAmI == Main.myPlayer && player.itemAnimation > 0 && player.itemTime == 0 && player.controlUseItem)
			{
				int i = Player.tileTargetX;
				int j = Player.tileTargetY;
				if (Main.tile[i, j].frameX % 36 == 18)
				{
					i--;
				}
				if (Main.tile[i, j].frameY % 36 == 18)
				{
					j--;
				}
				Point16 point = new Point16(i, j);
				if (TileEntity.ByPosition.ContainsKey(point) && TileEntity.ByPosition[point] is TEAbstractStorageUnit)
				{
					TEAbstractStorageUnit storage = (TEAbstractStorageUnit)TileEntity.ByPosition[point];
					storage.Inactive = !storage.Inactive;
					string activeText = storage.Inactive ? "Deactivated" : "Activated";
					Main.NewText("Storage Unit has been " + activeText);
					if (storage is TEStorageUnit)
					{
						TEStorageUnit storageUnit = (TEStorageUnit)storage;
						if (Main.netMode == NetmodeID.MultiplayerClient)
						{
							NetHelper.ClientSendDeactivate(storageUnit.ID, storageUnit.Inactive);
						}
						else
						{
							storageUnit.UpdateTileFrameWithNetSend();
							storageUnit.GetHeart().ResetCompactStage();
						}
					}
				}
			}
			return true;
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.ActuationRod);
			recipe.AddIngredient(null, "StorageComponent");
			recipe.AddTile(TileID.Anvils);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
