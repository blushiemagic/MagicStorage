using MagicStorage.Common;
using MagicStorage.CrossMod.Default;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.CrossMod.Storage {
	public enum StorageUnitFullness {
		Empty,
		PartiallyFull,
		Full
	}

	/// <summary>
	/// The base class containing information about a Storage Unit tier
	/// </summary>
	public abstract class StorageUnitTier : ModType {
		public static StorageUnitTier Basic { get; internal set; }

		public static StorageUnitTier Demonite { get; internal set; }

		public static StorageUnitTier Crimtane { get; internal set; }

		public static StorageUnitTier Hellstone { get; internal set; }

		public static StorageUnitTier Hallowed { get; internal set; }

		public static StorageUnitTier BlueChlorophyte { get; internal set; }

		public static StorageUnitTier Luminite { get; internal set; }

		public static StorageUnitTier Terra { get; internal set; }

		internal static StorageUnitTier Tiny { get; set; }

		public static StorageUnitTier Empty { get; internal set; }

		/// <summary>
		/// The ID of this <see cref="StorageUnitTier"/>
		/// </summary>
		public int Type { get; internal set; } = -1;

		/// <summary>
		/// The <see cref="ModContent.ItemType{T}"/> of the <see cref="Items.BaseStorageUpgradeItem"/> item that is used to apply this tier's upgrade to a Storage Unit.
		/// </summary>
		public abstract int UpgradeItemType { get; }

		/// <summary>
		/// The <see cref="ModContent.ItemType{T}"/> of the <see cref="Items.BaseStorageCore"/> item that is dropped when using a Storage Core Wrench on a Storage Unit with this tier.
		/// </summary>
		public abstract int CoreItemType { get; }

		/// <summary>
		/// How many item stacks can be stored in a Storage Unit with this tier.
		/// </summary>
		public abstract int Capacity { get; }

		/// <summary>
		/// The <see cref="ModContent.ItemType{T}"/> of the <see cref="Items.BaseStorageUnitItem"/> item that is dropped when destroying a Storage Unit with this tier.
		/// </summary>
		public abstract int StorageUnitItemType { get; }

		/// <summary>
		/// The <see cref="ModContent.TileType{T}"/> of the <see cref="Components.StorageUnit"/> tile that is used to represent a Storage Unit with this tier.
		/// </summary>
		public abstract int StorageUnitTileType { get; }

		/// <summary>
		/// The <see cref="Item.placeStyle"/> for the <see cref="Items.BaseStorageUnitItem"/> that places the Storage Unit with this tier.<br/>
		/// This property defaults to zero.
		/// </summary>
		public virtual int ItemPlaceStyle => 0;

		protected sealed override void Register() {
			ModTypeLookup<StorageUnitTier>.Register(this);
			Type = StorageUnitTierLoader.Add(this);
		}

		public sealed override void SetupContent() => SetStaticDefaults();

		protected override void ValidateType() {
			base.ValidateType();

			// The internal classes for the base tiers have special treatment, so these checks aren't needed for them
			if (this is not MagicStorageTier) {
				if (ModContent.GetModItem(UpgradeItemType) is not Items.BaseStorageUpgradeItem)
					throw new Exception($"{nameof(UpgradeItemType)} must refer to an item that inherits from {typeof(Items.BaseStorageUpgradeItem).FullName}");

				if (ModContent.GetModItem(CoreItemType) is not Items.BaseStorageCore)
					throw new Exception($"{nameof(CoreItemType)} must refer to an item that inherits from {typeof(Items.BaseStorageCore).FullName}");

				if (Capacity <= 0)
					throw new Exception($"{nameof(Capacity)} must be greater than zero");

				if (ModContent.GetModItem(StorageUnitItemType) is not Items.BaseStorageUnitItem)
					throw new Exception($"{nameof(StorageUnitItemType)} must refer to an item that inherits from {typeof(Items.BaseStorageUnitItem).FullName}");

				if (ModContent.GetModTile(StorageUnitTileType) is not Components.StorageUnit)
					throw new Exception($"{nameof(StorageUnitTileType)} must refer to a tile that inherits from {typeof(Components.StorageUnit).FullName}");

				if (ItemPlaceStyle < 0)
					throw new Exception($"{nameof(ItemPlaceStyle)} must be greater than or equal to zero");
			}
		}

		private readonly HashSet<int> _blacklisted = [];
		private readonly HashSet<int> _upgradedByHash = [];
		private readonly List<StorageUnitTier> _canBeUpgradedBy = [];

		internal static readonly CircularDependencyChecker<StorageUnitTier> circularDependencyChecker = new(
			static (a, b) => a.Type >= 0 && b.Type >= 0 ? a.Type == b.Type : object.ReferenceEquals(a, b),
			static upgrade => upgrade._canBeUpgradedBy,
			FullNameExceptFromMagicStorage
		);

		private static string FullNameExceptFromMagicStorage(StorageUnitTier tier) => tier.Mod is MagicStorageMod ? tier.Name : tier.FullName;

		/// <summary>
		/// A read-only list of tiers which this tier can be upgraded to.
		/// </summary>
		public IReadOnlyList<StorageUnitTier> NextTiers => (_canBeUpgradedBy ?? []).AsReadOnly();

		/// <summary>
		/// Returns whether a Storage Unit with <see langword="this"/> tier can be upgraded to <paramref name="other"/>
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		public bool CanUpgradeTo(StorageUnitTier other) {
			ArgumentNullException.ThrowIfNull(other);

			return !_blacklisted.Contains(other.Type) && _upgradedByHash.Contains(other.Type);
		}

		/// <summary>
		/// Marks <see langword="this"/> as upgradeable by <paramref name="nextTier"/> (<see langword="this"/> --> <paramref name="nextTier"/>).<br/>
		/// Does nothing if the connection was previously destroyed
		/// </summary>
		/// <exception cref="ArgumentException"/>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="InvalidOperationException"/>
		public void SetUpgradeableTo(StorageUnitTier nextTier) {
			ArgumentNullException.ThrowIfNull(nextTier);

			if (!StorageUnitTierLoader.Loading)
				throw new Exception("StorageUnitTier connections can only be established during mod loading");

			if (object.ReferenceEquals(nextTier, this))
				throw new ArgumentException("An upgrade cannot upgrade to itself", nameof(nextTier));

			if (_blacklisted.Contains(nextTier.Type)) {
				Mod.Logger.Warn($"Attempt to connect upgrade path ({FullNameExceptFromMagicStorage(this)} --> {FullNameExceptFromMagicStorage(nextTier)}) was blocked due to a mod preventing the connection.");
				return;
			}

			// this --> other
			if (_upgradedByHash.Add(nextTier.Type)) {
				_canBeUpgradedBy.Add(nextTier);

				// Check for circular upgrade paths
				var checker = circularDependencyChecker.Copy();
				if (!checker.Run(this)) {
					string path = string.Join(" -> ", checker.GetCircularDependencyPath());
					throw new InvalidOperationException($"Circular upgrade path detected\n  {path}");
				}
			}
		}

		/// <summary>
		/// Marks <paramref name="previousTier"/> as upgradeable by <see langword="this"/> (<paramref name="previousTier"/> --> <see langword="this"/>).
		/// </summary>
		/// <exception cref="ArgumentException"/>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="InvalidOperationException"/>
		public void SetUpgradeableFrom(StorageUnitTier previousTier) => previousTier.SetUpgradeableTo(this);

		/// <summary>
		/// Permanently removes the upgrade path from <see langword="this"/> to <paramref name="adjacentTier"/>, even if it currently does not exist.<br/>
		/// Attempting to reconnect the upgrade tiers again will not restore the connection.<br/>
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="Exception"/>
		public void RemoveConnections(StorageUnitTier adjacentTier) {
			ArgumentNullException.ThrowIfNull(adjacentTier);

			if (!StorageUnitTierLoader.Loading)
				throw new Exception("StorageUnitTier connections can only be removed during mod loading");

			if (_blacklisted.Add(adjacentTier.Type)) {
				// Remove the connection: this --> adjacentTier
				if (_upgradedByHash.Remove(adjacentTier.Type))
					_canBeUpgradedBy.Remove(adjacentTier);

				// Remove the connection: adjacentTier --> this
				adjacentTier.RemoveConnections(this);
			}
		}

		/// <summary>
		/// Permanently removes all existing upgrade paths for this upgrade tier.<br/>
		/// Attempting to reconnect any of the affected upgrade tiers will not restore the connections.<br/>
		/// </summary>
		/// <exception cref="Exception"/>
		public void RemoveAllConnections() {
			if (!StorageUnitTierLoader.Loading)
				throw new Exception("StorageUnitTier connections can only be removed during mod loading");

			foreach (var upgrade in _canBeUpgradedBy) {
				// Remove the connection: this --> upgrade
				_blacklisted.Add(upgrade.Type);
				// Remove the connection: upgrade --> this
				upgrade._blacklisted.Add(Type);
			}

			_canBeUpgradedBy.Clear();
			_upgradedByHash.Clear();
		}

		/// <summary>
		/// Return whether a tile with the given <see cref="Tile.TileFrameX"/> and <see cref="Tile.TileFrameY"/> has this upgrade tier.<br/>
		/// The ID of the tile will always be equal to <see cref="StorageUnitTileType"/>
		/// </summary>
		/// <param name="frameX">The X-coordinate of the top-leftmost corner of the Storage Unit</param>
		/// <param name="frameY">The Y-coordinate of the top-leftmost corner of the Storage Unit</param>
		public abstract bool IsValidTile(int frameX, int frameY);

		/// <summary>
		/// Set <paramref name="frameX"/> and <paramref name="frameY"/> based on the fullness of the Storage Unit and whether it is disabled.
		/// </summary>
		/// <param name="fullness">The fullness state of the Storage Unit</param>
		/// <param name="active"><see langword="true"/> if the Storage Unit is not disabled, <see langword="false"/> otherwise</param>
		/// <param name="frameX">The X-coordinate of the top-leftmost corner of the Storage Unit</param>
		/// <param name="frameY">The Y-coordinate of the top-leftmost corner of the Storage Unit</param>
		public abstract void Frame(StorageUnitFullness fullness, bool active, out int frameX, out int frameY);

		/// <summary>
		/// Get the fullness state and whether the Storage Unit is disabled based on its tile frame coordinates.
		/// </summary>
		/// <param name="frameX">The X-coordinate of the top-leftmost corner of the Storage Unit</param>
		/// <param name="frameY">The Y-coordinate of the top-leftmost corner of the Storage Unit</param>
		/// <param name="fullness">The fullness state of the Storage Unit</param>
		/// <param name="active"><see langword="true"/> if the Storage Unit is not disabled, <see langword="false"/> otherwise</param>
		public abstract void GetState(int frameX, int frameY, out StorageUnitFullness fullness, out bool active);
	}

	public static class StorageUnitTierLoader {
		private class Loadable : ILoadable {
			public void Load(Mod mod) {
				mod.AddContent(StorageUnitTier.Basic = new TierBasic());
				mod.AddContent(StorageUnitTier.Demonite = new TierDemonite());
				mod.AddContent(StorageUnitTier.Crimtane = new TierCrimtane());
				mod.AddContent(StorageUnitTier.Hellstone = new TierHellstone());
				mod.AddContent(StorageUnitTier.Hallowed = new TierHallowed());
				mod.AddContent(StorageUnitTier.BlueChlorophyte = new TierBlueChlorophyte());
				mod.AddContent(StorageUnitTier.Luminite = new TierLuminite());
				mod.AddContent(StorageUnitTier.Terra = new TierTerra());
				mod.AddContent(StorageUnitTier.Tiny = new TierTiny());
				mod.AddContent(StorageUnitTier.Empty = new TierEmpty());

				Loading = true;
			}

			public void Unload() {
				_tiers.Clear();
			}
		}

		internal static bool Loading { get; private set; }

		private static readonly List<StorageUnitTier> _tiers = [];

		public static int Count => _tiers.Count;

		internal static int Add(StorageUnitTier upgrade) {
			_tiers.Add(upgrade);
			return _tiers.Count - 1;
		}

		public static StorageUnitTier Get(int type) => type < 0 || type >= _tiers.Count ? null : _tiers[type];

		internal static void PostSetupContent() {
			Loading = false;
		}

		public static StorageUnitTier FindFromCoreItem(Items.BaseStorageCore core) {
			ArgumentNullException.ThrowIfNull(core);

			foreach (var tier in _tiers) {
				if (tier.CoreItemType == core.Type)
					return tier;
			}

			return null;
		}

		public static StorageUnitTier FindFromUpgradeItem(Items.BaseStorageUpgradeItem upgradeItem) {
			ArgumentNullException.ThrowIfNull(upgradeItem);

			foreach (var tier in _tiers) {
				if (tier.UpgradeItemType == upgradeItem.Type)
					return tier;
			}

			return null;
		}

		public static StorageUnitTier FindFromTile(int x, int y) {
			Tile tile = Main.tile[x, y];
			return FindFromTileFrame(tile.TileType, tile.TileFrameX, tile.TileFrameY);
		}

		public static StorageUnitTier FindFromTileFrame(int type, int frameX, int frameY) {
			if (TileLoader.GetTile(type) is not Components.StorageUnit)
				return null;

			foreach (var tier in _tiers) {
				if (tier.StorageUnitTileType == type && tier.IsValidTile(frameX, frameY))
					return tier;
			}

			return null;
		}
	}
}
