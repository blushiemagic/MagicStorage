using MagicStorage.Common.Systems;
using MagicStorage.Components;
using MagicStorage.CrossMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI.States {
	public abstract class BaseStorageUIAccessPage : BaseStorageUIPage {
		internal UISearchBar searchBar;
		internal UIText capacityText;
		internal NewUIScrollbar scrollBar;

		internal NewUISlotZone slotZone;  //The main slot zone that uses the scroll bar (e.g. recipes, items)

		//Used to order the buttons
		internal UIElement topBar;   //Search Bar, recipe buttons, Deposit All
		internal UIElement topBar2;  //Sorting options, dropdown menus
		internal UIElement topBar3;  //Filtering options

		private UIElement bottomBar;

		internal SortingOptionButtonChoice sortingButtons;
		internal FilteringOptionButtonChoice filteringButtons;

		internal UIDropdownMenu sortingDropdown;
		internal UIDropdownMenu filteringDropdown;

		public bool PendingZoneRefresh { get; private set; }
		
		internal bool pendingConfiguration;

		protected float lastKnownScrollBarViewPosition = -1;

		public BaseStorageUIAccessPage(BaseStorageUI parent, string name) : base(parent, name) {
			OnPageSelected += () => {
				StorageGUI.CheckRefresh();

				searchBar.active = true;
			};

			OnPageDeselected += () => {
				lastKnownScrollBarViewPosition = -1;
				
				slotZone.HoverSlot = -1;

				slotZone.ClearItems();

				searchBar.LoseFocus(forced: true);

				searchBar.active = false;

				sortingDropdown.Reset();
				filteringDropdown.Reset();
			};
		}

		protected float TopBar1Top {
			get => topBar.Top.Pixels;
			set => topBar.Top.Set(value, 0f);
		}

		protected float TopBar1Bottom => topBar.Top.Pixels + topBar.Height.Pixels;

		protected float TopBar2Top {
			get => topBar2.Top.Pixels;
			set => topBar2.Top.Set(value, 0f);
		}

		protected float TopBar2Bottom => topBar2.Top.Pixels + topBar2.Height.Pixels;

		protected float TopBar3Top {
			get => topBar3.Top.Pixels;
			set => topBar3.Top.Set(value, 0f);
		}

		protected float TopBar3Bottom => topBar3.Top.Pixels + topBar3.Height.Pixels;

		public override void OnInitialize() {
			topBar = new();
			topBar.Width.Set(0f, 1f);
			topBar.Height.Set(32f, 0f);
			Append(topBar);

			searchBar = new UISearchBar(Language.GetText("Mods.MagicStorage.SearchName"), StorageGUI.RefreshItems);
			topBar.Append(searchBar);

			topBar2 = new UIElement();
			topBar2.Width.Set(0f, 1f);
			topBar2.Height.Set(32f, 0f);
			topBar2.Top.Set(36f, 0f);

			topBar3 = new UIElement();
			topBar3.Width.Set(0f, 1f);
			topBar3.Height.Set(32f, 0f);
			topBar3.Top.Set(72f, 0f);

			slotZone = new(CraftingGUI.InventoryScale);

			slotZone.InitializeSlot += (slot, scale) => {
				MagicStorageItemSlot itemSlot = new(slot, scale: scale) {
					IgnoreClicks = true  // Purely visual
				};

				InitZoneSlotEvents(itemSlot);

				return itemSlot;
			};

			slotZone.OnScrollWheel += (evt, e) => scrollBar?.ScrollWheel(new(scrollBar, evt.MousePosition, evt.ScrollWheelValue));

			slotZone.Width.Set(0f, 1f);
			Append(slotZone);

			scrollBar = new();
			scrollBar.Left.Set(-20f, 1f);
			slotZone.Append(scrollBar);

			bottomBar = new();
			bottomBar.Width.Set(0f, 1f);
			Append(bottomBar);

			capacityText = new UIText("Items");
			capacityText.Left.Set(10f, 0f);
			capacityText.Top.Set(6f, 0f);

			bottomBar.Append(capacityText);

			sortingButtons = new(ModernConfigSortingButtonAction, 32, 15, onGearChoiceSelected: () => parentUI.OpenModernConfigPanel("Sorting"));
			filteringButtons = new(ModernConfigFilteringButtonAction, 32, 15, onGearChoiceSelected: () => parentUI.OpenModernConfigPanel("Filtering"));

			sortingDropdown = new(Language.GetTextValue("Mods.MagicStorage.UIPages.Sorting"), 150, 2, 250);
			sortingDropdown.Left.Set(10, 0f);
			sortingDropdown.Top = topBar2.Top;

			filteringDropdown = new(Language.GetTextValue("Mods.MagicStorage.UIPages.Filtering"), 150, 2, 250);
			filteringDropdown.Left.Set(sortingDropdown.Left.Pixels + sortingDropdown.Width.Pixels + 40, 0f);
			filteringDropdown.Top = topBar2.Top;
		}

		private void ModernConfigSortingButtonAction() => parentUI.ModernPanelButtonClicked("Sorting", sortingButtons);

		private void ModernConfigFilteringButtonAction() => parentUI.ModernPanelButtonClicked("Filtering", filteringButtons);

		protected abstract void GetZoneDimensions(out float top, out float bottomMargin);

		protected abstract float GetSearchBarRight();

		protected void AdjustCommonElements() {
			if (Main.gameMenu)
				return;

			float searchBarRight = GetSearchBarRight() + 24f;

			searchBar.Left.Set(searchBarRight + CraftingGUI.Padding, 0f);
			searchBar.Width.Set(-searchBarRight - 2 * CraftingGUI.Padding, 1f);
			searchBar.Height.Set(0f, 1f);

			searchBar.Recalculate();

			GetZoneDimensions(out float zoneTop, out float bottomMargin);

			slotZone.Top.Set(zoneTop, 0f);
			slotZone.Height.Set(-(zoneTop + bottomMargin), 1f);

			slotZone.Recalculate();
			
			bottomBar.Height.Set(bottomMargin, 0f);
			bottomBar.Top.Set(-bottomMargin, 1f);

			bottomBar.Recalculate();
		}

		public void ReformatPage(ButtonConfigurationMode current) {
			if (Main.gameMenu)
				return;

			//Top bars 2 and 3 might not be visible after reformatting
			topBar2.Remove();
			topBar2.RemoveAllChildren();

			topBar3.Remove();
			topBar3.RemoveAllChildren();

			//Dropdowns append to the page directly
			sortingDropdown.Remove();
			filteringDropdown.Remove();

			//Update the sorting and filtering button choices accordingly
			bool craftingGUI = StoragePlayer.LocalPlayer.StorageCrafting();

			switch (current) {
				case ButtonConfigurationMode.Legacy:
					sortingButtons.AssignOptions(SortingOptionLoader.BaseOptions.Where(o => o.GetDefaultVisibility(craftingGUI)));

					sortingButtons.UpdateButtonLayout(newButtonSize: 32, newMaxButtonsPerRow: 15);

					topBar2.Height = sortingButtons.Height;
					topBar2.Append(sortingButtons);

					if (MagicStorageConfig.ExtraFilterIcons) {
						filteringButtons.AssignOptions(FilteringOptionLoader.GetOptions(craftingGUI));

						filteringButtons.UpdateButtonLayout(newButtonSize: 21, newMaxButtonsPerRow: 22);
					} else {
						filteringButtons.AssignOptions(FilteringOptionLoader.BaseOptions.Where(o => o.GetDefaultVisibility(craftingGUI)));

						filteringButtons.UpdateButtonLayout(newButtonSize: 32, newMaxButtonsPerRow: 15);
					}
					
					TopBar3Top = TopBar2Bottom + 4;
					topBar3.Height = filteringButtons.Height;
					topBar3.Append(filteringButtons);

					Append(topBar2);
					Append(topBar3);
					break;
				case ButtonConfigurationMode.ModernPaged:
					//No buttons
					break;
				case ButtonConfigurationMode.ModernConfigurable:
					//This code will be executed when entering the page as well
					var sortingOptions = MagicStorageMod.Instance.optionsConfig.GetSortingOptions(craftingGUI);

					if (sortingOptions.Any()) {
						sortingButtons.AssignOptions(sortingOptions);

						sortingButtons.AutomaticallyUpdateButtonLayout();

						topBar2.Height = sortingButtons.Height;
						topBar2.Append(sortingButtons);
					} else
						topBar2.Height.Set(21, 0f);

					var filteringOptions = MagicStorageMod.Instance.optionsConfig.GetFilteringOptions(craftingGUI);

					if (filteringOptions.Any()) {
						filteringButtons.AssignOptions(filteringOptions);

						filteringButtons.AutomaticallyUpdateButtonLayout();

						topBar3.Height = filteringButtons.Height;
						topBar3.Append(filteringButtons);
					} else
						topBar3.Height.Set(21, 0f);
					
					TopBar3Top = TopBar2Bottom + 4;

					Append(topBar2);
					Append(topBar3);
					break;
				case ButtonConfigurationMode.LegacyWithGear:
				case ButtonConfigurationMode.LegacyBasicWithPaged:
					sortingButtons.AssignOptions(SortingOptionLoader.BaseOptions.Where(o => o.GetDefaultVisibility(craftingGUI)));

					sortingButtons.UpdateButtonLayout(newButtonSize: 32, newMaxButtonsPerRow: 15);

					topBar2.Height = sortingButtons.Height;
					topBar2.Append(sortingButtons);

					filteringButtons.AssignOptions(FilteringOptionLoader.BaseOptions.Where(o => o.GetDefaultVisibility(craftingGUI)));

					filteringButtons.UpdateButtonLayout(newButtonSize: 32, newMaxButtonsPerRow: 15);

					TopBar3Top = TopBar2Bottom + 4;
					topBar3.Height = filteringButtons.Height;
					topBar3.Append(filteringButtons);

					Append(topBar2);
					Append(topBar3);
					break;
				case ButtonConfigurationMode.ModernDropdown:
					//Initialize the menu
					sortingDropdown.Clear();
					sortingDropdown.AddRange(SortingOptionLoader.GetOptions(craftingGUI).Select(o => new SortingOptionElement(o)));

					//Initialize the menu
					filteringDropdown.Clear();
					filteringDropdown.AddRange(FilteringOptionLoader.GetOptions(craftingGUI).Select(o => new FilteringOptionElement(o)));

					//Can't just append them to "topBar2" since that would mess up the mouse events
					Append(sortingDropdown);
					Append(filteringDropdown);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			Recalculate();
		}

		protected abstract void InitZoneSlotEvents(MagicStorageItemSlot itemSlot);

		public override void Update(GameTime gameTime) {
			PendingZoneRefresh = false;

			if (pendingConfiguration) {
				//Use the current config just in case "pendingConfiguration" is modified where it's not intended to be
				ReformatPage(MagicStorageConfig.ButtonUIMode);
				pendingConfiguration = false;
			}

			bool oldBlock = MagicUI.BlockItemSlotActionsDetour;
			if (MagicStorageConfig.ButtonUIMode == ButtonConfigurationMode.ModernDropdown && (sortingDropdown.IsMouseHovering || filteringDropdown.IsMouseHovering))
				MagicUI.BlockItemSlotActionsDetour = false;

			base.Update(gameTime);

			MagicUI.BlockItemSlotActionsDetour = oldBlock;

			if (scrollBar.ViewPosition != lastKnownScrollBarViewPosition) {
				lastKnownScrollBarViewPosition = scrollBar.ViewPosition;
				PendingZoneRefresh = true;
			}

			TEStorageHeart heart = StoragePlayer.LocalPlayer.GetStorageHeart();
			int numItems = 0;
			int capacity = 0;
			if (heart is not null) {
				foreach (TEAbstractStorageUnit abstractStorageUnit in heart.GetStorageUnits()) {
					if (abstractStorageUnit is TEStorageUnit storageUnit) {
						numItems += storageUnit.NumItems;
						capacity += storageUnit.Capacity;
					}
				}
			}

			capacityText.SetText(Language.GetTextValue("Mods.MagicStorage.Capacity", numItems, capacity));

			if (ShouldHideItemIcons()) {
				Player player = Main.LocalPlayer;
				player.mouseInterface = true;
				player.cursorItemIconEnabled = false;
				InterfaceHelper.HideItemIconCache();
			}
		}

		public override void Draw(SpriteBatch spriteBatch) {
			bool oldBlock = MagicUI.BlockItemSlotActionsDetour;
			if (MagicStorageConfig.ButtonUIMode == ButtonConfigurationMode.ModernDropdown && (sortingDropdown.IsMouseHovering || filteringDropdown.IsMouseHovering))
				MagicUI.BlockItemSlotActionsDetour = false;

			base.Draw(spriteBatch);

			MagicUI.BlockItemSlotActionsDetour = oldBlock;
		}

		protected abstract bool ShouldHideItemIcons();
	}
}
