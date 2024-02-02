using MagicStorage.Common.Systems;
using Terraria;

namespace MagicStorage.UI.History {
	public class RecipeHistory : HistoryCollection<RecipeHistoryEntry, Recipe> {
		public override void Goto(int index) {
			if (index < 0 || index >= history.Count)
				return;

			CraftingGUI.SetSelectedRecipe(history[index].Value);
			MagicUI.SetRefresh();

			Current = index;
			RefreshEntries();
		}

		protected override void Clear() {
			if (CraftingGUI.selectedRecipe is not null) {
				AddHistory(CraftingGUI.selectedRecipe);
				RefreshEntries();
			}
		}

		protected override RecipeHistoryEntry CreateEntry(int index) => new RecipeHistoryEntry(index, this);

		protected override bool Matches(Recipe entry, Recipe existing) => Utility.RecipesMatchForHistory(entry, existing);
	}
}
