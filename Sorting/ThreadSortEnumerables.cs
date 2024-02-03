using MagicStorage.CrossMod;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Sorting {
	internal class ThreadSortOrderedEnumerable<T> : IOrderedEnumerable<T> {
		protected readonly StorageGUI.ThreadContext _context;
		protected readonly IEnumerable<T> _source;
		protected readonly Func<T, Item> _objToItem;
		protected readonly IOrderedEnumerable<T> _query;

		public ThreadSortOrderedEnumerable(StorageGUI.ThreadContext context, IEnumerable<T> source, Func<T, Item> objToItem) {
			_context = context;
			_source = source;
			_objToItem = objToItem;
			_query = CreateQuery();
		}

		protected virtual Item GetItem(T value) => _objToItem(value);

		protected virtual IOrderedEnumerable<T> SortFuzzy() => SortingCache.dictionary.SortFuzzy(_source, GetItem, _context.sortMode);

		private IOrderedEnumerable<T> CreateQuery() {
			try {
				if (_context.sortMode < 0)
					return new KeepItemsInPlaceEnumerable<T>(_source);

				//Apply "fuzzy" sorting since it's faster, but less accurate
				IOrderedEnumerable<T> orderedItems = SortFuzzy();

				var sorter = SortingOptionLoader.Get(_context.sortMode);

				if (!sorter.CacheFuzzySorting || sorter.SortAgainAfterFuzzy) {
					var sortFunc = sorter.Sorter.AsSafe(x => $"{x.Name} | ID: {x.type} | Mod: {x.ModItem?.Mod.Name ?? "Terraria"}");

					orderedItems = sorter.SortInDescendingOrder ? orderedItems.OrderByDescending(GetItem, sortFunc) : orderedItems.OrderBy(GetItem, sortFunc);
				}

				return orderedItems.ThenBy(GetItem, CompareID.Instance).ThenByDescending(GetItem, CompareValue.Instance);
			} catch (Exception ex) {
				MagicStorageMod.Instance.Logger.Error("Query attempt failed", ex);

				// Default to keeping the items in place
				return new KeepItemsInPlaceEnumerable<T>(_source);
			}
		}

		public IOrderedEnumerable<T> CreateOrderedEnumerable<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer, bool descending) => _query.CreateOrderedEnumerable(keySelector, comparer, descending);

		public IEnumerator<T> GetEnumerator() => _query.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	internal class ThreadSortOrderedItemEnumerable : ThreadSortOrderedEnumerable<Item> {
		public ThreadSortOrderedItemEnumerable(StorageGUI.ThreadContext context, IEnumerable<Item> source) : base(context, source, null) { }

		protected override Item GetItem(Item value) => value;

		protected override IOrderedEnumerable<Item> SortFuzzy() => SortingCache.dictionary.SortFuzzy(_source, _context.sortMode);
	}
}
