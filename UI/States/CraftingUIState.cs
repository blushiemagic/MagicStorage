using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.UI;

namespace MagicStorage.UI.States {
	public sealed class CraftingUIState : BaseStorageUI {
		public const float SmallerScale = 0.64f;

		private UIPanel recipePanel;
		private UIText recipePanelHeader;
		private UIText ingredientText;
		private UIText reqObjText;
		private UIText reqObjText2;
		private UIText storedItemsText;

		private UIPanel recipeWaitPanel;
		private UIText recipeWaitText;

		public readonly RecipeHistory history = new();
		private RecipePanelHistoryArrangement recipeHistory;
		private RecipeHistoryPanel recipeHistoryPanel;

		private NewUISlotZone ingredientZone;    //Recipe ingredients
		private NewUISlotZone storageZone;       //Items in storage valid for recipe ingredient
		private NewUISlotZone recipeHeaderZone;  //Preview item for result
		private NewUISlotZone resultZone;        //Result items already in storage (in one slot)

		private NewUIScrollbar storageScrollBar;
		private float storageScrollBarMaxViewSize = 2f;

		private UICraftButton craftButton;
		private UICraftAmountAdjustment craftP1, craftP10, craftP100, craftM1, craftM10, craftM100, craftMax, craftReset;
		private UIText craftAmount;

		private int lastKnownIngredientRows = 1;
		private bool lastKnownUseOldCraftButtons = false;
		private float lastKnownScrollBarViewPosition = -1;

