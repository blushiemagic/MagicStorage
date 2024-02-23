using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items {
	public abstract class BaseStorageUnitItem : ModItem {
		public abstract Components.StorageUnitTier Tier { get; }

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 10;
		}

		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 26;
			Item.maxStack = 99;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.createTile = ModContent.TileType<Components.StorageUnit>();
			Item.placeStyle = (int)Tier;
		}
	}

	public abstract class BaseStorageUnitItem<TUnit, TUpgrade> : BaseStorageUnitItem where TUnit : BaseStorageUnitItem where TUpgrade : BaseStorageUpgradeItem {
		public sealed override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<TUnit>()
				.AddIngredient<TUpgrade>()
				.Register();
		}
	}
}
