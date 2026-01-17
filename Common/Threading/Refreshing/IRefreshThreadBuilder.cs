namespace MagicStorage.Common.Threading.Refreshing {
	/// <summary>
	/// An interfacing representing a builder for <see cref="RefreshThread"/> threads.
	/// </summary>
	public interface IRefreshThreadBuilder {
		StorageViewControls CreateControls();

		RefreshThread CreateThread(StorageViewControls controls);
	}

	public interface IRefreshThreadBuilder<TSelf> : IRefreshThreadBuilder where TSelf : IRefreshThreadBuilder<TSelf> {
		static abstract TSelf Instance { get; }
	}

	public static class IRefreshThreadBuilderExtensions {
		public static RefreshThread CreateThread(this IRefreshThreadBuilder @this) => @this.CreateThread(@this.CreateControls());
	}
}
