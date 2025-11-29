using System;
using System.Collections.Generic;
using System.Linq;
using MagicStorage.Common.Systems;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.ModLoader;
using Terraria.ID;
using MagicStorage.CrossMod;
using System.Threading;
using System.Runtime.CompilerServices;
using MagicStorage.Common.Threading.UI;
using MagicStorage.Common;
using MagicStorage.Common.Threading;
using System.Text;

namespace MagicStorage.Sorting
{
	public static class ItemSorter
	{
		[Obsolete("No longer used by any APIs", error: true)]
		public class AggregateContext {
			public IEnumerable<Item> items;
			public IEnumerable<List<Item>> sourceItems;
			public ConditionalWeakTable<Item, byte[]> savedItemTagIO;
			internal List<List<Item>> enumeratedSource;

			public bool uniqueSlotPerItemStack;

			public AggregateContext(IEnumerable<Item> items) {
				this.items = items;
				sourceItems = enumeratedSource = new();
				savedItemTagIO = new();
			}
		}

		/// <summary>
		/// Filters, aggregates then sorts the item collection assigned to <paramref name="thread"/> based on its controls
		/// <para/>
		/// <see cref="RefreshThread.workingItemList"/> should contain the item collection.<br/>
		/// <see cref="RefreshThread.workingCounter"/> should contain the number of items in the item collection.<br/>
		/// <see cref="RefreshThread.workingFlag"/> should indicate whether aggregation should be ignored (if <see langword="true"/>, the items will only be ordered by type and prefix).
		/// </summary>
		/// <param name="thread">The refresh thread performing the operation.</param>
		/// <param name="attempt">The current attempt number for the operation (0-based); used to assign the name for the thread's task schedule.</param>
		/// <param name="takeCount">If specified, limits the number of results returned by <see cref="RefreshThread.aggregateResults"/></param>
		/// <param name="listClassification">An optional classification string to include in the task schedule name.</param>
		public static List<Item> SortAndFilterItems(RefreshThread thread, int attempt, int? takeCount = null, string listClassification = null) {
			thread.InitTaskSchedule(
				totalTasks: thread.workingCounter,
				taskName: SortAndFilter_GenerateTaskName("Filtering", attempt, "Items", listClassification)
			);

			List<Item> filteredItems = [.. DoFiltering(thread, thread.workingItemList).NotifyStepsTo(thread)];

			thread.InitTaskSchedule(
				totalTasks: filteredItems.Count,
				taskName: SortAndFilter_GenerateTaskName("Aggregating", attempt, "Items", listClassification)
			);

			if (thread.aggregateResults is null)
				thread.aggregateResults = new ItemAggregateResults(filteredItems);
			else
				thread.aggregateResults.SetSource(filteredItems);

			thread.aggregateResults.ResultCountLimit = takeCount;
			thread.aggregateResults.IncrementCounterOnResult = false;

			thread.aggregateResults.Aggregate(thread.cancellationToken, thread.workingFlag, ref thread.GetCounterReference());

			thread.InitTaskSchedule(
				totalTasks: thread.aggregateResults.ResultCount,
				taskName: SortAndFilter_GenerateTaskName("Sorting", attempt, "Items", listClassification)
			);

			var sortedItems = DoSorting(thread, thread.aggregateResults.GetResultItems());

			if (!thread.controls.showOnlyFavorites)
				sortedItems = OrderFavoritesFirst(sortedItems, item => item.favorited);

			return [.. sortedItems.NotifyStepsTo(thread)];
		}

		public static IEnumerable<T> OrderFavoritesFirst<T>(IEnumerable<T> source, Func<T, bool> isFavorited) {
			// Display both, but order favorites to be first
			List<T> notFavorited = [];

			foreach (T value in source) {
				if (isFavorited(value))
					yield return value;
				else
					notFavorited.Add(value);
			}

			foreach (T value in notFavorited)
				yield return value;
		}

		private static Dictionary<string, Dictionary<int, Dictionary<string, Dictionary<string, string>>>> _generateTaskNameCache = [];

		private static string SortAndFilter_GenerateTaskName(string task, int attempt, string collectionObjects, string listClassification) {
			// Optimize repeated calls to this method by caching the result via the supplied parameters
			listClassification ??= string.Empty;

			if (!_generateTaskNameCache.TryGetValue(task, out var attemptCache))
				_generateTaskNameCache[task] = attemptCache = [];

			if (!attemptCache.TryGetValue(attempt, out var collectionTypeCache))
				attemptCache[attempt] = collectionTypeCache = [];

			if (!collectionTypeCache.TryGetValue(collectionObjects, out var classificationCache))
				collectionTypeCache[listClassification] = classificationCache = [];

			if (classificationCache.TryGetValue(listClassification, out string taskName))
				return taskName;

			StringBuilder taskNameBuilder = new(task);

			if (!string.IsNullOrWhiteSpace(listClassification))
				taskNameBuilder.Append(' ').Append(listClassification);

			taskNameBuilder.Append(collectionObjects);

			if (attempt > 0)
				taskNameBuilder.Append(" (attempt ").Append(attempt + 1).Append(')');

			return classificationCache[listClassification] = taskNameBuilder.ToString();
		}

