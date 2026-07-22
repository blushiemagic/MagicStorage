using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;

namespace MagicStorage.CrossMod {
	/// <summary>
	/// A singleton type that allows hooking into the logic responsible for aggregating items in storage.
	/// </summary>
	public abstract class StorageAggregator : ModType {
		/// <summary>
		/// The loader-assigned ID of this storage aggregator.
		/// </summary>
		public int Type { get; private set; }

		private bool? _selectsData;
		internal bool SelectsItemData {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _selectsData ??= LoaderUtils.HasOverride(this, m => m.SelectData);
		}

		private bool? _selectGlobalData;
		internal bool SelectsGlobalData {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _selectGlobalData ??= LoaderUtils.HasOverride(this, m => m.SelectGlobalData);
		}

		/// <inheritdoc/>
		protected sealed override void Register() {
			ModTypeLookup<StorageAggregator>.Register(this);

			Type = StorageAggregatorLoader.Add(this);
		}

		/// <inheritdoc/>
		public sealed override void SetupContent() => SetStaticDefaults();

		/// <summary>
		/// Whether this aggregator applies to the given item.
		/// </summary>
		/// <param name="item">The item being aggregated</param>
		public virtual bool AppliesToItem(Item item) => true;

		/// <summary>
		/// Check if <paramref name="checking"/> can be aggregated into <paramref name="destination"/> here.<br/>
		/// Both items are guaranteed to have the same <see cref="Item.type"/> value.
		/// </summary>
		/// <param name="destination">The aggregating item stack</param>
		/// <param name="checking">The item being aggregated</param>
		/// <returns>
		/// <see langword="null"/> to use the default behavior which uses data comparisons, <see langword="false"/> to prevent the aggregation of the two items or <see langword="true"/> to force it.<br/>
		/// Returns <see langword="null"/> by default.
		/// </returns>
		public virtual bool? CanAggregateItems(Item destination, Item checking) => null;

		/// <summary>
		/// Checks whether <paramref name="checking"/> can be aggregated into <paramref name="destination"/>.
		/// </summary>
		/// <param name="destination">The aggregating item stack</param>
		/// <param name="checking">The item being aggregated</param>
		/// <param name="checkPrefix">Whether to check the prefixes of the items</param>
		/// <returns>If the items can be aggregated together.</returns>
		public static bool CanCombineItems(Item destination, Item checking, bool checkPrefix = true) => CanCombineItems(destination, checking, checkPrefix, true, null);

		/// <summary>
		/// Checks whether <paramref name="checking"/> can be aggregated into <paramref name="destination"/>.
		/// </summary>
		/// <param name="destination">The aggregating item stack</param>
		/// <param name="checking">The item being aggregated</param>
		/// <param name="checkPrefix">Whether to check the prefixes of the items</param>
		/// <param name="strict">Whether the data for both items should be compared.  This parameter is forced to <see langword="true"/> if <paramref name="savedItemTagIO"/> is not <see langword="null"/>.</param>
		/// <param name="savedItemTagIO">An optional cache for item data</param>
		/// <returns>If the items can be aggregated together</returns>
		public static bool CanCombineItems(Item destination, Item checking, bool checkPrefix, bool strict, ConditionalWeakTable<Item, byte[]> savedItemTagIO) => StorageAggregatorLoader.CanAggregateItems(destination, checking, checkPrefix, strict, savedItemTagIO);

		/// <summary>
		/// Remove data from <paramref name="tag"/> that is not needed for item comparisons.
		/// </summary>
		/// <param name="item">The modded item instance</param>
		/// <param name="tag">The TagCompound which <see cref="ModItem.SaveData(TagCompound)"/> saved data into.</param>
		/// <returns>Whether <paramref name="tag"/> was modified</returns>
		public virtual void SelectData(ModItem item, TagCompound tag) { }

		/// <summary>
		/// Remove data from <paramref name="tag"/> that is not needed for item comparisons.
		/// </summary>
		/// <param name="item">The global item instance</param>
		/// <param name="tag">The TagCompound which <see cref="GlobalItem.SaveData(Item, TagCompound)"/> saved data into.</param>
		/// <returns>Whether <paramref name="tag"/> was modified</returns>
		public virtual void SelectGlobalData(GlobalItem item, TagCompound tag) { }
	}

	internal static class StorageAggregatorLoader {
		private class Loadable : ILoadable {
			void ILoadable.Load(Mod mod) { }

			void ILoadable.Unload() {
				_aggregators.Clear();
				_itemDataAggregators = null;
				_globalDataAggregators = null;
				_dataAggregatorCacheDirty = true;
			}
		}

		private static readonly List<StorageAggregator> _aggregators = new();
		private static StorageAggregator[] _itemDataAggregators;
		private static StorageAggregator[] _globalDataAggregators;
		private static bool _dataAggregatorCacheDirty = true;

		public static int Count => _aggregators.Count;

		internal static int Add(StorageAggregator aggregator) {
			_aggregators.Add(aggregator);
			_dataAggregatorCacheDirty = true;
			return _aggregators.Count - 1;
		}

