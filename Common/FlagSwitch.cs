using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MagicStorage.Common {
	/// <summary>
	/// A structure used to temporarily overwrite the value in a boolean variable.  This type is intended to be used in <see langword="using"/> contexts
	/// </summary>
	public readonly ref struct FlagSwitch {
		private readonly Span<bool> _flag;
		private readonly bool _old;

		/// <summary>
		/// Stores the current value of <paramref name="flag"/> and replaces it with <paramref name="value"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public FlagSwitch(ref bool flag, bool value) {
			_old = flag;
			_flag = MemoryMarshal.CreateSpan(ref flag, 1);
			flag = value;
		}

		/// <summary>
		/// Restores the original flag value.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public void Dispose() {
			_flag[0] = _old;
		}

		/// <summary>
		/// Creates a switch that restores <paramref name="flag"/> when disposed.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static FlagSwitch Create(ref bool flag, bool value) => new FlagSwitch(ref flag, value);

		/// <summary>
		/// Temporarily sets <paramref name="flag"/> to <see langword="true"/> while preserving nested switch behavior.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static FlagSwitch ToggleTrue(ref bool flag) {
			flag = false;
			return new FlagSwitch(ref flag, true);
		}

		/// <summary>
		/// Temporarily sets <paramref name="flag"/> to <see langword="false"/> while preserving nested switch behavior.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static FlagSwitch ToggleFalse(ref bool flag) {
			flag = true;
			return new FlagSwitch(ref flag, false);
		}
	}
}
