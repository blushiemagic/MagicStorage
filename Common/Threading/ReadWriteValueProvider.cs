namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Provides a mutable value without a static backing source.
	/// </summary>
	/// <typeparam name="T">The provided value type.</typeparam>
	public class ReadWriteValueProvider<T> : IValueProvider<T> {
		/// <inheritdoc />
		T IReadOnlyValueProvider<T>.StaticSource => default;

		/// <inheritdoc />
		public T Value { get; set; }

		/// <summary>
		/// Creates a provider with the default value for <typeparamref name="T" />.
		/// </summary>
		public ReadWriteValueProvider() => Value = default;

		/// <summary>
		/// Creates a provider with the specified initial value.
		/// </summary>
		/// <param name="defaultValue">The initial value.</param>
		public ReadWriteValueProvider(T defaultValue) => Value = defaultValue;

		/// <inheritdoc />
		void IReadOnlyValueProvider<T>.ClearStatic() { }

		/// <inheritdoc />
		void IReadOnlyValueProvider<T>.CopyFromStatic() { }

		/// <inheritdoc />
		void IReadOnlyValueProvider<T>.CopyToStatic() { }
	}
}
