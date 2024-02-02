using MagicStorage.Common.Systems;
using MagicStorage.Common;
using SerousCommonLib.UI;
using Terraria;
using Terraria.UI;
using Microsoft.Xna.Framework.Input;
using Terraria.Audio;
using Terraria.ID;
using System.Text;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent;
using Terraria.UI.Chat;
using Microsoft.Xna.Framework;
using System;

namespace MagicStorage.UI.States {
	partial class CraftingUIState {
		protected void AddObjectTextToBuilder(StringBuilder text, string s, ref bool isEmpty) {
			if (!isEmpty)
				text.Append(',');

			Vector2 size = ChatManager.GetStringSize(FontAssets.MouseText.Value, text.ToString() + " " + s, Vector2.One);

			if (size.X > recipeWidth) {
				// Line has exceeded the width of the recipe panel.  Add a new text object and reset the string builder
				UIText line = new(text.ToString()) {
					DynamicallyScaleDownToWidth = true
				};

				reqObjTextLines.Add(line);

				text.Clear().Append(s);
			} else {
				// Line still has enough room for the next string
				if (!isEmpty)
					text.Append(' ');

				text.Append(s);
			}

			isEmpty = false;
		}

		private static int GetIngredientRows(int itemsNeeded) {
			int recipeRows = itemsNeeded / CraftingGUI.IngredientColumns;
			int extraRow = itemsNeeded % CraftingGUI.IngredientColumns != 0 ? 1 : 0;
			int totalRows = recipeRows + extraRow;
			if (totalRows < 1)
				totalRows = 1;

			return totalRows;
		}

		private static int GetIngredientDisplayRows(int totalRows) {
			if (totalRows < 3)
				return totalRows;

			// Maximum row count ranges from 3 to 8 depending on the screen height
			float actualHeight = Main.screenHeight / Main.UIScale;
			int maxRows = Utils.Clamp((int)Math.Round((actualHeight - 600) / 100f), 3, 8);
			return Math.Min(totalRows, maxRows);
		}

		protected static void GetMainPanelMinimumHeights(BaseStorageUIAccessPage mainPage, out float mainPageMinimumHeight, out float dropdownHeight) {
			mainPage.GetZoneDimensions(out float zoneTop, out float bottomMargin);

			float itemSlotHeight = TextureAssets.InventoryBack.Value.Height * StorageGUI.inventoryScale;

			mainPageMinimumHeight = zoneTop + itemSlotHeight + StorageGUI.padding + bottomMargin;

			if (MagicStorageConfig.ButtonUIMode == ButtonConfigurationMode.ModernDropdown) {
				dropdownHeight = mainPage.sortingDropdown.MaxExpandedHeight;

				dropdownHeight += mainPage.topBar2.Top.Pixels;
			} else
				dropdownHeight = 0;
		}

		protected static Item GetStorageItem(NewUIScrollbar scroll, int slot, ref int context) {
			if (MagicUI.CurrentlyRefreshing)
				return new Item();

			int index = slot + CraftingGUI.IngredientColumns * (int)Math.Round(scroll.ViewPosition);
			Item item = index < CraftingGUI.storageItems.Count ? CraftingGUI.storageItems[index] : new Item();
			if (CraftingGUI.blockStorageItems.Contains(new ItemData(item)))
				context = ItemSlot.Context.ChestItem;  // Red
			return item;
		}

		protected static void HandleStorageSlotLeftClick(NewUISlotZone zone, NewUIScrollbar scrollbar, MagicStorageItemSlot slot, int setItemCount, UISlotZone.GetItem getItem) {
			// Prevent actions while refreshing the items
			if (MagicUI.CurrentlyRefreshing)
				return;

			int index = slot.id + CraftingGUI.IngredientColumns * (int)Math.Round(scrollbar.ViewPosition);

			if (index >= CraftingGUI.storageItems.Count)
				return;

			ItemData data = new(slot.StoredItem);
			if (CraftingGUI.blockStorageItems.Contains(data))
				CraftingGUI.blockStorageItems.Remove(data);
			else
				CraftingGUI.blockStorageItems.Add(data);

			// Force the "recipe available" logic to update
			CraftingGUI.ResetRecentRecipeCache();

			zone.SetItemsAndContexts(setItemCount, getItem);
		}