		private float recipeLeft, recipeTop, recipeWidth, recipeHeight;

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
			recipeHeaderZone.InitializeSlot += static (slot, scale) => {
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
			ingredientZone.Width.Set(0f, 1f);

			ingredientZone.InitializeSlot += (slot, scale) => {
				MagicStorageItemSlot itemSlot = new(slot, scale: scale) {
					IgnoreClicks = true  // Purely visual
				};
				
				itemSlot.OnRightClick += (evt, e) => {
					// Prevent actions while refreshing the items
					if (StorageGUI.CurrentlyRefreshing)
						return;

					if (CraftingGUI.selectedRecipe is null)
						return;

					MagicStorageItemSlot obj = e as MagicStorageItemSlot;

					if (obj.slot >= CraftingGUI.selectedRecipe.requiredItem.Count)
						return;

					// Right click will do nothing when recursive crafting is enabled, since it would be pointless
					if (MagicStorageConfig.IsRecursionEnabled)
						return;

					// select ingredient recipe by right clicking
					Item item = CraftingGUI.selectedRecipe.requiredItem[obj.slot];
					if (MagicCache.ResultToRecipe.TryGetValue(item.type, out var itemRecipes) && itemRecipes.Length > 0) {
						Recipe selected = itemRecipes[0];

						using (FlagSwitch.ToggleTrue(ref CraftingGUI.disableNetPrintingForIsAvailable)) {
							foreach (Recipe r in itemRecipes[1..]) {
								if (CraftingGUI.IsAvailable(r)) {
									selected = r;
									break;
								}
							}
						}

						CraftingGUI.SetSelectedRecipe(selected);
						StorageGUI.SetRefresh();
						CraftingGUI.SetNextDefaultRecipeCollectionToRefresh(Array.Empty<Recipe>());

						UpdatePanelHeight(PanelHeight);

						history.AddHistory(CraftingGUI.selectedRecipe);
					}
				};

				return itemSlot;
			};

			recipePanel.Append(ingredientZone);

			reqObjText = new UIText(Language.GetText("LegacyInterface.22"));
			reqObjText2 = new UIText("");
			recipePanel.Append(reqObjText);
			recipePanel.Append(reqObjText2);

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

			storageZone.InitializeSlot += (slot, scale) => {
				MagicStorageItemSlot itemSlot = new(slot, scale: scale) {
					IgnoreClicks = true  // Purely visual
				};

				itemSlot.OnLeftClick += (evt, e) => {
					// Prevent actions while refreshing the items
					if (StorageGUI.CurrentlyRefreshing)
						return;

					MagicStorageItemSlot obj = e as MagicStorageItemSlot;

					int index = obj.slot + CraftingGUI.IngredientColumns * (int)Math.Round(storageScrollBar.ViewPosition);

					if (index >= CraftingGUI.storageItems.Count)
						return;

					ItemData data = new(obj.StoredItem);
					if (CraftingGUI.blockStorageItems.Contains(data))
						CraftingGUI.blockStorageItems.Remove(data);
					else
						CraftingGUI.blockStorageItems.Add(data);

					// Force the "recipe available" logic to update
					CraftingGUI.ResetRecentRecipeCache();

					storageZone.SetItemsAndContexts(int.MaxValue, GetStorage);
				};

				return itemSlot;
			};

			storageZone.OnScrollWheel += (evt, e) => {
				if (storageScrollBar is not null)
					storageScrollBar.ViewPosition -= evt.ScrollWheelValue / storageScrollBar.ScrollDividend;
			};

			storedItemsText = new UIText(Language.GetText("Mods.MagicStorage.StoredItems"));
			recipePanel.Append(storedItemsText);

			storageZone.Width.Set(0f, 1f);
			
			recipePanel.Append(storageZone);

			storageScrollBar = new(scrollDividend: 250f);
			storageScrollBar.Left.Set(-20f, 1f);
			storageScrollBar.SetView(CraftingGUI.ScrollBar2ViewSize, storageScrollBarMaxViewSize);
			storageZone.Append(storageScrollBar);

			craftButton = new UICraftButton(Language.GetText("LegacyMisc.72"));
			craftButton.Top.Set(-48f, 1f);
			craftButton.Width.Set(100f, 0f);
			craftButton.Height.Set(24f, 0f);
			craftButton.PaddingTop = 8f;
			craftButton.PaddingBottom = 8f;
			recipePanel.Append(craftButton);

			resultZone = new(CraftingGUI.InventoryScale);

			resultZone.InitializeSlot += (slot, scale) => {
				MagicStorageItemSlot itemSlot = new(slot, scale: scale) {
					IgnoreClicks = true  // Purely visual
				};

				itemSlot.OnLeftClick += (evt, e) => {
					// Prevent actions while refreshing the items
					if (StorageGUI.CurrentlyRefreshing)
						return;

					MagicStorageItemSlot obj = e as MagicStorageItemSlot;

					Item result = obj.StoredItem;

					if (Main.mouseItem.IsAir && result is not null && !result.IsAir) {
						bool shiny = result.newAndShiny;

						result.newAndShiny = false;

						if (shiny)
							resultZone.SetItemsAndContexts(1, CraftingGUI.GetResult);
					}

					Player player = Main.LocalPlayer;

					bool changed = false;
					if (!Main.mouseItem.IsAir && player.itemAnimation == 0 && player.itemTime == 0 && result is not null && Main.mouseItem.type == result.type) {
						if (CraftingGUI.TryDepositResult(Main.mouseItem))
							changed = true;
					} else if (Main.mouseItem.IsAir && result is not null && !result.IsAir) {
						if (Main.keyState.IsKeyDown(Keys.LeftAlt)) {
							result.favorited = !result.favorited;
							resultZone.SetItemsAndContexts(1, CraftingGUI.GetResult);
							CraftingGUI.SetNextDefaultRecipeCollectionToRefresh(Array.Empty<Recipe>());
						} else {
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
						StorageGUI.SetRefresh();

						SoundEngine.PlaySound(SoundID.Grab);

						obj.IgnoreNextHandleAction = true;
					}
				};

				itemSlot.OnRightMouseDown += static (evt, e) => {
					// Prevent actions while refreshing the items
					if (StorageGUI.CurrentlyRefreshing)
						return;

					MagicStorageItemSlot obj = e as MagicStorageItemSlot;

					Item result = obj.StoredItem;

					if (result is not null && !result.IsAir && (Main.mouseItem.IsAir || ItemCombining.CanCombineItems(Main.mouseItem, result) && Main.mouseItem.stack < Main.mouseItem.maxStack))
						CraftingGUI.slotFocus = true;

					if (CraftingGUI.slotFocus) {
						CraftingGUI.SlotFocusLogic();
						obj.IgnoreNextHandleAction = true;
					}
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
			craftMax.SetAmountInformation(int.MaxValue, false);
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

		const int AMOUNT_BUTTON_HEIGHT = 24;

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
					if (isWaitPanelWaitingToOpen && StorageGUI.CurrentThreadingDuration > 10) {
						isWaitPanelWaitingToOpen = false;

						if (recipeWaitPanel.Parent is null)
							recipePanel.Append(recipeWaitPanel);  // Delay appending to here
					}
				}

				if (!Main.mouseRight)
					CraftingGUI.ResetSlotFocus();

				if (CraftingGUI.slotFocus)
					CraftingGUI.SlotFocusLogic();

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

				int itemsNeeded = CraftingGUI.selectedRecipe?.requiredItem.Count ?? CraftingGUI.IngredientColumns;
				int recipeRows = itemsNeeded / CraftingGUI.IngredientColumns;
				int extraRow = itemsNeeded % CraftingGUI.IngredientColumns != 0 ? 1 : 0;
				int totalRows = recipeRows + extraRow;
				if (totalRows < 1)
					totalRows = 1;

				if (totalRows != lastKnownIngredientRows || storageScrollBar.ViewPosition != lastKnownScrollBarViewPosition)
					RecalculateRecipePanelElements(itemsNeeded, totalRows);

				UpdateRecipeText();
			} catch (Exception e) {
				Main.NewTextMultiline(e.ToString(), c: Color.White);
			}

			CraftingGUI.PlayerZoneCache.FreeCache(true);
		}

		public override void Draw(SpriteBatch spriteBatch) {
			using (FlagSwitch.Create(ref MagicUI.blockItemSlotActionsDetour, !recipeHistoryPanel.IsMouseHovering))
				base.Draw(spriteBatch);
		}
		
		const float ingredientZoneTop = 54f;

		private bool RecalculateRecipePanelElements(int itemsNeeded, int ingredientRows) {
			float itemSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.InventoryScale;
			float itemSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.InventoryScale;
			float smallSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.SmallScale;
			float smallSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.SmallScale;

			int extraRow = itemsNeeded % CraftingGUI.IngredientColumns != 0 ? 1 : 0;
			int totalRows = ingredientRows + extraRow;
			if (totalRows < 1)
				totalRows = 1;
			float ingredientZoneHeight = 30f * totalRows;

			ingredientZone.SetDimensions(CraftingGUI.IngredientColumns, totalRows);
			ingredientZone.Top.Set(ingredientZoneTop, 0f);
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

			storageZone.Height.Set(-storageZoneTop - 60 - AMOUNT_BUTTON_HEIGHT, 1f);

			storageZone.Recalculate();

			UpdatePanelHeight(PanelHeight);

			int numRows2 = (CraftingGUI.storageItems.Count + CraftingGUI.IngredientColumns - 1) / CraftingGUI.IngredientColumns;
			int displayRows2 = (int)storageZone.GetDimensions().Height / ((int)smallSlotHeight + CraftingGUI.Padding);

			if (numRows2 > 0 && displayRows2 <= 0) {
				lastKnownIngredientRows = -1;
				lastKnownScrollBarViewPosition = -1;

				MagicUI.CloseUIDueToHeightLimit();
				pendingUIChange = true;  //Failsafe
				return false;
			}

			storageZone.SetDimensions(CraftingGUI.IngredientColumns, displayRows2);
			int noDisplayRows2 = numRows2 - displayRows2;
			if (noDisplayRows2 < 0)
				noDisplayRows2 = 0;

			storageScrollBarMaxViewSize = 1 + noDisplayRows2;
			storageScrollBar.Height.Set(displayRows2 * (smallSlotHeight + CraftingGUI.Padding), 0f);
			storageScrollBar.SetView(CraftingGUI.ScrollBar2ViewSize, storageScrollBarMaxViewSize);

			storageScrollBar.Recalculate();

			resultZone.Top.Set(storageZoneTop + storageZone.GetDimensions().Height + 10, 0f);

			resultZone.Width.Set(itemSlotWidth, 0f);
			resultZone.Height.Set(itemSlotHeight, 0f);

			resultZone.Recalculate();

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

			lastKnownIngredientRows = ingredientRows;
			lastKnownScrollBarViewPosition = storageScrollBar.ViewPosition;
			return true;
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

		private void UpdateRecipeText() {
			if (CraftingGUI.selectedRecipe == null) {
				reqObjText2.SetText("");
				recipePanelHeader.SetText(Language.GetText("Mods.MagicStorage.SelectedRecipe").Value);
			} else {
				bool isEmpty = true;
				string text = "";
				int rows = 0;

				void AddText(string s) {
					if (!isEmpty)
						text += ", ";

					if ((text.Length + s.Length) / 35 > rows) {
						text += "\n";
						++rows;
					}

					text += s;
					isEmpty = false;
				}

				IEnumerable<int> requiredTiles;
				CraftingSimulation craftingSimulation = null;
				if (CraftingGUI.GetCraftingSimulationForCurrentRecipe() is CraftingSimulation simulation) {
					craftingSimulation = simulation;
					requiredTiles = simulation.RequiredTiles;
				} else
					requiredTiles = CraftingGUI.selectedRecipe.requiredTile;

				foreach (int tile in requiredTiles)
					AddText(Lang.GetMapObjectName(MapHelper.TileToLookup(tile, 0)));

				if (craftingSimulation is null) {
					if (CraftingGUI.selectedRecipe.HasCondition(Condition.NearWater))
						AddText(Language.GetTextValue("LegacyInterface.53"));

					if (CraftingGUI.selectedRecipe.HasCondition(Condition.NearHoney))
						AddText(Language.GetTextValue("LegacyInterface.58"));

					if (CraftingGUI.selectedRecipe.HasCondition(Condition.NearLava))
						AddText(Language.GetTextValue("LegacyInterface.56"));

					if (CraftingGUI.selectedRecipe.HasCondition(Condition.InSnow))
						AddText(Language.GetTextValue("LegacyInterface.123"));

					if (CraftingGUI.selectedRecipe.HasCondition(Condition.InGraveyard))
						AddText(Language.GetTextValue("LegacyInterface.124"));
				} else {
					if (craftingSimulation.HasCondition(Condition.NearWater))
						AddText(Language.GetTextValue("LegacyInterface.53"));

					if (craftingSimulation.HasCondition(Condition.NearHoney))
						AddText(Language.GetTextValue("LegacyInterface.58"));

					if (craftingSimulation.HasCondition(Condition.NearLava))
						AddText(Language.GetTextValue("LegacyInterface.56"));

					if (craftingSimulation.HasCondition(Condition.InSnow))
						AddText(Language.GetTextValue("LegacyInterface.123"));

					if (craftingSimulation.HasCondition(Condition.InGraveyard))
						AddText(Language.GetTextValue("LegacyInterface.124"));
				}

				if (isEmpty)
					text = Language.GetTextValue("LegacyInterface.23");

				reqObjText2.SetText(text);

				/*
				double dps = CompareDps.GetDps(CraftingGUI.selectedRecipe.createItem);
				string dpsText = dps >= 1d ? $"DPS = {dps:F}" : string.Empty;

				recipePanelHeader.SetText(dpsText);
				*/
			}
		}

		protected override void PostAppendPanel() {
			Append(recipePanel);
		}

		protected override void OnOpen() {
			StorageGUI.OnRefresh += Refresh;

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
			StorageGUI.OnRefresh -= Refresh;

			GetPage<RecipesPage>("Crafting").scrollBar.ViewPosition = 0f;
			storageScrollBar.ViewPosition = 0f;

			ingredientZone.HoverSlot = -1;
			storageZone.HoverSlot = -1;
			recipeHeaderZone.HoverSlot = -1;
			resultZone.HoverSlot = -1;

			ingredientZone.ClearItems();
			storageZone.ClearItems();
			recipeHeaderZone.ClearItems();
			resultZone.ClearItems();

			CraftingGUI.selectedRecipe = null;

			CloseRecipeHistoryPanel();

			CraftingGUI.Reset();
			CraftingGUI.ResetSlotFocus();
		}

		public override void Refresh() {
			if (Main.gameMenu || StorageGUI.CurrentlyRefreshing)
				return;

			MoveRecipePanel();

			int itemsNeeded = CraftingGUI.selectedRecipe?.requiredItem.Count ?? CraftingGUI.IngredientColumns;
			int recipeRows = itemsNeeded / CraftingGUI.IngredientColumns;
			int extraRow = itemsNeeded % CraftingGUI.IngredientColumns != 0 ? 1 : 0;
			int totalRows = recipeRows + extraRow;
			if (totalRows < 1)
				totalRows = 1;

			if (!RecalculateRecipePanelElements(itemsNeeded, totalRows))
				return;

			ingredientZone.SetItemsAndContexts(int.MaxValue, CraftingGUI.GetIngredient);

			storageZone.SetItemsAndContexts(int.MaxValue, GetStorage);

			recipeHeaderZone.SetItemsAndContexts(1, CraftingGUI.GetHeader);

			resultZone.SetItemsAndContexts(1, CraftingGUI.GetResult);

			history.RefreshEntries();

			GetPage<RecipesPage>("Crafting").Refresh();
		}

		internal Item GetStorage(int slot, ref int context) {
			if (StorageGUI.CurrentlyRefreshing)
				return new Item();

			int index = slot + CraftingGUI.IngredientColumns * (int)Math.Round(storageScrollBar.ViewPosition);
			Item item = index < CraftingGUI.storageItems.Count ? CraftingGUI.storageItems[index] : new Item();
			if (CraftingGUI.blockStorageItems.Contains(new ItemData(item)))
				context = ItemSlot.Context.ChestItem;  // Red

			return item;
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

			GetPage<RecipesPage>("Crafting").ReformatPage(current);
		}

		public override int GetSortingOption() => GetPage<SortingPage>("Sorting").option;

		public override int GetFilteringOption() => GetPage<FilteringPage>("Filtering").option;

		public override string GetSearchText() => GetPage<RecipesPage>("Crafting").searchBar.Text;

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
			var page = GetPage<RecipesPage>("Crafting");

			//Main page height
			page.GetZoneDimensions(out float zoneTop, out float bottomMargin);

			float itemSlotHeight = TextureAssets.InventoryBack.Value.Height * StorageGUI.inventoryScale;

			float mainPageMinimumHeight = zoneTop + itemSlotHeight + StorageGUI.padding + bottomMargin;

			float dropdownHeight;

			if (MagicStorageConfig.ButtonUIMode == ButtonConfigurationMode.ModernDropdown) {
				dropdownHeight = page.sortingDropdown.MaxExpandedHeight;

				dropdownHeight += page.topBar2.Top.Pixels;
			} else
				dropdownHeight = 0;

			//Recipe panel height
			float smallSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.SmallScale;

			int itemsNeeded = CraftingGUI.selectedRecipe?.requiredItem.Count ?? CraftingGUI.IngredientColumns;
			int recipeRows = itemsNeeded / CraftingGUI.IngredientColumns;
			int extraRow = itemsNeeded % CraftingGUI.IngredientColumns != 0 ? 1 : 0;
			int totalRows = recipeRows + extraRow;
			if (totalRows < 1)
				totalRows = 1;
			float ingredientZoneHeight = 30f * totalRows;

			float reqObjTextTop = ingredientZoneTop + ingredientZoneHeight + 11 * totalRows;
			float reqObjText2Top = reqObjTextTop + 24;

			int reqObjText2Rows = reqObjText2.Text.Count(c => c == '\n') + 1;
			float storedItemsTextTop = reqObjText2Top + 30 * reqObjText2Rows;
			float storageZoneTop = storedItemsTextTop + 24;

			float recipePanelMinimumHeight = storageZoneTop + smallSlotHeight + CraftingGUI.Padding + 68;

			return Math.Max(dropdownHeight, Math.Max(mainPageMinimumHeight, recipePanelMinimumHeight));
		}

		public class RecipesPage : BaseStorageUIAccessPage {
			internal NewUIButtonChoice recipeButtons;
			internal UIText stationText;

			internal NewUISlotZone stationZone;  //Item slots for the crafting stations

			private int lastKnownStationsCount = -1;
			private bool lastKnownConfigFavorites;
			private bool lastKnownConfigBlacklist;

			public RecipesPage(BaseStorageUI parent) : base(parent, "Crafting") {
				OnPageDeselected += () => {
					lastKnownStationsCount = -1;

					stationZone.HoverSlot = -1;

					stationZone.ClearItems();
				};
			}

			private const float stationTextTop = 12;
			private const float stationTop = stationTextTop + 26;

			public override void OnInitialize() {
				base.OnInitialize();

				float itemSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.InventoryScale;

				recipeButtons = new(() => StorageGUI.SetRefresh(forceFullRefresh: true), 32, 5, forceGearIconToNotBeCreated: true);
				InitFilterButtons();
				topBar.Append(recipeButtons);

				stationText = new UIText(Language.GetText("Mods.MagicStorage.CraftingStations"));
				Append(stationText);

				stationZone = new(CraftingGUI.InventoryScale / 1.55f);

				stationZone.InitializeSlot += static (slot, scale) => {
					MagicStorageItemSlot itemSlot = new(slot, scale: scale) {
						IgnoreClicks = true  // Purely visual
					};

					itemSlot.OnLeftClick += static (evt, e) => {
						MagicStorageItemSlot obj = e as MagicStorageItemSlot;

						TECraftingAccess access = CraftingGUI.GetCraftingEntity();
						if (access == null || obj.slot >= TECraftingAccess.ItemsTotal)
							return;

						Player player = Main.LocalPlayer;

						bool changed = false;
						if (obj.slot < access.stations.Count && ItemSlot.ShiftInUse) {
							access.TryWithdrawStation(obj.slot, true);
							changed = true;
						} else if (player.itemAnimation == 0 && player.itemTime == 0) {
							if (Main.mouseItem.IsAir) {
								if (!access.TryWithdrawStation(obj.slot).IsAir)
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
							StorageGUI.SetRefresh();
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

				StorageGUI.CheckRefresh();

				if (CraftingGUI.GetCraftingStations().Count != lastKnownStationsCount || PendingZoneRefresh)
					(parentUI as CraftingUIState).Refresh();

				if (lastKnownConfigFavorites != MagicStorageConfig.CraftingFavoritingEnabled || lastKnownConfigBlacklist != MagicStorageConfig.RecipeBlacklistEnabled)
					InitFilterButtons();
			}

			private bool UpdateZones() {
				if (Main.gameMenu || StorageGUI.CurrentlyRefreshing)
					return false;

				float itemSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.InventoryScale;

				int stationCount = CraftingGUI.GetCraftingStations().Count;
				int rows = stationCount / TECraftingAccess.Columns + 1;
				if (rows > TECraftingAccess.Rows)
					rows = TECraftingAccess.Rows;
				
				float top = MagicStorageConfig.ButtonUIMode switch {
					ButtonConfigurationMode.Legacy
					or ButtonConfigurationMode.ModernConfigurable
					or ButtonConfigurationMode.LegacyWithGear
					or ButtonConfigurationMode.LegacyBasicWithPaged => TopBar3Bottom,
					ButtonConfigurationMode.ModernPaged => TopBar1Bottom,
					ButtonConfigurationMode.ModernDropdown => TopBar2Bottom,
					_ => throw new ArgumentOutOfRangeException()
				};

				stationText.Top.Set(top + stationTextTop, 0f);

				stationZone.SetDimensions(TECraftingAccess.Columns, rows);
				stationZone.Height.Set(stationZone.ZoneHeight, 0f);
				stationZone.Top.Set(top + stationTop, 0f);

				stationZone.Recalculate();

				parentUI.UpdatePanelHeight(parentUI.PanelHeight);

				AdjustCommonElements();

				int count = StorageGUI.CurrentlyRefreshing ? 0 : CraftingGUI.recipes?.Count ?? 0;

				int numRows = (count + CraftingGUI.RecipeColumns - 1) / CraftingGUI.RecipeColumns;
				int displayRows = (int)slotZone.GetDimensions().Height / ((int)itemSlotHeight + CraftingGUI.Padding);

				if (numRows > 0 && displayRows <= 0) {
					lastKnownStationsCount = -1;
					lastKnownScrollBarViewPosition = -1;

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

			public void Refresh() {
				if (!UpdateZones())
					return;

				stationZone.SetItemsAndContexts(int.MaxValue, CraftingGUI.GetStation);

				slotZone.SetItemsAndContexts(int.MaxValue, GetRecipe);
			}

			internal Item GetRecipe(int slot, ref int context) {
				if (StorageGUI.CurrentlyRefreshing)
					return new Item();

				int index = slot + CraftingGUI.RecipeColumns * (int)Math.Round(scrollBar.ViewPosition);

				// Fail early if the index is invalid
				if (index < 0 || index >= CraftingGUI.recipes.Count)
					return new Item();

				try {
					Recipe recipe = CraftingGUI.recipes[index];
					bool available = CraftingGUI.recipeAvailable[index];

					Item item = recipe.createItem;

					// Air item should display as an "empty slot" without special contexts
					if (!item.IsAir) {
						bool selected = object.ReferenceEquals(recipe, CraftingGUI.selectedRecipe);

						if (selected)
							context = 6;

						if (!available)
							context = selected ? 4 : 3;
					
						if (StoragePlayer.LocalPlayer.FavoritedRecipes.Contains(item)) {
							item = item.Clone();
							item.favorited = true;
						}
					}

					return item;
				} catch {
					// Failsafe: return empty item on error
					return new Item();
				}
			}

			public override void GetZoneDimensions(out float top, out float bottomMargin) {
				bottomMargin = 36f;

				top = MagicStorageConfig.ButtonUIMode switch {
					ButtonConfigurationMode.Legacy
					or ButtonConfigurationMode.ModernConfigurable
					or ButtonConfigurationMode.LegacyWithGear
					or ButtonConfigurationMode.LegacyBasicWithPaged => TopBar3Bottom,
					ButtonConfigurationMode.ModernPaged => TopBar1Bottom,
					ButtonConfigurationMode.ModernDropdown => TopBar2Bottom,
					_ => throw new ArgumentOutOfRangeException()
				};

				top += stationTop + stationZone.ZoneHeight;
			}

			protected override float GetSearchBarRight() => recipeButtons.GetDimensions().Width;

			protected override void InitZoneSlotEvents(MagicStorageItemSlot itemSlot) {
				itemSlot.OnLeftClick += (evt, e) => {
					// Prevent actions while refreshing the items
					if (StorageGUI.CurrentlyRefreshing)
						return;

					MagicStorageItemSlot obj = e as MagicStorageItemSlot;

					int objSlot = obj.slot + CraftingGUI.RecipeColumns * (int)Math.Round(scrollBar.ViewPosition);

					if (objSlot >= CraftingGUI.recipes.Count)
						return;

					StoragePlayer storagePlayer = StoragePlayer.LocalPlayer;

					if (MagicStorageConfig.CraftingFavoritingEnabled && Main.keyState.IsKeyDown(Keys.LeftAlt)) {
						if (!storagePlayer.FavoritedRecipes.Add(obj.StoredItem))
							storagePlayer.FavoritedRecipes.Remove(obj.StoredItem);

						StorageGUI.SetRefresh();
					} else if (MagicStorageConfig.RecipeBlacklistEnabled && Main.keyState.IsKeyDown(Keys.LeftControl)) {
						if (recipeButtons.Choice == CraftingGUI.RecipeButtonsBlacklistChoice) {
							if (storagePlayer.HiddenRecipes.Remove(obj.StoredItem)) {
								Main.NewText(Language.GetTextValue("Mods.MagicStorage.RecipeRevealed", Lang.GetItemNameValue(obj.StoredItem.type)));

								slotZone.SetItemsAndContexts(int.MaxValue, GetRecipe);
							}
						} else {
							if (storagePlayer.HiddenRecipes.Add(obj.StoredItem)) {
								Main.NewText(Language.GetTextValue("Mods.MagicStorage.RecipeHidden", Lang.GetItemNameValue(obj.StoredItem.type)));

								slotZone.SetItemsAndContexts(int.MaxValue, GetRecipe);
							}
						}
					} else {
						CraftingGUI.SetSelectedRecipe(CraftingGUI.recipes[objSlot]);
						(parentUI as CraftingUIState).history.AddHistory(CraftingGUI.selectedRecipe);
						
						StorageGUI.RefreshUI = true;

						parentUI.UpdatePanelHeight(parentUI.PanelHeight);
					}
				};
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
