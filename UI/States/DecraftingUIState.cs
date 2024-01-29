using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.Shimmering;
using MagicStorage.UI.History;
using MagicStorage.UI.Shimmer;
using SerousCommonLib.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace MagicStorage.UI.States {
	public class DecraftingUIState : CraftingUIState {
		private UIElement zoneLayout;
		private UIText resultText;

		private ShimmerReportList shimmerReportList;
		
		private NewUIScrollbar reportListScrollBar;
		private NewUIScrollbar resultScrollBar;
		private const float reportListScrollBarMaxViewPosition = 4f;
		private float resultScrollBarMaxViewPosition = 2f;

		private float lastKnownResultScrollBarViewPosition = -1;

		public override string DefaultPage => "Shimmering";

		protected override IEnumerable<string> GetMenuOptions() {
			yield return "Shimmering";
			yield return "Sorting";
			yield return "Filtering";
		}

		protected override BaseStorageUIPage InitPage(string page)
			=> page switch {
				"Shimmering" => new ShimmeringPage(this),
				"Sorting" => new SortingPage(this),
				"Filtering" => new FilteringPage(this),
				_ => throw new ArgumentException("Unknown page: " + page, nameof(page))
			};

		protected override void PostInitializePages() {
			base.PostInitializePages();

			// Both zones need to take up only half of the remaining space
			zoneLayout = new UIElement();
			zoneLayout.Width.Set(0, 1f);

			recipePanel.Append(zoneLayout);

			resultText = new UIText(Language.GetText("Mods.MagicStorage.DecraftingGUI.StoredResults"));
			resultText.MaxWidth.Set(0, 1f);
			resultText.Top.Set(0, 0.5f);

			zoneLayout.Append(resultText);

			storageZone.Remove();
			resultZone.Remove();
			
			storageZone.Top.Set(0, 0f);
			storageZone.Width.Set(0, 1f);
			storageZone.Height.Set(-10, 0.5f);
			
			zoneLayout.Append(storageZone);

			resultZone.Top.Set(resultText.MinHeight.Pixels + 24, 0.5f);
			resultZone.Width.Set(0, 1f);
			resultZone.Height.Set(-resultText.MinHeight.Pixels - 24, 0.5f);
			
			zoneLayout.Append(resultZone);

			// Result zone is expanded to a slot zone for transformed items and results from decrafting
			resultZone.SetDimensions(CraftingGUI.IngredientColumns, 3);

			resultZone.Recalculate();

			InitializeScrollBar(resultZone, ref resultScrollBar, resultScrollBarMaxViewPosition);

			// Ingredient zone is replaced by the shimmer report list
			ingredientZone.Remove();
			ingredientScrollBar.Remove();

			ingredientText.SetText(Language.GetText("Mods.MagicStorage.DecraftingGUI.ShimmeringReports"));

			shimmerReportList = new ShimmerReportList();
			shimmerReportList.Width.Set(0, 1f);
			shimmerReportList.Height.Set(32 * 3 + 10, 1f);  // 3 rows

			recipePanel.Append(shimmerReportList);
		}

		public override int GetSortingOption() => GetPage<SortingPage>("Sorting").option;
		
		public override int GetFilteringOption() => GetPage<FilteringPage>("Filtering").option;

		public override string GetSearchText() => GetPage<ShimmeringPage>("Shimmering").searchBar.Text;

		// Crafting UI overrides

		protected override bool CanShowAllIngredientsToggle() => false;

		protected override UICraftButton CreateCraftButton() => new UIShimmerButton(Language.GetText("Mods.MagicStorage.DecraftingGUI.ShimmerButton"), "DecraftingGUI.ShimmerButtonTooltip");

		protected override IHistoryCollection CreateHistory() => new ShimmerHistory();

		// DepositItem not overridden since the implementation would be the same here

		protected override Item GetHeader(int slot, ref int context) => DecraftingGUI.GetHeader(slot, ref context);

		protected override Item GetIngredient(int slot, ref int context) => null;  // Zone is no longer used

		protected override int GetIngredientCount() => -1;  // Zone is no longer used

		protected override float GetRecipePanelMinimumHeight() {
			float smallSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.SmallScale;

			float reportListHeight = shimmerReportList.Height.Pixels;

			float reqObjTextTop = ingredientZoneTop + reportListHeight + 20;
			float reqObjText2Top = reqObjTextTop + 24;

			float storedItemsTextTop = reqObjText2Top + 30 * reqObjTextLines.Count;
			float storageZoneTop = storedItemsTextTop + 24;

			float zoneHeight = smallSlotHeight + 10 + resultText.MinHeight.Pixels + 24 + smallSlotHeight;

			return storageZoneTop + zoneHeight + 60 + AMOUNT_BUTTON_HEIGHT;
		}

		protected override Item GetResult(int slot, ref int context) {
			if (MagicUI.CurrentlyRefreshing)
				return new Item();

			int index = slot + CraftingGUI.IngredientColumns * (int)Math.Round(resultScrollBar.ViewPosition);
			Item item = index < DecraftingGUI.resultItems.Count ? DecraftingGUI.resultItems[index] : new Item();
			return item;
		}

		// GetStorage not overridden since the implementation would be the same here

		protected override bool HaveZonesChangedDueToScrolling() {
			return base.HaveZonesChangedDueToScrolling() || resultScrollBar.ViewPosition != lastKnownResultScrollBarViewPosition;
		}

		protected override bool IsPanelTooShortToContainContents() {
			if (base.IsPanelTooShortToContainContents())
				return true;

			GetResultZoneRows(out int totalRows, out int displayRows);

			return totalRows > 0 && displayRows <= 0;
		}

		protected override void RecalculateIngredientZone(int ingredientRows, ref float reqObjTextTop) {
			ResetReportList();

			shimmerReportList.Top.Set(ingredientZoneTop, 0f);

			shimmerReportList.Recalculate();

			reportListScrollBar.SetView(CraftingGUI.ScrollBar2ViewSize, reportListScrollBarMaxViewPosition);

			reportListScrollBar.Recalculate();

			reqObjTextTop += shimmerReportList.Height.Pixels + 20;
		}

		private void ResetReportList() {
			if (DecraftingGUI.selectedItem == -1) {
				shimmerReportList.Clear();
				shimmerReportList.Add(new NoResultReport());
			} else {
				shimmerReportList.Clear();
				shimmerReportList.Add(MagicCache.ShimmerInfos[DecraftingGUI.selectedItem].GetShimmerReports());
			}
		}

		protected override void RecalculateScrollBars() {
			base.RecalculateScrollBars();

			GetResultZoneRows(out int totalRows, out int displayRows);

			UpdateZoneAndScroll(resultZone, resultScrollBar, totalRows, displayRows, ref resultScrollBarMaxViewPosition);

			lastKnownResultScrollBarViewPosition = resultScrollBar.ViewPosition;
		}

		private void GetResultZoneRows(out int totalRows, out int displayRows) {
			float smallSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.SmallScale;

			totalRows = (DecraftingGUI.resultItems.Count + CraftingGUI.IngredientColumns - 1) / CraftingGUI.IngredientColumns;
			displayRows = (int)resultZone.GetDimensions().Height / ((int)smallSlotHeight + CraftingGUI.Padding);
		}

		protected override void RecalculateStorageZone(float storageZoneTop) {
			// Recalculate the area containing the zones instead
			zoneLayout.Top.Set(storageZoneTop, 0f);

			zoneLayout.Height.Set(-storageZoneTop - 60 - AMOUNT_BUTTON_HEIGHT, 1f);

			zoneLayout.Recalculate();
		}

		protected override void RecalculateResultZone(float storageZoneBottom) {
			// Do nothing; the zone will be handled in RecalculateScrollBars instead
		}

		protected override void RefreshZonesFromScrolling() {
			base.RefreshZonesFromScrolling();

			ResetReportList();
		}

		protected override void RefreshZonesFromThreadStart() {
			base.RefreshZonesFromThreadStart();

			shimmerReportList.Clear();
		}

		protected override void ResetScrollBarMemory() {
			base.ResetScrollBarMemory();

			lastKnownResultScrollBarViewPosition = -1;
		}

		protected override void SlotFocus(out RefContainer<bool> flag, out Action updateFocus, out Action resetFocus) {
			flag = new(ref DecraftingGUI.hasSlotFocus);
			updateFocus = DecraftingGUI.SlotFocusLogic;
			resetFocus = DecraftingGUI.ResetSlotFocus;
		}

		protected override void UpdateRecipeText() {
			if (MagicUI.CurrentlyRefreshing)
				return;  // Do not read anything until refreshing is completed

			foreach (var line in reqObjTextLines)
				line.Remove();
			reqObjTextLines.Clear();

			if (DecraftingGUI.selectedItem > ItemID.None) {
				bool isEmpty = true;
				StringBuilder text = new();

				if (ShimmerMetrics.GetTransformCondition(DecraftingGUI.selectedItem) is Condition condition)
					AddObjectTextToBuilder(text, condition.Description.Value, ref isEmpty);

				if (ShimmerMetrics.GetDecraftingRecipeFor(DecraftingGUI.selectedItem) is Recipe recipe) {
					foreach (var decraftCondition in recipe.DecraftConditions)
						AddObjectTextToBuilder(text, decraftCondition.Description.Value, ref isEmpty);
				}

				if (isEmpty) {
					var line = new UIText(Language.GetTextValue("LegacyInterface.23")) {
						DynamicallyScaleDownToWidth = true
					};

					reqObjTextLines.Add(line);
				}
			}
		}

		protected override Item WithdrawItem(Item item, bool toInventory) => DecraftingGUI.DoWithdraw(item, toInventory);

		public class ShimmeringPage : RecipesPage {
			public ShimmeringPage(BaseStorageUI parent) : base(parent, "Shimmering") { }

			public override void OnInitialize() {
				base.OnInitialize();

				stationText.Remove();
				stationZone.Remove();

				stationZone.SetDimensions(0, 0);  // Basically destroy the zone since it will not be used
			}

			// RecipesPage overrides

			protected override ItemTypeOrderedSet GetFavoriteSet(StoragePlayer storagePlayer) => storagePlayer.FavoritedShimmerItems;

			protected override HashSet<ItemDefinition> GetGlobalBlacklistSet() => MagicStorageConfig.GlobalShimmerItemBlacklist;

			protected override string GetGlobalBlacklistSetHideLocalizationKey() => "Mods.MagicStorage.DecraftingGUI.ItemHiddenGlobal";

			protected override string GetGlobalBlacklistSetRevealLocalizationKey() => "Mods.MagicStorage.DecraftingGUI.ItemRevealedGlobal";

			protected override ItemTypeOrderedSet GetHiddenSet(StoragePlayer storagePlayer) => storagePlayer.HiddenShimmerItems;

			protected override string GetHiddenSetHideLocalizationKey() => "Mods.MagicStorage.DecraftingGUI.ItemHidden";

			protected override string GetHiddenSetRevealLocalizationKey() => "Mods.MagicStorage.DecraftingGUI.ItemRevealed";

			protected override Item GetMainZoneItem(int slot, ref int context) {
				if (MagicUI.CurrentlyRefreshing)
					return new Item();

				int index = slot + CraftingGUI.RecipeColumns * (int)Math.Round(scrollBar.ViewPosition);

				// Fail early if the index is invalid
				if (index < 0 || index >= DecraftingGUI.viewingItems.Count)
					return new Item();

				try {
					int type = DecraftingGUI.viewingItems[index];
					bool available = DecraftingGUI.itemAvailable[index];

					Item item = ContentSamples.ItemsByType[type];

					// Air item should display as an "empty slot" without special contexts
					if (!item.IsAir) {
						bool selected = type == DecraftingGUI.selectedItem;

						if (selected)
							context = 6;

						if (!available)
							context = selected ? 4 : 3;
					
						if (MagicStorageConfig.CraftingFavoritingEnabled && StoragePlayer.LocalPlayer.FavoritedShimmerItems.Contains(item)) {
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

			protected override Item GetStation(int slot, ref int context) => null;  // Unused

			protected override int GetStationCount() => -1;  // Unused

			protected override int GetZoneItemCount() => MagicUI.CurrentlyRefreshing ? 0 : DecraftingGUI.viewingItems?.Count ?? 0;

			protected override void OnMainZoneItemBlacklistChanged(Item item, bool blacklisted) {
				DecraftingGUI.SetNextDefaultItemCollectionToRefresh(item.type);
			}

			protected override void OnMainZoneItemFavoriteChanged(Item item) {
				DecraftingGUI.SetNextDefaultItemCollectionToRefresh(item.type);
				DecraftingGUI.forceSpecificItemResort = true;
			}

			// OnMainZoneItemGlobalBlacklistChanged not overridden since the implementation would be the same here

			// OnMainZoneItemHiddenSetChanged not overridden since the implementation would be the same here

			protected override void OnMainZoneItemLeftClicked(int index) {
				DecraftingGUI.SetSelectedItem(DecraftingGUI.viewingItems[index]);
				(parentUI as DecraftingUIState).history.AddHistory(DecraftingGUI.selectedItem);
			}

			protected override void UpdateStationElements(out int stationCount) {
				// Don't let the elements be updated since they are not used
				stationCount = -1;
			}
		}
	}
}
