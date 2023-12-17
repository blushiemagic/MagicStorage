using System.Threading;

namespace MagicStorage.Common.Threading {
	public sealed class ConcurrentIndex {
		private int _index = -1;

		public void Reset() => _index = -1;

		public int GetNextIndex() => Interlocked.Increment(ref _index);

		public bool HasIndexReached(int index) => _index >= index;
	}
}
