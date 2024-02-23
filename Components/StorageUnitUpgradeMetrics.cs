using MagicStorage.Items;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace MagicStorage.Components {
	public enum StorageUnitTier {
		Basic = 0,
		Demonite = 1,
		Crimtane = 2,
		Hellstone = 3,
		Hallowed = 4,
		BlueChlorophyte = 5,
		Luminite = 6,
		Terra = 7,
		Tiny = 8,
		Empty = 9
	}

	internal class StorageUnitUpgradeMetrics {
		private static readonly Dictionary<StorageUnitTier, StorageUnitUpgradeMetrics> _metrics = new() {
			// Basic -> Demonite / Crimtane
			[StorageUnitTier.Basic] = Create<Items.StorageUnit, StorageCore>(40, new List<StorageUnitUpgradeBehavior> {
				new StorageUnitUpgradeBasicToDemonite(),
				new StorageUnitUpgradeBasicToCrimtane()
			}),
			// Demonite -> Hellstone
			[StorageUnitTier.Demonite] = Create<StorageUnitDemonite, StorageCoreDemonite>(80, new List<StorageUnitUpgradeBehavior> {
				new StorageUnitUpgradeDemoniteToHellstone()
			}),
			// Crimtane -> Hellstone
			[StorageUnitTier.Crimtane] = Create<StorageUnitCrimtane, StorageCoreCrimtane>(80, new List<StorageUnitUpgradeBehavior> {
				new StorageUnitUpgradeCrimtaneToHellstone()
			}),
			// Hellstone -> Hallowed
			[StorageUnitTier.Hellstone] = Create<StorageUnitHellstone, StorageCoreHellstone>(120, new List<StorageUnitUpgradeBehavior> {
				new StorageUnitUpgradeHellstoneToHallowed()
			}),
			// Hallowed -> Blue Chlorophyte
			[StorageUnitTier.Hallowed] = Create<StorageUnitHallowed, StorageCoreHallowed>(160, new List<StorageUnitUpgradeBehavior> {
				new StorageUnitUpgradeHallowedToBlueChlorophyte()
			}),
			// Blue Chlorophyte -> Luminite
			[StorageUnitTier.BlueChlorophyte] = Create<StorageUnitBlueChlorophyte, StorageCoreBlueChlorophyte>(240, new List<StorageUnitUpgradeBehavior> {
				new StorageUnitUpgradeBlueChlorophyteToLuminite()
			}),
			// Luminite -> Terra
			[StorageUnitTier.Luminite] = Create<StorageUnitLuminite, StorageCoreLuminite>(320, new List<StorageUnitUpgradeBehavior> {
				new StorageUnitUpgradeLuminiteToTerra()
			}),
			// Terra -> n/a
			[StorageUnitTier.Terra] = Create<StorageUnitTerra, StorageCoreTerra>(640, new List<StorageUnitUpgradeBehavior>())
		};

		public static bool AttemptUpgrade(ref int tileStyle, int upgradingItem) {
			if (tileStyle < 0 || tileStyle > (int)StorageUnitTier.Terra)
				return false;

			StorageUnitTier tier = (StorageUnitTier)tileStyle;

			if (_metrics.TryGetValue(tier, out var metric)) {
				foreach (var behavior in metric.upgrading) {
					if (behavior.UpgradeItem == upgradingItem) {
						tileStyle = behavior.UpgradeToStyle;
						return true;
					}
				}
			}

			return false;
		}

		public static int GetUnitItem(int tileStyle) {
			if (tileStyle < 0 || tileStyle > (int)StorageUnitTier.Terra)
				return 0;

			StorageUnitTier tier = (StorageUnitTier)tileStyle;

			if (_metrics.TryGetValue(tier, out var metric))
				return metric.unitItem;

			return 0;
		}

		public static int GetUnitItem(StorageUnitTier tier) {
			if (_metrics.TryGetValue(tier, out var metric))
				return metric.unitItem;

			return 0;
		}

		public static int GetCoreItem(int tileStyle) {
			if (tileStyle < 0 || tileStyle > (int)StorageUnitTier.Terra)
				return 0;

			StorageUnitTier tier = (StorageUnitTier)tileStyle;

			if (_metrics.TryGetValue(tier, out var metric))
				return metric.coreItem;

			return 0;
		}

		public static int GetCoreItem(StorageUnitTier tier) {
			if (_metrics.TryGetValue(tier, out var metric))
				return metric.coreItem;

			return 0;
		}

		public static int GetCapacity(int tileStyle) {
			if (tileStyle < 0 || tileStyle > (int)StorageUnitTier.Terra)
				return 0;

			StorageUnitTier tier = (StorageUnitTier)tileStyle;

			if (_metrics.TryGetValue(tier, out var metric))
				return metric.capacity;

			return 0;
		}

		public static int GetCapacity(StorageUnitTier tier) {
			if (_metrics.TryGetValue(tier, out var metric))
				return metric.capacity;

			return 0;
		}
		
		public readonly int capacity;
		public readonly int unitItem;
		public readonly int coreItem;
		public readonly List<StorageUnitUpgradeBehavior> upgrading;

		private StorageUnitUpgradeMetrics(int capacity, int unitItem, int coreItem, List<StorageUnitUpgradeBehavior> upgrading) {
			this.capacity = capacity;
			this.unitItem = unitItem;
			this.coreItem = coreItem;
			this.upgrading = upgrading;
		}

		private static StorageUnitUpgradeMetrics Create<TUnit, TCore>(int capacity, List<StorageUnitUpgradeBehavior> upgrading) where TCore : BaseStorageCore where TUnit : BaseStorageUnitItem {
			return new StorageUnitUpgradeMetrics(capacity, ModContent.ItemType<TUnit>(), ModContent.ItemType<TCore>(), upgrading);
		}
	}

	internal abstract class StorageUnitUpgradeBehavior {
		public abstract int RequiredFramingStyle { get; }

		public abstract int UpgradeToStyle { get; }

		public abstract int UpgradeItem { get; }
	}

	internal abstract class StorageUnitUpgradeBehavior<T> : StorageUnitUpgradeBehavior where T : BaseStorageUpgradeItem {
		public sealed override int UpgradeItem => ModContent.ItemType<T>();
	}

	internal sealed class StorageUnitUpgradeBasicToDemonite : StorageUnitUpgradeBehavior<UpgradeDemonite> {
		public override int RequiredFramingStyle => 0;

		public override int UpgradeToStyle => 1;
	}

	internal sealed class StorageUnitUpgradeBasicToCrimtane : StorageUnitUpgradeBehavior<UpgradeCrimtane> {
		public override int RequiredFramingStyle => 0;

		public override int UpgradeToStyle => 2;
	}

	internal sealed class StorageUnitUpgradeDemoniteToHellstone : StorageUnitUpgradeBehavior<UpgradeHellstone> {
		public override int RequiredFramingStyle => 1;

		public override int UpgradeToStyle => 3;
	}

	internal sealed class StorageUnitUpgradeCrimtaneToHellstone : StorageUnitUpgradeBehavior<UpgradeHellstone> {
		public override int RequiredFramingStyle => 2;

		public override int UpgradeToStyle => 3;
	}

	internal sealed class StorageUnitUpgradeHellstoneToHallowed : StorageUnitUpgradeBehavior<UpgradeHallowed> {
		public override int RequiredFramingStyle => 3;

		public override int UpgradeToStyle => 4;
	}

	internal sealed class StorageUnitUpgradeHallowedToBlueChlorophyte : StorageUnitUpgradeBehavior<UpgradeBlueChlorophyte> {
		public override int RequiredFramingStyle => 4;

		public override int UpgradeToStyle => 5;
	}

	internal sealed class StorageUnitUpgradeBlueChlorophyteToLuminite : StorageUnitUpgradeBehavior<UpgradeLuminite> {
		public override int RequiredFramingStyle => 5;

		public override int UpgradeToStyle => 6;
	}

	internal sealed class StorageUnitUpgradeLuminiteToTerra : StorageUnitUpgradeBehavior<UpgradeTerra> {
		public override int RequiredFramingStyle => 6;

		public override int UpgradeToStyle => 7;
	}
}
