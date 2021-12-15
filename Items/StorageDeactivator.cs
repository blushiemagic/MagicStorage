using MagicStorage.Components;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageDeactivator : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Storage Unit Wand");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Жезл Ячейки Хранилища");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish), "Różdżka jednostki magazynującej");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Baguette d'unité de stockage");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Varita de unidad de almacenamiento");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Baguetter d'unité de stockage");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "存储单元魔杖");

			Tooltip.SetDefault("<right> Storage Unit to toggle between Active/Inactive");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "<right> на Ячейке Хранилища что бы активировать/деактивировать ее");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish), "<right> aby przełączyć Jednostkę Magazynującą (wł./wył.)");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "<right> pour changer l'unité de stockage actif/inactif");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "<right> para cambiar el unidad de almacenamiento activo/inactivo");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "<right>存储单元使其切换启用/禁用");

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}

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
				if (Main.tile[i, j].frameX % 36 == 18)
					i--;
				if (Main.tile[i, j].frameY % 36 == 18)
					j--;

				Point16 point = new(i, j);
				if (TileEntity.ByPosition.TryGetValue(point, out TileEntity te) && te is TEAbstractStorageUnit storage)
				{
					storage.Inactive = !storage.Inactive;
					string activeText = storage.Inactive ? "Deactivated" : "Activated";
					Main.NewText("Storage Unit has been " + activeText);
					if (storage is TEStorageUnit storageUnit)
					{
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
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.ActuationRod);
			recipe.AddIngredient(null, "StorageComponent");
			recipe.AddTile(TileID.Anvils);
			recipe.Register();
		}
	}
}
