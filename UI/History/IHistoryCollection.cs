using SerousCommonLib.UI;
using System.Collections.Generic;

namespace MagicStorage.UI.History {
	public interface IHistoryCollection {
		int Current { get; }

		int Count { get; }

		IEnumerable<IHistory> History { get; }

		NewUIScrollbar Scrollbar { get; }

		NewUIList List { get; }

		void Goto(int index);

		void AddHistory(object value);

		void RefreshEntries();

		void Clear();
	}

	public interface IHistoryCollection<TValue> : IHistoryCollection {
		new IEnumerable<IHistory<TValue>> History { get; }

		void AddHistory(TValue value);
	}
}
