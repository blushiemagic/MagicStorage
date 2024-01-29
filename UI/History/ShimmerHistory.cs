using MagicStorage.Common.Systems;

namespace MagicStorage.UI.History {
	public class ShimmerHistory : HistoryCollection<ShimmerHistoryEntry, int> {
		public override void Goto(int index) {
			if (index < 0 || index >= history.Count)
				return;

			DecraftingGUI.SetSelectedItem(history[index].Value);
			MagicUI.SetRefresh();

			Current = index;
			RefreshEntries();
		}

		protected override ShimmerHistoryEntry CreateEntry(int index) => new ShimmerHistoryEntry(index, this);

		protected override bool Matches(int entry, int existing) {
			// Both values are just item IDs
			return entry == existing;
		}
	}
}
