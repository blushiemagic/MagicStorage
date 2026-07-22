using System;
using System.Runtime.InteropServices;

namespace MagicStorage.Common {
	/// <summary>
	/// A structure containing a reference to a value type
	/// </summary>
	public ref struct RefContainer<T> {
		private Span<T> _span;

		/// <summary>
		/// Gets a reference to the contained value.
		/// </summary>
		public readonly ref T Value => ref MemoryMarshal.GetReference(_span);

		/// <summary>
		/// Creates a container around <paramref name="value"/>.
		/// </summary>
		public RefContainer(ref T value) {
			_span = MemoryMarshal.CreateSpan(ref value, 1);
		}

		/// <summary>
		/// Reassigns this container to reference <paramref name="value"/>.
		/// </summary>
		public void Assign(ref T value) {
			_span = MemoryMarshal.CreateSpan(ref value, 1);
		}
	}
}