		public static IEnumerable<T> DoFiltering<T>(RefreshThread thread, IEnumerable<T> source, Func<T, Item> objToItem) {
			return new ThreadFilterGenericEnumerator<T>(thread, source, objToItem);
		}

		public static IEnumerable<Item> DoFiltering(RefreshThread thread, IEnumerable<Item> source) {
			return new ThreadFilterItemEnumerator(thread, source);
		}

		public static IEnumerable<Recipe> DoFiltering(RefreshThread thread, CraftingGUI.IFilterProvider<Recipe> provider, IEnumerable<Recipe> source) {
			return new ThreadFilterRecipeEnumerator(thread, source, provider);
		}

		public static IEnumerable<T> DoSorting<T>(RefreshThread thread, IEnumerable<T> source, Func<T, Item> objToItem) {
			return new ThreadSortOrderedGenericEnumerable<T>(thread, source, objToItem);
		}

		public static IEnumerable<Item> DoSorting(RefreshThread thread, IEnumerable<Item> source) {
			return new ThreadSortOrderedItemEnumerable(thread, source);
		}

		public static IEnumerable<Recipe> DoSorting(RefreshThread thread, IEnumerable<Recipe> source) {
			return new ThreadSortOrderedRecipeEnumerable(thread, source);
		}

		//Formerly returned IEnumerable<Item> for lazy evaluation
		//Needs to return a collection so that "context.enumeratedSource" is properly assigned
		[Obsolete("ItemSorter.AggregateContext is now obsolete; use ItemAggregateResults.Aggregate() instead.", error: true)]
		public static List<Item> Aggregate(AggregateContext context, CancellationToken token)
		{
			try
			{
				Item lastItem = null;

				int sourceIndex = 0;

				List<Item> aggregate = new();

				foreach (Item item in context.items.OrderBy(i => i.type).ThenBy(i => i.prefix))
				{
					if (lastItem is null)
					{
						lastItem = item.Clone();
						context.enumeratedSource.Add(new() { item });
						continue;
					}

					bool combiningPermitted = StorageAggregator.CanCombineItems(item, lastItem, checkPrefix: true, strict: true, savedItemTagIO: context.savedItemTagIO);
					if (combiningPermitted && (context.uniqueSlotPerItemStack || lastItem.stack + item.stack > 0))
					{
						if (!context.uniqueSlotPerItemStack)
						{
							if (item.favorited)
							{
								lastItem.favorited = true;

								foreach (var source in context.enumeratedSource[sourceIndex])
									source.favorited = true;
							}

							Utility.CallOnStackHooks(lastItem, item, item.stack);

							lastItem.stack += item.stack;
						}
						else
						{
							aggregate.Add(lastItem);
							lastItem = item.Clone();
						}

						context.enumeratedSource[sourceIndex].Add(item);
					}
					else
					{
						Item next = item.Clone();

						// Transfer stack from current item to "next item"
						if (combiningPermitted)
						{
							int transfer = int.MaxValue - lastItem.stack;

							Utility.CallOnStackHooks(lastItem, item, transfer);

							next.stack -= transfer;
							lastItem.stack = int.MaxValue;
						}

						aggregate.Add(lastItem);
						lastItem = next;
						context.enumeratedSource.Add(new() { item });
						sourceIndex++;
					}
				}

				if (lastItem is not null)
					aggregate.Add(lastItem);

				return aggregate;
			}
			catch when (token.IsCancellationRequested)
			{
				context.enumeratedSource.Clear();

				return new();
			}
			catch (Exception e) {
				MagicStorageMod.Instance.Logger.Error(e);
				return new();
			}
		}

		internal static Item GetRecipeResult(Recipe recipe) => recipe.createItem;

		[Obsolete("ItemSorter.AggregateContext is now obsolete; use SortAndFilterRecipes() instead", error: true)]
		public static ParallelQuery<Recipe> GetRecipes(StorageGUI.ThreadContext thread) => Array.Empty<Recipe>().AsParallel();

