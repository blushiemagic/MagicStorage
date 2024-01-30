using MagicStorage.Components;
using MagicStorage.Sorting;
using System.Threading.Tasks;
using System.Threading;
using System;
using MagicStorage.Common.Systems;
using MagicStorage.CrossMod;
using MagicStorage.UI.States;
using MagicStorage.UI;

namespace MagicStorage {
	partial class StorageGUI {
		public class ThreadContext {
			public ItemSorter.AggregateContext context;
			private readonly CancellationTokenSource tokenSource;
			public readonly CancellationToken token;
			public TEStorageHeart heart;
			public int sortMode, filterMode;
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

			public ThreadContext Clone(int? newSortMode = null, int? newFilterMode = null, string newSearchText = null, int? newModSearch = null) {
				return new ThreadContext(tokenSource, work, afterWork) {
					context = context,
					heart = heart,
					sortMode = newSortMode ?? sortMode,
					filterMode = newFilterMode ?? filterMode,
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
				MagicUI.CurrentlyRefreshing = true;

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

		[Obsolete("Use MagicUI.CurrentThreadingDuration instead", error: true)]
		public static int CurrentThreadingDuration { get; private set; }

		private static ThreadContext InitializeThreadContext(StorageUIState.StoragePage storagePage, bool clearItemLists) {
			if (clearItemLists) {
				didMatCheck.Clear();
				items.Clear();
				sourceItems.Clear();
			}

			TEStorageHeart heart = GetHeart();
			if (heart == null) {
				itemTypesToUpdate = null;
				storagePage?.RequestThreadWait(waiting: false);

				MagicUI.InvokeOnRefresh();
				return null;
			}

			NetHelper.Report(true, $"Refreshing {(itemTypesToUpdate is null ? "all" : $"{itemTypesToUpdate.Count}")} storage items");

			int sortMode = MagicUI.storageUI.GetPage<SortingPage>("Sorting").option;
			int filterMode = MagicUI.storageUI.GetPage<FilteringPage>("Filtering").option;

			// Force filtering to specific value to make deleting the bad item stacks easier
			if (itemDeletionMode)
				filterMode = FilteringOptionLoader.Definitions.All.Type;

			string searchText = storagePage.searchBar.Text;
			bool onlyFavorites = storagePage.filterFavorites.Value;
			int modSearch = storagePage.modSearchBox.ModIndex;

			return new ThreadContext(new CancellationTokenSource(), SortAndFilter, AfterSorting) {
				heart = heart,
				sortMode = sortMode,
				filterMode = filterMode,
				searchText = searchText,
				onlyFavorites = onlyFavorites,
				modSearch = modSearch,
				state = itemTypesToUpdate
			};
		}
	}
}
