using MagicStorage.Common.Threading.UI;
using System.Collections;
using System.Collections.Generic;

namespace MagicStorage.Common.Threading {
	internal class IterationIndexWatcher<T> : IEnumerable<T>, IEnumerator<T> {
		private readonly RefreshThread _thread;
		private readonly int _counterStart;

		private readonly IEnumerable<T> _source;
		private readonly IEnumerator<T> _enumerator;

		private T _current;
		private bool _completedOneStep;
		private bool _completedFinalStep;

		public T Current => _current;

		object IEnumerator.Current => Current;

		public IterationIndexWatcher(RefreshThread thread, IEnumerable<T> source) {
			_thread = thread;
			_counterStart = thread.GetCounterReference();
			_source = source;
			_enumerator = _source.GetEnumerator();
		}

		public void Dispose() {
			_enumerator.Dispose();
			_current = default;
		}

		public IEnumerator<T> GetEnumerator() => this;

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public bool MoveNext() {
			if (_enumerator.MoveNext()) {
				// Delay the increment to when the next value is obtained
				if (_completedOneStep)
					_thread.CompleteOne();

				_completedOneStep = true;
				return true;
			}

			// Increment the final step here
			if (!_completedFinalStep)
				_thread.CompleteOne();

			_completedFinalStep = true;
			return false;
		}

		public void Reset() {
			_thread.GetCounterReference() = _counterStart;
			_enumerator.Reset();
			_current = default;
			_completedOneStep = false;
			_completedOneStep = false;
		}
	}

	/// <summary/>
	public static class IterationIndexWatcherExtensions {
		/// <summary>
		/// Wraps <paramref name="this"/> enumeration with an object that notifies <paramref name="thread"/> when each item is iterated.
		/// </summary>
		/// <typeparam name="T">The type of items in the enumeration.</typeparam>
		/// <param name="this">The enumeration to wrap.</param>
		/// <param name="thread">The thread to notify.</param>
		public static IEnumerable<T> NotifyStepsTo<T>(this IEnumerable<T> @this, RefreshThread thread) => new IterationIndexWatcher<T>(thread, @this);
	}
}
