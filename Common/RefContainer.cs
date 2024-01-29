using System;
using System.Runtime.InteropServices;

namespace MagicStorage.Common {
	/// <summary>
	/// A structure containing a reference to a value type
	/// </summary>
	public ref struct RefContainer<T> {
		private Span<T> _span;

		public readonly ref T Value => ref MemoryMarshal.GetReference(_span);

		public RefContainer(ref T value) {
			_span = MemoryMarshal.CreateSpan(ref value, 1);
		}

		public void Assign(ref T value) {
			_span = MemoryMarshal.CreateSpan(ref value, 1);
		}
	}
}
