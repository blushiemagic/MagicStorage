using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Represents a message exchanged between worker tasks and the work manager.
	/// </summary>
	public interface IWorkMessage {
		/// <summary>
		/// Gets the worker that sent the message.
		/// </summary>
		Work Source { get; }

		/// <summary>
		/// Gets the wait handle signaled after the message is processed.
		/// </summary>
		AutoResetEvent Wait { get; }

		/// <summary>
		/// Runs on the manager thread when the message is read.
		/// </summary>
		void OnMessageRead();

		/// <summary>
		/// Runs on the sender after the message is queued.
		/// </summary>
		void OnMessageSend();
	}

	/// <summary>
	/// Requests the next item index for an indexed worker.
	/// </summary>
	/// <typeparam name="T">The collection element type.</typeparam>
	public readonly struct RequestForEachWorkMessage<T> : IWorkMessage {
		/// <inheritdoc />
		public Work Source => WorkSource;

		/// <inheritdoc />
		public AutoResetEvent Wait { get; } = new(false);

		/// <summary>
		/// Gets the indexed worker requesting more work.
		/// </summary>
		public IndexedWork<T> WorkSource { get; }

		/// <summary>
		/// Creates a request for the specified indexed worker.
		/// </summary>
		/// <param name="source">The indexed worker requesting more work.</param>
		public RequestForEachWorkMessage(IndexedWork<T> source) {
			WorkSource = source;
		}

		/// <inheritdoc />
		public void OnMessageRead() {
			if (WorkManager.ForEachState.IsFinished()) {
				Source.waitingForCompletion = true;
				return;
			}

			WorkSource.ReserveIndex();
		}

		/// <inheritdoc />
		public void OnMessageSend() { }
	}

	/// <summary>
	/// Reports that a worker completed its current for-each item.
	/// </summary>
	public readonly struct ForEachWorkCompletedMessage : IWorkMessage {
		/// <inheritdoc />
		public Work Source { get; }

		/// <inheritdoc />
		public AutoResetEvent Wait { get; } = new(false);

		/// <summary>
		/// Creates a completion message for the specified worker.
		/// </summary>
		/// <param name="source">The worker that completed its item.</param>
		public ForEachWorkCompletedMessage(Work source) {
			Source = source;
		}

		/// <inheritdoc />
		public void OnMessageRead() {
			WorkManager.ForEachState.MarkWorkDone();
		}

		/// <inheritdoc />
		public void OnMessageSend() { }
	}

	/// <summary>
	/// Reports an exception thrown by a worker.
	/// </summary>
	public readonly struct FailedWorkMessage : IWorkMessage {
		/// <inheritdoc />
		public Work Source { get; }

		/// <inheritdoc />
		public AutoResetEvent Wait { get; } = new(false);

		/// <summary>
		/// Gets the captured worker exception.
		/// </summary>
		public readonly ExceptionDispatchInfo info;

		/// <summary>
		/// Creates a failed-work message from the specified exception.
		/// </summary>
		/// <param name="source">The worker that failed.</param>
		/// <param name="exception">The exception thrown by the worker.</param>
		public FailedWorkMessage(Work source, Exception exception) {
			Source = source;
			info = ExceptionDispatchInfo.Capture(exception);
		}

		/// <inheritdoc />
		public void OnMessageRead() {
			WorkManager.workException = info;
		}

		/// <inheritdoc />
		public void OnMessageSend() { }
	}
}
