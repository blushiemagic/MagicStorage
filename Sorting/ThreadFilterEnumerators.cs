using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.Items.ErrorDisplay;
using SerousCommonLib.API.Iterators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;

namespace MagicStorage.Sorting {
	internal abstract class ThreadFilterEnumerator<T> : Iterator<T> {
		protected readonly RefreshThread _thread;
		protected readonly IEnumerable<T> _source;
		protected IEnumerator<T> _iterator;

		public bool ApplyStaticFilter { get; set; } = true;

		public ThreadFilterEnumerator(RefreshThread thread, IEnumerable<T> source) {
			ArgumentNullException.ThrowIfNull(thread);
			ArgumentNullException.ThrowIfNull(source);
			_thread = thread;
			_source = source;
		}

		protected abstract Item GetItem(T value);

		protected virtual bool PassesFavoriteFilter(T value) => !MagicStorageConfig.CraftingFavoritingEnabled || !_thread.controls.showOnlyFavorites || GetItem(value).favorited;

		private bool PassesFilters(T value) => PassesFilters_OptionsAndText(GetItem(value)) && PassesFavoriteFilter(value);

		private bool PassesFilters_OptionsAndText(Item item) {
			var controls = _thread.controls;

			return ApplyStaticFilter
				? controls.ItemPassesFilters(item)
				: controls.ItemPassesGeneralOptionFilters(item) && controls.ItemPassesTextFilter(item);
		}

		public override void Dispose() {
			_iterator?.Dispose();
			_iterator = null;

			base.Dispose();
		}

		public override bool MoveNext() {
			switch (base._state) {
				case 1:
					_iterator = _source.GetEnumerator();
					base._state = 2;
					goto case 2;
				case 2:
					while (_iterator.MoveNext()) {
						var current = _iterator.Current;

						if (typeof(T) == typeof(Item)) {
							if (Unsafe.As<T, Item>(ref current).ModItem is BaseErrorDummyItem)
								_thread.foundErrorItem = true;
						}

						if (PassesFilters(current)) {
							_current = current;
							return true;
						}
					}
					break;
			}

			Dispose();
			return false;
		}
	}

	internal class ThreadFilterGenericEnumerator<T> : ThreadFilterEnumerator<T> {
		private readonly Func<T, Item> _converter;

		public ThreadFilterGenericEnumerator(RefreshThread thread, IEnumerable<T> source, Func<T, Item> converter) : base(thread, source) {
			ArgumentNullException.ThrowIfNull(converter);
			_converter = converter;
		}

		public override Iterator<T> Clone() => new ThreadFilterGenericEnumerator<T>(_thread, _source, _converter);

		protected override Item GetItem(T value) => _converter(value);
	}

	internal class ThreadFilterItemEnumerator : ThreadFilterEnumerator<Item> {
		public ThreadFilterItemEnumerator(RefreshThread thread, IEnumerable<Item> source) : base(thread, source) { }

		public override Iterator<Item> Clone() => new ThreadFilterItemEnumerator(_thread, _source);

		protected override Item GetItem(Item value) => value;
	}

	internal class ThreadFilterRecipeEnumerator : ThreadFilterEnumerator<Recipe> {
		private readonly IFilterProvider<Recipe> _provider;

		public ThreadFilterRecipeEnumerator(RefreshThread thread, IEnumerable<Recipe> source, IFilterProvider<Recipe> provider) : base(thread, source) {
			ArgumentNullException.ThrowIfNull(provider);
			_provider = provider;
		}
		
		public override Iterator<Recipe> Clone() => new ThreadFilterRecipeEnumerator(_thread, _source, _provider);

		protected override Item GetItem(Recipe value) => value.createItem;

		protected override bool PassesFavoriteFilter(Recipe value)
			=> !MagicStorageConfig.CraftingFavoritingEnabled || !_thread.controls.showOnlyFavorites || _provider is null || _provider.IsFavorited(value);
	}

	internal abstract class ThreadFilterParallelEnumerator<T> : IEnumerable<T> {
		protected readonly RefreshThread _thread;
		protected readonly ParallelQuery<T> _query;

		public bool ApplyStaticFilter { get; set; } = true;

		public ThreadFilterParallelEnumerator(RefreshThread thread, ParallelQuery<T> query) {
			ArgumentNullException.ThrowIfNull(thread);
			ArgumentNullException.ThrowIfNull(query);
			_thread = thread;
			_query = query.Where(PassesFilters);
		}

		protected abstract Item GetItem(T value);

		protected virtual bool PassesFavoriteFilter(T value) => !MagicStorageConfig.CraftingFavoritingEnabled || !_thread.controls.showOnlyFavorites || GetItem(value).favorited;

		private bool PassesFilters(T value) => PassesFilters_OptionsAndText(GetItem(value)) && PassesFavoriteFilter(value);

		private bool PassesFilters_OptionsAndText(Item item) {
			var controls = _thread.controls;

			return ApplyStaticFilter
				? controls.ItemPassesFilters(item)
				: controls.ItemPassesGeneralOptionFilters(item) && controls.ItemPassesTextFilter(item);
		}

		public ParallelQuery<T> GetQuery() => _query;

		public IEnumerator<T> GetEnumerator() => _query.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _query.GetEnumerator();
	}

	internal class ThreadFilterParallelGenericEnumerator<T> : ThreadFilterParallelEnumerator<T> {
		private readonly Func<T, Item> _converter;

		public ThreadFilterParallelGenericEnumerator(RefreshThread thread, ParallelQuery<T> query, Func<T, Item> converter) : base(thread, query) {
			ArgumentNullException.ThrowIfNull(converter);
			_converter = converter;
		}

		protected override Item GetItem(T value) => _converter(value);
	}

	internal class ThreadFilterParallelItemEnumerator : ThreadFilterParallelEnumerator<Item> {
		public ThreadFilterParallelItemEnumerator(RefreshThread thread, ParallelQuery<Item> query) : base(thread, query) { }

		protected override Item GetItem(Item value) => value;
	}

	internal class ThreadFilterParallelRecipeEnumerator : ThreadFilterParallelEnumerator<Recipe> {
		private readonly IFilterProvider<Recipe> _provider;

		public ThreadFilterParallelRecipeEnumerator(RefreshThread thread, ParallelQuery<Recipe> query, IFilterProvider<Recipe> provider) : base(thread, query) {
			ArgumentNullException.ThrowIfNull(provider);
			_provider = provider;
		}

		protected override Item GetItem(Recipe value) => value.createItem;

		protected override bool PassesFavoriteFilter(Recipe value)
			=> !MagicStorageConfig.CraftingFavoritingEnabled || !_thread.controls.showOnlyFavorites || _provider is null || _provider.IsFavorited(value);
	}
}
