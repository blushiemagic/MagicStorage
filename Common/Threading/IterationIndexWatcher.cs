using MagicStorage.Common.Threading.Refreshing;
using SerousCommonLib.API.Iterators;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MagicStorage.Common.Threading {
	internal class IterationIndexWatcher<T> : Iterator<T> {
		private readonly RefreshThread _thread;
		private readonly IEnumerable<T> _source;
		private IEnumerator<T> _enumerator;

		public IterationIndexWatcher(RefreshThread thread, IEnumerable<T> source) {
			_thread = thread;
			_source = source;
		}

		public override Iterator<T> Clone() => new IterationIndexWatcher<T>(_thread, _source);

		public override void Dispose() {
			_enumerator?.Dispose();
			_enumerator = null;

			base.Dispose();
		}

		public override bool MoveNext() {
			switch (base._state) {
				case 1:
					_enumerator = _source.GetEnumerator();
					base._state = 2;
					goto case 2;
				case 2:
					if (_enumerator.MoveNext()) {
						// Delay the increment to when the next value is obtained
						base._current = _enumerator.Current;
						base._state = 3;
						return true;
					}

					// The enumeration was empty, so just mark one "iteration"
					ThreadStep();
					break;
				case 3:
					if (_enumerator.MoveNext()) {
						base._current = _enumerator.Current;
						ThreadStep();
						return true;
					}

					// The enumeration has finished; mark the final iteration
					ThreadStep();
					break;
			}

			Dispose();
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ThreadStep() {
			// Uncomment to make loading take longer
		//	Thread.Sleep(1);
			_thread.CompleteOne();
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
