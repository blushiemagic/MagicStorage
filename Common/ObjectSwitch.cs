using System.Runtime.InteropServices;
using System;

namespace MagicStorage.Common {
	/// <summary>
	/// A structure used to temporarily overwrite the value in a variable.  This type is intended to be used in <see langword="using"/> contexts
	/// </summary>
	public readonly ref struct ObjectSwitch<T> {
		private readonly Span<T> _span;
		private readonly T _old;

		/// <summary>
		/// Stores the current value of <paramref name="flag"/> and replaces it with <paramref name="value"/>.
		/// </summary>
		public ObjectSwitch(ref T flag, T value) {
			_old = flag;
			_span = MemoryMarshal.CreateSpan(ref flag, 1);
			flag = value;
		}

		/// <summary>
		/// Restores the original value.
		/// </summary>
		public void Dispose() {
			_span[0] = _old;
		}

		/// <summary>
		/// Creates a switch that restores <paramref name="flag"/> when disposed.
		/// </summary>
		public static ObjectSwitch<T> Create(ref T flag, T value) => new(ref flag, value);
	}

	/// <inheritdoc cref="ObjectSwitch{T}"/>/>
	public static class ObjectSwitch {
		/// <inheritdoc cref="ObjectSwitch{T}.Create(ref T, T)"/>
		public static ObjectSwitch<T> Create<T>(ref T flag, T value) => ObjectSwitch<T>.Create(ref flag, value);

		/// <summary>
		/// Temporarily assigns <see langword="null"/> to a reference value.
		/// </summary>
		public static ObjectSwitch<T> SwapNull<T>(ref T flag) where T : class => ObjectSwitch<T>.Create(ref flag, null);

		/// <summary>
		/// Temporarily assigns <see langword="null"/> to a nullable value.
		/// </summary>
		public static ObjectSwitch<T?> SwapNull<T>(ref T? flag) where T : struct => ObjectSwitch<T?>.Create(ref flag, null);
	}
}
