using MagicStorage.CrossMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Terraria;

namespace MagicStorage.Common {
	/// <summary>
	/// An object representing the results from aggregating <see cref="Item"/> objects in a collection
	/// </summary>
	public class ItemAggregateResults {
		private IEnumerable<Item> _originalSource;

		private readonly List<Item> _aggregatedSource = [];
		private readonly ConditionalWeakTable<Item, List<Item>> _aggregatedSourceToGroup = [];
		private readonly List<List<Item>> _aggregatedSourceGroups = [];

		private readonly List<Item> _results = [];
		private readonly ConditionalWeakTable<Item, List<Item>> _resultToGroup = [];
		private readonly List<List<Item>> _resultGroups = [];

		private readonly ConditionalWeakTable<Item, byte[]> _cachedSaveData = [];

		private bool _hasResults;
		private int _groupCount;

		/// <summary>
		/// The maximum amount of resulting item stacks that this aggregator should allow.<br/>
		/// If <see langword="null"/>, there is no limit.<br/>
		/// Defaults to <see langword="null"/>.
		/// </summary>
		public int? ResultCountLimit { get; set; }

		/// <summary>
		/// Whether <see cref="Aggregate"/> should increment the counter parameter when examining a source item (<see langword="false"/>) or when creating a result item (<see langword="true"/>).<br/>
		/// Defaults to <see langword="false"/>.
		/// </summary>
		public bool IncrementCounterOnResult { get; set; } = false;

		/// <summary>
		/// How many source item stacks were examined, or zero if aggregation has not yet been performed.
		/// </summary>
		public int SourceCount => _hasResults ? _aggregatedSource.Count : 0;

		/// <summary>
		/// How many resulting item stacks are present, or zero if aggregation has not yet been performed.
		/// </summary>
		public int ResultCount => _hasResults ? _results.Count : 0;

		/// <summary>
		/// How many groups of items were aggregated together, or zero if aggregation has not yet been performed.
		/// </summary>
		public int GroupCount => _hasResults ? _groupCount : 0;

		/// <summary>
		/// Creates a new <see cref="ItemAggregateResults"/> instance.
		/// </summary>
		/// <param name="source">The collection of items to aggregate from.</param>
		public ItemAggregateResults(IEnumerable<Item> source) {
			ArgumentNullException.ThrowIfNull(source);
			_originalSource = source;
		}

		/// <summary>
		/// Enumerates through the items from the source collection in the order they appear in the results.
		/// </summary>
		public IEnumerable<Item> GetAllSourceItems() {
			if (_hasResults) {
				foreach (var item in _aggregatedSource)
					yield return item;
			}
		}

		/// <summary>
		/// Enumerates through the items from the source collection of a specific type in the order they appear in the results.
		/// </summary>
		/// <param name="itemType">The item type to filter by.</param>
		public IEnumerable<Item> GetSourceItemsOfType(int itemType) {
			if (_hasResults) {
				foreach (var item in _aggregatedSource) {
					if (item.type == itemType)
						yield return item;
				}
			}
		}

		/// <summary>
		/// Enumerates through the item groups from the source collection in the order they appear in the results.
		/// </summary>
		public IEnumerable<IEnumerable<Item>> GetSourceGroups() {
			if (_hasResults) {
				foreach (var group in _aggregatedSourceGroups)
					yield return IterateThrough(group);
			}
		}

		private static IEnumerable<Item> IterateThrough(IEnumerable<Item> group) {
			foreach (var item in group)
				yield return item;
		}

		/// <summary>
		/// Copies all items from the source collection to a target list, in the order they appear in the results.
		/// </summary>
		/// <param name="destination">The target list to copy items to.</param>
		public void CopySourceItemsTo(List<Item> destination) {
			ArgumentNullException.ThrowIfNull(destination);

			if (_hasResults)
				destination.AddRange(_aggregatedSource);
		}

		/// <summary>
		/// Copies all item groups from the source collection to a target list of groups, in the order they appear in the results.
		/// </summary>
		/// <param name="destination">The target list of groups to copy item groups to.</param>
		public void CopySourceGroupsTo(List<List<Item>> destination) {
			ArgumentNullException.ThrowIfNull(destination);

			if (_hasResults) {
				foreach (var group in _aggregatedSourceGroups)
					destination.Add([.. group]);
			}
		}

