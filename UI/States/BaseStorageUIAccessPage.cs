using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Components;
using MagicStorage.CrossMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI.States {
	public abstract class BaseStorageUIAccessPage : BaseStorageUIPage {
		public UISearchBar searchBar;
		public UIText capacityText;
		public NewUIScrollbar scrollBar;
		public ModSearchBox modSearchBox;

		public NewUISlotZone slotZone;  //The main slot zone that uses the scroll bar (e.g. recipes, items)

		private UIPanel waitPanel;
		private UIText waitText;
		protected bool isWaitPanelWaitingToOpen;

		protected bool IsWaitTextVisible => waitPanel.Parent is null;

		//Used to order the buttons
		public UIElement topBar;   //Search Bar, recipe buttons, Deposit All
		public UIElement topBar2;  //Sorting options, dropdown menus
		public UIElement topBar3;  //Filtering options

		public UIElement bottomBar;

		public SortingOptionButtonChoice sortingButtons;
		public FilteringOptionButtonChoice filteringButtons;

		public UIDropdownMenu sortingDropdown;
		public UIDropdownMenu filteringDropdown;

		public bool PendingZoneRefresh { get; private set; }
		
		internal bool pendingConfiguration;

		protected float lastKnownScrollBarViewPosition = -1;

		public BaseStorageUIAccessPage(BaseStorageUI parent, string name) : base(parent, name) {
			OnPageSelected += () => {
				searchBar.active = true;

				//Search bar text is affected by this call
				modSearchBox.Reset(false);

				// Ensure that the UI is refreshed completely
				StorageGUI.SetRefresh(forceFullRefresh: true);
			};

			OnPageDeselected += () => {
				lastKnownScrollBarViewPosition = -1;
				
				slotZone.HoverSlot = -1;

				slotZone.ClearItems();

				searchBar.LoseFocus(forced: true);

				searchBar.active = false;

				sortingDropdown.Reset();
				filteringDropdown.Reset();

				modSearchBox.Reset(true);
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
			base.OnInitialize();

			topBar = new();
			topBar.Width.Set(0f, 1f);
			topBar.Height.Set(32f, 0f);
			Append(topBar);

			searchBar = new UISearchBar(Language.GetText("Mods.MagicStorage.SearchName"), static () => StorageGUI.SetRefresh(forceFullRefresh: true)) {
				GetHoverText = () => {
					return modSearchBox.ModIndex == ModSearchBox.ModIndexAll
						? Language.GetTextValue("Mods.MagicStorage.SearchTips.TipModAndTooltip")
						: Language.GetTextValue("Mods.MagicStorage.SearchTips.TipTooltipOnly");
				}
			};
			topBar.Append(searchBar);

			topBar2 = new UIElement();
			topBar2.Width.Set(0f, 1f);
			topBar2.Height.Set(21, 0f);
			topBar2.Top.Set(36f, 0f);

			modSearchBox = new(ModSearchChanged, 0.72f);
			modSearchBox.Left.Set(-190, 1f);
			modSearchBox.Width.Set(190, 0f);
			modSearchBox.Height.Set(21, 0f);
			modSearchBox.OverflowHidden = true;
			modSearchBox.PaddingTop = 3;
			modSearchBox.PaddingBottom = 2;

			topBar3 = new UIElement();
			topBar3.Width.Set(0f, 1f);
			topBar3.Height.Set(21, 0f);
			topBar3.Top.Set(72f, 0f);

			slotZone = new(CraftingGUI.InventoryScale);

			slotZone.InitializeSlot += (slot, scale) => {
				MagicStorageItemSlot itemSlot = new(slot, scale: scale) {
					IgnoreClicks = true  // Purely visual
				};

				InitZoneSlotEvents(itemSlot);

				return itemSlot;
			};

			slotZone.OnScrollWheel += (evt, e) => {
				if (scrollBar is not null)
					scrollBar.ViewPosition -= evt.ScrollWheelValue / scrollBar.ScrollDividend;
			};

			slotZone.Width.Set(0f, 1f);
			Append(slotZone);

			waitPanel = new UIPanel();

			waitPanel.Left.Set(0f, 0.1f);
			waitPanel.Top.Set(20f, 0f);
			waitPanel.Width.Set(0f, 0.8f);
			waitPanel.Height.Set(80f, 0f);

			waitText = new UIText(Language.GetText("Mods.MagicStorage.SortWaiting"), large: true) {
				HAlign = 0.5f,
				VAlign = 0.5f
			};

			waitPanel.Append(waitText);

			scrollBar = new(scrollDividend: 250f);
			scrollBar.Left.Set(-20f, 1f);
			slotZone.Append(scrollBar);

			bottomBar = new();
			bottomBar.Width.Set(0f, 1f);
			Append(bottomBar);

			capacityText = new UIText("Items");
			capacityText.Left.Set(10f, 0f);
			capacityText.Top.Set(6f, 0f);

			bottomBar.Append(capacityText);

			sortingButtons = new(ModernConfigSortingButtonAction, 21, 15, onGearChoiceSelected: () => parentUI.OpenModernConfigPanel("Sorting"));
			filteringButtons = new(ModernConfigFilteringButtonAction, 21, 22, onGearChoiceSelected: () => parentUI.OpenModernConfigPanel("Filtering"));

			sortingDropdown = new(Language.GetTextValue("Mods.MagicStorage.UIPages.Sorting"), 135, 2, 250);
			sortingDropdown.Left.Set(10, 0f);
			sortingDropdown.Top = topBar2.Top;

			filteringDropdown = new(Language.GetTextValue("Mods.MagicStorage.UIPages.Filtering"), 135, 2, 250);
			filteringDropdown.Left.Set(sortingDropdown.Left.Pixels + sortingDropdown.Width.Pixels + 30, 0f);
			filteringDropdown.Top = topBar2.Top;
		}

		private void ModSearchChanged(int old, int index) {
			bool oldNeedsMod = old == ModSearchBox.ModIndexAll;
			bool needsMod = index == ModSearchBox.ModIndexAll;

			if (oldNeedsMod != needsMod)
				searchBar.SetDefaultText(GetRandomSearchText(needsMod));

			StorageGUI.SetRefresh(forceFullRefresh: true);
		}

		private static readonly LocalizedText[] searchTextDefaults = new[] {
			Language.GetText("Mods.MagicStorage.SearchTips.SearchMod"),
			Language.GetText("Mods.MagicStorage.SearchTips.SearchModAndTooltip"),
		};

		private static readonly LocalizedText[] searchTextDefaultsWithoutMod = new[] {
			Language.GetText("Mods.MagicStorage.SearchTips.SearchName"),
			Language.GetText("Mods.MagicStorage.SearchTips.SearchTooltip"),
		};

		public static LocalizedText GetRandomSearchText(bool includeMod) => Main.rand.Next(includeMod ? searchTextDefaults : searchTextDefaultsWithoutMod);

		private void ModernConfigSortingButtonAction() => parentUI.ModernPanelButtonClicked("Sorting", sortingButtons);

		private void ModernConfigFilteringButtonAction() => parentUI.ModernPanelButtonClicked("Filtering", filteringButtons);

		public abstract void GetZoneDimensions(out float top, out float bottomMargin);

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

			waitPanel.Top.Set(zoneTop + 20f, 0f);

			waitPanel.Recalculate();
			
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

			topBar2.Append(modSearchBox);

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

					sortingButtons.UpdateButtonLayout(newButtonSize: 21, newMaxButtonsPerRow: 15);

					topBar2.Height = sortingButtons.Height;
					topBar2.Append(sortingButtons);

					if (MagicStorageConfig.ExtraFilterIcons) {
						filteringButtons.AssignOptions(FilteringOptionLoader.GetVisibleOptions(craftingGUI));

						filteringButtons.UpdateButtonLayout(newButtonSize: 21, newMaxButtonsPerRow: 22);
					} else {
						filteringButtons.AssignOptions(FilteringOptionLoader.BaseOptions.Where(o => o.GetDefaultVisibility(craftingGUI)));

						filteringButtons.UpdateButtonLayout(newButtonSize: 21, newMaxButtonsPerRow: 22);
					}
					
					TopBar3Top = TopBar2Bottom + 4;
					topBar3.Height = filteringButtons.Height;
					topBar3.Append(filteringButtons);

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

						sortingButtons.UpdateButtonLayout(newButtonSize: 21, newMaxButtonsPerRow: 15);

						topBar2.Height = sortingButtons.Height;
						topBar2.Append(sortingButtons);
					} else
						topBar2.Height.Set(21, 0f);

					var filteringOptions = MagicStorageMod.Instance.optionsConfig.GetFilteringOptions(craftingGUI);

					if (filteringOptions.Any()) {
						filteringButtons.AssignOptions(filteringOptions);

						filteringButtons.UpdateButtonLayout(newButtonSize: 21, newMaxButtonsPerRow: 22);

						topBar3.Height = filteringButtons.Height;
						topBar3.Append(filteringButtons);
					} else
						topBar3.Height.Set(21, 0f);
					
					TopBar3Top = TopBar2Bottom + 4;

					Append(topBar3);
					break;
				case ButtonConfigurationMode.LegacyWithGear:
				case ButtonConfigurationMode.LegacyBasicWithPaged:
					sortingButtons.AssignOptions(SortingOptionLoader.BaseOptions.Where(o => o.GetDefaultVisibility(craftingGUI)));

					sortingButtons.UpdateButtonLayout(newButtonSize: 21, newMaxButtonsPerRow: 15);

					topBar2.Height = sortingButtons.Height;
					topBar2.Append(sortingButtons);

					filteringButtons.AssignOptions(FilteringOptionLoader.BaseOptions.Where(o => o.GetDefaultVisibility(craftingGUI)));

					filteringButtons.UpdateButtonLayout(newButtonSize: 21, newMaxButtonsPerRow: 22);

					TopBar3Top = TopBar2Bottom + 4;
					topBar3.Height = filteringButtons.Height;
					topBar3.Append(filteringButtons);

					Append(topBar3);
					break;
				case ButtonConfigurationMode.ModernDropdown:
					//Initialize the menu
					sortingDropdown.Clear();
					sortingDropdown.AddRange(CreatePairedDropdownOptionElements(SortingOptionLoader.GetVisibleOptions(craftingGUI), sortingDropdown.list.ListPadding, CreateDropdownOption));

					foreach (var child in sortingDropdown.Children)
						child.Activate();

					//Initialize the menu
					filteringDropdown.Clear();
					filteringDropdown.AddRange(CreatePairedDropdownOptionElements(FilteringOptionLoader.GetVisibleOptions(craftingGUI), filteringDropdown.list.ListPadding, CreateDropdownOption));

					foreach (var child in filteringDropdown.Children)
						child.Activate();

					//Can't just append them to "topBar2" since that would mess up the mouse events
					Append(sortingDropdown);
					Append(filteringDropdown);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			Append(topBar2);

			Recalculate();

			PostReformatPage(current);

			parentUI.UpdatePanelHeight(parentUI.PanelHeight);

			Recalculate();
		}

		public abstract void PostReformatPage(ButtonConfigurationMode current);

		private static IEnumerable<UIElement> CreatePairedDropdownOptionElements<T>(IEnumerable<T> source, float padding, Func<T, UIElement> createElement) {
			UIElement first = null, second;

			foreach (var option in source) {
				if (first is null)
					first = createElement(option);
				else {
					second = createElement(option);

					//Pair the elements, then yield an element containing them
					UIDropdownElementRowContainer container = new(padding);

					container.SetElements(first, second);

					yield return container;

					first = null;
					second = null;
				}
			}

			//Last element is sad and alone
			if (first is not null) {
				UIDropdownElementRowContainer container = new(padding);

				container.SetElements(first);

				yield return container;
			}
		}

		private SortingOptionElement CreateDropdownOption(SortingOption option) {
			SortingOptionElement element = new(option);

			element.OnClick += parentUI.GetPage<SortingPage>("Sorting").ClickOption;

			return element;
		}

		private FilteringOptionElement CreateDropdownOption(FilteringOption option) {
			FilteringOptionElement element = new(option);

			element.OnClick += parentUI.GetPage<FilteringPage>("Filtering").ClickOption;

			return element;
		}

		protected abstract void InitZoneSlotEvents(MagicStorageItemSlot itemSlot);

		protected virtual void SetThreadWait(bool waiting) {
			if (waiting) {
				if (waitPanel.Parent is null)
					isWaitPanelWaitingToOpen = true;
			} else {
				waitPanel.Remove();
				isWaitPanelWaitingToOpen = false;
			}
		}

		private bool? delayedThreadWait;

		public void RequestThreadWait(bool waiting) {
			if (AssetRepository.IsMainThread)
				SetThreadWait(waiting);
			else
				delayedThreadWait = waiting;
		}

		public override void Update(GameTime gameTime) {
			PendingZoneRefresh = false;

			if (delayedThreadWait is { } waiting) {
				SetThreadWait(waiting);
				delayedThreadWait = null;
			}

			// Wait for at least 10 game ticks to display the prompt
			if (isWaitPanelWaitingToOpen && StorageGUI.CurrentThreadingDuration > 10) {
				isWaitPanelWaitingToOpen = false;

				if (waitPanel.Parent is null) {
					PendingZoneRefresh = true;
					Append(waitPanel);  // Delay appending to here
				}
			}

			if (pendingConfiguration) {
				//Use the current config just in case "pendingConfiguration" is modified where it's not intended to be
				ReformatPage(MagicStorageConfig.ButtonUIMode);
				pendingConfiguration = false;
			}

			bool block = MagicStorageConfig.ButtonUIMode == ButtonConfigurationMode.ModernDropdown && (sortingDropdown.IsMouseHovering || filteringDropdown.IsMouseHovering);
			using (FlagSwitch.Create(ref MagicUI.blockItemSlotActionsDetour, !block)) {
				base.Update(gameTime);
			}

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
			bool block = MagicStorageConfig.ButtonUIMode == ButtonConfigurationMode.ModernDropdown && (sortingDropdown.IsMouseHovering || filteringDropdown.IsMouseHovering);
			using (FlagSwitch.Create(ref MagicUI.blockItemSlotActionsDetour, !block)) {
				base.Draw(spriteBatch);
			}
		}

		protected abstract bool ShouldHideItemIcons();
	}
}
