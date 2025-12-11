namespace MagicStorage.Common.Threading.Refreshing {
	public interface IFilterProvider<T> {
		bool ShowOnlyBlacklisted { get; }

		bool IsHidden(T value);

		internal bool IsVisible(T value) => !IsHidden(value);

		bool IsFavorited(T value);
	}
}
