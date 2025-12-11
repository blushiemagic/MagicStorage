namespace MagicStorage.Common.Threading {
	public interface IReadOnlyValueProvider<T> {
		T Value { get; }

		void ClearStatic();

		void CopyFromStatic();

		void CopyToStatic();
	}

	public static class IReadOnlyValueProviderExtentions {
		public static void OverwriteStatic<T>(this IReadOnlyValueProvider<T> @this) {
			@this.ClearStatic();
			@this.CopyToStatic();
		}
	}
}
