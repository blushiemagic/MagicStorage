using System;
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
			Tooltip.SetDefault("<right> Storage Unit to toggle between Active/Inactive");
			DisplayName.AddTranslation(GameCulture.Russian, "Посох блока хранения");
			Tooltip.AddTranslation(GameCulture.Russian, "<right> по Блоку Хранения для переключения между Включенным/Выключенным состоянием);
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
					TEAbstractStorageUnit storageUnit = (TEAbstractStorageUnit)TileEntity.ByPosition[point];
					storageUnit.Inactive = !storageUnit.Inactive;
					string activeText = storageUnit.Inactive ? "Deactivated" : "Activated";
					Main.NewText("Storage Unit has been " + activeText);
					NetHelper.ClientSendTEUpdate(storageUnit.ID);
					if (storageUnit is TEStorageUnit)
					{
						((TEStorageUnit)storageUnit).UpdateTileFrameWithNetSend();
						if (Main.netMode == 0)
						{
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