		/// <summary>
		/// Copies all item groups from the source collection to a target lookup table.
		/// </summary>
		/// <param name="lookupTable">The target lookup table to copy item groups to.</param>
		public void CopySourceGroupsTo(ConditionalWeakTable<Item, List<Item>> lookupTable) {
			ArgumentNullException.ThrowIfNull(lookupTable);

			if (_hasResults) {
				foreach (var (item, group) in _aggregatedSourceToGroup)
					lookupTable.Add(item, group);
			}
		}

		/// <summary>
		/// Enumerates through the resulting aggregated items in the order they were created.
		/// </summary>
		public IEnumerable<Item> GetResultItems() {
			if (_hasResults) {
				foreach (var item in _results)
					yield return item;
			}
		}

		/// <summary>
		/// Enumerates through the resulting aggregated items of a specific type in the order they were created.
		/// </summary>
		/// <param name="itemType">The item type to filter by.</param>
		public IEnumerable<Item> GetResultItemsOfType(int itemType) {
			if (_hasResults) {
				foreach (var item in _results) {
					if (item.type == itemType)
						yield return item;
				}
			}
		}

		/// <summary>
		/// Enumerates through the resulting aggregated item groups in the order they were created.
		/// </summary>
		public IEnumerable<IEnumerable<Item>> GetResultItemGroups() {
			if (_hasResults) {
				foreach (var group in _resultGroups)
					yield return IterateThrough(group);
			}
		}

		/// <summary>
		/// Copies all resulting aggregated items to a target list, in the order they were created.
		/// </summary>
		/// <param name="destination">The target list to copy items to.</param>
		public void CopyResultItemsTo(List<Item> destination) {
			ArgumentNullException.ThrowIfNull(destination);

			if (_hasResults)
				destination.AddRange(_results);
		}

		/// <summary>
		/// Copies all resulting aggregated item groups to a target list of groups, in the order they were created.
		/// </summary>
		/// <param name="destination">The target list of groups to copy item groups to.</param>
		public void CopyResultGroupsTo(List<List<Item>> destination) {
			ArgumentNullException.ThrowIfNull(destination);

			if (_hasResults) {
				foreach (var group in _resultGroups)
					destination.Add([.. group]);
			}
		}

		/// <summary>
		/// Copies all resulting aggregated item groups to a target lookup table.
		/// </summary>
		/// <param name="lookupTable">The target lookup table to copy item groups to.</param>
		public void CopyResultGroupsTo(ConditionalWeakTable<Item, List<Item>> lookupTable) {
			ArgumentNullException.ThrowIfNull(lookupTable);

			if (_hasResults) {
				foreach (var (item, group) in _resultToGroup)
					lookupTable.Add(item, group);
			}
		}

