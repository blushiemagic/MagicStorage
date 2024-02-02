using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Components;
using MagicStorage.UI.History;
using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader.Config;

namespace MagicStorage.UI.States {
	partial class CraftingUIState {
		protected virtual bool CanShowAllIngredientsToggle() => true;

		protected virtual void ClampCraftAmount() => CraftingGUI.ClampCraftAmount();

		protected virtual UICraftButton CreateCraftButton() => new UICraftButton(Language.GetText("LegacyMisc.72"), "CraftTooltip");

		protected virtual IHistoryCollection CreateHistory() => new RecipeHistory();

		protected virtual bool DepositItem(Item item) => CraftingGUI.TryDepositResult(item);

		protected virtual string GetCraftAmountLocalizationKey() => "Mods.MagicStorage.Crafting.Amount";

		protected virtual Item GetHeader(int slot, ref int context) => CraftingGUI.GetHeader(slot, ref context);

		protected virtual Item GetIngredient(int slot, ref int context) => CraftingGUI.GetIngredient(slot, ref context);

		protected virtual int GetIngredientCount() => CraftingGUI.selectedRecipe?.requiredItem.Count ?? CraftingGUI.IngredientColumns;

		protected virtual float GetRecipePanelMinimumHeight() {
			float smallSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.SmallScale;

			int itemsNeeded = GetIngredientCount();
			int totalRows = GetIngredientRows(itemsNeeded);
			int displayRows = GetIngredientDisplayRows(totalRows);

			float ingredientZoneHeight = 30f * displayRows;

			float reqObjTextTop = ingredientZoneTop + ingredientZoneHeight + 11 * displayRows;
			float reqObjText2Top = reqObjTextTop + 24;

		//	int reqObjText2Rows = reqObjText2.Text.Count(c => c == '\n') + 1;
			float storedItemsTextTop = reqObjText2Top + reqObjTextHolder.Height.Pixels;
			float storageZoneTop = storedItemsTextTop + 24;

			if (MagicStorageConfig.IsRecursionEnabled)
				storageZoneTop += recursionButton.Height.Pixels + 10;

			return storageZoneTop + smallSlotHeight + CraftingGUI.Padding + 68;
		}

		protected virtual Item GetResult(int slot, ref int context) => CraftingGUI.GetResult(slot, ref context);

		protected virtual Item GetStorage(int slot, ref int context) => GetStorageItem(storageScrollBar, slot, ref context);

		protected virtual bool HaveZonesChangedDueToScrolling() => storageScrollBar.ViewPosition != lastKnownScrollBarViewPosition || ingredientScrollBar.ViewPosition != lastKnownIngredientScrollBarViewPosition;

		protected virtual bool IsPanelTooShortToContainContents() {
			GetStorageZoneRows(out int numRows2, out int displayRows2);

			return numRows2 > 0 && displayRows2 <= 0;
		}

		protected virtual void RecalculateIngredientZone(int ingredientRows, ref float reqObjTextTop) {
			int ingredientDisplayRows = GetIngredientDisplayRows(ingredientRows);
			float ingredientZoneHeight = 30f * ingredientDisplayRows;

			ingredientZone.Top.Set(ingredientZoneTop, 0f);

			UpdateZoneAndScroll(ingredientZone, ingredientScrollBar, ingredientRows, ingredientDisplayRows, CraftingGUI.ScrollBar2ViewSize, ref ingredientScrollBarMaxViewSize);

			ingredientZone.Height.Set(ingredientZone.ZoneHeight, 0f);
			
			ingredientZone.Recalculate();

			reqObjTextTop += ingredientZoneHeight + 11 * ingredientDisplayRows;
		}

		protected virtual void RecalculateScrollBars() {
			GetStorageZoneRows(out int numRows2, out int displayRows2);

			UpdateZoneAndScroll(storageZone, storageScrollBar, numRows2, displayRows2, CraftingGUI.ScrollBar2ViewSize, ref storageScrollBarMaxViewSize);

			lastKnownScrollBarViewPosition = storageScrollBar.ViewPosition;
			lastKnownIngredientScrollBarViewPosition = ingredientScrollBar.ViewPosition;
		}

		protected virtual void RecalculateStorageZone(float storageZoneTop) {
			storageZone.Top.Set(storageZoneTop, 0f);

			storageZone.Height.Set(-storageZoneTop - 60 - AMOUNT_BUTTON_HEIGHT, 1f);

			storageZone.Recalculate();
		}

		protected virtual void RecalculateResultZone(float storageZoneBottom) {
			float itemSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.InventoryScale;
			float itemSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.InventoryScale;

			float top = storageZoneBottom + 10;

			resultZone.Top.Set(top, 0f);

			resultZone.Width.Set(itemSlotWidth, 0f);
			resultZone.Height.Set(itemSlotHeight, 0f);

			resultZone.Recalculate();
		}

		protected virtual void RefreshZonesFromScrolling() {
			ingredientZone.SetItemsAndContexts(int.MaxValue, GetIngredient);
			storageZone.SetItemsAndContexts(int.MaxValue, GetStorage);
			resultZone.SetItemsAndContexts(int.MaxValue, GetResult);
		}

		protected virtual void RefreshZonesFromThreadStart() {
			// Clear out the contexts and items
			ingredientZone.SetItemsAndContexts(int.MaxValue, NullItem);

			storageZone.SetItemsAndContexts(int.MaxValue, NullItem);

			recipeHeaderZone.SetItemsAndContexts(1, NullItem);

			resultZone.SetItemsAndContexts(int.MaxValue, NullItem);
		}