		protected void HandleResultSlotLeftClick(NewUISlotZone zone, MagicStorageItemSlot slot, int setItemCount, UISlotZone.GetItem getItem) {
			// Prevent actions while refreshing the items
			if (MagicUI.CurrentlyRefreshing)
				return;

			Item item = slot.StoredItem;

			if (Main.mouseItem.IsAir && item is not null && !item.IsAir) {
				bool shiny = item.newAndShiny;

				item.newAndShiny = false;

				if (shiny)
					zone.SetItemsAndContexts(setItemCount, getItem);
			}

			Player player = Main.LocalPlayer;

			bool changed = false;
			if (!Main.mouseItem.IsAir && player.itemAnimation == 0 && player.itemTime == 0 && item is not null && Main.mouseItem.type == item.type) {
				if (DepositItem(Main.mouseItem))
					changed = true;
			} else if (Main.mouseItem.IsAir && item?.IsAir is false) {
				if (Main.keyState.IsKeyDown(Keys.LeftAlt)) {
					item.favorited = !item.favorited;
					zone.SetItemsAndContexts(setItemCount, getItem);
					MagicUI.SetRefresh();
				} else {
					Main.mouseItem = WithdrawItem(item, ItemSlot.ShiftInUse);
							
					if (ItemSlot.ShiftInUse)
						Main.mouseItem = player.GetItem(Main.myPlayer, Main.mouseItem, GetItemSettings.InventoryEntityToPlayerInventorySettings);
							
					changed = true;
				}
			}

			if (changed) {
				MagicUI.SetRefresh();

				SoundEngine.PlaySound(SoundID.Grab);

				slot.IgnoreNextHandleAction = true;
			}
		}

		protected void HandleResultSlotRightHold(MagicStorageItemSlot slot) {
			// Prevent actions while refreshing the items
			if (MagicUI.CurrentlyRefreshing)
				return;

			Item result = slot.StoredItem;

			SlotFocus(out var flag, out var updateFocus, out _);

			if (result is not null && !result.IsAir && (Main.mouseItem.IsAir || ItemCombining.CanCombineItems(Main.mouseItem, result) && Main.mouseItem.stack < Main.mouseItem.maxStack))
				flag.Value = true;

			if (flag.Value) {
				updateFocus();
				slot.IgnoreNextHandleAction = true;
			}
		}

		protected static void InitializeScrollBar(UIElement parent, ref NewUIScrollbar scrollBar, float maxView) {
			scrollBar = new(scrollDividend: 250f);
			scrollBar.Left.Set(-20f, 1f);
			scrollBar.SetView(CraftingGUI.ScrollBar2ViewSize, maxView);
			parent.Append(scrollBar);
		}

		protected static Item NullItem(int slot, ref int context) {
			context = ItemSlot.Context.InventoryItem;
			return new Item();
		}

		private void RightClickIngredient(MagicStorageItemSlot slot) {
			// Prevent actions while refreshing the items
			if (MagicUI.CurrentlyRefreshing)
				return;

			if (CraftingGUI.selectedRecipe is null)
				return;

			if (slot.id >= CraftingGUI.selectedRecipe.requiredItem.Count)
				return;

			// select ingredient recipe by right clicking
			Item item = CraftingGUI.selectedRecipe.requiredItem[slot.id];
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
				MagicUI.SetRefresh();

				UpdatePanelHeight(PanelHeight);

				history.AddHistory(CraftingGUI.selectedRecipe);
			}
		}

		protected static void UpdateZoneAndScroll(NewUISlotZone zone, NewUIScrollbar scroll, int totalRows, int displayRows, float viewSize, ref float maxViewSize) {
			float smallSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.SmallScale;

			zone.SetDimensions(CraftingGUI.IngredientColumns, displayRows);

			int noDisplayRows2 = totalRows - displayRows;
			if (noDisplayRows2 < 0)
				noDisplayRows2 = 0;

			maxViewSize = 1 + noDisplayRows2;
			scroll.Height.Set(displayRows * (smallSlotHeight + CraftingGUI.Padding), 0f);
			scroll.SetView(viewSize, maxViewSize);

			scroll.Recalculate();
		}

		partial class RecipesPage {
			private float GetZoneTop() {
				return MagicStorageConfig.ButtonUIMode switch {
					ButtonConfigurationMode.Legacy
					or ButtonConfigurationMode.ModernConfigurable
					or ButtonConfigurationMode.LegacyWithGear
					or ButtonConfigurationMode.LegacyBasicWithPaged => TopBar3Bottom,
					ButtonConfigurationMode.ModernPaged => TopBar1Bottom,
					ButtonConfigurationMode.ModernDropdown => TopBar2Bottom,
					_ => throw new ArgumentOutOfRangeException()
				};
			}
		}
	}
}
