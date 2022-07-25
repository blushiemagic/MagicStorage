using MagicStorage.CrossMod;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace MagicStorage.UI.States {
	internal class CraftingUI : BaseStorageUI {
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

		private static void CheckRefresh() {
			if (needRefresh)
				CraftingGUI.RefreshItems();

			needRefresh = false;
		}

		private class CraftingPage : BaseStorageUIPage {
			public CraftingPage(BaseStorageUI parent) : base(parent, "Crafting") {
				OnPageSelected += CraftingUI.CheckRefresh;
			}
		}

		private abstract class BaseCraftingUIPage : BaseStorageUIPage {
			public int option;

			public BaseCraftingUIPage(BaseStorageUI parent, string name) : base(parent, name) { }
		}

		private abstract class BaseCraftingUIPage<TOption, TElement> : BaseCraftingUIPage where TElement : BaseOptionElement {
			private readonly List<TElement> buttons = new();

			public BaseCraftingUIPage(BaseStorageUI parent, string name) : base(parent, name) {
				OnPageSelected += InitOptionButtons;
			}

			public abstract IEnumerable<TOption> GetOptions();

			public abstract TElement CreateElement(TOption option);

			public abstract int GetOptionType(TElement element);

			public override void OnInitialize() => InitOptionButtons();

			public override void Recalculate() {
				base.Recalculate();

				InitOptionButtons();
			}

			private void InitOptionButtons() {
				foreach (TElement button in buttons)
					button.Remove();

				buttons.Clear();

				const int leftOrig = 20, topOrig = 20;

				CalculatedStyle dims = GetInnerDimensions();

				const int buttonSizeWithBuffer = 32 + 10;

				int columns = Math.Max(1, (int)(dims.Width - leftOrig * 2) / buttonSizeWithBuffer);

				int index = 0;

				foreach (TOption option in GetOptions()) {
					TElement element = CreateElement(option);
					element.OnClick += ClickOption;

					element.Left.Set(leftOrig + buttonSizeWithBuffer * (index % columns), 0f);
					element.Top.Set(topOrig + buttonSizeWithBuffer * (index / columns), 0f);

					Append(element);
					buttons.Add(element);
				}
			}

			private void ClickOption(UIMouseEvent evt, UIElement e) {
				int newOption = GetOptionType(e as TElement);

				if (newOption != option)
					CraftingUI.needRefresh = true;

				option = newOption;
			}
		}

		private class SortingPage : BaseCraftingUIPage<SortingOption, SortingOptionElement> {
			public SortingPage(BaseStorageUI parent) : base(parent, "Sorting") { }

			public override SortingOptionElement CreateElement(SortingOption option) => new(option);

			public override IEnumerable<SortingOption> GetOptions() => SortingOptionLoader.GetOptions(craftingGUI: true);

			public override int GetOptionType(SortingOptionElement element) => element.option.Type;
		}

		private class FilteringPage : BaseCraftingUIPage<FilteringOption, FilteringOptionElement> {
			public FilteringPage(BaseStorageUI parent) : base(parent, "Filtering") { }

			public override FilteringOptionElement CreateElement(FilteringOption option) => new(option);

			public override IEnumerable<FilteringOption> GetOptions() => FilteringOptionLoader.GetOptions(craftingGUI: true);

			public override int GetOptionType(FilteringOptionElement element) => element.option.Type;
		}
	}
}
