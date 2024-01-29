using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Components;
using MagicStorage.UI.History;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using SerousCommonLib.UI;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI.States {
	public partial class CraftingUIState : BaseStorageUI {
		public const float SmallerScale = 0.64f;

		protected UIPanel recipePanel;
		protected UIText recipePanelHeader;
		protected UIText ingredientText;
		protected UIText reqObjText;
	//	private UIText reqObjText2;
		protected List<UIText> reqObjTextLines;
		protected UIText storedItemsText;

		protected UIPanel recipeWaitPanel;
		protected UIText recipeWaitText;

		public IHistoryCollection History => history;

		public IHistoryCollection history;
		protected RecipePanelHistoryArrangement recipeHistory;
		protected RecipeHistoryPanel recipeHistoryPanel;

		protected NewUISlotZone ingredientZone;    //Recipe ingredients
		protected NewUISlotZone storageZone;       //Items in storage valid for recipe ingredient
		protected NewUISlotZone recipeHeaderZone;  //Preview item for result
		protected NewUISlotZone resultZone;        //Result items already in storage (in one slot)

		protected NewUIScrollbar ingredientScrollBar;
		protected NewUIScrollbar storageScrollBar;
		protected float ingredientScrollBarMaxViewSize = 2f;
		protected float storageScrollBarMaxViewSize = 2f;

		protected UIToggleLabel recursionButton;

		protected UICraftButton craftButton;
		protected UICraftAmountAdjustment craftP1, craftP10, craftP100, craftM1, craftM10, craftM100, craftMax, craftReset;
		protected UIText craftAmount;

		protected int lastKnownIngredientRows = 1;
		protected bool lastKnownUseOldCraftButtons = false;
		protected float lastKnownIngredientScrollBarViewPosition = -1;
		protected float lastKnownScrollBarViewPosition = -1;

		protected float recipeLeft, recipeTop, recipeWidth, recipeHeight;

		public override string DefaultPage => "Crafting";

		protected override IEnumerable<string> GetMenuOptions() {
			yield return "Crafting";
		//	yield return "Tree";
			yield return "Sorting";
			yield return "Filtering";
		}

		protected override BaseStorageUIPage InitPage(string page)
			=> page switch {
				"Crafting" => new RecipesPage(this),
			//	"Tree" => new RecursiveTreeViewPage(this),
				"Sorting" => new SortingPage(this),
				"Filtering" => new FilteringPage(this),
				_ => throw new ArgumentException("Unknown page: " + page, nameof(page))
			};

		protected override void PostInitializePages() {
			float itemSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.InventoryScale;
			float itemSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.InventoryScale;
			float smallSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.SmallScale;

			panel.OnRecalculate += MoveRecipePanel;

			recipePanel = new();

			recipeTop = PanelTop;
			recipeLeft = PanelLeft + PanelWidth;
			recipeWidth = CraftingGUI.IngredientColumns * (smallSlotWidth + CraftingGUI.Padding) + 20f + CraftingGUI.Padding;
			recipeWidth += recipePanel.PaddingLeft + recipePanel.PaddingRight;
			recipeHeight = PanelHeight;

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
			recipeHeaderZone.InitializeSlot = static (slot, scale) => {
				MagicStorageItemSlot itemSlot = new(slot, scale: scale) {
					IgnoreClicks = true  // Purely visual
				};

				return itemSlot;
			};

			recipeHeaderZone.SetDimensions(1, 1);

			recipeHeaderZone.Width.Set(recipeHeaderZone.ZoneWidth, 0);
			recipeHeaderZone.Height.Set(recipeHeaderZone.ZoneHeight, 0);
			recipePanel.Append(recipeHeaderZone);

			ingredientZone = new(CraftingGUI.SmallScale);

			ingredientZone.InitializeSlot = (slot, scale) => {
				MagicStorageItemSlot itemSlot = new(slot, scale: scale) {
					IgnoreClicks = true  // Purely visual
				};
				
				itemSlot.OnRightClick += (evt, e) => RightClickIngredient((MagicStorageItemSlot)e);  // Turned into a method for readability

				return itemSlot;
			};

			recipePanel.Append(ingredientZone);

			InitializeScrollBar(ingredientZone, ref ingredientScrollBar, ingredientScrollBarMaxViewSize);

			reqObjText = new UIText(Language.GetText("LegacyInterface.22"));

			reqObjText.OnUpdate += static e => {
				UIText text = e as UIText;
				if (CraftingGUI.lastKnownRecursionErrorForObjects is { Length: >0 }) {
					// Error color
					text.TextColor = Color.Red;
				} else {
					// Default color
					text.TextColor = Color.White;
				}
			};

			reqObjText.OnMouseOver += static (evt, e) => {
				if (CraftingGUI.lastKnownRecursionErrorForObjects is { Length: >0 } error)
					MagicUI.mouseText = error;
			};

			reqObjText.OnMouseOut += static (evt, e) => {
				MagicUI.mouseText = "";
			};

			reqObjTextLines = new();

		//	reqObjText2 = new UIText("");
			recipePanel.Append(reqObjText);
		//	recipePanel.Append(reqObjText2);

			history = CreateHistory();

			recipeHistory = new(history, 1f);

			recipeHistory.OnButtonClicked += () => {
				OpenRecipeHistoryPanel();
				SoundEngine.PlaySound(SoundID.MenuTick);
			};

			recipePanel.Append(recipeHistory);

			recipeHistoryPanel = new(true, history, new[] { ("History", Language.GetText("Mods.MagicStorage.UIPages.History")) });

			recipeHistoryPanel.OnMenuClose += CloseRecipeHistoryPanel;

			recipeHistoryPanel.SetActivePage("History");

			storageZone = new(CraftingGUI.SmallScale);

			storageZone.InitializeSlot = (slot, scale) => {
				MagicStorageItemSlot itemSlot = new(slot, scale: scale) {
					IgnoreClicks = true  // Purely visual
				};

				itemSlot.OnLeftClick += (evt, e) => HandleStorageSlotLeftClick(storageZone, storageScrollBar, (MagicStorageItemSlot)e, int.MaxValue, GetStorage);

				return itemSlot;
			};

			storageZone.OnScrollWheel += (evt, e) => {
				if (storageScrollBar is not null)
					storageScrollBar.ViewPosition -= evt.ScrollWheelValue / storageScrollBar.ScrollDividend;
			};

			storedItemsText = new UIText(Language.GetText("Mods.MagicStorage.StoredItems"));

			storedItemsText.OnUpdate += static e => {
				UIText text = e as UIText;
				if (CraftingGUI.lastKnownRecursionErrorForStoredItems is { Length: >0 }) {
					// Error color
					text.TextColor = Color.Red;
				} else {
					// Default color
					text.TextColor = Color.White;
				}
			};

			storedItemsText.OnMouseOver += static (evt, e) => {
				if (CraftingGUI.lastKnownRecursionErrorForStoredItems is { Length: >0 } error)
					MagicUI.mouseText = error;
			};

			storedItemsText.OnMouseOut += static (evt, e) => {
				MagicUI.mouseText = "";
			};

			recipePanel.Append(storedItemsText);

			recursionButton = new(Language.GetText("Mods.MagicStorage.CraftingGUI.ShowAllIngredients"));
			recursionButton.mouseOver = Color.White;
			recursionButton.Left.Set(18, 0f);
			recursionButton.Width.Set(recursionButton.Text.MinWidth.Pixels + 30, 0f);
			recursionButton.OnLeftClick += static (evt, e) => {
				UIToggleLabel label = e as UIToggleLabel;
				CraftingGUI.showAllPossibleIngredients = label.IsOn;

				MagicUI.SetRefresh(forceFullRefresh: true);
			};

			storageZone.Width.Set(0f, 1f);
			
			recipePanel.Append(storageZone);

			InitializeScrollBar(storageZone, ref storageScrollBar, storageScrollBarMaxViewSize);

			craftButton = CreateCraftButton();
			craftButton.Top.Set(-48f, 1f);
			craftButton.Width.Set(100f, 0f);
			craftButton.Height.Set(24f, 0f);
			craftButton.PaddingTop = 8f;
			craftButton.PaddingBottom = 8f;
			recipePanel.Append(craftButton);

			resultZone = new(CraftingGUI.InventoryScale);

			resultZone.InitializeSlot = (slot, scale) => {
				MagicStorageItemSlot itemSlot = new(slot, scale: scale) {
					CanShareItemToChat = true
				};

				itemSlot.OnLeftClick += (evt, e) => HandleResultSlotLeftClick(resultZone, (MagicStorageItemSlot)e, int.MaxValue, GetResult);

				itemSlot.OnRightMouseDown += (evt, e) => HandleResultSlotRightHold((MagicStorageItemSlot)e);

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

			craftP1 = new UICraftAmountAdjustment(Language.GetText("Mods.MagicStorage.Crafting.Plus1"), SmallerScale);
			craftP1.SetAmountInformation(+1, true);
			craftP10 = new UICraftAmountAdjustment(Language.GetText("Mods.MagicStorage.Crafting.Plus10"), SmallerScale);
			craftP10.SetAmountInformation(+10, true);
			craftP100 = new UICraftAmountAdjustment(Language.GetText("Mods.MagicStorage.Crafting.Plus100"), SmallerScale);
			craftP100.SetAmountInformation(+100, true);
			craftM1 = new UICraftAmountAdjustment(Language.GetText("Mods.MagicStorage.Crafting.Minus1"), SmallerScale);
			craftM1.SetAmountInformation(-1, true);
			craftM10 = new UICraftAmountAdjustment(Language.GetText("Mods.MagicStorage.Crafting.Minus10"), SmallerScale);
			craftM10.SetAmountInformation(-10, true);
			craftM100 = new UICraftAmountAdjustment(Language.GetText("Mods.MagicStorage.Crafting.Minus100"), SmallerScale);
			craftM100.SetAmountInformation(-100, true);
			craftMax = new UICraftAmountAdjustment(Language.GetText("Mods.MagicStorage.Crafting.MaxStack"), SmallerScale);
			craftMax.SetAmountInformation(9999, false);
			craftReset = new UICraftAmountAdjustment(Language.GetText("Mods.MagicStorage.Crafting.Reset"), SmallerScale);
			craftReset.SetAmountInformation(1, false);

			craftAmount = new UIText(Language.GetText("Mods.MagicStorage.Crafting.Amount"), CraftingGUI.SmallScale);

			InitCraftButtonDimensions();

			ToggleCraftButtons(hide: config);

			recipeWaitPanel = new UIPanel();

			recipeWaitPanel.Left.Set(0f, 0.05f);
			recipeWaitPanel.Top.Set(70f, 0f);
			recipeWaitPanel.Width.Set(0f, 0.9f);
			recipeWaitPanel.Height.Set(50f, 0f);

			recipeWaitText = new UIText(Language.GetText("Mods.MagicStorage.SortWaiting"), textScale: 1.2f) {
				HAlign = 0.5f,
				VAlign = 0.5f
			};

			recipeWaitPanel.Append(recipeWaitText);

			lastKnownUseOldCraftButtons = config;
		}

		private void MoveRecipePanel() {
			recipeTop = PanelTop;
			recipeLeft = PanelRight;
			recipePanel.Left.Set(recipeLeft, 0f);
			recipePanel.Top.Set(recipeTop, 0f);

			recipeHeight = panel.Height.Pixels;
			recipePanel.Height.Set(recipeHeight, 0);
			
			recipeHistory.Left.Set(-recipeHistory.Width.Pixels, 1f);

			recipeHistoryPanel.Width.Set(280, 0);
			recipeHistoryPanel.Left.Set(10, 0f);
			recipeHistoryPanel.Height.Set(Math.Min(recipeHeight * 3f / 7f, 300), 0f);

			recipePanel.Recalculate();
		}

		protected const int AMOUNT_BUTTON_HEIGHT = 24;

		private void InitCraftButtonDimensions() {
			craftAmount.Top.Set(craftButton.Top.Pixels - 16, 1f);
			craftAmount.Left.Set(12, 0f);
			craftAmount.Width.Set(75f, 0f);
			craftAmount.Height.Set(24f * CraftingGUI.SmallScale, 0f);
			craftAmount.PaddingTop = 0;
			craftAmount.PaddingBottom = 0;
			craftAmount.TextOriginX = 0f;

			craftP1.Left.Set(craftButton.Width.Pixels + 4, 0f);
			craftP1.Width.Set(24, 0f);
			craftP1.Height.Set(AMOUNT_BUTTON_HEIGHT, 0f);
			craftP1.PaddingTop = 4;
			craftP1.PaddingBottom = 3;

			craftP10.Left.Set(craftP1.Left.Pixels + craftP1.Width.Pixels + 16, 0f);
			craftP10.Width.Set(32, 0f);
			craftP10.Height = craftP1.Height;
			craftP10.PaddingTop = craftP1.PaddingTop;
			craftP10.PaddingBottom = craftP1.PaddingBottom;

			craftP100.Left.Set(craftP10.Left.Pixels + craftP10.Width.Pixels + 13, 0f);
			craftP100.Width.Set(48, 0f);
			craftP100.Height = craftP1.Height;
			craftP100.PaddingTop = craftP1.PaddingTop;
			craftP100.PaddingBottom = craftP1.PaddingBottom;

			craftM1.Left = craftP1.Left;
			craftM1.Width = craftP1.Width;
			craftM1.Height = craftP1.Height;
			craftM1.PaddingTop = craftP1.PaddingTop;
			craftM1.PaddingBottom = craftP1.PaddingBottom;

			craftM10.Left = craftP10.Left;
			craftM10.Width = craftP10.Width;
			craftM10.Height = craftP1.Height;
			craftM10.PaddingTop = craftP1.PaddingTop;
			craftM10.PaddingBottom = craftP1.PaddingBottom;

			craftM100.Left = craftP100.Left;
			craftM100.Width = craftP100.Width;
			craftM100.Height = craftP1.Height;
			craftM100.PaddingTop = craftP1.PaddingTop;
			craftM100.PaddingBottom = craftP1.PaddingBottom;

			craftMax.Left.Set(craftP1.Left.Pixels, 0f);
			craftMax.Width.Set(93 * CraftingGUI.SmallScale, 0f);
			craftMax.Height = craftP1.Height;
			craftMax.PaddingTop = craftP1.PaddingTop;
			craftMax.PaddingBottom = craftP1.PaddingBottom;

			craftReset.Left.Set(craftMax.Left.Pixels + craftMax.Width.Pixels + 18, 0f);
			craftReset.Width.Set(58 * CraftingGUI.SmallScale, 0f);
			craftReset.Height = craftP1.Height;
			craftReset.PaddingTop = craftP1.PaddingTop;
			craftReset.PaddingBottom = craftP1.PaddingBottom;
		}

		private bool? pendingPanelChange;
		private bool isWaitPanelWaitingToOpen;

		private void SetThreadWait(bool waiting) {
			if (waiting) {
				if (!AssetRepository.IsMainThread)
					pendingPanelChange = waiting;
				else if (recipeWaitPanel.Parent is null)
					isWaitPanelWaitingToOpen = true;
			} else {
				recipeWaitPanel.Remove();
				isWaitPanelWaitingToOpen = false;
			}
		}

		public override void Update(GameTime gameTime) {
			CraftingGUI.PlayerZoneCache.Cache();

			try {
				using (FlagSwitch.Create(ref MagicUI.blockItemSlotActionsDetour, !recipeHistoryPanel.IsMouseHovering)) {
					base.Update(gameTime);

					if (pendingPanelChange is bool { } waiting) {
						SetThreadWait(waiting);
						pendingPanelChange = null;
					}

					// Wait for at least 10 game ticks to display the prompt
					if (isWaitPanelWaitingToOpen && MagicUI.CurrentThreadingDuration >= StorageGUI.WAIT_PANEL_MINIMUM_TICKS) {
						isWaitPanelWaitingToOpen = false;

						if (recipeWaitPanel.Parent is null)
							recipePanel.Append(recipeWaitPanel);  // Delay appending to here
					}
				}

				SlotFocus(out var flag, out var updateFocus, out var resetFocus);

				if (!Main.mouseRight)
					resetFocus();

				if (flag.Value)
					updateFocus();

				CraftingGUI.ClampCraftAmount();

				bool config = MagicStorageConfig.UseOldCraftMenu;

				if (config)
					CraftingGUI.craftAmountTarget = 1;
				else
					craftAmount.SetText(Language.GetTextValue("Mods.MagicStorage.Crafting.Amount", CraftingGUI.craftAmountTarget));

				if (lastKnownUseOldCraftButtons != config) {
					ToggleCraftButtons(hide: config);
					lastKnownUseOldCraftButtons = config;
					lastKnownIngredientRows = -1;  //Force a recipe panel heights refresh
				}

				int itemsNeeded = GetIngredientCount();
				int totalRows = GetIngredientRows(itemsNeeded);

				if ((totalRows != lastKnownIngredientRows || HaveZonesChangedDueToScrolling()) && RecalculateRecipePanelElements(totalRows))
					RefreshZonesFromScrolling();
			} catch (Exception e) {
				Main.NewTextMultiline(e.ToString(), c: Color.White);
			}

			CraftingGUI.PlayerZoneCache.FreeCache(true);
		}

		public override void Draw(SpriteBatch spriteBatch) {
			using (FlagSwitch.Create(ref MagicUI.blockItemSlotActionsDetour, !recipeHistoryPanel.IsMouseHovering))
				base.Draw(spriteBatch);
		}
		
		protected const float ingredientZoneTop = 54f;

		private bool RecalculateRecipePanelElements(int ingredientRows) {
			/*
			float itemSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.InventoryScale;
			float itemSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.InventoryScale;
			float smallSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.SmallScale;
			float smallSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.SmallScale;
			*/

			float reqObjTextTop = ingredientZoneTop;

			RecalculateIngredientZone(ingredientRows, ref reqObjTextTop);

			reqObjText.Top.Set(reqObjTextTop, 0f);
		//	reqObjText2.Top.Set(reqObjText2Top, 0f);

			reqObjText.Recalculate();
		//	reqObjText2.Recalculate();

			UpdateRecipeText();

			float reqObjText2Top = reqObjTextTop + 24;

			foreach (var line in reqObjTextLines) {
				line.Top.Set(reqObjText2Top, 0f);
				line.Recalculate();
				reqObjText2Top += 30;
			}

		//	int reqObjText2Rows = reqObjText2.Text.Count(c => c == '\n') + 1;
			float storedItemsTextTop = reqObjText2Top;
			float storageZoneTop = storedItemsTextTop + 24;

			storedItemsText.Top.Set(storedItemsTextTop, 0f);

			storedItemsText.Recalculate();

			if (MagicStorageConfig.IsRecursionEnabled && CanShowAllIngredientsToggle()) {
				storedItemsTextTop += recursionButton.Height.Pixels + 10;
				storageZoneTop += recursionButton.Height.Pixels + 10;

				recursionButton.Top.Set(storedItemsTextTop + 2, 0f);

				if (recursionButton.Parent is null)
					recipePanel.Append(recursionButton);

				recursionButton.Recalculate();
			} else {
				if (recursionButton.Parent is not null)
					recursionButton.Remove();
			}

			RecalculateStorageZone(storageZoneTop);

			RecalculateResultZone(storageZoneTop + storageZone.GetDimensions().Height);

			AttemptCraftButtonRecalculate();

			UpdatePanelHeight(PanelHeight);

			if (IsPanelTooShortToContainContents()) {
				ResetScrollBarMemory();

				// Attempt to force the UI layout to one that takes up less vertical space
				if (MagicUI.AttemptForcedLayoutChange(this))
					return false;

				MagicUI.CloseUIDueToHeightLimit();
				pendingUIChange = true;  //Failsafe
				return false;
			}

			RecalculateScrollBars();

			lastKnownIngredientRows = ingredientRows;
			return true;
		}

		protected void GetStorageZoneRows(out int totalRows, out int displayRows) {
			float smallSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.SmallScale;

			totalRows = (CraftingGUI.storageItems.Count + CraftingGUI.IngredientColumns - 1) / CraftingGUI.IngredientColumns;
			displayRows = (int)storageZone.GetDimensions().Height / ((int)smallSlotHeight + CraftingGUI.Padding);
		}

		private void AttemptCraftButtonRecalculate() {
			if (!MagicStorageConfig.UseOldCraftMenu) {
				InitCraftButtonDimensions();

				float craftButtonTop = craftButton.Top.Pixels;

				craftP1.Top.Set(craftButtonTop - AMOUNT_BUTTON_HEIGHT - 6, 1f);
				
				craftP1.Recalculate();

				craftP10.Top = craftP1.Top;
				craftP10.Recalculate();

				craftP100.Top = craftP1.Top;
				craftP100.Recalculate();

				craftM1.Top.Set(craftP1.Top.Pixels + craftP1.Height.Pixels + 2, 1f);
				craftM1.Recalculate();

				craftM10.Top = craftM1.Top;
				craftM10.Recalculate();

				craftM100.Top = craftM1.Top;
				craftM100.Recalculate();

				craftMax.Top.Set(craftM1.Top.Pixels + craftM1.Height.Pixels + 4, 1f);
				craftMax.Recalculate();

				craftReset.Top = craftMax.Top;
				craftReset.Recalculate();
			}
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
			MagicUI.OnRefresh += Refresh;

			if (MagicStorageConfig.UseConfigFilter) {
				var page = GetPage<RecipesPage>("Crafting");

				page.recipeButtons.Choice = MagicStorageConfig.ShowAllRecipes ? 1 : 0;
				page.recipeButtons.OnChanged();
			}

			if (MagicStorageConfig.ClearRecipeHistory)
				history.Clear();

			if (history.Current >= 0)
				history.Goto(history.Current);
		}

		protected override void OnClose() {
			MagicUI.OnRefresh -= Refresh;

			GetPage<BaseStorageUIAccessPage>(DefaultPage).scrollBar.ViewPosition = 0f;
			storageScrollBar.ViewPosition = 0f;
			ingredientScrollBar.ViewPosition = 0f;

			ingredientZone.SetHoverSlot(-1);
			storageZone.SetHoverSlot(-1);
			recipeHeaderZone.SetHoverSlot(-1);
			resultZone.SetHoverSlot(-1);

			ingredientZone.ClearItems();
			storageZone.ClearItems();
			recipeHeaderZone.ClearItems();
			resultZone.ClearItems();

			CraftingGUI.selectedRecipe = null;

			CloseRecipeHistoryPanel();

			CraftingGUI.Reset();
			CraftingGUI.ResetSlotFocus();

			CraftingGUI.lastKnownRecursionErrorForStoredItems = null;
			CraftingGUI.lastKnownRecursionErrorForObjects = null;

			CraftingGUI.showAllPossibleIngredients = false;
			recursionButton.SetState(false);
		}

		public sealed override void Refresh() {
			if (Main.gameMenu || MagicUI.CurrentlyRefreshing)
				return;

			MoveRecipePanel();

			int itemsNeeded = GetIngredientCount();
			int totalRows = GetIngredientRows(itemsNeeded);

			if (!RecalculateRecipePanelElements(totalRows))
				return;

			ingredientZone.SetItemsAndContexts(int.MaxValue, GetIngredient);

			storageZone.SetItemsAndContexts(int.MaxValue, GetStorage);

			recipeHeaderZone.SetItemsAndContexts(1, GetHeader);

			resultZone.SetItemsAndContexts(int.MaxValue, GetResult);

			history.RefreshEntries();

			GetPage(DefaultPage).Refresh();
		}

		public sealed override void OnRefreshStart() {
			RefreshZonesFromThreadStart();

			GetPage(DefaultPage).OnRefreshStart();
		}

		protected override void OnButtonConfigChanged(ButtonConfigurationMode current) {
			//Hide or show the tabs when applicable
			switch (current) {
				case ButtonConfigurationMode.Legacy:
				case ButtonConfigurationMode.LegacyWithGear:
				case ButtonConfigurationMode.ModernDropdown:
					panel.HideTab("Sorting");
					panel.HideTab("Filtering");

					if (currentPage is SortingPage or FilteringPage)
						SetPage(DefaultPage);

					break;
				case ButtonConfigurationMode.ModernPaged:
				case ButtonConfigurationMode.ModernConfigurable:
				case ButtonConfigurationMode.LegacyBasicWithPaged:
					panel.ShowTab("Sorting");
					panel.ShowTab("Filtering");
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			GetPage<BaseStorageUIAccessPage>(DefaultPage).ReformatPage(current);
		}

		public override int GetSortingOption() => GetPage<SortingPage>("Sorting").option;

		public override int GetFilteringOption() => GetPage<FilteringPage>("Filtering").option;

		public override string GetSearchText() => GetPage<BaseStorageUIAccessPage>(DefaultPage).searchBar.Text;

		protected override void GetConfigPanelLocation(out float left, out float top) {
			base.GetConfigPanelLocation(out left, out top);

			left += recipeWidth;
		}

		private void OpenRecipeHistoryPanel() {
			if (recipeHistoryPanel.Parent is not null)
				return;

			recipeHistory.Remove();
			recipePanel.Append(recipeHistoryPanel);
			recipePanel.Recalculate();
		}

		private void CloseRecipeHistoryPanel() {
			if (recipeHistoryPanel.Parent is null)
				return;

			recipePanel.Append(recipeHistory);
			recipeHistoryPanel.Remove();
			recipePanel.Recalculate();
		}

		public override float GetMinimumResizeHeight() {
			//Main page height
			GetMainPanelMinimumHeights(GetPage<BaseStorageUIAccessPage>(DefaultPage), out float mainPageMinimumHeight, out float dropdownHeight);

			//Recipe panel height
			float recipePanelMinimumHeight = GetRecipePanelMinimumHeight();

			return Math.Max(dropdownHeight, Math.Max(mainPageMinimumHeight, recipePanelMinimumHeight));
		}

		public partial class RecipesPage : BaseStorageUIAccessPage {
			internal NewUIButtonChoice recipeButtons;
			internal UIText stationText;

			internal NewUISlotZone stationZone;  //Item slots for the crafting stations

			private int lastKnownStationsCount = -1;
			private bool lastKnownConfigFavorites;
			private bool lastKnownConfigBlacklist;

			protected RecipesPage(BaseStorageUI parent, string name) : base(parent, name) {
				OnPageDeselected += DeselectPage;
			}

			public RecipesPage(BaseStorageUI parent) : base(parent, "Crafting") {
				OnPageDeselected += DeselectPage;
			}

			private void DeselectPage() {
				lastKnownStationsCount = -1;

				stationZone.SetHoverSlot(-1);

				stationZone.ClearItems();
			}

			private const float stationTextTop = 12;
			private const float stationTop = stationTextTop + 26;

			public override void OnInitialize() {
				base.OnInitialize();

				float itemSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.InventoryScale;

				recipeButtons = new(() => MagicUI.SetRefresh(forceFullRefresh: true), 32, 5, forceGearIconToNotBeCreated: true);
				InitFilterButtons();
				topBar.Append(recipeButtons);

				stationText = new UIText(Language.GetText("Mods.MagicStorage.CraftingStations"));
				Append(stationText);

				stationZone = new(CraftingGUI.InventoryScale / 1.55f);

				stationZone.InitializeSlot = static (slot, scale) => {
					MagicStorageItemSlot itemSlot = new(slot, scale: scale) {
						IgnoreClicks = true  // Purely visual
					};

					itemSlot.OnLeftClick += static (evt, e) => {
						MagicStorageItemSlot obj = e as MagicStorageItemSlot;

						TECraftingAccess access = CraftingGUI.GetCraftingEntity();
						if (access == null || obj.id >= TECraftingAccess.ItemsTotal)
							return;

						Player player = Main.LocalPlayer;

						bool changed = false;
						if (obj.id < access.stations.Count && ItemSlot.ShiftInUse) {
							access.TryWithdrawStation(obj.id, true);
							changed = true;
						} else if (player.itemAnimation == 0 && player.itemTime == 0) {
							if (Main.mouseItem.IsAir) {
								if (!access.TryWithdrawStation(obj.id).IsAir)
									changed = true;
							} else {
								int oldType = Main.mouseItem.type;
								int oldStack = Main.mouseItem.stack;

								Main.mouseItem = access.TryDepositStation(Main.mouseItem);
								
								if (oldType != Main.mouseItem.type || oldStack != Main.mouseItem.stack)
									changed = true;
							}
						}

						if (changed) {
							MagicUI.SetRefresh();
							SoundEngine.PlaySound(SoundID.Grab);

							obj.IgnoreNextHandleAction = true;
						}
					};

					return itemSlot;
				};

				stationZone.Width.Set(0f, 1f);
				
				Append(stationZone);

				lastKnownConfigFavorites = MagicStorageConfig.CraftingFavoritingEnabled;
				lastKnownConfigBlacklist = MagicStorageConfig.RecipeBlacklistEnabled;

				AdjustCommonElements();
			}

			private void InitFilterButtons() {
				List<Asset<Texture2D>> assets = new() {
					MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/RecipeAvailable", AssetRequestMode.ImmediateLoad),
					MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/RecipeAll", AssetRequestMode.ImmediateLoad)
				};

				List<LocalizedText> texts = new() {
					Language.GetText("Mods.MagicStorage.RecipeAvailable"),
					Language.GetText("Mods.MagicStorage.RecipeAll")
				};

				if (MagicStorageConfig.CraftingFavoritingEnabled) {
					assets.Add(MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/FilterMisc", AssetRequestMode.ImmediateLoad));
					texts.Add(Language.GetText("Mods.MagicStorage.ShowOnlyFavorited"));
				}

				if (MagicStorageConfig.RecipeBlacklistEnabled) {
					assets.Add(MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/RecipeAll", AssetRequestMode.ImmediateLoad));
					texts.Add(Language.GetText("Mods.MagicStorage.RecipeBlacklist"));
				}

				recipeButtons.AssignButtons(assets.ToArray(), texts.ToArray());

				lastKnownConfigFavorites = MagicStorageConfig.CraftingFavoritingEnabled;
				lastKnownConfigBlacklist = MagicStorageConfig.RecipeBlacklistEnabled;
			}

			public override void PostReformatPage(ButtonConfigurationMode current) {
				//Adjust the position of elements here
				Refresh();
			}

			protected override void SetThreadWait(bool waiting) {
				base.SetThreadWait(waiting);
				(parentUI as CraftingUIState).SetThreadWait(waiting);
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);

				MagicUI.CheckRefresh();

				if (GetStationCount() != lastKnownStationsCount || PendingZoneRefresh)
					parentUI.Refresh();

				if (lastKnownConfigFavorites != MagicStorageConfig.CraftingFavoritingEnabled || lastKnownConfigBlacklist != MagicStorageConfig.RecipeBlacklistEnabled)
					InitFilterButtons();
			}

			private bool UpdateZones() {
				if (Main.gameMenu || MagicUI.CurrentlyRefreshing)
					return false;

				float itemSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.InventoryScale;

				UpdateStationElements(out int stationCount);

				parentUI.UpdatePanelHeight(parentUI.PanelHeight);

				AdjustCommonElements();

				int count = GetZoneItemCount();

				int numRows = (count + CraftingGUI.RecipeColumns - 1) / CraftingGUI.RecipeColumns;
				int displayRows = (int)slotZone.GetDimensions().Height / ((int)itemSlotHeight + CraftingGUI.Padding);

				if (numRows > 0 && displayRows <= 0) {
					lastKnownStationsCount = -1;
					lastKnownScrollBarViewPosition = -1;

					// Attempt to force the UI layout to one that takes up less vertical space
					if (MagicUI.AttemptForcedLayoutChange(parentUI))
						return false;

					MagicUI.CloseUIDueToHeightLimit();
					parentUI.pendingUIChange = true;  //Failsafe
					return false;
				}

				slotZone.SetDimensions(CraftingGUI.RecipeColumns, displayRows);

				int noDisplayRows = numRows - displayRows;
				if (noDisplayRows < 0)
					noDisplayRows = 0;
				
				float recipeScrollBarMaxViewSize = 1 + noDisplayRows;
				scrollBar.Height.Set(displayRows * (itemSlotHeight + CraftingGUI.Padding), 0f);
				scrollBar.SetView(CraftingGUI.RecipeScrollBarViewSize, recipeScrollBarMaxViewSize);

				scrollBar.Recalculate();

				lastKnownStationsCount = stationCount;
				lastKnownScrollBarViewPosition = scrollBar.ViewPosition;
				return true;
			}

			public override void Refresh() {
				if (!UpdateZones())
					return;

				stationZone.SetItemsAndContexts(int.MaxValue, GetStation);

				slotZone.SetItemsAndContexts(int.MaxValue, GetMainZoneItem);
			}

			public override void OnRefreshStart() {
				stationZone.ClearContexts();

				slotZone.ClearContexts();
			}

			public override void GetZoneDimensions(out float top, out float bottomMargin) {
				bottomMargin = 36f;

				top = GetZoneTop() + stationTop + stationZone.ZoneHeight;
			}

			protected override float GetSearchBarRight() => recipeButtons.GetDimensions().Width;

			protected override void InitZoneSlotEvents(MagicStorageItemSlot itemSlot) {
				itemSlot.CanShareItemToChat = true;

				itemSlot.OnLeftClick += (evt, e) => {
					// Prevent actions while refreshing the items
					if (MagicUI.CurrentlyRefreshing)
						return;

					MagicStorageItemSlot obj = e as MagicStorageItemSlot;

					int objSlot = obj.id + CraftingGUI.RecipeColumns * (int)Math.Round(scrollBar.ViewPosition);

					if (objSlot >= GetZoneItemCount())
						return;

					Item item = obj.StoredItem;

					if (MagicStorageConfig.CraftingFavoritingEnabled && Main.keyState.IsKeyDown(Main.FavoriteKey)) {
						var set = GetFavoriteSet(StoragePlayer.LocalPlayer);

						if (!set.Add(item))
							set.Remove(item);

						MagicUI.SetRefresh();
						OnMainZoneItemFavoriteChanged(item);
					} else if (MagicStorageConfig.RecipeBlacklistEnabled && Main.keyState.IsKeyDown(Keys.LeftControl)) {
						bool whitelisting = recipeButtons.Choice == CraftingGUI.RecipeButtonsBlacklistChoice;

						if (whitelisting) {
							// Force revealing to happen for both the player and global lists
							RevealItemGlobal(item);
							RevealItem(item);
						} else {
							if (Main.keyState.IsKeyDown(Keys.LeftShift))
								HideItemGlobal(item);
							else
								HideItem(item);
						}

						MagicUI.SetRefresh();
						OnMainZoneItemBlacklistChanged(item, !whitelisting);
					} else {
						MagicUI.SetRefresh();
						OnMainZoneItemLeftClicked(objSlot);

						parentUI.UpdatePanelHeight(parentUI.PanelHeight);
					}
				};
			}

			private void RevealItem(Item item) {
				if (GetHiddenSet(StoragePlayer.LocalPlayer).Remove(item)) {
					Main.NewText(Language.GetTextValue(GetHiddenSetRevealLocalizationKey(), Lang.GetItemNameValue(item.type)));
					OnMainZoneItemHiddenSetChanged(item, false);
					slotZone.SetItemsAndContexts(int.MaxValue, GetMainZoneItem);
				}
			}

			private void HideItem(Item item) {
				if (GetHiddenSet(StoragePlayer.LocalPlayer).Add(item)) {
					Main.NewText(Language.GetTextValue(GetHiddenSetHideLocalizationKey(), Lang.GetItemNameValue(item.type)));
					OnMainZoneItemHiddenSetChanged(item, true);
					slotZone.SetItemsAndContexts(int.MaxValue, GetMainZoneItem);
				}
			}

			private void RevealItemGlobal(Item item) {
				if (GetGlobalBlacklistSet().Remove(new(item.type))) {
					Main.NewText(Language.GetTextValue(GetGlobalBlacklistSetRevealLocalizationKey(), Lang.GetItemNameValue(item.type)));
					OnMainZoneItemGlobalBlacklistChanged(item, false);
					slotZone.SetItemsAndContexts(int.MaxValue, GetMainZoneItem);
				}
			}

			private void HideItemGlobal(Item item) {
				if (GetGlobalBlacklistSet().Add(new(item.type))) {
					Main.NewText(Language.GetTextValue(GetGlobalBlacklistSetHideLocalizationKey(), Lang.GetItemNameValue(item.type)));
					OnMainZoneItemGlobalBlacklistChanged(item, true);
					slotZone.SetItemsAndContexts(int.MaxValue, GetMainZoneItem);
				}
			}

			protected override bool ShouldHideItemIcons() {
				CraftingUIState parent = parentUI as CraftingUIState;

				return Main.mouseX > parentUI.PanelLeft && Main.mouseX < parent.recipeLeft + parent.recipeWidth && Main.mouseY > parentUI.PanelTop && Main.mouseY < parentUI.PanelBottom;
			}
		}

		public class RecursiveTreeViewPage : BaseStorageUIPage {
			// TODO: design UI.  should this even be another page?

			public RecursiveTreeViewPage(BaseStorageUI parent) : base(parent, "Tree") { }
		}
	}
}
