using System.Threading;

namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Provides a thread-safe monotonically increasing index counter.
	/// </summary>
	public sealed class ConcurrentIndex {
		private int _index = -1;

		/// <summary>
		/// Resets the counter so the next reserved index is zero.
		/// </summary>
		public void Reset() => _index = -1;

		/// <summary>
		/// Reserves and returns the next index.
		/// </summary>
		/// <returns>The reserved index.</returns>
		public int GetNextIndex() => Interlocked.Increment(ref _index);

		/// <summary>
		/// Checks whether the counter has reached at least the specified index.
		/// </summary>
		/// <param name="index">The index to compare against.</param>
		/// <returns><see langword="true" /> if the counter has reached <paramref name="index" />; otherwise, <see langword="false" />.</returns>
		public bool HasIndexReached(int index) => _index >= index;
	}
}
