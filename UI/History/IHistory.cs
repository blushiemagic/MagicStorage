namespace MagicStorage.UI.History {
	public interface IHistory {
		object Value { get; }

		int Index { get; }

		IHistoryCollection History { get; }

		void Refresh();
	}

	public interface IHistory<T> : IHistory {
		new T Value { get; }

		new IHistoryCollection<T> History { get; }

		void SetValue(T value);
	}
}
