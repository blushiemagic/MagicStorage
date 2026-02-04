using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MagicStorage.Common.Players;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.Components;
using MagicStorage.Edits;
using MagicStorage.UI;
using MagicStorage.UI.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace MagicStorage.Common.Systems;

public class MagicUI : ModSystem
{
	public static UserInterface uiInterface;

	public static BaseStorageUI craftingUI, storageUI, environmentUI, decraftingUI, securityUI;

	public static bool HasOpenUI() => uiInterface?.CurrentState is not null;

	public static bool IsStorageUIOpen() => storageUI is not null && object.ReferenceEquals(uiInterface?.CurrentState, storageUI);

	public static bool IsCraftingUIOpen() => craftingUI is not null && object.ReferenceEquals(uiInterface?.CurrentState, craftingUI);

	public static bool IsEnvironmentUIOpen() => environmentUI is not null && object.ReferenceEquals(uiInterface?.CurrentState, environmentUI);

	public static bool IsDecraftingUIOpen() => decraftingUI is not null && object.ReferenceEquals(uiInterface?.CurrentState, decraftingUI);

	public static bool IsSecurityUIOpen() => securityUI is not null && object.ReferenceEquals(uiInterface?.CurrentState, securityUI);

	private static bool _refreshUI;
	[Obsolete("Use the methods for requesting refresh threads instead", error: true)]
	public static bool RefreshUI {
		get => _refreshUI;
		set => _refreshUI |= value;
	}

	[Obsolete]
	internal static bool Obsolete_RefreshUI() => RefreshUI;

	// NOTE: Checks RefreshUI because of a delay between the call to SetRefresh() and the thread actually starting, and this property could be checked between them.
	//       Even though RefreshUI is obsolete, it still needs to be accounted for.
	public static bool CurrentlyRefreshing => _refreshUI || activeRefreshingThread is { IsRunning: true };

	public static bool HasActiveThread<T>() => activeRefreshingThread is { IsRunning: true } and T;

	public static bool HasActiveThread<T>(out T thread) {
		if (activeRefreshingThread is { IsRunning: true } and T activeThread) {
			thread = activeThread;
			return true;
		}

		thread = default;
		return false;
	}

	public static event Action OnRefresh;
		
	private static bool forceFullRefresh;
	[Obsolete("This property was renamed to " + nameof(IgnoreSpecificZoneRefreshing), error: true)]
	public static bool ForceNextRefreshToBeFull {
		get => forceFullRefresh || StorageGUI.Obsolete_needRefresh();
		set => forceFullRefresh |= value;
	}

	[Obsolete]
	private static bool Obsolete_get_ForceNextRefreshToBeFull() => ForceNextRefreshToBeFull;

	[Obsolete]
	private static void Obsolete_set_ForceNextRefreshToBeFull(bool value) => ForceNextRefreshToBeFull = value;

	/// <summary>
	/// If <see langword="true"/>, the next main zone refresh will refresh its entire list instead of the specific items from optimization calls like <see cref="SetNextCollectionsToRefresh(int)"/>
	/// </summary>
	public static bool IgnoreSpecificZoneRefreshing {
		get => Obsolete_get_ForceNextRefreshToBeFull();
		set => Obsolete_set_ForceNextRefreshToBeFull(value);
	}

	// TODO: replace the above with IgnoreSpecificZoneRefreshing

	internal static RefreshThread activeRefreshingThread;
	internal static IRefreshThreadBuilder pendingThread;

	public static int CurrentThreadingDuration { get; internal set; }

	/// <summary>
	/// Shorthand for setting <see cref="RefreshUI"/> to <see langword="true"/> and also setting <see cref="ForceNextRefreshToBeFull"/>
	/// </summary>
	[Obsolete("Use the methods for requesting refresh threads instead", error: true)]
	public static void SetRefresh(bool forceFullRefresh = false) {
		RefreshUI = true;
		ForceNextRefreshToBeFull = forceFullRefresh;

		if (!_hasPrintedObsoleteMessage) {
			_hasPrintedObsoleteMessage = true;
			
			var trace = new StackTrace(1, true);

			string errorMessage = Utility.TryScanStackTraceForMods(trace, out Mod recentModCaller)
				? $"Mod \"{recentModCaller.Name}\" is using the obsolete MagicUI.SetRefresh() method and needs to update to use the new refresh thread system."
				: "An unknown mod is using the obsolete MagicUI.SetRefresh() method and needs to update to use the new refresh thread system.";

			MagicStorageMod.Instance.Logger.Error($"{errorMessage}\n{trace}");

			if (Main.netMode != NetmodeID.Server) {
				var m = errorMessage;
				Main.QueueMainThreadAction(() => Main.NewTextMultiline(m, c: Color.Red));
			}
		}
	}

