using MagicStorage.Components;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageDeactivator : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 28;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 15;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.tileBoost = 20;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.sellPrice(silver: 40);
		}

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI == Main.myPlayer && player.itemAnimation > 0 && player.itemTime == 0 && player.controlUseItem)
			{
				int i = Player.tileTargetX;
				int j = Player.tileTargetY;
				if (Main.tile[i, j].TileFrameX % 36 == 18)
					i--;
				if (Main.tile[i, j].TileFrameY % 36 == 18)
					j--;

				Point16 point = new(i, j);
				if (TileEntity.ByPosition.TryGetValue(point, out TileEntity te) && te is TEAbstractStorageUnit storage)
				{
					storage.Inactive = !storage.Inactive;
					string activeText = storage.Inactive ? "Deactivated" : "Activated";
					Main.NewText(Language.GetTextValue($"Mods.MagicStorage.StorageUnit{activeText}"));
					if (storage is TEStorageUnit storageUnit)
					{
						if (Main.netMode == NetmodeID.MultiplayerClient)
						{
							NetHelper.ClientSendDeactivate(storageUnit.Position, storageUnit.Inactive);
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
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.ActuationRod);
			recipe.AddIngredient<StorageComponent>();
			recipe.AddTile(TileID.Anvils);
			recipe.Register();
		}
	}
}
