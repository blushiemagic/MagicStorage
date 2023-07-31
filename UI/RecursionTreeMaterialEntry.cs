using MagicStorage.Common.Systems.RecurrentRecipes;
using Terraria.GameContent.UI.Elements;

namespace MagicStorage.UI {
	public class RecursionTreeMaterialEntry : UIPanel {
		public MagicStorageItemSlot resultSlot;
		public NewUISlotZone ingredientZone;
		public RecursionTreeMaterialRecipeSlectionButton prev, next;

		// TODO: incomplete implementation

		private RecipeIngredientInfo ingredient;

		public override void OnInitialize() {
			resultSlot = new(0, scale: CraftingGUI.InventoryScale) {
				IgnoreClicks = true
			};

			Append(resultSlot);

			ingredientZone = new(CraftingGUI.InventoryScale * 0.55f);

			ingredientZone.InitializeSlot += (slot, scale) => {
				return new(slot, scale: scale) {
					IgnoreClicks = true
				};
			};

			Append(ingredientZone);
		}
	}
}
