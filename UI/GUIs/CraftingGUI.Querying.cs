using System.Collections.Generic;
using System.Linq;
using System;
using MagicStorage.Common;
using MagicStorage.Sorting;
using Terraria;

namespace MagicStorage {
	partial class CraftingGUI {
		internal class QueryResults<T> {
			public readonly List<T> results;
			public readonly List<bool> resultAvailable;

			public QueryResults(List<T> results, List<bool> resultAvailable) {
				this.results = results;
				this.resultAvailable = resultAvailable;
			}
		}

		internal delegate ParallelQuery<T> QuerySearchItems<T>(StorageGUI.ThreadContext thread);
		internal delegate IEnumerable<T> SortSearchItems<T>(StorageGUI.ThreadContext thread, CommonCraftingState state, IEnumerable<T> source);

		internal class Query<T> {
			public readonly QueryResults<T> collection;
			public readonly QuerySearchItems<T> getQuery;
			public readonly SortSearchItems<T> sortQuery;
			public readonly Func<T, bool> isAvailable;

			public Query(QueryResults<T> collection, QuerySearchItems<T> getQuery, SortSearchItems<T> sortQuery, Func<T, bool> isAvailable) {
				this.collection = collection;
				this.getQuery = getQuery;
				this.sortQuery = sortQuery;
				this.isAvailable = isAvailable;
			}
		}

		internal delegate bool SpecificQueryItemValid<T>(StorageGUI.ThreadContext thread, T item);
		internal delegate bool SpecificQueryItemCanBeAdded<T>(StorageGUI.ThreadContext thread, CommonCraftingState state, T item);

		internal class SpecificQuery<T> {
			public readonly QueryResults<T> collection;
			public readonly SortSearchItems<T> sortQuery;
			public readonly Func<T, bool> isAvailable;
			public readonly SpecificQueryItemValid<T> validItem;
			public readonly SpecificQueryItemCanBeAdded<T> canBeAdded;

			public SpecificQuery(QueryResults<T> collection, SortSearchItems<T> sortQuery, Func<T, bool> isAvailable, SpecificQueryItemValid<T> validItem, SpecificQueryItemCanBeAdded<T> canBeAdded) {
				this.collection = collection;
				this.sortQuery = sortQuery;
				this.isAvailable = isAvailable;
				this.validItem = validItem;
				this.canBeAdded = canBeAdded;
			}
		}

		internal static void DoFiltering<T>(StorageGUI.ThreadContext thread, CommonCraftingState state, Query<T> query)
		{
			var queryResults = query.collection;

			try {
				NetHelper.Report(true, "Retrieving values from query...");

				var queryItems = query.getQuery(thread);

				thread.CompleteOneTask();

				NetHelper.Report(true, "Sorting values from query...");

				var sortedQuery = query.sortQuery(thread, state, queryItems);

				thread.CompleteOneTask();

				queryResults.results.Clear();
				queryResults.resultAvailable.Clear();

				if (state.recipeFilterChoice == RecipeButtonsAvailableChoice)
				{
					NetHelper.Report(true, "Filtering out only available values...");

					queryResults.results.AddRange(sortedQuery.Where(query.isAvailable));

					thread.CompleteOneTask();

					queryResults.resultAvailable.AddRange(Enumerable.Repeat(true, queryResults.results.Count));

					thread.CompleteOneTask();
				}
				else
				{
					queryResults.results.AddRange(sortedQuery);

					thread.CompleteOneTask();

					queryResults.resultAvailable.AddRange(queryResults.results.AsParallel().AsOrdered().Select(query.isAvailable));

					thread.CompleteOneTask();
				}
			} catch when (thread.token.IsCancellationRequested) {
				queryResults.results.Clear();
				queryResults.resultAvailable.Clear();
			}
		}

		internal static void RefreshSpecificQueryItems<T>(StorageGUI.ThreadContext thread, CommonCraftingState state, T[] queryItems, SpecificQuery<T> specificQuery, bool forcedSort, string queryObject) {
			NetHelper.Report(true, $"Refreshing {queryItems.Length} {queryObject}");

			// Task count: N recipes, 3 tasks from SortRecipes, adding recipes, adding recipe availability
			thread.InitTaskSchedule(queryItems.Length + 5, $"Refreshing {queryItems.Length} {queryObject}");

			//Assumes that the recipes are visible in the GUI
			bool needsResort = forcedSort;

			var queryResults = specificQuery.collection;

			foreach (T item in queryItems) {
				if (!specificQuery.validItem(thread, item)) {
					thread.CompleteOneTask();
					continue;
				}

				int index = queryResults.results.IndexOf(item);

				using (FlagSwitch.ToggleTrue(ref disableNetPrintingForIsAvailable)) {
					if (!specificQuery.isAvailable(item)) {
						if (index >= 0) {
							if (state.recipeFilterChoice == RecipeButtonsAvailableChoice) {
								// Remove the item
								queryResults.results.RemoveAt(index);
								queryResults.resultAvailable.RemoveAt(index);
							} else {
								// Simply mark the item as unavailable
								recipeAvailable[index] = false;
							}
						}
					} else {
						if (state.recipeFilterChoice == RecipeButtonsAvailableChoice) {
							if (index < 0 && specificQuery.canBeAdded(thread, state, item)) {
								// Add the item
								specificQuery.collection.results.Add(item);
								needsResort = true;
							}
						} else {
							if (index >= 0) {
								// Simply mark the item as available
								specificQuery.collection.resultAvailable[index] = true;
							}
						}
					}
				}

				thread.CompleteOneTask();
			}

			if (needsResort) {
				var sorted = new List<T>(queryResults.results)
					.AsParallel()
					.AsOrdered();

				IEnumerable<T> sortedQuery = specificQuery.sortQuery(thread, state, sorted);

				queryResults.results.Clear();
				queryResults.resultAvailable.Clear();

				queryResults.results.AddRange(sortedQuery);

				thread.CompleteOneTask();

				queryResults.resultAvailable.AddRange(Enumerable.Repeat(true, queryResults.results.Count));

				thread.CompleteOneTask();
			}
		}

		internal static bool DoesItemPassFilters(StorageGUI.ThreadContext thread, CommonCraftingState state, Item item) {
			return ItemSorter.FilterBySearchText(item, thread.searchText, thread.modSearch)
				// show only blacklisted recipes only if choice = 2, otherwise show all other
				&& (!MagicStorageConfig.RecipeBlacklistEnabled || state.recipeFilterChoice == RecipeButtonsBlacklistChoice == state.IsHidden(item.type))
				// show only favorited items if selected
				&& (!MagicStorageConfig.CraftingFavoritingEnabled || state.recipeFilterChoice != RecipeButtonsFavoritesChoice || state.favoritedTypes.Contains(item));
		}
	}
}