		/// <summary>
		/// Aggregates the source collection of items into result item stacks.<br/>
		/// If aggregation has already been performed, this method does nothing.
		/// </summary>
		/// <param name="token">A cancellation token to cancel the aggregation operation.</param>
		/// <param name="uniqueSlotPerItemStack">Whether each item stack should occupy its own slot.  If <see langword="true"/>, the result items will only be ordered and not aggregated together.</param>
		/// <param name="progressCounter">A reference to an integer that will be updated to reflect the progress of the aggregation operation.</param>
		/// <returns>The current <see cref="ItemAggregateResults"/> instance.</returns>
		public ItemAggregateResults Aggregate(CancellationToken token, bool uniqueSlotPerItemStack, ref int progressCounter) {
			if (_hasResults) {
				// No need to re-aggregate
				progressCounter = _results.Count;
				return this;
			}

			progressCounter = 0;

			// Ensure that the collections are actually empty
			Reset();

			// The following logic was originally in ItemSorter
			try {
				Item aggregateDestination = null;
				List<Item> currentResultGroup = null;
				List<Item> currentSourceGroup = null;

				bool forcedFavorite = false;
				int resultLimit = ResultCountLimit ?? int.MaxValue;
				int iteration = 0;

				foreach (var source in _originalSource.OrderBy(static i => i.type).ThenBy(static i => i.prefix)) {
					if ((++iteration & 0b1111) == 0)
						token.ThrowIfCancellationRequested();

					if (aggregateDestination is null) {
						// The first item group is being created
						aggregateDestination = source.Clone();
						currentResultGroup = [ aggregateDestination ];
						currentSourceGroup = [ source ];

						_groupCount++;
						
						_results.Add(aggregateDestination);
						_resultToGroup.Add(aggregateDestination, currentResultGroup);
						_resultGroups.Add(currentResultGroup);
						_aggregatedSourceToGroup.Add(source, currentSourceGroup);
						_aggregatedSourceGroups.Add(currentSourceGroup);

						// Both a result item was created and a source item was examined, so the property shouldn't be checked here
						progressCounter++;

						continue;
					}

					bool combiningPermitted = StorageAggregator.CanCombineItems(aggregateDestination, source, checkPrefix: true, strict: true, savedItemTagIO: _cachedSaveData);
					if (combiningPermitted) {
						// Any favorited item will make the entire stack favorited
						if (!forcedFavorite && source.favorited) {
							if (!aggregateDestination.favorited) {
								// Update the already-aggregated items to be favorited
								foreach (var resultItem in currentResultGroup)
									resultItem.favorited = true;
							}

							aggregateDestination.favorited = true;
							forcedFavorite = true;
						} else if (forcedFavorite && !aggregateDestination.favorited)
							aggregateDestination.favorited = true;

						if (!uniqueSlotPerItemStack) {
							if ((uint)aggregateDestination.stack + (uint)source.stack <= int.MaxValue) {
								// Aggregate the incoming stack onto the existing item
								Utility.CallOnStackHooks(aggregateDestination, source, source.stack);

								aggregateDestination.stack += source.stack;
							} else {
								// The item stack would overflow, so transfer the remaining stack to the item to aggregate next
								int transfer = int.MaxValue - aggregateDestination.stack;

								Utility.CallOnStackHooks(aggregateDestination, source, transfer);

								aggregateDestination.stack = int.MaxValue;

								if (_results.Count >= resultLimit) {
									// Result limit has been reached; new result can't be added
									_hasResults = true;
									return this;
								}

								Item remaining = source.Clone();
								remaining.stack -= transfer;

								OnNewResult(remaining, clone: false, ref aggregateDestination, currentResultGroup, ref progressCounter);
							}
						} else {
							if (_results.Count >= resultLimit) {
								// Result limit has been reached; new result can't be added
								_hasResults = true;
								return this;
							}

							// Aggregation is disabled; always make a new element
							OnNewResult(source, clone: true, ref aggregateDestination, currentResultGroup, ref progressCounter);
						}
					} else {
						if (_results.Count >= resultLimit) {
							// Result limit has been reached; new result can't be added
							_hasResults = true;
							return this;
						}

						forcedFavorite = false;
						currentResultGroup = [];
						currentSourceGroup = [];

						_resultGroups.Add(currentResultGroup);
						_aggregatedSourceGroups.Add(currentSourceGroup);

						_groupCount++;

						OnNewResult(source, clone: true, ref aggregateDestination, currentResultGroup, ref progressCounter);
					}

					currentSourceGroup.Add(source);

					_aggregatedSource.Add(source);
					_aggregatedSourceToGroup.Add(source, currentSourceGroup);

					if (!IncrementCounterOnResult)
						progressCounter++;
				}
			} catch (OperationCanceledException) {
				Reset();
				throw;
			}

			_hasResults = true;
			return this;
		}

		private void OnNewResult(Item source, bool clone, ref Item aggregateDestination, List<Item> currentResultGroup, ref int progressCounter) {
			aggregateDestination = clone ? source.Clone() : source;
			currentResultGroup.Add(aggregateDestination);

			_results.Add(aggregateDestination);
			_resultToGroup.Add(aggregateDestination, currentResultGroup);

			if (IncrementCounterOnResult)
				progressCounter++;
			
		}

		/// <summary>
		/// Sets a new source collection of items to aggregate from, clearing any existing results.
		/// </summary>
		/// <param name="newSource">The new source collection of items.</param>
		/// <returns>The current <see cref="ItemAggregateResults"/> instance.</returns>
		public ItemAggregateResults SetSource(IEnumerable<Item> newSource) {
			ArgumentNullException.ThrowIfNull(newSource);
			_originalSource = newSource;
			return Reset();
		}

		/// <summary>
		/// Resets the aggregator, clearing any existing results.
		/// </summary>
		/// <returns>The current <see cref="ItemAggregateResults"/> instance.</returns>
		public ItemAggregateResults Reset() {
			_results.Clear();
			_resultToGroup.Clear();
			_resultGroups.Clear();
			_aggregatedSource.Clear();
			_aggregatedSourceToGroup.Clear();
			_aggregatedSourceGroups.Clear();
			_cachedSaveData.Clear();
			_hasResults = false;
			_groupCount = 0;
			return this;
		}
	}
}