	private static bool _hasPrintedObsoleteMessage;

	/// <inheritdoc cref="SetRefresh"/>
	[Obsolete]
	internal static void Obsolete_SetRefresh(bool forceFullRefresh = false) => SetRefresh(forceFullRefresh);

	private static bool _pendingWatchdogPulse;

	public static void PulseWatchdogs() => _pendingWatchdogPulse = true;

	private static bool _checkedAccessibility;

	internal static void CheckRefresh() {
		if (!StoragePlayer.IsCurrentLocalNetworkAccessible()) {
			if (!_checkedAccessibility)
				SecuritySystem.PrintStorageInaccessible();

			_checkedAccessibility = true;
			_pendingWatchdogPulse = false;
		} else if (!CurrentlyRefreshing && _pendingWatchdogPulse) {
			if (IsCraftingUIOpen())
				CraftingGUI.ExecuteInCraftingGuiEnvironment(HandleWatchdogs);
			else
				HandleWatchdogs();

			_pendingWatchdogPulse = false;
		}

		if (Obsolete_RefreshItems()) {
			// Old logic; ignore the thread requests
		} else if (_requestingFullThread) {
			StartFullRefreshThread(caller: "MagicUI.CheckRefresh()");
			ResetThreadRequests();
		} else if (_requestingZoneThread) {
			StartMainZoneRefreshThread(caller: "MagicUI.CheckRefresh()");
			ResetThreadRequests();
		}

		if (CurrentlyRefreshing)
			CurrentThreadingDuration++;
		else
			CurrentThreadingDuration = 0;
	}

	private static void HandleWatchdogs() {
		// Check the watchdogs
		foreach (var watchdog in _watchdogs)
			watchdog.Handle();
	}

	internal static void InvokeOnRefresh() {
		OnRefresh?.Invoke();
		StorageGUI.InvokeOnRefresh();
	}

	public static void SetNextCollectionsToRefresh(int itemType) {
	//	SetRefresh();
		if (IsStorageUIOpen())
			StorageGUI.SetNextItemTypeToRefresh(itemType);
		else if (IsCraftingUIOpen())
			CraftingGUI.SetNextDefaultRecipeCollectionToRefresh(itemType);
		else if (IsDecraftingUIOpen())
			DecraftingGUI.SetNextDefaultItemCollectionToRefresh(itemType);
	}

	public static void SetNextCollectionsToRefresh(IEnumerable<int> itemTypes) {
	//	SetRefresh();
		if (IsStorageUIOpen())
			StorageGUI.SetNextItemTypesToRefresh(itemTypes);
		else if (IsCraftingUIOpen())
			CraftingGUI.SetNextDefaultRecipeCollectionToRefresh(itemTypes);
		else if (IsDecraftingUIOpen())
			DecraftingGUI.SetNextDefaultItemCollectionToRefresh(itemTypes);
	}

	internal static void StopCurrentThread() {
		_watchdogs.Clear();

		activeRefreshingThread?.Stop();
		// NOTE: RefreshThread is responsible for setting this to null when the active thread finishes execution
	//	activeRefreshingThread = null;
	}

	private static bool _requestingFullThread;
	private static bool _requestingZoneThread;

	public static void RequestFullRefresh() => _requestingFullThread = true;

	public static void RequestMainZoneThread() => _requestingZoneThread = true;

	private static void ResetThreadRequests() {
		_refreshUI = false;
		StorageGUI.Obsolete_needRefresh() = false;
		_requestingFullThread = false;
		_requestingZoneThread = false;
		forceFullRefresh = false;
	}

	public static void StartFullRefreshThread(string caller) {
		if (IsStorageUIOpen())
			StorageGUI.CreateFullRefreshThread(caller).Start();
		else if (IsCraftingUIOpen())
			CraftingGUI.CreateFullRefreshThread(caller).Start();
		else if (IsDecraftingUIOpen())
			DecraftingGUI.CreateFullRefreshThread(caller).Start();
	}

