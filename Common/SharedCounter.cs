namespace MagicStorage.Common {
	/// <summary>
	/// An object used to share the craft target between subrecipe options when performing recursion crafting
	/// </summary>
	public sealed class SharedCounter {
		private int _counter;

		public SharedCounter(int counter) {
			_counter = counter;
		}

		public void EnsureNotNegative() {
			if (_counter < 0)
				_counter = 0;
		}

		public void Reset() {
			_counter = 0;
		}

		internal void SetToAtMinimum(int value) {
			if (_counter < value)
				_counter = value;
		}

		public static implicit operator int(SharedCounter counter) => counter._counter;

		public static SharedCounter operator +(SharedCounter counter, int value) {
			counter._counter += value;
			return counter;
		}

		public static SharedCounter operator -(SharedCounter counter, int value) {
			counter._counter -= value;
			return counter;
		}
	}
}
