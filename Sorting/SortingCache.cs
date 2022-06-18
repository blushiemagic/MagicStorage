using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Sorting {
	/// <summary>
	/// A helper class that stores a cache of sorting order for every item ID
	/// </summary>
	public static class SortingCache {
		public static readonly SortingCacheDictionary dictionary = new();
	}

	public class SortingCacheDictionary {
		private struct Entry {
			public int[] indexByType;

			public int FindIndex(int itemType) {
				if (indexByType is null)
					return -1;
				
				return itemType < 0 || itemType >= indexByType.Length ? -1 : indexByType[itemType];
			}
		}
		
		private readonly Dictionary<SortMode, Entry> cache = new();

		public int FindIndex(SortMode mode, int itemType) => cache.TryGetValue(mode, out Entry entry) ? entry.FindIndex(itemType) : -1;

		public void Fill() {
			cache.Clear();
			
			cache[SortMode.Default] = Create(SortMode.Default);
			cache[SortMode.Id] =      Create(SortMode.Id);
			cache[SortMode.Name] =    Create(SortMode.Name);
			//These two (value and dps) are variable on item stats, but having a baseline will make the actual sorting slightly more efficient
			cache[SortMode.Value] =   Create(SortMode.Value);
			cache[SortMode.Dps] =     Create(SortMode.Dps);
			cache[SortMode.AsIs] =    new();
		}

		private static Entry Create(SortMode mode) {
			CompareFunction func = ItemSorter.MakeSortFunction(mode);
			
			var items = GetItemRange()
				.AsParallel()
				.Select(i => (new Item(i), i))
				.OrderBy(x => x.Item1, func)
				.ToList();

			int[] indices = new int[items.Count];
			Array.Fill(indices, -1);

			for (int i = 0; i < items.Count; i++) {
				(Item item, int type) = items[i];

				indices[type] = item.IsAir ? -1 : i;
			}

			return new Entry() { indexByType = indices };
		}
		
		private static IEnumerable<int> GetItemRange() {
			for (int i = 0; i < ItemLoader.ItemCount; i++)
				yield return i;
		}

		/// <summary>
		/// Sorts the items based on the cached item order
		/// </summary>
		/// <returns>A sorted collection</returns>
		public IEnumerable<Item> SortFuzzy(IEnumerable<Item> items, SortMode mode) {
			Entry entry = cache[mode];

			if (entry.indexByType is null)
				return items;

			List<Item>[] aggregate = new List<Item>[entry.indexByType.Length];
			
			foreach (Item item in items) {
				int index = entry.FindIndex(item.type);

				if (index < 0)
					continue;

				if (aggregate[index] is not List<Item> list)
					list = aggregate[index] = new();

				list.Add(item);
			}

			return aggregate.Where(e => e is not null).SelectMany(e => e);
		}
	}

	internal struct SortingCacheEntry {
		public readonly SortMode sort;
		public readonly int item;

		public SortingCacheEntry(SortMode sort, int item) {
			this.sort = sort;
			this.item = item;
		}
	}
}
