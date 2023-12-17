using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace MagicStorage.Common.Threading {
	public interface IWorkMessage {
		Work Source { get; }

		AutoResetEvent Wait { get; }

		void OnMessageRead();

		void OnMessageSend();
	}

	public readonly struct RequestForEachWorkMessage<T> : IWorkMessage {
		public Work Source => WorkSource;
		public AutoResetEvent Wait { get; } = new(false);

		public IndexedWork<T> WorkSource { get; }

		public RequestForEachWorkMessage(IndexedWork<T> source) {
			WorkSource = source;
		}

		public void OnMessageRead() {
			if (WorkManager.ForEachState.IsFinished()) {
				Source.waitingForCompletion = true;
				return;
			}

			WorkSource.ReserveIndex();
		}

		public void OnMessageSend() { }
	}

	public readonly struct ForEachWorkCompletedMessage : IWorkMessage {
		public Work Source { get; }
		public AutoResetEvent Wait { get; } = new(false);

		public ForEachWorkCompletedMessage(Work source) {
			Source = source;
		}

		public void OnMessageRead() {
			WorkManager.ForEachState.MarkWorkDone();
		}

		public void OnMessageSend() { }
	}

	public readonly struct FailedWorkMessage : IWorkMessage {
		public Work Source { get; }
		public AutoResetEvent Wait { get; } = new(false);

		public readonly ExceptionDispatchInfo info;

		public FailedWorkMessage(Work source, Exception exception) {
			Source = source;
			info = ExceptionDispatchInfo.Capture(exception);
		}

		public void OnMessageRead() {
			WorkManager.workException = info;
		}

		public void OnMessageSend() { }
	}
}
