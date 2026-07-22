namespace MagicStorage.Common {
	/// <summary>
	/// An object used to share the craft target between subrecipe options when performing recursion crafting
	/// </summary>
	public sealed class SharedCounter {
		private int _counter;

		/// <summary>
		/// Creates a shared counter with an initial value.
		/// </summary>
		public SharedCounter(int counter) {
			_counter = counter;
		}

		/// <summary>
		/// Clamps the counter to zero if it is negative.
		/// </summary>
		public void EnsureNotNegative() {
			if (_counter < 0)
				_counter = 0;
		}

		/// <summary>
		/// Resets the counter to zero.
		/// </summary>
		public void Reset() {
			_counter = 0;
		}

		internal void SetToAtMinimum(int value) {
			if (_counter < value)
				_counter = value;
		}

		/// <summary>
		/// Reads the current counter value.
		/// </summary>
		public static implicit operator int(SharedCounter counter) => counter._counter;

		/// <summary>
		/// Adds <paramref name="value"/> to the shared counter and returns the same instance.
		/// </summary>
		public static SharedCounter operator +(SharedCounter counter, int value) {
			counter._counter += value;
			return counter;
		}

		/// <summary>
		/// Subtracts <paramref name="value"/> from the shared counter and returns the same instance.
		/// </summary>
		public static SharedCounter operator -(SharedCounter counter, int value) {
			counter._counter -= value;
			return counter;
		}
	}
}
