namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Provides a mutable working value copied from a static source.
	/// </summary>
	/// <typeparam name="T">The value type exposed by this provider.</typeparam>
	public interface IValueProvider<T> : IReadOnlyValueProvider<T> {
		/// <inheritdoc />
		T IReadOnlyValueProvider<T>.Value => Value;

		/// <summary>
		/// Gets or sets the local working value.
		/// </summary>
		T Value { get; set; }
	}
}
