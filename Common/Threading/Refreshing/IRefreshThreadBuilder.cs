namespace MagicStorage.Common.Threading.Refreshing {
	/// <summary>
	/// An interfacing representing a builder for <see cref="RefreshThread"/> threads.
	/// </summary>
	public interface IRefreshThreadBuilder {
		StorageViewControls CreateControls();

		RefreshThread CreateThread(StorageViewControls controls);

		public RefreshThread CreateThread() => CreateThread(CreateControls());
	}
}
