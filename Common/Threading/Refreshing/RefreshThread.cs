using MagicStorage.Common.Systems;
using MagicStorage.Components;
using MagicStorage.UI;
using MagicStorage.UI.States;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Common.Threading.Refreshing {
	/// <summary>
	/// Contains information used when refreshing the UIs for this mod.<br/>
	/// Only one instance of this object can be "active" at once, and attempting to start a new thread will wait for the current thread to be cancelled.
	/// </summary>
	public abstract class RefreshThread {
		/// <summary>
		/// The <see cref="BaseStorageUI"/> that is being refreshed by this thread.
		/// </summary>
		public readonly BaseStorageUI refreshingUI;
		/// <summary>
		/// The parameters for collecting and displaying the UI's contents.
		/// </summary>
		public StorageViewControls controls;

		private readonly CancellationTokenSource _tokenSource;
		/// <summary>
		/// A token that can be used to monitor for cancellation requests.
		/// </summary>
		public readonly CancellationToken cancellationToken;

		private readonly List<IWaitProvider> _externalWork = [];

		/// <summary>
		/// An enumeration that can be used to store intermediate results from LINQ queries, iterator methods, etc.
		/// </summary>
		public IEnumerable<Item> workingItemList;

		/// <summary>
		/// An integer that can be used to store the count of items in <see cref="workingItemList"/>
		/// </summary>
		public int workingCounter;

		/// <summary>
		/// An object that can be used to store the results from aggregating <see cref="workingItemList"/>
		/// </summary>
		public ItemAggregateResults aggregateResults;

		/// <summary>
		/// A boolean that can be used for flags, etc.
		/// </summary>
		public bool workingFlag;

		/// <summary>
		/// An error message related to the search bar, if any.
		/// </summary>
		public string searchBarError;

		/// <summary>
		/// An error message related to stored items, if any.
		/// </summary>
		public string storedItemsError;

		/// <summary>
		/// The <see cref="TEStorageHeart"/> being accessed by the UI being refreshed, if any.<br/>
		/// This property will only be non-<see langword="null"/> when <see cref="CollectObjects"/> is invoked.
		/// </summary>
		public TEStorageHeart Heart { get; private set; }

		private bool _hasStarted;

		/// <summary>
		/// Whether this thread is currently running.
		/// </summary>
		public bool IsRunning { get; private set; }

		/// <summary>
		/// Whether this thread completed successfully without being cancelled or throwing an exception.
		/// </summary>
		public bool HasSuccessfulCompletion { get; private set; }

		/// <summary>
		/// Whether this thread only refreshes part of the UI's contents.<br/>
		/// For example, this is <see langword="true"/> for threads that only refresh the stored ingredients for recipes.
		/// </summary>
		public abstract bool IsPartialThread { get; }

		/// <summary>
		/// If <see cref="IsPartialThread"/> is <see langword="true"/>, this property indicates whether a full refresh has been completed for the target UI.<br/>
		/// If <see langword="false"/>, <see cref="FullRefreshBuilder"/> will be used to create and execute another thread instead of this thread.
		/// </summary>
		public abstract bool HasCompleteData { get; }

		/// <summary>
		/// If <see cref="IsPartialThread"/> is <see langword="true"/>, this property provides a builder for creating a full refresh thread for the target UI.<br/>
		/// This property will be used if <see cref="HasCompleteData"/> is <see langword="false"/> before this thread is executed.
		/// </summary>
		public abstract IRefreshThreadBuilder FullRefreshBuilder { get; }

		/// <summary>
		/// Creates a new <see cref="RefreshThread"/> instance.
		/// </summary>
		/// <param name="refreshingUI">The <see cref="BaseStorageUI"/> that is being refreshed by this thread.</param>
		/// <param name="controls">The parameters for collecting and displaying the UI's contents.</param>
		protected RefreshThread(BaseStorageUI refreshingUI, StorageViewControls controls) {
			ArgumentNullException.ThrowIfNull(refreshingUI);
			ArgumentNullException.ThrowIfNull(controls);

			this.refreshingUI = refreshingUI;
			this.controls = controls;
			
			_tokenSource = new CancellationTokenSource();
			cancellationToken = _tokenSource.Token;
		}

		public string DebugName => _debugName ?? GetType().FullName;
		private string _debugName;

		/// <summary>
		/// Sets the debug name for this thread.
		/// </summary>
		public void SetDebugName(string name) => _debugName = name;

		private static int _executionLock;
		private const int UNLOCKED = 0;
		private const int LOCKED = 1;

		private int _finishLock = LOCKED;

		private static void BlockUntilExecutionAllowed() {
			while (Interlocked.CompareExchange(ref _executionLock, LOCKED, UNLOCKED) == LOCKED)
				Thread.Yield();
		}

		private static void AllowNewThreadToExecute() {
			Interlocked.Exchange(ref _executionLock, UNLOCKED);
		}

		private void BlockUntilFinished() {
			while (Interlocked.CompareExchange(ref _finishLock, UNLOCKED, UNLOCKED) == LOCKED)
				Thread.Yield();
		}

		private void MarkAsFinished() {
			Interlocked.Exchange(ref _finishLock, UNLOCKED);
		}

		/// <summary>
		/// Adds a provider that will be waited on after the main work for this thread is complete.
		/// </summary>
		/// <typeparam name="T">The type of the provider.</typeparam>
		/// <param name="provider">The provider to add.</param>
		/// <returns>The current <see cref="RefreshThread"/> instance.</returns>
		/// <exception cref="InvalidOperationException"></exception>
		public RefreshThread AddCompletionTask<T>(T provider) where T : class, IWaitProvider {
			if (_hasStarted)
				throw new InvalidOperationException("Cannot add providers to a thread that has already started");

			_externalWork.Add(provider);
			return this;
		}

		/// <summary>
		/// Starts the thread, stopping the currently running <see cref="RefreshThread"/>, if any.
		/// </summary>
		/// <exception cref="InvalidOperationException"/>
		public void Start() {
			if (_hasStarted)
				throw new InvalidOperationException("Thread state has already started");

			_hasStarted = true;

			if (IsPartialThread && !HasCompleteData) {
				var name = DebugName;

				var builder = FullRefreshBuilder
					?? throw new InvalidOperationException($"Partial refresh thread \"{name}\" was requested with incomplete data, but this.{nameof(FullRefreshBuilder)} was null");

				NetHelper.Report(true, name + ": Partial UI state detected, falling back to full refresh thread...");

				var fullThread = builder.CreateThread(controls);
				fullThread.SetDebugName(name + " (Full Refresh)");
				fullThread.Start();
				return;
			}

			BlockUntilExecutionAllowed();

			if (object.ReferenceEquals(this, MagicUI.activeRefreshingThread))
				throw new InvalidOperationException($"Thread \"{DebugName}\" is already the active refreshing thread");

			if (!Start_LocateStorageHeart())
				return;

			NetHelper.Report(true, DebugName + ": Starting refreshing thread...");

			if (refreshingUI.currentPage is BaseStorageUIAccessPage accessPage)
				accessPage.RequestThreadWait(waiting: true);

			CollectObjects();

			new Task(Tick, TaskCreationOptions.LongRunning).Start();
		}

		private bool Start_LocateStorageHeart() {
			if (StoragePlayer.LocalPlayer.GetStorageHeart() is TEStorageHeart heart && SecuritySystem.CanPlayerAccessImmediately(Main.LocalPlayer, heart.assignedNetwork)) {
				Heart = heart;
				return true;
			}

			NetHelper.Report(true, DebugName + ": Start invoked with no heart or inaccessible network");

			ClearStaticCollections();

			if (refreshingUI.currentPage is BaseStorageUIAccessPage accessPage)
				accessPage.RequestThreadWait(waiting: false);

			if (!MagicUI.CurrentlyRefreshing) {
				// Any active thread will refresh when it completes
				// For the case when there isn't one, a refresh needs to be manually called
				if (IsPartialThread)
					Main.QueueMainThreadAction(PopulateUIZones);
				else
					Main.QueueMainThreadAction(MagicUI.InvokeOnRefresh);
			}

			Heart = null;
			return false;
		}

		/// <summary>
		/// Requests the cancellation of this thread.
		/// </summary>
		public void Stop() {
			_tokenSource.Cancel();
		}

		/// <summary>
		/// Requests the cancellation of this thread and waits for it to stop.
		/// </summary>
		public void StopAndWait() {
			_tokenSource.Cancel();
			BlockUntilFinished();
		}

		private int _targetSteps;
		private int _currentStep;

		/// <summary>
		/// Gets a reference to the current task's action counter for use with functions like <see cref="ItemAggregateResults.Aggregate"/>
		/// </summary>
		public ref int GetCounterReference() => ref _currentStep;

		/// <summary>
		/// The name of the current task being executed.
		/// </summary>
		public string CurrentTask { get; private set; }

		/// <summary>
		/// The progress of this thread's current task, if any, from 0 to 1.
		/// </summary>
		public float Progress {
			get {
				int current = _currentStep;  // One read to preserve atomic operations
				return _targetSteps <= 0 ? 0 : current >= _targetSteps ? 1 : current / (float)_targetSteps;
			}
		}

		/// <summary>
		/// Initializes a new task schedule for this thread.
		/// </summary>
		/// <param name="totalTasks">The total number of actions to be completed.</param>
		/// <param name="taskName">The name of the first action.</param>
		public void InitTaskSchedule(int totalTasks, string taskName) {
			_targetSteps = totalTasks;
			_currentStep = 0;
			CurrentTask = taskName;
		}

		/// <summary>
		/// Resets the number of completed actions for the current task to zero.
		/// </summary>
		public void ResetTaskCompletion() => _currentStep = 0;

		/// <summary>
		/// Initializes a new completed task schedule for this thread.
		/// </summary>
		/// <param name="taskName">The name of the completed task.</param>
		public void InitAsCompleted(string taskName) {
			_targetSteps = 1;
			_currentStep = 1;
			CurrentTask = taskName;
		}

		/// <summary>
		/// Marks the current action as completed for the current task.
		/// </summary>
		public void CompleteOne() => Interlocked.Increment(ref _currentStep);

		private void Initialize() {
			StopActiveThreadAndWait();

			// The prompt is closed if the old thread was cancelled, so make it appear again
			if (refreshingUI.currentPage is BaseStorageUIAccessPage accessPage)
				accessPage.RequestThreadWait(waiting: true);

			// The thread is now ready for reference by external methods
			IsRunning = true;
			MagicUI.activeRefreshingThread = this;

			// Since the active thread is now running, the static collections can be safely cleared
			ClearStaticCollections();

			AllowNewThreadToExecute();
		}

		private void StopActiveThreadAndWait() {
			if (refreshingUI.currentPage is BaseStorageUIAccessPage accessPage)
				accessPage.RequestThreadWait(waiting: true);

			if (MagicUI.HasActiveThread(out RefreshThread previousThread)) {
				NetHelper.Report(true, "Waiting for previous refreshing thread to stop...");
				previousThread.StopAndWait();
			}

			// Always cause the "current UI" to reset its slot zones, etc.
			if (IsPartialThread)
				PrepareUIZones();
			else
				refreshingUI.OnRefreshStart();
		}

		private void Tick() {
			bool hasError = false;

			try {
				Initialize();

				Execute();
				HasSuccessfulCompletion = true;

				NetHelper.Report(true, "Main work for thread finished");
			} catch (OperationCanceledException) {
				NetHelper.Report(true, "Thread work was cancelled");
			} catch (Exception ex) {
				hasError = true;
				MagicStorageMod.Instance.Logger.Error("An exception occurred during a refresh thread's execution:", ex);

				if (Main.netMode != NetmodeID.Server)
					Main.NewTextMultiline("An error occurred while refreshing a UI from Magic Storage.\nCheck your \"tModLoader-Logs/client.log\" file for more information.", c: Color.Red);
			} finally {
				try {
					Cleanup();

					NetHelper.Report(true, "Cleanup for thread finished");

					if (HasSuccessfulCompletion) {
						if (_externalWork.Count > 0)
							NetHelper.Report(true, "External work for thread finished");

						foreach (var provider in _externalWork)
							provider.Wait();
					} else
						ClearStaticCollections();
				} catch (OperationCanceledException) {
					NetHelper.Report(true, "Thread cleanup was cancelled");
				} catch (Exception ex) {
					MagicStorageMod.Instance.Logger.Error("An exception occurred during a refresh thread's cleanup:", ex);

					if (!hasError && Main.netMode != NetmodeID.Server)
						Main.NewTextMultiline("An error occurred while refreshing a UI from Magic Storage.\nCheck your \"tModLoader-Logs/client.log\" file for more information.", c: Color.Red);
				} finally {
					IsRunning = false;

					if (refreshingUI.currentPage is BaseStorageUIAccessPage accessPage)
						accessPage.RequestThreadWait(waiting: false);

					if (object.ReferenceEquals(this, MagicUI.activeRefreshingThread))
						MagicUI.activeRefreshingThread = null;

					// Always ensure that a new thread can be started if no thread is currently active
					if (MagicUI.activeRefreshingThread is null)
						AllowNewThreadToExecute();

					if (!cancellationToken.IsCancellationRequested) {
						// Ensure that race conditions with the UI can't occur
						// QueueMainThreadAction will execute the logic in a very specific place
						if (IsPartialThread)
							Main.QueueMainThreadAction(PopulateUIZones);
						else
							Main.QueueMainThreadAction(MagicUI.InvokeOnRefresh);
					}

					MarkAsFinished();
				}
			}
		}

		/// <summary>
		/// Collects all objects that need to be processed by this thread.<br/>
		/// This method is called before the <see cref="Task"/> for this thread is started.
		/// </summary>
		protected abstract void CollectObjects();

		/// <summary>
		/// The main work for this thread.<br/>
		/// This method is called when the thread starts, and should contain all logic for refreshing the UI.
		/// </summary>
		protected abstract void Execute();

		/// <summary>
		/// Cleans up any resources used by this thread.<br/>
		/// This method is called after <see cref="Execute"/> finishes, regardless of whether it completed successfully, was cancelled or threw an exception.<br/>
		/// Use <see cref="HasSuccessfulCompletion"/> to check whether the thread was able to complete successfully.
		/// </summary>
		protected abstract void Cleanup();

		/// <summary>
		/// Used to clear any static collections used by this thread type.
		/// </summary>
		public abstract void ClearStaticCollections();

		/// <summary>
		/// Prepares the <see cref="NewUISlotZone"/> objects for <see cref="refreshingUI"/> before refreshing starts.<br/>
		/// This method is only invoked if <see cref="IsPartialThread"/> is <see langword="true"/>.
		/// </summary>
		public abstract void PrepareUIZones();

		/// <summary>
		/// Populates the <see cref="NewUISlotZone"/> objects for <see cref="refreshingUI"/> with their items after refreshing has completed.<br/>
		/// This method is only invoked if <see cref="IsPartialThread"/> is <see langword="true"/>.
		/// </summary>
		public abstract void PopulateUIZones();
	}
}
