using System;
using System.Threading;

namespace MagicStorage.Common {
	/// <summary>
	/// A mutex that allows multiple threads to safely change the console color
	/// </summary>
	public readonly ref struct ConsoleColorLock {
		private static int _lock;

		private readonly ConsoleColor _fg, _bg;
		private readonly bool _reset;
		private readonly bool _valid;

		private ConsoleColorLock(ConsoleColor fg, ConsoleColor bg, bool reset) {
			_fg = fg;
			_bg = bg;
			_reset = reset;
			_valid = true;
		}

		/// <summary>
		/// Acquires the console color lock and applies the requested colors until disposal.
		/// </summary>
		public static ConsoleColorLock Acquire(ConsoleColor fg, ConsoleColor bg) {
			int localLock = Interlocked.Increment(ref _lock);

			if (localLock > 0) {
				// Spin until this lock is the topmost one
				while (Interlocked.CompareExchange(ref _lock, localLock, localLock) != localLock)
					Thread.Yield();
			}

			var oldFG = Console.ForegroundColor;
			var oldBG = Console.BackgroundColor;
			Console.ForegroundColor = fg;
			Console.BackgroundColor = bg;
			return new ConsoleColorLock(oldFG, oldBG, false);
		}

		/// <summary>
		/// Acquires the console color lock and resets console colors until disposal.
		/// </summary>
		public static ConsoleColorLock Acquire() {
			int localLock = Interlocked.Increment(ref _lock);

			if (localLock > 0) {
				// Spin until this lock is the topmost one
				while (Interlocked.CompareExchange(ref _lock, localLock, localLock) != localLock)
					Thread.Yield();
			}

			Console.ResetColor();
			return new ConsoleColorLock(default, default, true);
		}

		/// <summary>
		/// Restores the previous console colors and releases the lock.
		/// </summary>
		public void Dispose() {
			if (!_valid)
				return;

			if (_reset) {
				// Just in case colors got changed midway
				Console.ResetColor();
			} else {
				// Restore the previous console colors
				Console.ForegroundColor = _fg;
				Console.BackgroundColor = _bg;
			}

			// Release the lock
			Interlocked.Decrement(ref _lock);
		}
	}
}
