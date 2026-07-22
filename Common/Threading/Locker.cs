using System.Runtime.CompilerServices;
using System.Threading;

namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Lightweight disposable spin lock over an integer lock field.
	/// </summary>
	public ref struct Locker {
		private const int UNLOCKED = 0;
		private const int LOCKED = 1;

		private ref int _lock;
		private readonly bool _hasLock;

		/// <summary>
		/// Acquires the specified integer lock.
		/// </summary>
		/// <param name="lock">The integer lock field to acquire.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Locker(ref int @lock) {
			_lock = ref @lock;
			_hasLock = true;

			while (Interlocked.CompareExchange(ref @lock, LOCKED, UNLOCKED) == LOCKED)
				Thread.Yield();
		}

		/// <summary>
		/// Releases the lock if it was acquired.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose() {
			if (_hasLock)
				Interlocked.Exchange(ref _lock, UNLOCKED);
		}
	}
}
