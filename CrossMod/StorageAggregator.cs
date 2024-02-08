using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.CrossMod {
	/// <summary>
	/// A singleton type that allows hooking into the logic responsible for aggregating items in storage.
	/// </summary>
	public abstract class StorageAggregator : ModType {
		public int Type { get; private set; }

		protected sealed override void Register() {
			ModTypeLookup<StorageAggregator>.Register(this);

			Type = StorageAggregatorLoader.Add(this);
		}

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
		/// <see langword="null"/> to use the default behavior which uses strict data comparisons, <see langword="false"/> to prevent the aggregation of the two items or <see langword="true"/> to force it.<br/>
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
	}

	internal static class StorageAggregatorLoader {
		private class Loadable : ILoadable {
			void ILoadable.Load(Mod mod) { }

			void ILoadable.Unload() {
				_aggregators.Clear();
			}
		}

		private static readonly List<StorageAggregator> _aggregators = new();

		public static int Count => _aggregators.Count;

		internal static int Add(StorageAggregator aggregator) {
			_aggregators.Add(aggregator);
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
	}
}
