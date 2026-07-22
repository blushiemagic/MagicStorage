using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace MagicStorage.Common.Threading {
	/// <summary>
	/// An object wrapping over a working <see cref="Task"/>
	/// </summary>
	public abstract class Work {
		private readonly Task _task;
		private readonly CancellationToken _token;

		/// <summary>
		/// Gets the backing task that executes this work item.
		/// </summary>
		public Task Task => _task;

		internal bool waitingForWork;

		internal bool waitingForCompletion;
		
		/// <summary>
		/// Creates a work wrapper that observes the specified cancellation token.
		/// </summary>
		/// <param name="token">The cancellation token passed to the backing task.</param>
		public Work(CancellationToken token) {
			_task = new Task(DoWork, token, TaskCreationOptions.LongRunning);
			_token = token;
		}

		internal void Start() => _task.Start();

		/// <summary>
		/// Runs the worker loop until the work manager reports completion.
		/// </summary>
		public void DoWork() {
			foreach (object _ in TickLoop())
				Thread.Yield();
		}

		private IEnumerable TickLoop() {
			while (true) {
				waitingForWork = true;
				yield return SendRequest();
				waitingForWork = false;

				if (!waitingForCompletion) {
					Exception exception = null;

					try {
						Tick();
					} catch (Exception ex) {
						exception = ex;
					}

					if (exception is null)
						yield return SendCompletion();
					else {
						yield return WorkManager.SendMessage(new FailedWorkMessage(this, exception));
						throw new Exception("An error occurred while processing a work task", exception);
					}
				} else
					yield break;
			}
		}

		/// <summary>
		/// Sends a request message for the next unit of work.
		/// </summary>
		/// <returns>The yielded request token.</returns>
		protected abstract object SendRequest();

		/// <summary>
		/// Sends a completion message for the current unit of work.
		/// </summary>
		/// <returns>The yielded completion token.</returns>
		protected abstract object SendCompletion();

		/// <summary>
		/// Processes one reserved unit of work.
		/// </summary>
		protected abstract void Tick();
	}

	/// <summary>
	/// Processes elements from an array by reserving indexes through a shared counter.
	/// </summary>
	/// <typeparam name="T">The array element type.</typeparam>
	public sealed class IndexedWork<T> : Work {
		private readonly T[] _collection;
		private readonly ConcurrentIndex _index;
		private readonly Action<T> _work;

		private int _reservedIndex;

		/// <summary>
		/// Creates a worker for the specified collection and action.
		/// </summary>
		/// <param name="collection">The collection to process.</param>
		/// <param name="index">The shared index counter used to reserve work.</param>
		/// <param name="token">The cancellation token passed to the backing task.</param>
		/// <param name="work">The action to invoke for each reserved item.</param>
		public IndexedWork(T[] collection, ConcurrentIndex index, CancellationToken token, Action<T> work) : base(token) {
			_collection = collection;
			_index = index;
			_work = work;
		}

		/// <inheritdoc />
		protected override object SendRequest() => WorkManager.SendMessage(new RequestForEachWorkMessage<T>(this));

		/// <inheritdoc />
		protected override object SendCompletion() => WorkManager.SendMessage(new ForEachWorkCompletedMessage(this));

		/// <inheritdoc />
		protected override void Tick() {
			if (_reservedIndex >= _collection.Length)
				return;

			_work(_collection[_reservedIndex]);
		}

		/// <summary>
		/// Reserves the next collection index for this worker.
		/// </summary>
		public void ReserveIndex() => _reservedIndex = _index.GetNextIndex();
	}
}
