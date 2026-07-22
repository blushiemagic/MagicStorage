namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Provides a working value copied from a static source.
	/// </summary>
	/// <typeparam name="T">The value type exposed by this provider.</typeparam>
	public interface IReadOnlyValueProvider<T> {
		/// <summary>
		/// Gets the local working value.
		/// </summary>
		T Value { get; }

		/// <summary>
		/// Gets the static source value mirrored by this provider.
		/// </summary>
		T StaticSource { get; }

		/// <summary>
		/// Clears the static source value.
		/// </summary>
		void ClearStatic();

		/// <summary>
		/// Copies the static source value into the local working value.
		/// </summary>
		void CopyFromStatic();

		/// <summary>
		/// Copies the local working value back into the static source value.
		/// </summary>
		void CopyToStatic();
	}

	/// <summary>
	/// Extension helpers for static-source value providers.
	/// </summary>
	public static class IReadOnlyValueProviderExtentions {
		/// <summary>
		/// Replaces the static source value with the provider's current local working value.
		/// </summary>
		/// <typeparam name="T">The value type exposed by the provider.</typeparam>
		/// <param name="this">The provider to copy from.</param>
		public static void OverwriteStatic<T>(this IReadOnlyValueProvider<T> @this) {
			@this.ClearStatic();
			@this.CopyToStatic();
		}
	}
}
