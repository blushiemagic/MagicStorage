using MagicStorage.Common.Systems;
using MagicStorage.Common.Threading.UI;
using MagicStorage.Components;
using MagicStorage.CrossMod;
using MagicStorage.Sorting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Terraria;

namespace MagicStorage {
	partial class StorageGUI {
		private class StorageRefreshThread : RefreshThread {
			public readonly HashSet<int> targetItemTypes;
			public List<Item> allStoredItems;
			public bool uniqueSlotPerItemStack;

			public StorageRefreshThread(
				StorageViewControls controls,
				ActionMode currentMode,
				HashSet<int> itemTypesToUpdate
			) : base(MagicUI.storageUI, HijackControls(controls, currentMode)) {
				targetItemTypes = itemTypesToUpdate is null ? null : [.. itemTypesToUpdate];
				uniqueSlotPerItemStack = currentMode is ActionMode.Deletion;
			}

			private static StorageViewControls HijackControls(StorageViewControls original, ActionMode currentMode) {
				if (currentMode is ActionMode.Deletion) {
					// Item Deletion Mode needs to always show all items
					return original.CreateCopy(
						filteringOptionOverride: FilteringOptionLoader.Definitions.All.Type,
						generalFiltersOverride: []
					);
				} else
					return original;
			}

			protected override void CollectObjects() {
				// Get all items now; filtering will be handled in the thread
				allStoredItems = [.. base.Heart.GetStoredItems()];
			}

			protected override void Execute() {
				IEnumerable<Item> items;

				if (targetItemTypes is not { Count: > 0 }) {
					// Use the items as they are in storage
					items = allStoredItems;
				} else {
					// Order the items to where items that don't need to update will be in the same general order
					// This should reduce the execution time when sorting
					items = AdjustToUpdateSet(allStoredItems, targetItemTypes);
				}

				// Adjust further based on the filter setting
				if (base.controls.filteringOption == FilteringOptionLoader.Definitions.Recent.Type) {
					items = AdjustToDepositHistory(this, items);
					base.workingCounter = RECENT_FILTER_ITEM_COUNT;
				} else
					base.workingCounter = allStoredItems.Count;

				base.workingItemList = items;
				base.workingFlag = uniqueSlotPerItemStack;

				SortAndFilter(this);

				MagicUI.lastKnownSearchBarErrorReason = base.searchBarError;
			}

			protected override void Cleanup() { }

			public override void ClearStaticCollections() => StorageGUI.ClearAllCollections();
		}

		#region ThreadContext
		[Obsolete("Use " + nameof(RefreshThread) + " instead", error: true)]
		public class ThreadContext {
			public ItemSorter.AggregateContext context;
			private readonly CancellationTokenSource tokenSource;
			public readonly CancellationToken token;
			public TEStorageHeart heart;
			public int sortMode, filterMode;
			public HashSet<int> generalFilters;
			public string searchText;
			public bool onlyFavorites;
			public int modSearch;
			private readonly Action<ThreadContext> work;
			private readonly Action<ThreadContext> afterWork;
			public object state;
			private readonly ManualResetEvent cancelWait = new(false);

			private int totalTasks;
			private int currentTasksCompleted;

			public string CurrentTask { get; private set; }

			public float Progress {
				get {
					int current = currentTasksCompleted;  // One read to preserve atomic operations
					return totalTasks <= 0 ? 0 : current >= totalTasks ? 1 : current / (float)totalTasks;
				}
			}

			public ThreadContext(CancellationTokenSource tokenSource, Action<ThreadContext> work, Action<ThreadContext> afterWork) {
				ArgumentNullException.ThrowIfNull(tokenSource);
				ArgumentNullException.ThrowIfNull(work);

				this.tokenSource = tokenSource;
				token = tokenSource.Token;
				this.work = work;
				this.afterWork = afterWork;
			}

			public ThreadContext Clone(int? newSortMode = null, int? newFilterMode = null, HashSet<int> newGeneralFilters = null, string newSearchText = null, int? newModSearch = null) {
				return new ThreadContext(tokenSource, work, afterWork) {
					context = context,
					heart = heart,
					sortMode = newSortMode ?? sortMode,
					filterMode = newFilterMode ?? filterMode,
					generalFilters = newGeneralFilters ?? generalFilters,
					searchText = newSearchText ?? searchText,
					onlyFavorites = onlyFavorites,
					modSearch = newModSearch ?? modSearch,
					state = state
				};
			}

			public void InitTaskSchedule(int totalTasks, string taskName) {
				this.totalTasks = totalTasks;
				currentTasksCompleted = 0;
				CurrentTask = taskName;
			}

			public void ResetTaskCompletion() {
				currentTasksCompleted = 0;
			}

			public void InitAsCompleted(string taskName) {
				totalTasks = 1;
				currentTasksCompleted = 1;
				CurrentTask = taskName;
			}

			public void CompleteOneTask() {
				Interlocked.Increment(ref currentTasksCompleted);
			}

			public bool Running { get; private set; }

			public static void Begin(ThreadContext incoming) {
				MagicUI.StopCurrentThread();

				if (incoming.Running)
					throw new ArgumentException("Incoming thread state was already running");

				MagicUI.activeThread = incoming;
				MagicUI.activeThread.Running = true;
			//	MagicUI.CurrentlyRefreshing = true;

				// Variable capturing
				ThreadContext ctx = incoming;

				NetHelper.Report(true, "Threading logic started");

				Task.Run(() => {
					try {
						ctx.work(ctx);
						NetHelper.Report(true, "Main work for thread finished");

						ctx.afterWork?.Invoke(ctx);
						if (ctx.afterWork is not null)
							NetHelper.Report(true, "Final work for thread finished");
					} catch when (ctx.token.IsCancellationRequested) {
						NetHelper.Report(true, "Thread work was cancelled");
					} finally {
						ctx.cancelWait.Set();
					}
				});
			}

			public void Stop() {
				if (!Running)
					return;

				Running = false;
				MagicUI.CurrentThreadingDuration = 0;
				tokenSource.Cancel();
				cancelWait.WaitOne();

				NetHelper.Report(true, "Current thread halted");
			}
		}
		#endregion
	}
}
