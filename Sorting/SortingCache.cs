﻿using MagicStorage.CrossMod;
using Mono.Cecil;
using SerousCommonLib.API;
using System;
using System.Collections;
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
			if (IndexByType is null || itemType < 0 || itemType >= IndexByType.Length)
				return -1;

			return IndexByType[itemType];
		}
	}

	private class OrderByEntryOrderedEnumerable<T> : IOrderedEnumerable<T> {
		private readonly IEnumerable<T> _source;
		private readonly Entry _entry;
		private readonly Func<T, Item> _objToItem;
		private readonly IOrderedEnumerable<T> _query;

		public OrderByEntryOrderedEnumerable(IEnumerable<T> source, Entry entry, Func<T, Item> objToItem) {
			_source = source;
			_entry = entry;
			_objToItem = objToItem;
			_query = CreateQuery();
		}

		protected virtual Item GetItem(T value) => _objToItem(value);

		private int Order(T value) {
			Item item = GetItem(value);
			return _entry.FindIndex(item.type);
		}

		private IOrderedEnumerable<T> CreateQuery() {
			if (_source is null)
				return new KeepItemsInPlaceEnumerable<T>(Array.Empty<T>()); // Failsafe - a pointless collection

			if (_entry?.IndexByType is null)
				return new KeepItemsInPlaceEnumerable<T>(_source); // Preserve item order, likely because the sorter uses runtime values

			return _source.OfType<T>().OrderBy(Order);
		}

		public IOrderedEnumerable<T> CreateOrderedEnumerable<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer, bool descending) => _query.CreateOrderedEnumerable(keySelector, comparer, descending);

		public IEnumerator<T> GetEnumerator() => _query.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	private class OrderByEntryOrderedItemEnumerable : OrderByEntryOrderedEnumerable<Item> {
		public OrderByEntryOrderedItemEnumerable(IEnumerable<Item> source, Entry entry) : base(source, entry, null!) { }

		protected override Item GetItem(Item value) => value;
	}

	private readonly Dictionary<int, Entry> cache = new();

	public int FindIndex(int mode, int itemType) => cache.TryGetValue(mode, out var entry) ? entry.FindIndex(itemType) : -1;

	public void Fill()
	{
		cache.Clear();

		foreach (var option in SortingOptionLoader.Options) {
			try {
				Create(option);
			} catch (Exception ex) {
				throw new InvalidOperationException($"SortingOption.Sorter for type \"{option.GetType().GetSimplifiedGenericTypeName()}\" was invalid" +
					(option == SortingOptionLoader.Definitions.Default ? $"\nDefinitions.Default most recent class: {SortClassList.actualException_class ?? "none"}" : ""),
					ex);
			}
		}
	}

	private void Create(SortingOption option)
	{
		if (!option.CacheFuzzySorting) {
			cache[option.Type] = new Entry();
			return;
		}

		var sorter = option.Sorter.AsSafe(x => $"{x.Name} | ID: {x.type} | Mod: {x.ModItem?.Mod.Name ?? "Terraria"}");

		var itemsQuery = ContentSamples.ItemsByType.Select((pair, i) => (item: pair.Value, type: i));
		itemsQuery = option.SortInDescendingOrder ? itemsQuery.OrderByDescending(x => x.item, sorter) : itemsQuery.OrderBy(x => x.item, sorter);
		var items = itemsQuery.ToArray();

		int[] indices = new int[items.Length];

		for (int i = 0; i < items.Length; i++)
		{
			(Item item, int type) = items[i];

			indices[type] = item.IsAir ? -1 : i;
		}

		var entry = new Entry(indices);

		cache[option.Type] = entry;
	}

	/// <summary>
	///		Sorts a collection based on a <see cref="Item"/> selector
	/// </summary>
	/// <returns>A sorted collection</returns>
	public IOrderedEnumerable<T> SortFuzzy<T>(IEnumerable<T> source, Func<T, Item> objToItem, int mode) {
		ArgumentNullException.ThrowIfNull(objToItem);

		Entry entry = cache[mode];

		return new OrderByEntryOrderedEnumerable<T>(source, entry, objToItem);
	}

	public IOrderedEnumerable<Item> SortFuzzy(IEnumerable<Item> source, int mode) {
		Entry entry = cache[mode];

		return new OrderByEntryOrderedItemEnumerable(source, entry);
	}
}
