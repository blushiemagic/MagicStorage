using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;

#nullable enable

namespace MagicStorage.Sorting;

/// <summary>
///     A helper class that stores a cache of sorting order for every item ID
/// </summary>
public static class SortingCache
{
	public static readonly SortingCacheDictionary dictionary = new();
}

public class SortingCacheDictionary
{
	private class Entry
	{
		public int[]? IndexByType { get; }

		public Entry(int[]? indexByType = null)
		{
			IndexByType = indexByType;
		}

		public int FindIndex(int itemType)
		{
			if (IndexByType is null)
				return -1;

			return itemType < 0 || itemType >= IndexByType.Length ? -1 : IndexByType[itemType];
		}
	}

	private readonly Dictionary<SortMode, Entry> cache = new();

	public int FindIndex(SortMode mode, int itemType) => cache.TryGetValue(mode, out var entry) ? entry.FindIndex(itemType) : -1;

	public void Fill()
	{
		cache.Clear();

		cache[SortMode.Default] = Create(SortMode.Default);
		cache[SortMode.Id] = Create(SortMode.Id);
		cache[SortMode.Name] = Create(SortMode.Name);
		//These two (value and dps) are variable on item stats, but having a baseline will make the actual sorting slightly more efficient
		cache[SortMode.Value] = Create(SortMode.Value);
		cache[SortMode.Dps] = Create(SortMode.Dps);
		cache[SortMode.AsIs] = new();
	}

	private static Entry Create(SortMode mode)
	{
		var items = ContentSamples.ItemsByType
			.Select((pair, i) => (item: pair.Value, type: i))
			.OrderBy(x => x.item, ItemSorter.GetSortFunction(mode))
			.ToArray();

		int[] indices = new int[items.Length];

		for (int i = 0; i < items.Length; i++)
		{
			(Item item, int type) = items[i];

			indices[type] = item.IsAir ? -1 : i;
		}

		return new Entry(indices);
	}

	/// <summary>
	///     Sorts the items based on the cached item order
	/// </summary>
	/// <returns>A sorted collection</returns>
	public IOrderedEnumerable<Item> SortFuzzy(IEnumerable<Item> items, SortMode mode)
	{
		Entry entry = cache[mode];

		if (entry.IndexByType is null)
			return items.OrderBy(_ => 1); //Preserve item order

		return items.OrderBy(i => entry.FindIndex(i.type));
	}
}
