using MagicStorage.Common.Threading.Refreshing;
using SerousCommonLib.API.Iterators;
using System.Collections.Generic;
using System.Linq;

namespace MagicStorage.Common.Threading {
	internal class CancellationTokenWatcher<T> : Iterator<T> {
		private readonly RefreshThread _thread;
		private readonly int _iterationsPerCheck;
		private readonly IEnumerable<T> _source;
		private int _remainingIterations;
		private IEnumerator<T> _enumerator;

		public CancellationTokenWatcher(RefreshThread thread, int stepsPerCheck, IEnumerable<T> source) {
			_thread = thread;
			_iterationsPerCheck = stepsPerCheck;
			_source = source;
		}

		public override Iterator<T> Clone() => new CancellationTokenWatcher<T>(_thread, _iterationsPerCheck, _source);

		public override void Dispose() {
			_enumerator?.Dispose();
			_enumerator = null;

			base.Dispose();
		}

		public override bool MoveNext() {
			switch (base._state) {
				case 1:
					_remainingIterations = _iterationsPerCheck;
					_enumerator = _source.GetEnumerator();
					base._state = 2;
					goto case 2;
				case 2:
					if (--_remainingIterations <= 0) {
						_thread.cancellationToken.ThrowIfCancellationRequested();
						_remainingIterations = _iterationsPerCheck;
					}

					if (_enumerator.MoveNext()) {
						base._current = _enumerator.Current;
						return true;
					}

					break;
			}

			Dispose();
			return false;
		}
	}

	public static class CancellationTokenWatcherExtensions {
		/// <summary>
		/// Wraps <paramref name="this"/> enumeration with an object that checks the cancellation token of <paramref name="thread"/> every <paramref name="iterationsPerCheck"/> iterations.
		/// </summary>
		public static IEnumerable<T> WatchForCancellation<T>(this IEnumerable<T> @this, RefreshThread thread, int iterationsPerCheck) => new CancellationTokenWatcher<T>(thread, iterationsPerCheck, @this);

		/// <summary>
		/// Converts <paramref name="this"/> enumeration to a parallel query which checks the cancellation token of <paramref name="thread"/> every <paramref name="iterationsPerCheck"/> iterations.
		/// </summary>
		public static ParallelQuery<T> ToCancellableQuery<T>(this IEnumerable<T> @this, RefreshThread thread, int iterationsPerCheck) => @this.WatchForCancellation(thread, iterationsPerCheck).AsParallel().WithCancellation(thread.cancellationToken);

		/// <summary>
		/// Converts <paramref name="this"/> enumeration to an ordered parallel query which checks the cancellation token of <paramref name="thread"/> every <paramref name="iterationsPerCheck"/> iterations.
		/// </summary>
		public static ParallelQuery<T> ToCancellableOrderedQuery<T>(this IEnumerable<T> @this, RefreshThread thread, int iterationsPerCheck) => @this.WatchForCancellation(thread, iterationsPerCheck).AsParallel().AsOrdered().WithCancellation(thread.cancellationToken);
	}
}
