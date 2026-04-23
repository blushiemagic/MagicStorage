using MagicStorage.Components;
using MagicStorage.CrossMod.Storage;
using MagicStorage.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Examples {
	// This file showcases how to make a basic tier for Storage Units
	// To implement a custom Storage Unit tier, you need several components:
	//   1. The StorageUnitTier which defines the tier's properties and upgrade paths
	//   2. A StorageUnit tile that uses the tier
	//   3. A BaseStorageUnitItem item which places the tile
	//   4. A BaseStorageUpgradeItem item that applies this tier to a placed Storage Unit of the previous tier
	//   5. A BaseStorageCoreItem item that is extracted when using a Storage Core Wrench
	// Most of this is handled automatically for you, and you just have to define the relations between the components and their tier
	// In this example, the Demonite and Crimtane tiers can be upgraded to the Example tier, which can then be upgraded to the Hellstone tier

	// IMPORTANT NOTE:  As of v0.7.0.11, TEStorageUnit has an oversight which results in custom Storage Units deleting their tile entities
	//                  due to the entities thinking that the tile they're placed on is invalid.
	//                  Version 0.7.1 will fix this oversight, but in the meantime, you'll also need to declare and use a custom tile entity.

	[Autoload(false)]  // Make sure to remove this line when copying this code!
	internal class ExampleStorageUnitTier : StorageUnitTier {
		public override int UpgradeItemType => ModContent.ItemType<ExampleUpgrade>();

		public override int CoreItemType => ModContent.ItemType<ExampleCore>();

		// Since this tier is between Demonite/Crimtane and Hellstone, let's make it be the average of the tiers (100)
		// You can find the capacity of the base Magic Storage tiers at:  MagicStorage/CrossMod/Default/StorageUnitTiers.cs
		public override int Capacity => 100;

		public override int StorageUnitItemType => ModContent.ItemType<ExampleStorageUnitItem>();

		public override int StorageUnitTileType => ModContent.TileType<ExampleStorageUnitTile>();

		public override void SetStaticDefaults() {
			// Set the upgrade paths for this tier in SetStaticDefaults
			// The Demonite and Crimtane tiers can be upgraded to this tier, and this tier can be upgraded to Hellstone
			this.SetUpgradeableFrom(StorageUnitTier.Demonite);
			this.SetUpgradeableFrom(StorageUnitTier.Crimtane);
			this.SetUpgradeableTo(StorageUnitTier.Hellstone);
		}

		public override void Frame(StorageUnitFullness fullness, bool active, out int frameX, out int frameY) {
			// Based on the ExampleStorageUnitTile.png spritesheet:
			//   Style columns are 36 pixels wide total
			//   Active styles are in the first three columns
			//   Each fullness type takes up one style column
			//   There is only one row of styles
			const int STYLE_WIDTH = 36;
			const int FULLNESS_STYLES = 3;

			frameX = active ? 0 : FULLNESS_STYLES * STYLE_WIDTH;
			frameX += (int)fullness * STYLE_WIDTH;
			frameY = 0;
		}

		public override void GetState(int frameX, int frameY, out StorageUnitFullness fullness, out bool active) {
			// Based on the ExampleStorageUnitTile.png spritesheet:
			//   Style columns are 36 pixels wide total
			//   Active styles are in the first three columns
			//   Each fullness type takes up one style column
			const int STYLE_WIDTH = 36;
			const int FULLNESS_STYLES = 3;

			active = frameX < FULLNESS_STYLES * STYLE_WIDTH;
			fullness = (StorageUnitFullness)(frameX % (FULLNESS_STYLES * STYLE_WIDTH) / STYLE_WIDTH);
		}

		public override bool IsValidTile(int frameX, int frameY) {
			// Since this example only has one row of styles, there's no check needed
			// However, if you were using a spritesheet with multiple tiers, you can check the framing here
			return true;
		}
	}

	// Reminder from the note above: TEStorageUnit is broken in v0.7.0.11 and prior versions, so a custom tile entity that implemnts a band-aid fix is required

	[Autoload(false)]  // Make sure to remove this line when copying this code!
	internal class ExampleStorageUnitEntity : TEStorageUnit {
		public override bool ValidTile(in Tile tile) => TileLoader.GetTile(tile.TileType) is ExampleStorageUnitTile && tile.TileFrameX % 36 == 0 && tile.TileFrameY % 36 == 0;
	}

	[Autoload(false)]  // Make sure to remove this line when copying this code!
	internal class ExampleStorageUnitTile : MagicStorage.Components.StorageUnit {
		public override void ModifyObjectData() {
			// By default, StorageUnit expects spritesheets to have 6 style columns per row
			// If you want to modify this behavior, update:
			//   TileObjectData.newTile.StyleHorizontal
			//   TileObjectData.newTile.StyleMultiplier
			//   TileObjectData.newTile.StyleWrapLimit
			base.ModifyObjectData();
		}

		public override TEStorageUnit GetTileEntity() {
			// By default, StorageUnit places a TEStorageUnit tile entity
			// Use this method to place a custom tile entity if needed

			// Reminder from the note above: TEStorageUnit is broken in v0.7.0.11 and prior versions, so a custom tile entity that implemnts a band-aid fix is required
			return ModContent.GetInstance<ExampleStorageUnitEntity>();
		}

		protected override bool GetGlowmask(int x, int y, int type, int frameX, int frameY, out Asset<Texture2D> asset, out Color drawColor) {
			// By default, StorageUnit.GetGlowMask() uses Texture + "_Glow" for loading the glowmask texture
			// It will also emit a Color.White light blended with the world's lighting
			// Use this method to modify the glowmask behavior if needed
			return base.GetGlowmask(x, y, type, frameX, frameY, out asset, out drawColor);
		}
	}

	[Autoload(false)]  // Make sure to remove this line when copying this code!
	internal class ExampleStorageUnitItem : BaseStorageUnitItem {
		public override StorageUnitTier Tier => ModContent.GetInstance<ExampleStorageUnitTier>();

		public override void SetDefaults() {
			base.SetDefaults();
			Item.rare = ItemRarityID.Pink;  // This rarity will be automatically applied to the ExampleUpgrade and ExampleCore items as well
		}
	}

	[Autoload(false)]  // Make sure to remove this line when copying this code!
	internal class ExampleUpgrade : BaseStorageUpgradeItem {
		public override StorageUnitTier Tier => ModContent.GetInstance<ExampleStorageUnitTier>();
	}

	[Autoload(false)]  // Make sure to remove this line when copying this code!
	internal class ExampleCore : BaseStorageCore {
		public override StorageUnitTier Tier => ModContent.GetInstance<ExampleStorageUnitTier>();

		// The tooltip will default to the localization at:  Mods.MagicStorage.Items.StorageCore.CommonTooltip
		// If you want to customize the tooltip, you can override it here
		public override LocalizedText Tooltip => base.Tooltip;
	}
}