		protected virtual void ResetScrollBarMemory() {
			lastKnownIngredientRows = -1;
			lastKnownScrollBarViewPosition = -1;
			lastKnownIngredientScrollBarViewPosition = -1;
		}

		protected virtual void SlotFocus(out RefContainer<bool> flag, out Action updateFocus, out Action resetFocus) {
			flag = new(ref CraftingGUI.slotFocus);
			updateFocus = CraftingGUI.SlotFocusLogic;
			resetFocus = CraftingGUI.ResetSlotFocus;
		}

		protected virtual void UpdateRecipeText() {
			if (MagicUI.CurrentlyRefreshing)
				return;  // Do not read anything until refreshing is completed

			ClearObjectText();

			if (CraftingGUI.selectedRecipe is not null) {
				bool isEmpty = true;
				StringBuilder text = new();

				IEnumerable<int> requiredTiles;
				IEnumerable<Condition> conditions;
				bool useRecursion = MagicStorageConfig.IsRecursionEnabled && CraftingGUI.selectedRecipe.HasRecursiveRecipe();
				if (useRecursion && CraftingGUI.GetCraftingSimulationForCurrentRecipe() is CraftingSimulation { AmountCrafted: > 0 } simulation) {
					requiredTiles = simulation.RequiredTiles;
					conditions = simulation.RequiredConditions;
				} else {
					// Not using recursion crafting or the recursion simulation produced no results
					// Use the original recipe as a fallback
					requiredTiles = CraftingGUI.selectedRecipe.requiredTile;
					conditions = CraftingGUI.selectedRecipe.Conditions;

					CraftingGUI.lastKnownRecursionErrorForObjects = MagicStorageConfig.IsRecursionEnabled
						? Language.GetTextValue("Mods.MagicStorage.CraftingGUI.RecursionErrors.NoObjects")
						: null;
				}

				foreach (int tile in requiredTiles)
					AddObjectTextToBuilder(text, Lang.GetMapObjectName(MapHelper.TileToLookup(tile, 0)), ref isEmpty);

				foreach (Condition condition in conditions)
					AddObjectTextToBuilder(text, condition.Description.Value, ref isEmpty);

				if (isEmpty)
					AppendNoneObjectText();
				else
					AppendObjectText(text.ToString());
			} else {
				// Recipe isn't available
				AppendNoneObjectText();
			}
		}

		protected virtual Item WithdrawItem(Item item, bool toInventory) => CraftingGUI.DoWithdrawResult(item.stack, toInventory);

		partial class RecipesPage {
			protected virtual ItemTypeOrderedSet GetFavoriteSet(StoragePlayer storagePlayer) => storagePlayer.FavoritedRecipes;

			protected virtual HashSet<ItemDefinition> GetGlobalBlacklistSet() => MagicStorageConfig.GlobalRecipeBlacklist;

			protected virtual string GetGlobalBlacklistSetHideLocalizationKey() => "Mods.MagicStorage.RecipeHiddenGlobal";

			protected virtual string GetGlobalBlacklistSetRevealLocalizationKey() => "Mods.MagicStorage.RecipeRevealedGlobal";

			protected virtual ItemTypeOrderedSet GetHiddenSet(StoragePlayer storagePlayer) => storagePlayer.HiddenRecipes;

			protected virtual string GetHiddenSetHideLocalizationKey() => "Mods.MagicStorage.RecipeHidden";

			protected virtual string GetHiddenSetRevealLocalizationKey() => "Mods.MagicStorage.RecipeRevealed";

			protected virtual Item GetMainZoneItem(int slot, ref int context) {
				if (MagicUI.CurrentlyRefreshing)
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
					
						if (MagicStorageConfig.CraftingFavoritingEnabled && StoragePlayer.LocalPlayer.FavoritedRecipes.Contains(item)) {
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

			protected virtual Item GetStation(int slot, ref int context) => CraftingGUI.GetStation(slot, ref context);

			protected virtual int GetStationCount() => CraftingGUI.GetCraftingStations().Count;

			protected virtual int GetZoneItemCount() => MagicUI.CurrentlyRefreshing ? 0 : CraftingGUI.recipes?.Count ?? 0;

			protected virtual void OnMainZoneItemBlacklistChanged(Item item, bool blacklisted) {
				CraftingGUI.SetNextDefaultRecipeCollectionToRefresh(item.type);
			}

			protected virtual void OnMainZoneItemFavoriteChanged(Item item) {
				CraftingGUI.SetNextDefaultRecipeCollectionToRefresh(item.type);
				CraftingGUI.forceSpecificRecipeResort = true;
			}

			protected virtual void OnMainZoneItemGlobalBlacklistChanged(Item item, bool blacklisted) => Utility.SaveModConfig(MagicStorageConfig.Instance);

			protected virtual void OnMainZoneItemHiddenSetChanged(Item item, bool hidden) { }

			protected virtual void OnMainZoneItemLeftClicked(int index) {
				CraftingGUI.SetSelectedRecipe(CraftingGUI.recipes[index]);
				(parentUI as CraftingUIState).history.AddHistory(CraftingGUI.selectedRecipe);
			}

			protected virtual void UpdateStationElements(out int stationCount) {
				stationCount = CraftingGUI.GetCraftingStations().Count;
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
			}
		}
	}
}