	public static void StartMainZoneRefreshThread(string caller) {
		if (IsStorageUIOpen()) {
			// Start a full refresh thread, since the main zone is the only thing present
			StorageGUI.CreateFullRefreshThread(caller).Start();
		} else if (IsCraftingUIOpen()) {
			// Start a refresh thread that updates the recipe list
			CraftingGUI.CreateRecipeListRefreshThread(caller).Start();
		} else if (IsDecraftingUIOpen()) {
			// Start a refresh thread that updates the item list
			DecraftingGUI.CreateItemListRefreshThread(caller).Start();
		}
	}

	public static void StartSelectedObjectRefreshThread(string caller) {
		if (IsCraftingUIOpen()) {
			// Start a refresh thread that updates the stored ingredients for the current recipe
			CraftingGUI.CreateSelectedRecipeRefreshThread(CraftingGUI.selectedRecipe, CraftingGUI.craftAmountTarget, caller).Start();
		} else if (IsDecraftingUIOpen()) {
			// Start a refresh thread that updates the stored items for the current shimmering item
			DecraftingGUI.CreateSelectedItemRefreshThread(DecraftingGUI.selectedItem, CraftingGUI.craftAmountTarget, caller).Start();
		}
	}

	public static void StartSelectedObjectRefreshThread<T>(T selectedObject, int amountTarget, string caller) {
		if (typeof(T) == typeof(Recipe)) {
			if (IsCraftingUIOpen()) {
				// Start a refresh thread that updates the stored ingredients list for the provided recipe
				CraftingGUI.CreateSelectedRecipeRefreshThread(Unsafe.As<T, Recipe>(ref selectedObject), amountTarget, caller);
			}
		} else if (typeof(T) == typeof(int)) {
			if (IsDecraftingUIOpen()) {
				// Start a refresh thread that updates the stored items for the provided shimmering item
				DecraftingGUI.CreateSelectedItemRefreshThread(Unsafe.As<T, int>(ref selectedObject), amountTarget, caller);
			}
		} else {
			// Unsupported type
			throw new ArgumentException("The type of the provided object is not supported by any UIs: " + typeof(T).FullName);
		}
	}

	[Obsolete]
	private static bool Obsolete_RefreshItems() {
		if (RefreshUI) {
			RefreshItems();
			return true;
		}

		return false;
	}

	[Obsolete("Use " + nameof(RequestFullRefresh) + " or " + nameof(StartFullRefreshThread) + " instead", error: true)]
	public static void RefreshItems() {
		if (IsStorageUIOpen()) {
			CraftingGUI.ClearAllCollections();
			DecraftingGUI.ClearAllCollections(callCraftingClear: false);

			StorageGUI.RefreshItems_Inner();
		} else if (IsCraftingUIOpen()) {
			StorageGUI.ClearAllCollections();
			DecraftingGUI.ClearAllCollections(callCraftingClear: false);

			CraftingGUI.RefreshItems_Inner();
		} else if (IsDecraftingUIOpen()) {
			StorageGUI.ClearAllCollections();
			CraftingGUI.ClearAllCollections();

			DecraftingGUI.RefreshItems();
		} else {
			StorageGUI.ClearAllCollections();
			CraftingGUI.ClearAllCollections();
			DecraftingGUI.ClearAllCollections(callCraftingClear: false);
		}

		ResetThreadRequests();
	}

	internal static IEntitySource GetShimmeringSpawnSource() => new EntitySource_Parent(Main.LocalPlayer);

	private static readonly ConcurrentBag<RefreshUIWatchdog> _watchdogs = new();

	public static void AddRefreshWatchdog(IRefreshUIWatchTarget target, bool? initialStateOverride = null) {
		ArgumentNullException.ThrowIfNull(target);

		_watchdogs.Add(new RefreshUIWatchdog(target, initialStateOverride ?? target.GetCurrentState()));
	}

	internal static void ClearRefreshWatchdogs() => _watchdogs.Clear();

	//Assign text to this value instead of using Main.instance.MouseText() in the MouseOver and MouseOut events
	internal static string mouseText;

	internal static string lastKnownSearchBarErrorReason;

	internal static bool blockItemSlotActionsDetour;

	public override void Load() {
		if (Main.dedServ)
			return;

		uiInterface = new();
		craftingUI = new CraftingUIState();
		storageUI = new StorageUIState();
		environmentUI = new EnvironmentUIState();
		decraftingUI = new DecraftingUIState();
		securityUI = new SecurityUIState();

		Main.OnResolutionChanged += PendingResolutionChange;
	}

