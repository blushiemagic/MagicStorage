using MagicStorage.CrossMod;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace MagicStorage.UI.States {
	internal class CraftingUIState : BaseStorageUI {
		private UIPanel recipePanel;

		public override string DefaultPage => "Crafting";

		protected override IEnumerable<string> GetMenuOptions() {
			yield return "Crafting";
			yield return "Sorting";
			yield return "Filtering";
		}

		protected override BaseStorageUIPage InitPage(string page)
			=> page switch {
				"Crafting" => new CraftingPage(this),
				"Sorting" => new SortingPage(this),
				"Filtering" => new FilteringPage(this),
				_ => throw new ArgumentException("Unknown page: " + page, nameof(page))
			};

		protected override void PostInitializePages() {
			float itemSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.InventoryScale;
			float smallSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.SmallScale;

			float panelTop = Main.instance.invBottom + 60;
			float panelLeft = 20f;
			float innerPanelWidth = CraftingGUI.RecipeColumns * (itemSlotWidth + CraftingGUI.Padding) + 20f + CraftingGUI.Padding;
			float panelWidth = panel.PaddingLeft + innerPanelWidth + panel.PaddingRight;
			float panelHeight = Main.screenHeight - panelTop;

			panel.Left.Set(panelLeft, 0f);
			panel.Top.Set(panelTop, 0f);
			panel.Width.Set(panelWidth, 0f);
			panel.Height.Set(panelHeight, 0f);

			panel.OnRecalculate += MoveRecipePanel;

			recipePanel = new();
			float recipeTop = panelTop;
			float recipeLeft = panelLeft + panelWidth;
			float recipeWidth = CraftingGUI.IngredientColumns * (smallSlotWidth + CraftingGUI.Padding) + 20f + CraftingGUI.Padding;
			recipeWidth += recipePanel.PaddingLeft + recipePanel.PaddingRight;
			float recipeHeight = panelHeight;
			recipePanel.Left.Set(recipeLeft, 0f);
			recipePanel.Top.Set(recipeTop, 0f);
			recipePanel.Width.Set(recipeWidth, 0f);
			recipePanel.Height.Set(recipeHeight, 0f);
		}

		private void MoveRecipePanel() {
			float recipeTop = panel.Top.Pixels;
			float recipeLeft = panel.Left.Pixels + panel.Width.Pixels;
			recipePanel.Left.Set(recipeLeft, 0f);
			recipePanel.Top.Set(recipeTop, 0f);

			recipePanel.Recalculate();
		}

		private class CraftingPage : BaseStorageUIPage {
			public CraftingPage(BaseStorageUI parent) : base(parent, "Crafting") {
				OnPageSelected += StorageGUI.CheckRefresh;
			}
		}
	}
}
