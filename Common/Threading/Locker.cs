using System.Runtime.CompilerServices;
using System.Threading;

namespace MagicStorage.Common.Threading {
	public ref struct Locker {
		private const int UNLOCKED = 0;
		private const int LOCKED = 1;

		private ref int _lock;
		private readonly bool _hasLock;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Locker(ref int @lock) {
			_lock = ref @lock;
			_hasLock = true;

			while (Interlocked.CompareExchange(ref @lock, LOCKED, UNLOCKED) == LOCKED)
				Thread.Yield();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose() {
			if (_hasLock)
				Interlocked.Exchange(ref _lock, UNLOCKED);
		}
	}
}