	public override void Unload() {
		if (!Main.dedServ) {
			(craftingUI as CraftingUIState)?.history?.Clear();
			(decraftingUI as DecraftingUIState)?.history?.Clear();

			Main.OnResolutionChanged -= PendingResolutionChange;
		}

		uiInterface = null;
		craftingUI = null;
		storageUI = null;
		environmentUI = null;
		decraftingUI = null;
		securityUI = null;

		Obsolete_ClearSearchBars();
	}

	[Obsolete]
	private static void Obsolete_ClearSearchBars() {
		UISearchBar.ClearList();
	}

	private static void PendingResolutionChange(Vector2 resolution) {
		pendingUIChangeForAnyReason = true;
	}

	internal class MouseCache {
		public readonly int oldMouseX, oldMouseY, oldPIMouseX, oldPIMouseY, oldPIOrigMouseX, oldPIOrigMouseY, oldPIOrigLastMouseX, oldPIOrigLastMouseY;
		public readonly bool oldMouseLeft, oldMouseLeftRelease, oldMouseRight, oldMouseRightRelease;

		private MouseCache() {
			oldMouseX = Main.mouseX;
			oldMouseY = Main.mouseY;
			oldMouseLeft = Main.mouseLeft;
			oldMouseLeftRelease = Main.mouseLeftRelease;
			oldMouseRight = Main.mouseRight;
			oldMouseRightRelease = Main.mouseRightRelease;
			oldPIMouseX = PlayerInput.MouseX;
			oldPIMouseY = PlayerInput.MouseY;
			oldPIOrigMouseX = PlayerInput._originalMouseX;
			oldPIOrigMouseY = PlayerInput._originalMouseY;
			oldPIOrigLastMouseX = PlayerInput._originalLastMouseX;
			oldPIOrigLastMouseY = PlayerInput._originalLastMouseY;
		}

		private static MouseCache cache;
		public static bool didBlockActions;

		public static void Cache() {
			if (cache is not null)
				return;

			cache = new();
		}

		public static void Block() {
			if (cache is null)
				return;

			Main.mouseX = -1;
			Main.mouseY = -1;
			Main.mouseLeft = Main.mouseLeftRelease = Main.mouseRight = Main.mouseRightRelease = false;
			PlayerInput.MouseX = -1;
			PlayerInput.MouseY = -1;
			PlayerInput._originalMouseX = -1;
			PlayerInput._originalMouseY = -1;
			PlayerInput._originalLastMouseX = -1;
			PlayerInput._originalLastMouseY = -1;
		}

