using MagicStorage.Common.Systems;
using MagicStorage.CrossMod.Storage;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items {
	public abstract class BaseStorageUnitItem : ModItem, IValidateAtPostSetupContent {
		public abstract StorageUnitTier Tier { get; }

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 10;
		}

		void IValidateAtPostSetupContent.ValidateType() {
			if (Tier.StorageUnitItemType != Type)
				throw new Exception($"Storage Unit item \"{FullName}\" does not match the item ID assigned to its Storage Unit tier \"{Tier.FullName}\"");
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
			Item.createTile = Tier.StorageUnitTileType;
			Item.placeStyle = Tier.ItemPlaceStyle;
		}

		public override void AddRecipes() {
			// For every tier that this item's tier can upgrade to, create a recipe
			// this.Tier.StorageUnitItemType should be the same ID as this item, so no need to reference it
			foreach (var nextTier in Tier.NextTiers) {
			//	Mod.Logger.Info($"Registering recipe for Storage tier ({Tier.FullName} -> {nextTier.FullName}): {FullName} + {ItemLoader.GetItem(nextTier.UpgradeItemType)?.FullName ?? "<null>"} = {ItemLoader.GetItem(nextTier.StorageUnitItemType)?.FullName ?? "<null>"}");

				Recipe.Create(nextTier.StorageUnitItemType)
					.AddIngredient(this)
					.AddIngredient(nextTier.UpgradeItemType)
					.Register();
			}
		}
	}
}
