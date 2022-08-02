using MagicStorage.Common.Systems;
using MagicStorage.CrossMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI.States {
	internal class CraftingUIState : BaseStorageUI {
		private UIPanel recipePanel;
		private static UIText recipePanelHeader;
		private static UIText ingredientText;
		private static UIText reqObjText;
		private static UIText reqObjText2;
		private static UIText storedItemsText;

		private static NewUISlotZone ingredientZone;    //Recipe ingredients
		private static NewUISlotZone storageZone;       //Items in storage valid for recipe ingredient
		private static NewUISlotZone recipeHeaderZone;  //Preview item for result
		private static NewUISlotZone resultZone;        //Result items already in storage (in one slot)

		private static UIScrollbar storageScrollBar;
		private static float storageScrollBarMaxViewSize = 2f;

		private static UITextPanel<LocalizedText> craftButton;
		private static UITextPanel<LocalizedText> craftP1, craftP10, craftP100, craftM1, craftM10, craftM100, craftMax, craftReset;
		private static UIText craftAmount;

		private int lastKnownIngredientRows = 1;
		private bool lastKnownUseOldCraftButtons = false;

		public override string DefaultPage => "Crafting";

		protected override IEnumerable<string> GetMenuOptions() {
			yield return "Crafting";
			yield return "Sorting";
			yield return "Filtering";
		}

		protected override BaseStorageUIPage InitPage(string page)
			=> page switch {
				"Crafting" => new RecipesPage(this),
				"Sorting" => new SortingPage(this),
				"Filtering" => new FilteringPage(this),
				_ => throw new ArgumentException("Unknown page: " + page, nameof(page))
			};

		protected override void PostInitializePages() {
			float itemSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.InventoryScale;
			float itemSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.InventoryScale;
			float smallSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.SmallScale;
			float smallSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.SmallScale;

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

			recipePanelHeader = new UIText(Language.GetText("Mods.MagicStorage.SelectedRecipe"));
			recipePanelHeader.Left.Set(60, 0f);
			recipePanel.Append(recipePanelHeader);

			ingredientText = new UIText(Language.GetText("Mods.MagicStorage.Ingredients"));
			ingredientText.Top.Set(30f, 0f);
			ingredientText.Left.Set(60, 0f);
			recipePanel.Append(ingredientText);

			recipeHeaderZone = new(CraftingGUI.SmallScale);
			recipeHeaderZone.SetDimensions(1, 1);
			recipePanel.Append(recipeHeaderZone);

			ingredientZone = new(CraftingGUI.SmallScale);
			ingredientZone.Width.Set(0f, 1f);

			ingredientZone.InitializeSlot += (slot, scale) => {
				MagicStorageItemSlot itemSlot = new(slot, scale: scale);
				
				itemSlot.OnRightClick += (evt, e) => {
					if (CraftingGUI.selectedRecipe is null)
						return;

					MagicStorageItemSlot obj = e as MagicStorageItemSlot;

					if (obj.slot >= CraftingGUI.selectedRecipe.requiredItem.Count)
						return;

					// select ingredient recipe by right clicking
					Item item = CraftingGUI.selectedRecipe.requiredItem[obj.slot];
					if (MagicCache.ResultToRecipe.TryGetValue(item.type, out var itemRecipes) && itemRecipes.Length > 0) {
						Recipe selected = itemRecipes[0];

						foreach (Recipe r in itemRecipes[1..]) {
							if (CraftingGUI.IsAvailable(r)) {
								selected = r;
								break;
							}
						}

						CraftingGUI.SetSelectedRecipe(selected);
					}
				};

				return itemSlot;
			};

			recipePanel.Append(ingredientZone);

			reqObjText = new UIText(Language.GetText("LegacyInterface.22"));
			reqObjText2 = new UIText("");
			recipePanel.Append(reqObjText);
			recipePanel.Append(reqObjText2);

			storageZone = new(CraftingGUI.SmallScale);

			storageZone.InitializeSlot += (slot, scale) => {
				MagicStorageItemSlot itemSlot = new(slot, scale: scale);

				itemSlot.OnClick += (evt, e) => {
					MagicStorageItemSlot obj = e as MagicStorageItemSlot;

					ItemData data = new(obj.StoredItem);
					if (CraftingGUI.blockStorageItems.Contains(data))
						CraftingGUI.blockStorageItems.Remove(data);
					else
						CraftingGUI.blockStorageItems.Add(data);
				};

				return itemSlot;
			};

			storedItemsText = new UIText(Language.GetText("Mods.MagicStorage.StoredItems"));
			recipePanel.Append(storedItemsText);

			storageZone.Width.Set(0f, 1f);
			
			recipePanel.Append(storageZone);

			storageScrollBar = new();
			storageScrollBar.Left.Set(-20f, 1f);
			storageScrollBar.SetView(CraftingGUI.ScrollBar2ViewSize, storageScrollBarMaxViewSize);
			storageZone.Append(storageScrollBar);

			craftButton.Top.Set(-48f, 1f);
			craftButton.Width.Set(100f, 0f);
			craftButton.Height.Set(24f, 0f);
			craftButton.PaddingTop = 8f;
			craftButton.PaddingBottom = 8f;
			recipePanel.Append(craftButton);

			resultZone = new(CraftingGUI.InventoryScale);

			resultZone.InitializeSlot += (slot, scale) => {
				MagicStorageItemSlot itemSlot = new(slot, scale: scale);

				itemSlot.OnClick += (evt, e) => {
					MagicStorageItemSlot obj = e as MagicStorageItemSlot;

					Item result = obj.StoredItem;

					if (Main.mouseItem.IsAir && result is not null && !result.IsAir)
						result.newAndShiny = false;

					Player player = Main.LocalPlayer;

					bool changed = false;
					if (!Main.mouseItem.IsAir && player.itemAnimation == 0 && player.itemTime == 0 && result is not null && Main.mouseItem.type == result.type) {
						if (CraftingGUI.TryDepositResult(Main.mouseItem))
							changed = true;
					} else if (Main.mouseItem.IsAir && result is not null && !result.IsAir) {
						if (Main.keyState.IsKeyDown(Keys.LeftAlt))
							result.favorited = !result.favorited;
						else {
							Item toWithdraw = result.Clone();
							
							if (toWithdraw.stack > toWithdraw.maxStack)
								toWithdraw.stack = toWithdraw.maxStack;

							Main.mouseItem = CraftingGUI.DoWithdrawResult(toWithdraw, ItemSlot.ShiftInUse);
							
							if (ItemSlot.ShiftInUse)
								Main.mouseItem = player.GetItem(Main.myPlayer, Main.mouseItem, GetItemSettings.InventoryEntityToPlayerInventorySettings);
							
							changed = true;
						}
					}

					if (changed) {
						StorageGUI.needRefresh = true;
						SoundEngine.PlaySound(SoundID.Grab);
					}
				};

				itemSlot.OnRightClick += (evt, e) => {
					MagicStorageItemSlot obj = e as MagicStorageItemSlot;

					Item result = obj.StoredItem;

					if (result is not null && !result.IsAir && (Main.mouseItem.IsAir || ItemData.Matches(Main.mouseItem, result) && Main.mouseItem.stack < Main.mouseItem.maxStack))
						CraftingGUI.slotFocus = true;

					if (CraftingGUI.slotFocus)
						CraftingGUI.SlotFocusLogic();
				};

				return itemSlot;
			};

			resultZone.SetDimensions(1, 1);

			bool config = MagicStorageConfig.UseOldCraftMenu;

			if (!config)
				resultZone.Left.Set(-itemSlotWidth - 15, 1f);
			else
				resultZone.Left.Set(-itemSlotWidth, 1f);

			resultZone.Width.Set(itemSlotWidth, 0f);
			resultZone.Height.Set(itemSlotHeight, 0f);
			recipePanel.Append(resultZone);

			craftP1 = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.Plus1"));
			craftP10 = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.Plus10"));
			craftP100 = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.Plus100"));
			craftM1 = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.Minus1"));
			craftM10 = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.Minus10"));
			craftM100 = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.Minus100"));
			craftMax = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.MaxStack"), CraftingGUI.SmallScale);
			craftReset = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.Reset"), CraftingGUI.SmallScale);
			craftAmount = new UIText(Language.GetText("Mods.MagicStorage.Crafting.Amount"), CraftingGUI.SmallScale);

			craftAmount.Top.Set(craftButton.Top.Pixels - 20, 1f);
			craftAmount.Left.Set(12, 0f);
			craftAmount.Width.Set(250f, 0f);
			craftAmount.Height.Set(24f * CraftingGUI.SmallScale, 0f);
			craftAmount.PaddingTop = 0;
			craftAmount.PaddingBottom = 0;
			craftAmount.TextOriginX = 0f;

			craftP1.Top.Set(resultZone.Top.Pixels - 30, 0f);
			craftP1.Width.Set(60, 0f);
			craftP1.Height.Set(24f * CraftingGUI.SmallScale, 0f);
			craftP1.PaddingTop = 8f;
			craftP1.PaddingBottom = 8f;
			recipePanel.Append(craftP1);

			craftP10.Top = craftP1.Top;
			craftP10.Left.Set(craftP1.Left.Pixels + craftP1.Width.Pixels + 10, 0f);
			craftP10.Width.Set(60, 0f);
			craftP10.Height.Set(24f * CraftingGUI.SmallScale, 0f);
			craftP10.PaddingTop = 8f;
			craftP10.PaddingBottom = 8f;
			recipePanel.Append(craftP10);

			craftP100.Top = craftP1.Top;
			craftP100.Left.Set(craftP10.Left.Pixels + craftP10.Width.Pixels + 10, 0f);
			craftP100.Width.Set(60, 0f);
			craftP100.Height.Set(24f * CraftingGUI.SmallScale, 0f);
			craftP100.PaddingTop = 8f;
			craftP100.PaddingBottom = 8f;
			recipePanel.Append(craftP100);

			craftM1.Top.Set(craftP1.Top.Pixels + craftP1.Height.Pixels + 15, 0f);
			craftM1.Width.Set(60, 0f);
			craftM1.Height.Set(24f * CraftingGUI.SmallScale, 0f);
			craftM1.PaddingTop = 8f;
			craftM1.PaddingBottom = 8f;
			recipePanel.Append(craftM1);

			craftM10.Top = craftM1.Top;
			craftM10.Left.Set(craftM1.Left.Pixels + craftM1.Width.Pixels + 10, 0f);
			craftM10.Width.Set(60, 0f);
			craftM10.Height.Set(24f * CraftingGUI.SmallScale, 0f);
			craftM10.PaddingTop = 8f;
			craftM10.PaddingBottom = 8f;
			recipePanel.Append(craftM10);

			craftM100.Top = craftM1.Top;
			craftM100.Left.Set(craftM10.Left.Pixels + craftM10.Width.Pixels + 10, 0f);
			craftM100.Width.Set(60, 0f);
			craftM100.Height.Set(24f * CraftingGUI.SmallScale, 0f);
			craftM100.PaddingTop = 8f;
			craftM100.PaddingBottom = 8f;
			recipePanel.Append(craftM100);

			craftMax.Top.Set(craftM1.Top.Pixels + craftM1.Height.Pixels + 15, 0f);
			craftMax.Width.Set(160f * CraftingGUI.SmallScale, 0f);
			craftMax.Height.Set(24f * CraftingGUI.SmallScale, 0f);
			craftMax.PaddingTop = 8f;
			craftMax.PaddingBottom = 8f;
			recipePanel.Append(craftMax);

			craftReset.Top = craftMax.Top;
			craftReset.Left.Set(craftMax.Left.Pixels + craftMax.Width.Pixels + 10, 0f);
			craftReset.Width.Set(100f * CraftingGUI.SmallScale, 0f);
			craftReset.Height.Set(24f * CraftingGUI.SmallScale, 0f);
			craftReset.PaddingTop = 8f;
			craftReset.PaddingBottom = 8f;
			recipePanel.Append(craftReset);

			RecalculateRecipePanelElements(0, 1);
			ToggleCraftButtons(hide: config);

			lastKnownUseOldCraftButtons = config;
		}

		private void MoveRecipePanel() {
			float recipeTop = panel.Top.Pixels;
			float recipeLeft = panel.Left.Pixels + panel.Width.Pixels;
			recipePanel.Left.Set(recipeLeft, 0f);
			recipePanel.Top.Set(recipeTop, 0f);

			recipePanel.Recalculate();
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);

			bool config = MagicStorageConfig.UseOldCraftMenu;

			if (config)
				CraftingGUI.craftAmountTarget = 1;
			else
				craftAmount.SetText(Language.GetTextValue("Mods.MagicStorage.Crafting.Amount", CraftingGUI.craftAmountTarget));

			if (lastKnownUseOldCraftButtons != config) {
				ToggleCraftButtons(hide: config);
				lastKnownIngredientRows = -1;  //Force a recipe panel heights refresh
			}

			int itemsNeeded = CraftingGUI.selectedRecipe?.requiredItem.Count ?? CraftingGUI.IngredientColumns;
			int recipeRows = itemsNeeded / CraftingGUI.IngredientColumns;
			int extraRow = itemsNeeded % CraftingGUI.IngredientColumns != 0 ? 1 : 0;
			int totalRows = recipeRows + extraRow;
			if (totalRows < 1)
				totalRows = 1;

			if (recipeRows != lastKnownIngredientRows)
				RecalculateRecipePanelElements(itemsNeeded, totalRows);
		}

		private void RecalculateRecipePanelElements(int itemsNeeded, int ingredientRows) {
			float itemSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.InventoryScale;
			float itemSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.InventoryScale;
			float smallSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.SmallScale;
			float smallSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.SmallScale;

			int extraRow = itemsNeeded % CraftingGUI.IngredientColumns != 0 ? 1 : 0;
			int totalRows = ingredientRows + extraRow;
			if (totalRows < 1)
				totalRows = 1;
			const float ingredientZoneTop = 54f;
			float ingredientZoneHeight = 30f * totalRows;

			ingredientZone.SetDimensions(CraftingGUI.IngredientColumns, totalRows);
			ingredientZone.Height.Set(ingredientZoneHeight, 0f);
			
			ingredientZone.Recalculate();

			float reqObjTextTop = ingredientZoneTop + ingredientZoneHeight + 11 * totalRows;
			float reqObjText2Top = reqObjTextTop + 24;

			reqObjText.Top.Set(reqObjTextTop, 0f);
			reqObjText2.Top.Set(reqObjText2Top, 0f);

			reqObjText.Recalculate();
			reqObjText2.Recalculate();

			int reqObjText2Rows = reqObjText2.Text.Count(c => c == '\n') + 1;
			float storedItemsTextTop = reqObjText2Top + 30 * reqObjText2Rows;
			float storageZoneTop = storedItemsTextTop + 24;
			storedItemsText.Top.Set(storedItemsTextTop, 0f);

			storedItemsText.Recalculate();

			storageZone.Top.Set(storageZoneTop, 0f);

			storageZone.Recalculate();

			bool config = MagicStorageConfig.UseOldCraftMenu;

			if (!config)
				storageZone.Height.Set(-storageZoneTop - 200, 1f);
			else
				storageZone.Height.Set(-storageZoneTop - 36, 1f);

			int numRows2 = (CraftingGUI.storageItems.Count + CraftingGUI.IngredientColumns - 1) / CraftingGUI.IngredientColumns;
			int displayRows2 = (int)storageZone.GetDimensions().Height / ((int)smallSlotHeight + CraftingGUI.Padding);
			storageZone.SetDimensions(CraftingGUI.IngredientColumns, displayRows2);
			int noDisplayRows2 = numRows2 - displayRows2;
			if (noDisplayRows2 < 0)
				noDisplayRows2 = 0;

			storageScrollBarMaxViewSize = 1 + noDisplayRows2;
			storageScrollBar.Height.Set(displayRows2 * (smallSlotHeight + CraftingGUI.Padding), 0f);
			storageScrollBar.SetView(CraftingGUI.ScrollBar2ViewSize, storageScrollBarMaxViewSize);

			storageScrollBar.Recalculate();

			if (!config)
				resultZone.Top.Set(storageZoneTop + storageZone.GetDimensions().Height + 40, 0f);
			else
				resultZone.Top.Set(-itemSlotHeight, 1f);

			resultZone.Width.Set(itemSlotWidth, 0f);
			resultZone.Height.Set(itemSlotHeight, 0f);

			resultZone.Recalculate();

			lastKnownIngredientRows = ingredientRows;

			Refresh();
		}

		private void ToggleCraftButtons(bool hide) {
			if (hide) {
				craftAmount.Remove();
				craftP1.Remove();
				craftP10.Remove();
				craftP100.Remove();
				craftM1.Remove();
				craftM10.Remove();
				craftM100.Remove();
				craftMax.Remove();
				craftReset.Remove();
			} else {
				if (craftAmount.Parent is null)
					recipePanel.Append(craftAmount);
				if (craftP1.Parent is null)
					recipePanel.Append(craftP1);
				if (craftP10.Parent is null)
					recipePanel.Append(craftP10);
				if (craftP100.Parent is null)
					recipePanel.Append(craftP100);
				if (craftM1.Parent is null)
					recipePanel.Append(craftM1);
				if (craftM10.Parent is null)
					recipePanel.Append(craftM10);
				if (craftM100.Parent is null)
					recipePanel.Append(craftM100);
				if (craftMax.Parent is null)
					recipePanel.Append(craftMax);
				if (craftReset.Parent is null)
					recipePanel.Append(craftReset);
			}
		}

		protected override void PostAppendPanel() {
			Append(recipePanel);
		}

		protected override void OnOpen() {
			StorageGUI.OnRefresh += Refresh;
		}

		protected override void OnClose() {
			StorageGUI.OnRefresh -= Refresh;
		}

		private void Refresh() {
			ingredientZone.SetItemsAndContexts(CraftingGUI.selectedRecipe?.requiredItem.Count ?? 0, CraftingGUI.GetIngredient);

			storageZone.SetItemsAndContexts(CraftingGUI.storageItems?.Count ?? 0, CraftingGUI.GetStorage);

			recipeHeaderZone.SetItemsAndContexts(1, CraftingGUI.GetHeader);

			resultZone.SetItemsAndContexts(1, CraftingGUI.GetResult);
		}

		private class RecipesPage : BaseStorageUIPage {
			public RecipesPage(BaseStorageUI parent) : base(parent, "Crafting") {
				OnPageSelected += StorageGUI.CheckRefresh;
			}

			public override void OnInitialize() {
				CraftingUIState parent = parentUI as CraftingUIState;

				var basePanel = parent.panel;

				UIElement topBar = new();
				topBar.Width.Set(0f, 1f);
				topBar.Height.Set(32f, 0f);
				basePanel.Append(topBar);
			}
		}
	}
}
