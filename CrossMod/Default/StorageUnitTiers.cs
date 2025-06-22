using MagicStorage.CrossMod.Storage;
using MagicStorage.Items;
using System;
using Terraria.ModLoader;

namespace MagicStorage.CrossMod.Default {
	internal abstract class MagicStorageTier : StorageUnitTier {
		public sealed override int StorageUnitTileType => ModContent.TileType<Components.StorageUnit>();

		public sealed override int ItemPlaceStyle => FrameRow;

		protected abstract int FrameRow { get; }

		protected abstract StorageUnitTier UpgradesFrom { get; }

		public override void SetStaticDefaults() {
			if (UpgradesFrom is not null)
				SetUpgradeableFrom(UpgradesFrom);
		}

		public override bool IsValidTile(int frameX, int frameY) => frameY / 36 == FrameRow;

		public override void Frame(StorageUnitFullness fullness, bool active, out int frameX, out int frameY) {
			frameX = (int)fullness * 36;
			if (!active)
				frameX += 3 * 36;

			frameY = FrameRow * 36;
		}

		public override void GetState(int frameX, int frameY, out StorageUnitFullness fullness, out bool active) {
			fullness = (StorageUnitFullness)(frameX / 36 % 3);
			active = frameX < 3 * 36;
		}
	}

	[Autoload(false)]
	internal sealed class TierBasic : MagicStorageTier {
		public override int UpgradeItemType => -1;
		public override int CoreItemType => ModContent.ItemType<StorageCore>();
		public override int Capacity => 40;
		public override int StorageUnitItemType => ModContent.ItemType<StorageUnit>();
		protected override int FrameRow => 0;
		protected override StorageUnitTier UpgradesFrom => null;
	}

	[Autoload(false)]
	internal sealed class TierDemonite : MagicStorageTier {
		public override int UpgradeItemType => ModContent.ItemType<UpgradeDemonite>();
		public override int CoreItemType => ModContent.ItemType<StorageCoreDemonite>();
		public override int Capacity => 80;
		public override int StorageUnitItemType => ModContent.ItemType<StorageUnitDemonite>();
		protected override int FrameRow => 1;
		protected override StorageUnitTier UpgradesFrom => Basic;
	}

	[Autoload(false)]
	internal sealed class TierCrimtane : MagicStorageTier {
		public override int UpgradeItemType => ModContent.ItemType<UpgradeCrimtane>();
		public override int CoreItemType => ModContent.ItemType<StorageCoreCrimtane>();
		public override int Capacity => 80;
		public override int StorageUnitItemType => ModContent.ItemType<StorageUnitCrimtane>();
		protected override int FrameRow => 2;
		protected override StorageUnitTier UpgradesFrom => Basic;
	}

	[Autoload(false)]
	internal sealed class TierHellstone : MagicStorageTier {
		public override int UpgradeItemType => ModContent.ItemType<UpgradeHellstone>();
		public override int CoreItemType => ModContent.ItemType<StorageCoreHellstone>();
		public override int Capacity => 120;
		public override int StorageUnitItemType => ModContent.ItemType<StorageUnitHellstone>();
		protected override int FrameRow => 3;
		protected override StorageUnitTier UpgradesFrom => throw new NotImplementedException();

		public override void SetStaticDefaults() {
			SetUpgradeableFrom(Demonite);
			SetUpgradeableFrom(Crimtane);
		}
	}

	[Autoload(false)]
	internal sealed class TierHallowed : MagicStorageTier {
		public override int UpgradeItemType => ModContent.ItemType<UpgradeHallowed>();
		public override int CoreItemType => ModContent.ItemType<StorageCoreHallowed>();
		public override int Capacity => 160;
		public override int StorageUnitItemType => ModContent.ItemType<StorageUnitHallowed>();
		protected override int FrameRow => 4;
		protected override StorageUnitTier UpgradesFrom => Hellstone;
	}

	[Autoload(false)]
	internal sealed class TierBlueChlorophyte : MagicStorageTier {
		public override int UpgradeItemType => ModContent.ItemType<UpgradeBlueChlorophyte>();
		public override int CoreItemType => ModContent.ItemType<StorageCoreBlueChlorophyte>();
		public override int Capacity => 240;
		public override int StorageUnitItemType => ModContent.ItemType<StorageUnitBlueChlorophyte>();
		protected override int FrameRow => 5;
		protected override StorageUnitTier UpgradesFrom => Hallowed;
	}

	[Autoload(false)]
	internal sealed class TierLuminite : MagicStorageTier {
		public override int UpgradeItemType => ModContent.ItemType<UpgradeLuminite>();
		public override int CoreItemType => ModContent.ItemType<StorageCoreLuminite>();
		public override int Capacity => 320;
		public override int StorageUnitItemType => ModContent.ItemType<StorageUnitLuminite>();
		protected override int FrameRow => 6;
		protected override StorageUnitTier UpgradesFrom => BlueChlorophyte;
	}

	[Autoload(false)]
	internal sealed class TierTerra : MagicStorageTier {
		public override int UpgradeItemType => ModContent.ItemType<UpgradeTerra>();
		public override int CoreItemType => ModContent.ItemType<StorageCoreTerra>();
		public override int Capacity => 400;
		public override int StorageUnitItemType => ModContent.ItemType<StorageUnitTerra>();
		protected override int FrameRow => 7;
		protected override StorageUnitTier UpgradesFrom => Luminite;
	}

	// NOTE: this isn't actually used, it's just kept for backwards compatibility
	[Autoload(false)]
	internal sealed class TierTiny : MagicStorageTier {
		public override int UpgradeItemType => -1;
		public override int CoreItemType => -1;
		public override int Capacity => 4;
		public override int StorageUnitItemType => ModContent.ItemType<StorageUnitTiny>();
		protected override int FrameRow => 8;
		protected override StorageUnitTier UpgradesFrom => null;
	}

	[Autoload(false)]
	internal sealed class TierEmpty : MagicStorageTier {
		public override int UpgradeItemType => -1;
		public override int CoreItemType => -1;
		public override int Capacity => 0;
		public override int StorageUnitItemType => ModContent.ItemType<StorageUnitEmpty>();
		protected override int FrameRow => 9;
		protected override StorageUnitTier UpgradesFrom => null;

		public override void GetState(int frameX, int frameY, out StorageUnitFullness fullness, out bool active) {
			// Empty units are always inactive and empty
			fullness = StorageUnitFullness.Empty;
			active = false;
		}
	}
}
