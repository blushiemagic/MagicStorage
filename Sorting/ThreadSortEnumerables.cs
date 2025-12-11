using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.CrossMod;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Sorting {
	internal abstract class ThreadSortOrderedEnumerable<T> : IOrderedEnumerable<T> {
		protected readonly RefreshThread _thread;
		protected readonly IEnumerable<T> _source;
		protected IOrderedEnumerable<T> _query;

		public ThreadSortOrderedEnumerable(RefreshThread thread, IEnumerable<T> source) {
			_thread = thread;
			_source = source;
		}

		protected abstract Item GetItem(T value);

		protected virtual IOrderedEnumerable<T> SortFuzzy() => SortingCache.dictionary.SortFuzzy(_source, GetItem, _thread.controls.sortingOption);

		private IOrderedEnumerable<T> CreateQuery() {
			try {
				if (_thread.controls.sortingOption < 0)
					return new KeepItemsInPlaceEnumerable<T>(_source);

				//Apply "fuzzy" sorting since it's faster, but less accurate
				IOrderedEnumerable<T> orderedItems = SortFuzzy();

				var sorter = SortingOptionLoader.Get(_thread.controls.sortingOption);

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

		public IOrderedEnumerable<T> CreateOrderedEnumerable<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer, bool descending) {
			_query ??= CreateQuery();
			return _query.CreateOrderedEnumerable(keySelector, comparer, descending);
		}

		public IEnumerator<T> GetEnumerator() {
			_query ??= CreateQuery();
			return _query.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	internal class ThreadSortOrderedGenericEnumerable<T> : ThreadSortOrderedEnumerable<T> {
		private readonly Func<T, Item> _converter;

		public ThreadSortOrderedGenericEnumerable(RefreshThread thread, IEnumerable<T> source, Func<T, Item> converter) : base(thread, source) {
			ArgumentNullException.ThrowIfNull(converter);
			_converter = converter;
		}

		protected override Item GetItem(T value) => _converter(value);

		protected override IOrderedEnumerable<T> SortFuzzy() => SortingCache.dictionary.SortFuzzy(_source, _converter, base._thread.controls.sortingOption);
	}

	internal class ThreadSortOrderedItemEnumerable : ThreadSortOrderedEnumerable<Item> {
		public ThreadSortOrderedItemEnumerable(RefreshThread thread, IEnumerable<Item> source) : base(thread, source) { }

		protected override Item GetItem(Item value) => value;

		protected override IOrderedEnumerable<Item> SortFuzzy() => SortingCache.dictionary.SortFuzzy(_source, base._thread.controls.sortingOption);
	}

	internal class ThreadSortOrderedRecipeEnumerable : ThreadSortOrderedEnumerable<Recipe> {
		public ThreadSortOrderedRecipeEnumerable(RefreshThread thread, IEnumerable<Recipe> source) : base(thread, source) { }

		protected override Item GetItem(Recipe value) => value.createItem;
		
		protected override IOrderedEnumerable<Recipe> SortFuzzy() => SortingCache.dictionary.SortFuzzy(_source, r => r.createItem, base._thread.controls.sortingOption);
	}
}