		/// <summary>
		/// Filters then sorts recipes based on the controls assigne do <paramref name="thread"/>
		/// </summary>
		/// <param name="thread">The refresh thread performing the operation.</param>
		/// <param name="attempt">The current attempt number for the operation (0-based); used to assign the name for the thread's task schedule.</param>
		/// <param name="provider">An optional recipe filter provider for additional filtering controls.</param>
		/// <param name="listClassification">An optional classification string to include in the task schedule name.</param>
		public static List<Recipe> SortAndFilterRecipes(RefreshThread thread, int attempt, CraftingGUI.IFilterProvider<Recipe> provider = null, string listClassification = null) {
			bool useStaticFilter;
			Recipe[] allRecipes;

			FilteringOption filterOption = FilteringOptionLoader.Get(thread.controls.filteringOption);

			if (filterOption.UsesFilterCache) {
				allRecipes = MagicCache.FilteredRecipesCache[filterOption.Type];
				useStaticFilter = false;
			} else {
				allRecipes = MagicCache.EnabledRecipes;
				useStaticFilter = true;
			}

			thread.InitTaskSchedule(
				totalTasks: allRecipes.Length,
				taskName: SortAndFilter_GenerateTaskName("Filtering", attempt, "Recipes", listClassification)
			);

			// NOTE: AsParallel().AsOrdered() is not used here
			var query = allRecipes.NotifyStepsTo(thread).AsParallel().Where(HiddenRecipes.IsVisible);

			if (provider is not null) {
				// Apply additional filters
				query = provider.ShowOnlyBlacklisted
					? query.Where(provider.IsHidden)
					: query.Where(provider.IsVisible);
			}

			var enumerator = new ThreadFilterParallelRecipeEnumerator(thread, query, provider) {
				ApplyStaticFilter = useStaticFilter
			};

			// Evaluate the collection here
			List<Recipe> filteredRecipes = [.. enumerator];

			thread.InitTaskSchedule(
				totalTasks: filteredRecipes.Count,
				taskName: SortAndFilter_GenerateTaskName("Sorting", attempt, "Recipes", listClassification)
			);

			var sortedRecipes = DoSorting(thread, filteredRecipes);

			if (provider is not null && !thread.controls.showOnlyFavorites)
				sortedRecipes = OrderFavoritesFirst(sortedRecipes, provider.IsFavorited);

			return [.. sortedRecipes.NotifyStepsTo(thread)];
		}

		public static List<int> SortAndFilterShimmerableItems(RefreshThread thread, int attempt, CraftingGUI.IFilterProvider<int> provider = null, string listClassification = null) {
			bool useStaticFilter;
			Item[] allItems;

			FilteringOption filterOption = FilteringOptionLoader.Get(thread.controls.filteringOption);

			if (filterOption.UsesFilterCache) {
				allItems = MagicCache.FilteredItemsCache[filterOption.Type];
				useStaticFilter = false;
			} else {
				allItems = MagicCache.ItemSamples;
				useStaticFilter = true;
			}

			thread.InitTaskSchedule(
				totalTasks: allItems.Length,
				taskName: SortAndFilter_GenerateTaskName("Filtering", attempt, "Shimmerable Items", listClassification)
			);

			// NOTE: AsParallel().AsOrdered() is not used here
			var query = allItems.NotifyStepsTo(thread).AsParallel().Select(i => i.type);

			if (provider is not null) {
				// Apply additional filters
				query = provider.ShowOnlyBlacklisted
					? query.Where(provider.IsHidden)
					: query.Where(provider.IsVisible);
			}

			var enumerator = new ThreadFilterParallelGenericEnumerator<int>(thread, query, Utility.GetItemSample) {
				ApplyStaticFilter = useStaticFilter
			};

			// Evaluate the collection here
			List<int> filteredItems = [.. enumerator];

			thread.InitTaskSchedule(
				totalTasks: filteredItems.Count,
				taskName: SortAndFilter_GenerateTaskName("Sorting", attempt, "Shimmerable Items", listClassification)
			);

			var sortedItems = DoSorting(thread, filteredItems, Utility.GetItemSample);

			if (provider is not null && !thread.controls.showOnlyFavorites)
				sortedItems = OrderFavoritesFirst(sortedItems, provider.IsFavorited);

			return [.. sortedItems.NotifyStepsTo(thread)];

			/*
			try {
				bool useStaticFilter;
				IEnumerable<Item> items;

				FilteringOption filterOption = FilteringOptionLoader.Get(thread.controls.filteringOption);

				if (filterOption.UsesFilterCache) {
					items = MagicCache.FilteredItemsCache[filterOption.Type];
					useStaticFilter = false;
				} else {
					items = MagicCache.ItemSamples;
					useStaticFilter = true;
				}

				var enumerator = new ThreadFilterParallelItemEnumerator(thread, query: items.AsParallel().AsOrdered()) {
					ApplyStaticFilter = useStaticFilter
				};

				return enumerator.GetQuery().Select(i => i.type);
			} catch when (thread.cancellationToken.IsCancellationRequested) {
				return Array.Empty<int>().AsParallel();
			}
			*/
		}
	}
}
