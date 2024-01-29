using System.Runtime.InteropServices;
using System;

namespace MagicStorage.Common {
	/// <summary>
	/// A structure used to temporarily overwrite the value in a variable.  This type is intended to be used in <see langword="using"/> contexts
	/// </summary>
	public readonly ref struct ObjectSwitch<T> {
		private readonly Span<T> _span;
		private readonly T _old;

		public ObjectSwitch(ref T flag, T value) {
			_old = flag;
			_span = MemoryMarshal.CreateSpan(ref flag, 1);
			flag = value;
		}

		public void Dispose() {
			_span[0] = _old;
		}

		public static ObjectSwitch<T> Create(ref T flag, T value) => new(ref flag, value);
	}

	/// <inheritdoc cref="ObjectSwitch{T}"/>/>
	public static class ObjectSwitch {
		public static ObjectSwitch<T> Create<T>(ref T flag, T value) => ObjectSwitch<T>.Create(ref flag, value);

		public static ObjectSwitch<T> SwapNull<T>(ref T flag) where T : class => ObjectSwitch<T>.Create(ref flag, null);

		public static ObjectSwitch<T?> SwapNull<T>(ref T? flag) where T : struct => ObjectSwitch<T?>.Create(ref flag, null);
	}
}
