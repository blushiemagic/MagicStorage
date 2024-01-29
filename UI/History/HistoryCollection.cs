using SerousCommonLib.UI;
using System.Collections.Generic;
using Terraria.UI;

namespace MagicStorage.UI.History {
	public abstract class HistoryCollection<TEntry, TValue> : IHistoryCollection<TValue> where TEntry : UIElement, IHistory<TValue> {
		public int Current { get; protected set; } = -1;

		public int Count => history.Count;

		IEnumerable<IHistory> IHistoryCollection.History => history.AsReadOnly();

		IEnumerable<IHistory<TValue>> IHistoryCollection<TValue>.History => history.AsReadOnly();

		public NewUIScrollbar Scrollbar { get; }

		public NewUIList List { get; }

		protected readonly List<TEntry> history;

		public HistoryCollection() {
			List = new() {
				DisplayChildrenInReverseOrder = true
			};
			List.SetPadding(0);
			List.Width = StyleDimension.Fill;
			List.Height = StyleDimension.Fill;

			Scrollbar = new();
			Scrollbar.Width.Set(20, 0);
			Scrollbar.Height.Set(0, 0.825f);
			Scrollbar.Left.Set(-30, 1f);
			Scrollbar.Top.Set(0, 0.1f);

			List.SetScrollbar(Scrollbar);
			List.Append(Scrollbar);
			List.ListPadding = 4;

			history = new();
		}

		public abstract void Goto(int index);

		protected abstract bool Matches(TValue entry, TValue existing);

		protected abstract TEntry CreateEntry(int index);

		void IHistoryCollection.AddHistory(object value) => AddHistory((TValue)value);

		public void AddHistory(TValue value) {
			// If the entry is already present, just jump to it
			int existingEntry = history.FindIndex(h => Matches(value, h.Value));
			if (existingEntry >= 0) {
				Current = existingEntry;
				return;
			}

			TEntry entry = CreateEntry(Current + 1);

			if (Current < history.Count - 1) {
				//History was moved back.  Remove all entries after it
				int start = Current + 1;
				int count = history.Count - start;

				//Remove entries from the list
				for (int i = history.Count - 1; i >= start; i--)
					List.Remove(history[i]);

				history.RemoveRange(start, count);
			}

			history.Add(entry);
			List.Add(entry);

			entry.Activate();

			entry.SetValue(value);

			List.Recalculate();

			Current++;
		}

		public void RefreshEntries() {
			foreach (var entry in history)
				entry.Refresh();
		}

		public void Clear() {
			Current = -1;
			List.Clear();
			history.Clear();
		}
	}
}