		public static void FreeCache(bool destroy) {
			if (cache is not MouseCache c)
				return;

			if (destroy)
				cache = null;

			Main.mouseX = c.oldMouseX;
			Main.mouseY = c.oldMouseY;
			Main.mouseLeft = c.oldMouseLeft;
			Main.mouseLeftRelease = c.oldMouseLeftRelease;
			Main.mouseRight = c.oldMouseRight;
			Main.mouseRightRelease = c.oldMouseRightRelease;
			PlayerInput.MouseX = c.oldPIMouseX;
			PlayerInput.MouseY = c.oldPIMouseY;
			PlayerInput._originalMouseX = c.oldPIOrigMouseX;
			PlayerInput._originalMouseY = c.oldPIOrigMouseY;
			PlayerInput._originalLastMouseX = c.oldPIOrigLastMouseX;
			PlayerInput._originalLastMouseY = c.oldPIOrigLastMouseY;
		}
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int inventoryIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Inventory");
		if (inventoryIndex != -1) {
			//Panel logic
			layers.Insert(0, new LegacyGameInterfaceLayer("MagicStorage: UI Panel Logic",
				() => {
					if (ItemSlotDetours.PreventActions()) {
						MouseCache.Cache();
						MouseCache.Block();
						MouseCache.didBlockActions = true;
					}

					return true;
				}, InterfaceScaleType.UI));

			inventoryIndex++;

			layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer("MagicStorage: StorageAccess",
				() => {
					if (MouseCache.didBlockActions) {
						MouseCache.FreeCache(true);
						MouseCache.didBlockActions = false;
					}

					if (uiInterface?.CurrentState is not null) {
						Main.hidePlayerCraftingMenu = true;

						blockItemSlotActionsDetour = true;

						uiInterface.Draw(Main.spriteBatch, new GameTime());
						if (CanUpdateMouseText())
							UICommon.TooltipMouseText(mouseText);

						blockItemSlotActionsDetour = false;
					}

					return true;
				}, InterfaceScaleType.UI));
		}
	}

	private static GameTime lastGameTime;

	public static bool CanUpdateSearchBars { get; private set; }

	internal static bool pendingUIChangeForAnyReason;
	private static float lastKnownUIScale = -1;

	private static bool pendingClose;
	private static bool layoutWasForciblyChanged;

	public override void UpdateUI(GameTime gameTime) {
		if (lastKnownUIScale != Main.UIScale) {
			lastKnownUIScale = Main.UIScale;
			pendingUIChangeForAnyReason = true;
		}

		if (!Main.playerInventory)
			StoragePlayer.LocalPlayer.CloseStorage();  //Failsafe

		CanUpdateSearchBars = false;
		lastGameTime = gameTime;

		//Some UI elements couldn't easily be updated to the UIElement API, so these two fields still need to be updated
		StorageGUI.oldMouse = StorageGUI.curMouse;
		StorageGUI.curMouse = Mouse.GetState();
		
		blockItemSlotActionsDetour = true;

		if (pendingUIChangeForAnyReason) {
			storageUI.pendingUIChange = true;
			craftingUI.pendingUIChange = true;
			environmentUI.pendingUIChange = true;
			decraftingUI.pendingUIChange = true;
			securityUI.pendingUIChange = true;
			
			pendingUIChangeForAnyReason = false;
		}

		TEStorageHeart heart = StoragePlayer.LocalPlayer.GetStorageHeart();
		bool viewingStorage = heart is not null;

		if (viewingStorage) {
			foreach (var module in heart.GetModules())
				module.PreUpdateUI();
		}

		DummyNPCPool.ResetUpdates();  // Need to enforce at most one update, otherwise the NPCs start ZOOMING

		uiInterface?.Update(gameTime);

		if (viewingStorage) {
			foreach (var module in heart.GetModules())
				module.PostUpdateUI();
		}

		if (layoutWasForciblyChanged) {
			Main.NewTextMultiline(Language.GetTextValue("Mods.Magicstorage.ForcedLayoutChange"), c: Color.Red);
			layoutWasForciblyChanged = false;
			pendingClose = false;
		} else if (pendingClose) {
			Main.NewTextMultiline(Language.GetTextValue("Mods.MagicStorage.PanelTooSmol"), c: Color.Red);
			StoragePlayer.LocalPlayer.CloseStorage();
			pendingClose = false;
		}

		blockItemSlotActionsDetour = false;
	}

	private static bool CanUpdateMouseText()
		=> uiInterface.CurrentState is not null && !object.ReferenceEquals(uiInterface.CurrentState.GetElementAt(new Vector2(Main.mouseX, Main.mouseY)), uiInterface.CurrentState);

	public override void PostUpdateInput() {
		CanUpdateSearchBars = true;

		if (Main.dedServ)
			return;

		Obsolete_UpdateSearchBars();
	}

	[Obsolete]
	private static void Obsolete_UpdateSearchBars() {
		foreach (var searchBar in UISearchBar.SearchBars)
			searchBar.Update(lastGameTime);
	}

	internal static void OpenUI() {
		Main.playerInventory = true;

		if (uiInterface.CurrentState is not null)
			return;  //UI is already open

		Player player = Main.LocalPlayer;
		StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
		Point16 storageAccess = modPlayer.ViewingStorage();
		if (storageAccess.X < 0 || storageAccess.Y < 0)
			return;

		ModTile modTile = TileLoader.GetTile(Main.tile[storageAccess.X, storageAccess.Y].TileType);
		if (modTile is not StorageAccess access)
			return;

		if (player.GetModPlayer<SecurityPlayer>().RequestingSecurityUI)
			uiInterface.SetState(securityUI);
		else if (access is EnvironmentAccess)
			uiInterface.SetState(environmentUI);
		else if (access is CraftingAccess)
			uiInterface.SetState(craftingUI);
		else if (access is DecraftingAccess)
			uiInterface.SetState(decraftingUI);
		else
			uiInterface.SetState(storageUI);

		_checkedAccessibility = false;
	}

	internal static void CloseUI() {
		StopCurrentThread();

		uiInterface.SetState(null);

		mouseText = "";
	}

	internal static void CloseUIDueToHeightLimit() {
		pendingClose = true;
		layoutWasForciblyChanged = false;
	}

	internal static bool AttemptForcedLayoutChange(BaseStorageUI ui) {
		if (MagicStorageConfig.ButtonUIMode is ButtonConfigurationMode.ModernPaged)
			return false;

		layoutWasForciblyChanged = true;
		pendingClose = false;
		ui.ForceLayoutTo(ButtonConfigurationMode.ModernPaged);
		return true;
	}
}
