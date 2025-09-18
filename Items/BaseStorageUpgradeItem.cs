using MagicStorage.Common.Systems;
using MagicStorage.CrossMod.Storage;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items {
	public abstract class BaseStorageUpgradeItem : ModItem, IValidateAtPostSetupContent {
		public abstract StorageUnitTier Tier { get; }

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 10;
		}

		public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) {
			itemGroup = ContentSamples.CreativeHelper.ItemGroup.RemainingUseItems;
		}

		void IValidateAtPostSetupContent.ValidateType() {
			if (Tier.UpgradeItemType != Type)
				throw new Exception($"Storage Upgrade item \"{FullName}\" does not match the item ID assigned to its Storage Unit tier \"{Tier.FullName}\"");
		}

		public override void SetDefaults() {
			Item.width = 12;
			Item.height = 12;
			Item.maxStack = 99;

			int unitItem = Tier.StorageUnitItemType;
			if (ContentSamples.ItemsByType is null || !ContentSamples.ItemsByType.TryGetValue(unitItem, out Item sample))
				sample = new Item(unitItem);

			Item.rare = sample.rare;
		}
	}
}
