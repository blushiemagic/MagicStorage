using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Terraria;

namespace MagicStorage.Common.Threading {
	/// <summary>
	/// A class responsible for managing work threads while allowing for state reading while the work is being done.
	/// </summary>
	public static class WorkManager {
		private static readonly ConcurrentQueue<Ref<IWorkMessage>> _messageQueue = new();

		internal static object SendMessage<T>(T message) where T : IWorkMessage {
			message.OnMessageSend();

			Ref<IWorkMessage> msg = new(message);

			_messageQueue.Enqueue(msg);

			msg.Value.Wait.WaitOne();

			return null;
		}

		private static void ReadOneMessage() {
			if (!_messageQueue.TryDequeue(out var message))
				return;

			message.Value.OnMessageRead();

			message.Value.Wait.Set();
		}

		private static Work[] _threads;

		private static readonly CancellationTokenSource tokenSource = new();

		public static bool IsWorking { get; private set; }

		internal static class ForEachState {
			public static readonly ConcurrentIndex index = new();
			public static int collectionLength;
			public static int currentWorkDone;

			private static object[] _locksByIndex;

			internal static void Initialize<T>(T[] collection) {
				collectionLength = collection.Length;

				_locksByIndex = new object[collection.Length];

				for (int i = 0; i < collection.Length; i++)
					_locksByIndex[i] = new object();

				index.Reset();

				currentWorkDone = 0;
			}

			public static bool IsFinished() => index.HasIndexReached(collectionLength);

			public static void MarkWorkDone() => Interlocked.Increment(ref currentWorkDone);

			public static void Lock(int index) => Monitor.Enter(_locksByIndex[index]);

			public static void Unlock(int index) => Monitor.Exit(_locksByIndex[index]);
		}

		private static Action TaskCompletionWaitingEvent;

		public static void ForEach<T>(T[] collection, Action<T> work, Action<int, int> reportWork) {
			StopActiveWork();

			_threads ??= new Work[Environment.ProcessorCount];

			for (int i = 0; i < Environment.ProcessorCount; i++)
				_threads[i] = new IndexedWork<T>(collection, ForEachState.index, tokenSource.Token, work);

			ForEachState.Initialize(collection);

			TaskCompletionWaitingEvent = () => {
				int current = ForEachState.currentWorkDone;
				int total = ForEachState.collectionLength;

				reportWork(current, total);
			};

			TickLoop();
		}

		private static void StopActiveWork() {
			if (_threads is null)
				return;
			
			tokenSource.Cancel();

			while (_threads.Any(static t => t?.Task is not null && t.Task.Status is TaskStatus.Running))
				Thread.Yield();
		}

		private static void StartTasks() {
			IsWorking = true;

			foreach (Work work in _threads)
				work.Start();
		}

		internal static ExceptionDispatchInfo workException;

		private static void TickLoop() {
			StartTasks();

			while (!_threads.All(static t => t.waitingForCompletion) || !_messageQueue.IsEmpty) {
				Tick();

				Thread.Yield();
			}

			TaskCompletionWaitingEvent = null;
			IsWorking = false;
		}

		private static void Tick() {
			ReadOneMessage();

			workException?.Throw();

			TaskCompletionWaitingEvent?.Invoke();
		}
	}
}
