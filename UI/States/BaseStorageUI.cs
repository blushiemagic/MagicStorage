using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Components;
using MagicStorage.CrossMod;
using MagicStorage.UI.Security;
using Microsoft.Xna.Framework;
using SerousCommonLib.UI;
using SerousCommonLib.UI.Layouts;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

		private UIResizeButton resize;

		private bool _requestingPassword;
		private PasswordRequestPopup _passwordRequest;
		private MissingHeartPopup _missingHeart;
		private UIElement _networkAccessBlocker;
		public UITextPanel<LocalizedText> clientResponses;
		private bool _clientResponsesDirty;
		private bool _clearPopup;

		private int _requestedNetwork;

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

		public BaseStorageUI() {
			panel = new(true, GetMenuOptions().Select(p => (p, Language.GetText("Mods.MagicStorage.UIPages." + p))));
			pages = new();

			foreach ((string key, var tab) in panel.menus)
				pages[key] = InitPage(key);

			config = new(true, [
				("Sorting", Language.GetText("Mods.MagicStorage.UIPages.Sorting")),
				("Filtering", Language.GetText("Mods.MagicStorage.UIPages.Filtering"))
			]);

			configPages = new();

			InitConfigPage("Sorting", new SortingPage(this) { filterBaseOptions = true });
			InitConfigPage("Filtering", new FilteringPage(this) { filterBaseOptions = true });

			resize = new() {
				ResizeWidth = false
			};

			_networkAccessBlocker = new UIElement();
			clientResponses = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Security.UI.NoResponse"));
		}

		public BaseStorageUIPage GetPage(string page) => pages?.TryGetValue(page, out var pageValue) is true ? pageValue : null;

		public BaseStorageUIPage GetDefaultPage() => GetPage(DefaultPage);

		public T GetPage<T>(string page) where T : BaseStorageUIPage {
			if (pages is null)
				return null;

			BaseStorageUIPage pageObj = GetPage(page)
				?? throw new ArgumentException($"Requested page \"{GetType().Name}:{page}\" does not exist");

			if (pageObj is not T typedPageObj)
				throw new InvalidCastException($"The underlying object for page \"{GetType().Name}:{page}\" cannot be converted to " + typeof(T).FullName);

			return typedPageObj;
		}

		public bool TryGetPage<T>(string page, [NotNullWhen(true)] out T result) where T : BaseStorageUIPage {
			if (pages is null || !pages.TryGetValue(page, out var pageValue) || pageValue is not T typedPageValue) {
				result = null;
				return false;
			}

			result = typedPageValue;
			return true;
		}

		public T GetDefaultPage<T>() where T : BaseStorageUIPage => GetPage<T>(DefaultPage);

		public bool TryGetDefaultPage<T>([NotNullWhen(true)] out T result) where T : BaseStorageUIPage => TryGetPage(DefaultPage, out result);

		public override void OnInitialize() {
			float itemSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.InventoryScale;

			panel.OnMenuClose += CloseCompletely;

			panel.OnRecalculate += UpdateFields;
			panel.OnMenuReset += () => pendingUIChange = true;

			PanelTop = Main.instance.invBottom + 60;
			PanelLeft = 20f;
			float innerPanelWidth = CraftingGUI.RecipeColumns * (itemSlotWidth + CraftingGUI.Padding) + 20f + CraftingGUI.Padding;
			PanelWidth = panel.PaddingLeft + innerPanelWidth + panel.PaddingRight + 2 * UIDragablePanel.cornerPadding;
			PanelHeight = Main.screenHeight - (PanelTop + 2 * UIDragablePanel.cornerPadding);

			// UIPanelTab creation automatically sets UIElement.Left based on the width of the localization string.
			// Normally, this would be fine, but apparently the localization can sometimes not load by the time the
			//   constructor for BaseStorageUI is called?
			// To remedy this, we just recalculate the alignments again here.
			float left = 0;

			foreach ((string key, var tab) in panel.menus) {
				var page = pages[key];
				page.Width = StyleDimension.Fill;
				page.Height = StyleDimension.Fill;

				// Force the MinWidth to be changed for the tab by reassigning its text
				tab.SetText(tab._text);
				tab.Left.Set(left, 0f);
				
				// FIX: v0.7.0.10 - The text was being reset correctly, but not the text dimensions
				tab.Recalculate();

				left += tab.GetDimensions().Width + 10;
				tab.OnLeftClick += (evt, e) => {
					SoundEngine.PlaySound(SoundID.MenuTick);
					SetPage((e as UIPanelTab).Name);
				};
			}

			// FIX: v0.7.0.10 - Some code references UI elements in the Modern pages, so those must be manually initialized
			if (TryGetPage<BaseOptionUIPage>("Sorting", out var sortingPage))
				sortingPage.InitOptionButtons(true);
			if (TryGetPage<BaseOptionUIPage>("Filtering", out var filteringPage))
				filteringPage.InitOptionButtons(true);

			PostInitializePages();

			lastKnownMode = MagicStorageConfig.ButtonUIMode;

			config.OnMenuClose += CloseModernConfigPanel;

			//Prevent moving the panel
			config.OnUpdate += e => (e as UIDragablePanel).Dragging = false;

			config.Width.Set(200f, 0f);
			config.viewArea.SetPadding(0);

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

			/*
			_networkAccessBlocker.GetLayoutManager().Attributes = new LayoutAttributes()
				.AddConstraint(LayoutConstraintType.TopToTopOf, panel, LayoutUnit.Zero)
				.AddConstraint(LayoutConstraintType.BottomToBottomOf, panel, LayoutUnit.Zero)
				.AddConstraint(LayoutConstraintType.LeftToLeftOf, panel, LayoutUnit.Zero)
				.AddConstraint(LayoutConstraintType.RightToRightOf, panel, LayoutUnit.Zero);
			*/
			_networkAccessBlocker.Width.Set(0, 1f);
			_networkAccessBlocker.Height.Set(0, 1f);

			/*
			clientResponses.GetLayoutManager().Attributes = new LayoutAttributes()
				.AddConstraint(LayoutConstraintType.LeftToLeftOf, null, new LayoutUnit(pixels: 8f))
				.AddConstraint(LayoutConstraintType.RightToRightOf, null, new LayoutUnit(pixels: 8f))
				.AddConstraint(LayoutConstraintType.BottomToBottomOf, null, new LayoutUnit(pixels: 8f))
				.InheritSizeFrom(clientResponses);
			*/
			clientResponses.Left.Set(8f, 0f);
			clientResponses.SetBottomAlignment(8f);
			clientResponses.Width.Set(-16f, 1f);

			_networkAccessBlocker.Append(clientResponses);

			needsRecalculate = false;
			preventHeightClamping = false;
		}

		private void InitConfigPage(string page, BaseOptionUIPage instance) {
			var configPage = configPages[page] = instance;

			configPage.OnOptionClicked += (evt, e, optionType) => {
				//Clicks from the config panel's buttons

				BaseOptionUIPage obj = e.Parent as BaseOptionUIPage;
				
				var optionPage = obj.parentUI.GetPage<BaseOptionUIPage>(obj.Name);
				
				optionPage.SetSelection(optionType);

				//Deselect the option in the main UI
				if (!optionPage.IsOptionGeneral(e)) {
					var defPage = obj.parentUI.GetDefaultPage<BaseStorageUIAccessPage>();
					if (obj.Name == "Sorting")
						defPage.sortingButtons.Choice = -1;
					else if (obj.Name == "Filtering")
						defPage.filteringButtons.Choice = -1;
				}
				
				MagicUI.StartMainZoneRefreshThread(caller: "BaseStorageUI.configPages[].OptionClicked()");
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

		public sealed override void OnActivate() {
			Open();

			// Ensure that the button layout is accurate
			OnButtonConfigChanged(lastKnownMode);
		}

		public sealed override void OnDeactivate() => Close();

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
					currentPage.RemoveAndDeactivate();

					// Sanity check
					currentPage.IsOpening = false;
				}

				currentPage = newPage;

				panel.viewArea.Append(currentPage);

				currentPage.IsOpening = true;
				
				currentPage.Activate();

				currentPage.InvokeOnPageSelected();

				return true;
			}

			return false;
		}

		public void Open() {
			if (currentPage is not null)
				return;

			SetPage(DefaultPage);

			OnOpen();

			// Restore the saved sorting/filtering options
			if (GetPage("Sorting") is SortingPage sortingPage)
				SortingOptionLoader.Selected = sortingPage.selected;

			if (GetPage("Filtering") is FilteringPage filteringPage) {
				FilteringOptionLoader.Selected = filteringPage.selected;
				FilteringOptionLoader.GeneralSelections.Clear();
				FilteringOptionLoader.GeneralSelections.UnionWith(filteringPage.generalSelections);
			}

			_requestedNetwork = -1;

			_pendingAccess = false;
			_pendingResult = default;
			_pendingActionData = null;

			if (this is not SecurityUIState && !StoragePlayer.IsCurrentLocalNetworkAccessible()) {
				NetHelper.Report(true, "BaseStorageUI: Attempted to access an inaccessible network...");

				_requestingPassword = true;
				SecuritySystem.OnClientResultUpdated += SetResponse;
				SecuritySystem.OnInformResultToClient += ReceiveClientResult;

				// Safe to assume that the component actually exists at this point
				TEStorageComponent component = StoragePlayer.LocalPlayer.GetStorageComponent();
				SecuritySystem.NetworkView view = SecuritySystem.GetNetwork(component.assignedNetwork);

				_requestedNetwork = view.id;

				if (component.GetHeart() is null) {
					NetHelper.Report(false, "  Storage component did not have an assigned Storage Heart");

					_missingHeart?.Remove();
					_missingHeart = new MissingHeartPopup(view);

					_missingHeart.Activate();
					_missingHeart.Recalculate();

					_networkAccessBlocker.Append(_missingHeart);
				} else {
					NetHelper.Report(false, "  Storage component had an assigned Storage Heart");

					if (_passwordRequest is null) {
						_passwordRequest = new PasswordRequestPopup(view);
						_passwordRequest.OnPasswordEntered += CheckPassword;
						_passwordRequest.OnCancel += self => CloseCompletely();
						// Popup sets its layout attributes in its constructor
					} else {
						_passwordRequest.UpdateView(view);
					}

					_passwordRequest.Activate();
					_passwordRequest.ClearInputs();

					_networkAccessBlocker.Append(_passwordRequest);
				}

				panel.Append(_networkAccessBlocker);

				needsRecalculate = true;
			}

			timeSpentOpen = 0;
		}

		private void SetResponse(LocalizedText response) {
			clientResponses.SetText(response);
			_clientResponsesDirty = true;
		}

		private bool _pendingAccess;
		private NetworkActionResult _pendingResult;
		private object _pendingActionData;

		private void ReceiveClientResult(NetworkActionResult result, NetworkReportCategory category) {
			_pendingResult = result;
		}

		private void CheckPassword(string enteredPassword) {
			NetHelper.Report(true, "BaseStorageUI: Checking entered password...");

			var storagePlayer = StoragePlayer.LocalPlayer;
			TEStorageComponent component = storagePlayer.GetStorageComponent();
			SecuritySystem.NetworkView view = SecuritySystem.GetNetwork(component.assignedNetwork);

			NetworkActionResult result = SecuritySystem.JoinNetwork(view.id, enteredPassword);

			SecuritySystem.ReportNetworkResult(result, NetworkReportCategory.Join);

			NetHelper.Report(false, $"  Result: {result}");

			_pendingAccess = true;
			_pendingActionData = enteredPassword;
		}

		private void CheckPassword_Result() {
			NetHelper.Report(true, $"BaseStorageUI: Delayed password check result: {_pendingResult}");

			SecuritySystem.HandleNetworkAccessibilityOnJoin(_pendingResult, Main.LocalPlayer, StoragePlayer.LocalPlayer.GetStorageComponent().assignedNetwork, (string)_pendingActionData);

			if (_pendingResult.IsSuccess()) {
				_clearPopup = true;
				MagicUI.SetRefresh(forceFullRefresh: true);  // Force the UI to populate the relevant collections
			}
		}

		protected virtual void OnOpen() { }

		private void CloseCompletely() {
			StoragePlayer.LocalPlayer.CloseStorage();
			CloseModernConfigPanel();
		}

		public void Close() {
			if (currentPage is not null) {
				OnClose();

				currentPage.InvokeOnPageDeselected();

				currentPage.RemoveAndDeactivate();

				// Save the sorting/filtering options
				if (GetPage("Sorting") is SortingPage sortingPage)
					sortingPage.selected = SortingOptionLoader.Selected;

				if (GetPage("Filtering") is FilteringPage filteringPage) {
					filteringPage.selected = FilteringOptionLoader.Selected;
					filteringPage.generalSelections.Clear();
					filteringPage.generalSelections.UnionWith(FilteringOptionLoader.GeneralSelections);
				}
			}

			_passwordRequest?.RemoveAndDeactivate();
			_missingHeart.RemoveAndDeactivate();

			_networkAccessBlocker.RemoveAndDeactivate();

			OnAccessDeniedPopupsCleared();

			if (_requestingPassword) {
				SecuritySystem.OnClientResultUpdated -= SetResponse;
				SecuritySystem.OnInformResultToClient -= ReceiveClientResult;

				_passwordRequest?.RemoveAndDeactivate();
				_missingHeart.RemoveAndDeactivate();
				_networkAccessBlocker.RemoveAndDeactivate();

				_requestingPassword = false;
				_clearPopup = false;
				_clientResponsesDirty = false;

				_pendingResult = default;
				_pendingActionData = null;
				
				needsRecalculate = false;
			}

			currentPage = null;

			resize.Dragging = false;
		}

		protected virtual void OnClose() { }

		public virtual void ResetSearchBars() {
			if (GetDefaultPage() is BaseStorageUIAccessPage page)
				page.searchBar.State.Reset();
		}

		public bool pendingUIChange;

		private int timeSpentOpen;

		public override void Update(GameTime gameTime) {
			if (_requestedNetwork >= 0 && StoragePlayer.LocalPlayer.GetStorageComponent() is TEStorageComponent component && component.assignedNetwork != _requestedNetwork) {
				// Assigned network has changed, force the UI to close
				CloseCompletely();
				return;
			}

			if (_requestingPassword) {
				if (_pendingAccess && _pendingResult is not NetworkActionResult.NeedsServerApproval) {
					CheckPassword_Result();

					_pendingAccess = false;
					_pendingResult = default;
					_pendingActionData = null;
				}

				if (_clearPopup) {
					_clearPopup = false;
					_passwordRequest?.RemoveAndDeactivate();
					_missingHeart.RemoveAndDeactivate();
					_networkAccessBlocker.RemoveAndDeactivate();

					_requestingPassword = false;
					_clientResponsesDirty = false;
					needsRecalculate = true;

					SecuritySystem.OnClientResultUpdated -= SetResponse;
					SecuritySystem.OnInformResultToClient -= ReceiveClientResult;
				}

				if (_clientResponsesDirty) {
					_clientResponsesDirty = false;
					clientResponses.Recalculate();

					SoundEngine.PlaySound(SoundID.MenuTick);
				}
			}

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
				MagicUI.RefreshItems();

				pendingUIChange = false;
			}

			// Refreshing slots?  prevent resizing
			if (MagicUI.CurrentlyRefreshing)
				resize.Dragging = false;

			// At this point, any refreshing/reformatting/etc. is done, so it's safe to allow threads to be started
			currentPage.IsOpening = false;

			// Prevent item slot interactions immediately after opening the UI
			if (timeSpentOpen < 60) {
				using (FlagSwitch.Create(ref MagicUI.blockItemSlotActionsDetour, true))
					base.Update(gameTime);
			} else
				base.Update(gameTime);

			if (needsRecalculate) {
				Refresh();
				Recalculate();
			}

			timeSpentOpen++;
		}

		public override void Recalculate() {
			base.Recalculate();

			needsRecalculate = false;
		}

		public virtual void Refresh() { }

		public virtual void OnRefreshStart() { }

		internal void ForceLayoutTo(ButtonConfigurationMode mode) {
			var old = MagicStorageConfig.Instance.buttonLayout;

			if (old == mode)
				return;

			MagicStorageConfig.Instance.buttonLayout = mode;
			Utility.SaveModConfig(MagicStorageConfig.Instance);

			OnButtonConfigChanged(mode);
		}

		protected virtual void OnButtonConfigChanged(ButtonConfigurationMode current) { }

		internal void OpenModernConfigPanel(string page) {
			if (page == "Sorting")
				OpenModernConfigPage("Sorting", "Filtering");
			else
				OpenModernConfigPage("Filtering", "Sorting");

			config.Activate();

			Append(config);
		}

		private void CloseModernConfigPanel() {
			if (currentConfigPage is null)
				return;

			currentConfigPage.InvokeOnPageDeselected();

			currentConfigPage.RemoveAndDeactivate();

			currentConfigPage = null;

			config.RemoveAndDeactivate();
		}

		private void OpenModernConfigPage(string pageToOpen, string pageToClose) {
			var target = configPages[pageToOpen];

			if (!object.ReferenceEquals(currentConfigPage, target)) {
				if (currentConfigPage is not null) {
					currentConfigPage.InvokeOnPageDeselected();

					currentConfigPage.RemoveAndDeactivate();
				}

				config.HideTab(pageToClose);
				config.ShowTab(pageToOpen);

				currentConfigPage = target;

				config.viewArea.Append(currentConfigPage);

				currentConfigPage.InvokeOnPageSelected();
			}
		}

		protected virtual void OnAccessDeniedPopupsShown() { }

		protected virtual void OnAccessDeniedPopupsCleared() { }
	}
}
