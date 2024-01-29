using MagicStorage.Common;
using System;
using Terraria;
using Terraria.UI;

namespace MagicStorage.UI.History {
	public class RecipeHistoryEntry : HistoryEntry<Recipe> {
		public NewUISlotZone ingredientZone;

		public RecipeHistoryEntry(int index, IHistoryCollection<Recipe> history) : base(index, history) { }

		public override void OnInitialize() {
			base.OnInitialize();

			ingredientZone = new(CraftingGUI.InventoryScale * 0.55f);
			ingredientZone.Left.Set(resultSlot.Width.Pixels + 4, 0f);

			ingredientZone.InitializeSlot = (slot, scale) => {
				return new(slot, scale: scale) {
					IgnoreClicks = true
				};
			};

			Append(ingredientZone);
		}

		protected override Item GetItemForResult() => Value.createItem;

		protected override void OnValueSet(Recipe value) {
			ingredientZone.SetDimensions(7, Math.Max((value.requiredItem.Count - 1) / 7 + 1, 1));

			ingredientZone.Width.Set(ingredientZone.ZoneWidth, 0f);
			ingredientZone.Height.Set(ingredientZone.ZoneHeight, 0f);

			Width.Set(resultSlot.Width.Pixels + 4 + ingredientZone.ZoneWidth + 4, 0f);
			Height.Set(Math.Max(resultSlot.Height.Pixels, ingredientZone.ZoneHeight) + 4, 0f);

			ingredientZone.SetItemsAndContexts(value.requiredItem.Count, GetIngredient);
		}

		private Item GetIngredient(int slot, ref int context) {
			Recipe recipe = Value;
			return slot < recipe.requiredItem.Count ? recipe.requiredItem[slot] : new Item();
		}

		protected override void GetResultContext(Recipe value, ref int context) {
			using (FlagSwitch.ToggleTrue(ref CraftingGUI.disableNetPrintingForIsAvailable)) {
				if (value == CraftingGUI.selectedRecipe)
					context = ItemSlot.Context.TrashItem;
				else if (!CraftingGUI.IsAvailable(value))
					context = ItemSlot.Context.ChestItem;
			}
		}
	}
}
