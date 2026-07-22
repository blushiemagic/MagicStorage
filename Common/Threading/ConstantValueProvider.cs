namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Provides a constant read-only working value.
	/// </summary>
	/// <typeparam name="T">The provided value type.</typeparam>
	/// <param name="constantValue">The constant value to expose.</param>
	public class ConstantValueProvider<T>(T constantValue) : IReadOnlyValueProvider<T> {
		/// <inheritdoc />
		T IReadOnlyValueProvider<T>.StaticSource { get; } = default;

		/// <inheritdoc />
		T IReadOnlyValueProvider<T>.Value { get; } = constantValue;

		/// <inheritdoc />
		void IReadOnlyValueProvider<T>.ClearStatic() { }

		/// <inheritdoc />
		void IReadOnlyValueProvider<T>.CopyFromStatic() { }

		/// <inheritdoc />
		void IReadOnlyValueProvider<T>.CopyToStatic() { }
	}
}