		internal static bool CanAggregateItems(Item destination, Item checking, bool checkPrefix = true, bool strict = true, ConditionalWeakTable<Item, byte[]> savedItemTagIO = null) {
			// Operation was performed with data caching, force strict item comparison
			if (savedItemTagIO is not null)
				strict = true;

			int prefixDestination = destination.prefix;
			int prefixChecking = checking.prefix;

			if (!checkPrefix) {
				destination.prefix = 0;
				checking.prefix = 0;
			}

			if ((checkPrefix && !ItemData.Matches(destination, checking)) || destination.type != checking.type) {
				destination.prefix = prefixDestination;
				checking.prefix = prefixChecking;
				return false;
			}

			if (CheckAggregators(destination, checking) is bool aggregatorResult) {
				destination.prefix = prefixDestination;
				checking.prefix = prefixChecking;
				return aggregatorResult;
			}

			bool combine = ItemLoader.CanStack(destination, checking);

			if (combine && strict)
				combine &= Utility.AreStrictlyEqual(destination, checking, checkStack: false, checkPrefix: checkPrefix, savedItemTagIO: savedItemTagIO);

			destination.prefix = prefixDestination;
			checking.prefix = prefixChecking;

			return combine;
		}

		private static bool? CheckAggregators(Item destination, Item checking) {
			bool? combine = null;

			try {
				foreach (var aggregator in _aggregators) {
					if (aggregator.AppliesToItem(destination) && aggregator.AppliesToItem(checking)) {
						bool? result = aggregator.CanAggregateItems(destination, checking);

						if (result is bool resultValue) {
							if (!resultValue)
								return false;

							combine = true;
						}
					}
				}
			} catch {
				// Swallow the exception and prevent stacking
				combine = false;
			}

			return combine;
		}

		internal static bool GetItemData(Item item, out TagCompound tag) {
			try {
				RefreshDataAggregatorCache();
				var instanceAggregators = _itemDataAggregators;
				var globalAggregators = _globalDataAggregators;
				if (instanceAggregators.Length == 0 && globalAggregators.Length == 0) {
					// Use default behavior if no aggregator selects item data
					tag = null;
					return false;
				}

				tag = new();

				if (item.type <= ItemID.None)
					return false;

				if (item.ModItem is ModItem modItem) {
					tag["mod"] = modItem.Mod.Name;
					tag["name"] = modItem.Name;

					TagCompound saveData = new();
					modItem.SaveData(saveData);
					foreach (var aggregator in instanceAggregators)
						aggregator.SelectData(modItem, saveData);

					if (saveData.Count > 0)
						tag["data"] = saveData;
				} else {
					tag["mod"] = "Terraria";
					tag["id"] = item.netID;
				}

				if (PrefixLoader.GetPrefix(item.prefix) is ModPrefix prefix) {
					if (prefix is UnloadedPrefix) {
						UnloadedGlobalItem unloadedGlobalItem = item.GetGlobalItem<UnloadedGlobalItem>();
						tag.Set("modPrefixMod", unloadedGlobalItem.ModPrefixMod);
						tag.Set("modPrefixName", unloadedGlobalItem.ModPrefixName);
					} else {
						tag.Set("modPrefixMod", prefix.Mod.Name);
						tag.Set("modPrefixName", prefix.Name);
					}
				} else if (item.prefix != 0 && item.prefix < PrefixID.Count)
					tag.Set("prefix", (byte)item.prefix);

				if (item.stack > 1)
					tag.Set("stack", item.stack);

				if (item.favorited)
					tag.Set("fav", true);

				tag["globalData"] = GetGlobalItemData(item, globalAggregators);

				return true;
			} catch {
				// Swallow the exception and prevent item data selection
				tag = null;
				return false;
			}
		}

		private static void RefreshDataAggregatorCache() {
			if (!_dataAggregatorCacheDirty)
				return;

			_itemDataAggregators = [.. _aggregators.Where(static a => a.SelectsItemData)];
			_globalDataAggregators = [.. _aggregators.Where(static a => a.SelectsGlobalData)];
			_dataAggregatorCacheDirty = false;
		}

		private static List<TagCompound> GetGlobalItemData(Item item, StorageAggregator[] aggregators) {
			if (item.ModItem is UnloadedItem)
				return null;  // UnloadedItems cannot have global data

			List<TagCompound> list = [];
			TagCompound saveData = new();

			foreach (var globalItem in item.Globals) {
				if (globalItem is UnloadedGlobalItem unloaded) {
					list.AddRange(unloaded.data);
					continue;
				}

				globalItem.SaveData(item, saveData);

				foreach (var aggregator in aggregators)
					aggregator.SelectGlobalData(globalItem, saveData);

				if (saveData.Count == 0)
					continue;

				list.Add(new() {
					["mod"] = globalItem.Mod.Name,
					["name"] = globalItem.Name,
					["data"] = saveData
				});

				saveData = new();
			}

			return list.Count > 0 ? list : null;
		}
	}
}
