using MagicStorage.Common.Threading;
using Terraria;

namespace MagicStorage {
	partial class CraftingGUI {
		private class ShowAllIngredientsProvider(bool defaultValue) : IReadOnlyValueProvider<bool> {
			public bool StaticSource => showAllPossibleIngredients;
			public bool Value { get; private set; } = defaultValue;
			public void ClearStatic() => showAllPossibleIngredients = false;
			public void CopyFromStatic() => Value = showAllPossibleIngredients;
			public void CopyToStatic() => showAllPossibleIngredients = Value;
		}

		private class SelectionProvider : IReadOnlyValueProvider<Recipe> {
			public Recipe StaticSource => selectedRecipe;
			public Recipe Value { get; private set; }
			public SelectionProvider() => CopyFromStatic();
			public SelectionProvider(Recipe defaultValue) => Value = defaultValue;
			public void ClearStatic() { }
			public void CopyFromStatic() => Value = selectedRecipe;
			public void CopyToStatic() => selectedRecipe = Value;
		}

		internal class CraftAmountTargetProvider : IValueProvider<int> {
			public int StaticSource => craftAmountTarget;
			public int Value { get; set; }
			public CraftAmountTargetProvider() => CopyFromStatic();
			public CraftAmountTargetProvider(int defaultValue) => Value = defaultValue;
			public void ClearStatic() => craftAmountTarget = 1;
			public void CopyFromStatic() => Value = craftAmountTarget;
			public void CopyToStatic() => craftAmountTarget = Value;
		}

		internal class CreativeUnitPresentProvider : IValueProvider<bool> {
			public bool StaticSource => allItemsAreInfinite;
			public bool Value { get; set; }
			public void ClearStatic() => allItemsAreInfinite = false;
			public void CopyFromStatic() => Value = allItemsAreInfinite;
			public void CopyToStatic() => allItemsAreInfinite = Value;
		}
	}
}
