namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Thread-local scalar value wrapper that can copy a value to and from shared static storage.
	/// </summary>
	/// <typeparam name="T">The value type exposed by this provider.</typeparam>
	public class ValueProvider<T> : IValueProvider<T> {
		private readonly StaticValue<T> _staticValue;

		/// <inheritdoc />
		public T StaticSource => _staticValue.Value;

		/// <inheritdoc />
		public T Value { get; set; }

		/// <summary>
		/// Creates a provider backed by the specified shared static value.
		/// </summary>
		/// <param name="staticValue">The shared static value.</param>
		public ValueProvider(StaticValue<T> staticValue) {
			_staticValue = staticValue;
			Value = staticValue.Value;
		}

		/// <inheritdoc />
		public void ClearStatic() => _staticValue.Value = default;

		/// <inheritdoc />
		public void CopyFromStatic() => Value = _staticValue.Value;

		/// <inheritdoc />
		public void CopyToStatic() => _staticValue.Value = Value;
	}

	/// <summary>
	/// Mutable shared value storage for <see cref="ValueProvider{T}" />.
	/// </summary>
	/// <typeparam name="T">The stored value type.</typeparam>
	public sealed class StaticValue<T> {
		/// <summary>
		/// Gets or sets the shared value.
		/// </summary>
		public T Value { get; set; }
	}
}
