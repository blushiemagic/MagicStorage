using MagicStorage.Common.Systems;
using MagicStorage.CrossMod;
using Microsoft.Xna.Framework;
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
	/// <summary>
	/// A base class for common elements in Magic Storage's GUIs
	/// </summary>
	public abstract class BaseStorageUI : UIState {
		protected UIDragablePanel panel;

		protected Dictionary<string, BaseStorageUIPage> pages;

		public BaseStorageUIPage currentPage;

		private UIDragablePanel config;
		private Dictionary<string, BaseOptionUIPage> configPages;
		private BaseOptionUIPage currentConfigPage;
		
		private bool needsRecalculate;

		internal UIResizeButton resize;

		public float PanelLeft {
			get => panel.Left.Pixels;
			set {
				if (panel.Left.Pixels != value)
					needsRecalculate = true;

				panel.Left.Set(value, 0f);
			}
		}
		
		public float PanelTop {
			get => panel.Top.Pixels;
			set {
				if (panel.Top.Pixels != value)
					needsRecalculate = true;

				panel.Top.Set(value, 0f);
			}
		}
		
		public float PanelWidth {
			get => panel.Width.Pixels;
			set {
				if (panel.Width.Pixels != value)
					needsRecalculate = true;

				panel.Width.Set(value, 0f);
			}
		}

		//Needed to prevent clamping in the initialization code
		// TODO: width clamping?
		private bool preventHeightClamping = true;
		
		public float PanelHeight {
			get => panel.Height.Pixels;
			set {
				if (panel.Height.Pixels != value)
					needsRecalculate = true;

				panel.Height.Set(value, 0f);
			}
		}

		public void UpdatePanelHeight(float height) {
			if (!preventHeightClamping) {
				float min = GetMinimumResizeHeight();

				//Panel view area top/bottom
				min += panel.viewArea.Top.Pixels + (-panel.viewArea.Height.Pixels);

				if (height < min) {
					height = min;
					needsRecalculate = true;
				}

				if (PanelHeight != height)
					needsRecalculate = true;
			}

			PanelHeight = height;

			if (needsRecalculate)
				Recalculate();
		}

		public float PanelRight {
			get => PanelLeft + PanelWidth;
			protected set => PanelLeft = value - PanelWidth;
		}
		
		public float PanelBottom {
			get => PanelTop + PanelHeight;
			protected set => PanelTop = value - PanelHeight;
		}

		public abstract float GetMinimumResizeHeight();

		private ButtonConfigurationMode lastKnownMode;

		protected abstract IEnumerable<string> GetMenuOptions();

		protected abstract BaseStorageUIPage InitPage(string page);

		public abstract string DefaultPage { get; }

		public BaseStorageUIPage GetPage(string page) => pages[page];

		public T GetPage<T>(string page) where T : BaseStorageUIPage => pages is null
			? null
			: (pages[page] as T ?? throw new InvalidCastException($"The underlying object for page \"{GetType().Name}:{page}\" cannot be converted to " + typeof(T).FullName));

		public override void OnInitialize() {
			float itemSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.InventoryScale;

			panel = new(true, GetMenuOptions().Select(p => (p, Language.GetText("Mods.MagicStorage.UIPages." + p))));

			panel.OnMenuClose += () => {
				StoragePlayer.LocalPlayer.CloseStorage();
				CloseModernConfigPanel();
			};

			panel.OnRecalculate += UpdateFields;
			panel.OnMenuReset += () => pendingUIChange = true;

			PanelTop = Main.instance.invBottom + 60;
			PanelLeft = 20f;
			float innerPanelWidth = CraftingGUI.RecipeColumns * (itemSlotWidth + CraftingGUI.Padding) + 20f + CraftingGUI.Padding;
			PanelWidth = panel.PaddingLeft + innerPanelWidth + panel.PaddingRight + 2 * UIDragablePanel.cornerPadding;
			PanelHeight = Main.screenHeight - (PanelTop + 2 * UIDragablePanel.cornerPadding);

			pages = new();

			foreach ((string key, var tab) in panel.menus) {
				var page = pages[key] = InitPage(key);
				page.Width = StyleDimension.Fill;
				page.Height = StyleDimension.Fill;

				tab.OnClick += (evt, e) => {
					SoundEngine.PlaySound(SoundID.MenuTick);
					SetPage((e as UIPanelTab).Name);
				};
			}

			PostInitializePages();

			//Need to manually activate the pages
			foreach (var page in pages.Values)
				page.Activate();

			lastKnownMode = MagicStorageConfig.ButtonUIMode;

			config = new(true, new (string, LocalizedText)[] {
				("Sorting", Language.GetText("Mods.MagicStorage.UIPages.Sorting")),
				("Filtering", Language.GetText("Mods.MagicStorage.UIPages.Filtering"))
			});

			config.OnMenuClose += CloseModernConfigPanel;

			//Prevent moving the panel
			config.OnUpdate += e => (e as UIDragablePanel).Dragging = false;

			config.Width.Set(200f, 0f);
			config.viewArea.SetPadding(0);

			configPages = new();

			InitConfigPage("Sorting", new SortingPage(this) { filterBaseOptions = true });
			InitConfigPage("Filtering", new FilteringPage(this) { filterBaseOptions = true });

			resize = new() {
				ResizeWidth = false
			};

			// NOTE: this isn't called in UIResizeButton.Recalculate and for good reason
			resize.OnDragging += r => {
				float old = PanelHeight;
				UpdatePanelHeight(old + r.OffsetDelta.Y);
				
				r.OffsetDelta.Y = PanelHeight - old;

				Refresh();
				Recalculate();
			};

			resize.Left.Set(-4 - resize.Width.Pixels, 1f);
			resize.Top.Set(-4 - resize.Height.Pixels, 1f);

			panel.Append(resize);

			Append(panel);

			PostAppendPanel();

			OnButtonConfigChanged(lastKnownMode);

			needsRecalculate = false;
			preventHeightClamping = false;
		}

		private void InitConfigPage(string page, BaseOptionUIPage instance) {
			var configPage = configPages[page] = instance;

			configPage.OnOptionClicked += (evt, e, option) => {
				//Clicks from the config panel's buttons

				BaseOptionUIPage obj = e.Parent as BaseOptionUIPage;
				
				var optionPage = obj.parentUI.GetPage<BaseOptionUIPage>(obj.Name);
				optionPage.option = option;
				optionPage.SetLoaderSelection(option);

				//Deselect the option in the main UI
				var defPage = obj.parentUI.GetPage<BaseStorageUIAccessPage>(obj.parentUI.DefaultPage);
				if (obj.Name == "Sorting")
					defPage.sortingButtons.Choice = -1;
				else if (obj.Name == "Filtering")
					defPage.filteringButtons.Choice = -1;
				
				StorageGUI.needRefresh = true;
			};

			configPage.Width = StyleDimension.Fill;
			configPage.Height = StyleDimension.Fill;
			configPage.buttonSize = 21;
		}

		private void UpdateFields() {
			GetConfigPanelLocation(out float left, out float top);

			config.Left.Set(left, 0f);
			config.Top.Set(top, 0f);

			config.Height.Set(Math.Min(PanelHeight, 300f), 0f);

			config.Recalculate();
		}

		public sealed override void OnActivate() => Open();

		public sealed override void OnDeactivate() => Close();

		public abstract int GetSortingOption();

		public abstract int GetFilteringOption();

		public abstract string GetSearchText();

		protected virtual void PostInitializePages() { }

		protected virtual void PostAppendPanel() { }

		protected virtual void GetConfigPanelLocation(out float left, out float top) {
			left = PanelRight;
			top = PanelTop;
		}

		public bool SetPage(string page) {
			BaseStorageUIPage newPage = pages[page];

			if (!object.ReferenceEquals(currentPage, newPage)) {
				panel.SetActivePage(page);

				if (currentPage is not null) {
					currentPage.InvokeOnPageDeselected();

					currentPage.Remove();
				}

				currentPage = newPage;

				panel.viewArea.Append(currentPage);

				currentPage.InvokeOnPageSelected();

				return true;
			}

			return false;
		}

		public void Open() {
			if (currentPage is not null)
				return;

			OnOpen();

			SetPage(DefaultPage);
		}

		protected virtual void OnOpen() { }

		public void Close() {
			if (currentPage is not null) {
				OnClose();

				currentPage.InvokeOnPageDeselected();

				currentPage.Remove();
			}

			currentPage = null;

			resize.Dragging = false;
		}

		protected virtual void OnClose() { }

		public bool pendingUIChange;

		public override void Update(GameTime gameTime) {
			if (needsRecalculate) {
				Refresh();
				Recalculate();
			}
			
			ButtonConfigurationMode currentMode = MagicStorageConfig.ButtonUIMode;
			if (lastKnownMode != currentMode) {
				OnButtonConfigChanged(currentMode);
				lastKnownMode = currentMode;
			}

			if (pendingUIChange) {
				float itemSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.InventoryScale;
				float top = Main.instance.invBottom + 60;
				PanelTop = top;
				PanelLeft = 20f;
				float innerPanelWidth = CraftingGUI.RecipeColumns * (itemSlotWidth + CraftingGUI.Padding) + 20f + CraftingGUI.Padding;
				PanelWidth = panel.PaddingLeft + innerPanelWidth + panel.PaddingRight + 2 * UIDragablePanel.cornerPadding;
				PanelHeight = Main.screenHeight - (top + 2 * UIDragablePanel.cornerPadding);
				panel.Recalculate();

				//RefreshItems will conveniently update the zone heights
				StorageGUI.RefreshItems();

				pendingUIChange = false;
			}

			base.Update(gameTime);

			if (needsRecalculate) {
				Refresh();
				Recalculate();
			}
		}

		public override void Recalculate() {
			base.Recalculate();

			needsRecalculate = false;
		}

		public virtual void Refresh() { }

		protected virtual void OnButtonConfigChanged(ButtonConfigurationMode current) { }

		internal void ModernPanelButtonClicked(string page, NewUIButtonChoice buttons) {
			//Clicks from the main UI page's buttons

			var optionPage = GetPage<BaseOptionUIPage>(page);
			
			optionPage.option = buttons.SelectionType;
			optionPage.SetLoaderSelection(optionPage.option);
			
			StorageGUI.needRefresh = true;
		}

		internal void OpenModernConfigPanel(string page) {
			if (page == "Sorting")
				OpenModernConfigPage("Sorting", "Filtering");
			else
				OpenModernConfigPage("Filtering", "Sorting");

			Append(config);
		}

		private void CloseModernConfigPanel() {
			if (currentConfigPage is null)
				return;

			currentConfigPage.InvokeOnPageDeselected();

			currentConfigPage.Remove();

			currentConfigPage = null;

			config.Remove();
		}

		private void OpenModernConfigPage(string pageToOpen, string pageToClose) {
			var target = configPages[pageToOpen];

			if (!object.ReferenceEquals(currentConfigPage, target)) {
				if (currentConfigPage is not null) {
					currentConfigPage.InvokeOnPageDeselected();

					currentConfigPage.Remove();
				}

				config.HideTab(pageToClose);
				config.ShowTab(pageToOpen);

				currentConfigPage = target;

				config.viewArea.Append(currentConfigPage);

				currentConfigPage.InvokeOnPageSelected();
			}
		}
	}
}
