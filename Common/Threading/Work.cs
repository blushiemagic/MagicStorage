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

		public Task Task => _task;

		internal bool waitingForWork;

		internal bool waitingForCompletion;
		
		public Work(CancellationToken token) {
			_task = new Task(DoWork, token, TaskCreationOptions.LongRunning);
		}

		internal void Start() => _task.Start();

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
					Exception? exception = null;

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

		protected abstract object SendRequest();

		protected abstract object SendCompletion();

		protected abstract void Tick();
	}

	public sealed class IndexedWork<T> : Work {
		private readonly T[] _collection;
		private readonly ConcurrentIndex _index;
		private readonly Action<T> _work;

		private int _reservedIndex;

		public IndexedWork(T[] collection, ConcurrentIndex index, CancellationToken token, Action<T> work) : base(token) {
			_collection = collection;
			_index = index;
			_work = work;
		}

		protected override object SendRequest() => WorkManager.SendMessage(new RequestForEachWorkMessage<T>(this));

		protected override object SendCompletion() => WorkManager.SendMessage(new ForEachWorkCompletedMessage(this));

		protected override void Tick() {
			if (_reservedIndex >= _collection.Length)
				return;

			_work(_collection[_reservedIndex]);
		}

		public void ReserveIndex() => _reservedIndex = _index.GetNextIndex();
	}
}
