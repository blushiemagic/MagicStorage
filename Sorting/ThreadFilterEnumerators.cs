using SerousCommonLib.API.Iterators;
using System.Collections.Generic;
using System;
using Terraria;
using MagicStorage.CrossMod;
using System.Linq;

namespace MagicStorage.Sorting {
	internal class ThreadFilterEnumerator<T> : Iterator<T> {
		protected readonly StorageGUI.ThreadContext _context;
		protected readonly ItemFilter.Filter _filter;
		protected readonly IEnumerable<T> _source;
		protected readonly IEnumerator<T> _iterator;
		protected readonly Func<T, Item> _objToItem;

		public ThreadFilterEnumerator(StorageGUI.ThreadContext context, IEnumerable<T> source, Func<T, Item> objToItem) {
			var filter = FilteringOptionLoader.Get(context.filterMode)?.Filter
				?? throw new ArgumentOutOfRangeException(nameof(context) + "." + nameof(context.filterMode), "Filtering ID was invalid or its definition had a null filter");

			_context = context;
			_filter = filter;
			_source = source;
			_iterator = _source.GetEnumerator();
			_objToItem = objToItem;
		}

		private ThreadFilterEnumerator(StorageGUI.ThreadContext context, ItemFilter.Filter filter, IEnumerable<T> source, Func<T, Item> objToItem) {
			_context = context;
			_filter = filter;
			_source = source;
			_iterator = _source.GetEnumerator();
			_objToItem = objToItem;
		}

		protected virtual Item GetItem(T value) => _objToItem(value);

		private bool Filter(T value) {
			Item item = GetItem(value);
			return _filter(item) && ItemSorter.ItemPassesAllGenericFilters(item, _context) && ItemSorter.FilterBySearchText(item, _context.searchText, _context.modSearch);
		}

		public override Iterator<T> Clone() => new ThreadFilterEnumerator<T>(_context, _filter, _source, _objToItem);

		public override bool MoveNext() {
			_current = default;

			while (_iterator.MoveNext()) {
				var current = _iterator.Current;

				if (Filter(current)) {
					_current = current;
					return true;
				}
			}

			return false;
		}
	}

	internal class ThreadFilterItemEnumerator : ThreadFilterEnumerator<Item> {
		public ThreadFilterItemEnumerator(StorageGUI.ThreadContext context, IEnumerable<Item> source) : base(context, source, null) { }

		protected override Item GetItem(Item value) => value;
	}

	internal class ThreadFilterParallelEnumerator<T> {
		protected readonly StorageGUI.ThreadContext _context;
		protected readonly ItemFilter.Filter _filter;
		protected readonly ParallelQuery<T> _query;
		protected readonly Func<T, Item> _objToItem;

		public ThreadFilterParallelEnumerator(StorageGUI.ThreadContext context, ParallelQuery<T> query, Func<T, Item> objToItem) {
			var filter = FilteringOptionLoader.Get(context.filterMode)?.Filter
				?? throw new ArgumentOutOfRangeException(nameof(context) + "." + nameof(context.filterMode), "Filtering ID was invalid or its definition had a null filter");

			_context = context;
			_filter = filter;
			_query = query.Where(Filter);
			_objToItem = objToItem;
		}

		protected virtual Item GetItem(T value) => _objToItem(value);

		private bool Filter(T value) {
			Item item = GetItem(value);
			return _filter(item) && ItemSorter.ItemPassesAllGenericFilters(item, _context) && ItemSorter.FilterBySearchText(item, _context.searchText, _context.modSearch);
		}

		public ParallelQuery<T> GetQuery() => _query;
	}

	internal class ThreadFilterParallelItemEnumerator : ThreadFilterParallelEnumerator<Item> {
		public ThreadFilterParallelItemEnumerator(StorageGUI.ThreadContext context, ParallelQuery<Item> query) : base(context, query, null) { }

		protected override Item GetItem(Item value) => value;
	}
}
