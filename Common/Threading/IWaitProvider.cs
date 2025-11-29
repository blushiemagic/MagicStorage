namespace MagicStorage.Common.Threading {
	/// <summary>
	/// An interface representing an object whose execution can be waited upon.
	/// </summary>
	public interface IWaitProvider {
		/// <summary>
		/// Blocks the calling thread until this object's execution is no longer running.
		/// </summary>
		void Wait();
	}
}
