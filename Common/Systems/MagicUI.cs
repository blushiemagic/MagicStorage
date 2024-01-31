using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using MagicStorage.Components;
using MagicStorage.Edits;
using MagicStorage.UI;
using MagicStorage.UI.Input;
using MagicStorage.UI.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.Common.Systems;

public class MagicUI : ModSystem
{
	public static UserInterface uiInterface;

	public static BaseStorageUI craftingUI, storageUI, environmentUI, decraftingUI;

	public static bool IsStorageUIOpen() => storageUI is not null && object.ReferenceEquals(uiInterface?.CurrentState, storageUI);

	public static bool IsCraftingUIOpen() => craftingUI is not null && object.ReferenceEquals(uiInterface?.CurrentState, craftingUI);

	public static bool IsEnvironmentUIOpen() => environmentUI is not null && object.ReferenceEquals(uiInterface?.CurrentState, environmentUI);

	public static bool IsDecraftingUIOpen() => decraftingUI is not null && object.ReferenceEquals(uiInterface?.CurrentState, decraftingUI);

	private static bool _refreshUI;
	public static bool RefreshUI {
		get => _refreshUI;
		set => _refreshUI |= value;
	}

	public static bool CurrentlyRefreshing { get; internal set; }

	public static event Action OnRefresh;
		
	private static bool forceFullRefresh;
	public static bool ForceNextRefreshToBeFull {
		get => forceFullRefresh || StorageGUI.Obsolete_needRefresh();
		set => forceFullRefresh |= value;
	}

	internal static StorageGUI.ThreadContext activeThread;

	public static int CurrentThreadingDuration { get; internal set; }

	/// <summary>
	/// Shorthand for setting <see cref="RefreshUI"/> to <see langword="true"/> and also setting <see cref="ForceNextRefreshToBeFull"/>
	/// </summary>
	public static void SetRefresh(bool forceFullRefresh = false) {
		RefreshUI = true;
		ForceNextRefreshToBeFull = forceFullRefresh;
	}

	private static bool _pendingWatchdogPulse;

	public static void PulseWatchdogs() => _pendingWatchdogPulse = true;

	internal static void CheckRefresh() {
		if (!CurrentlyRefreshing && _pendingWatchdogPulse) {
			if (IsCraftingUIOpen())
				CraftingGUI.ExecuteInCraftingGuiEnvironment(HandleWatchdogs);
			else
				HandleWatchdogs();

			_pendingWatchdogPulse = false;
		}

		if (RefreshUI)
			RefreshItems();

		if (activeThread?.Running is true)
			CurrentThreadingDuration++;
		else
			CurrentThreadingDuration = 0;
	}

	private static void HandleWatchdogs() {
		// Check the watchdogs
		foreach (var watchdog in _watchdogs) {
			if (watchdog.Observe()) {
				watchdog.OnStateChange(out bool forceFullRefresh);
				SetRefresh(forceFullRefresh);
			}
		}
	}

	internal static void InvokeOnRefresh() {
		OnRefresh?.Invoke();
		StorageGUI.InvokeOnRefresh();
	}

	public static void SetNextCollectionsToRefresh(int itemType) {
		SetRefresh();
		StorageGUI.SetNextItemTypeToRefresh(itemType);
		CraftingGUI.SetNextDefaultRecipeCollectionToRefresh(itemType);
		DecraftingGUI.SetNextDefaultItemCollectionToRefresh(itemType);
	}

	public static void SetNextCollectionsToRefresh(IEnumerable<int> itemTypes) {
		SetRefresh();
		StorageGUI.SetNextItemTypesToRefresh(itemTypes);
		CraftingGUI.SetNextDefaultRecipeCollectionToRefresh(itemTypes);
		DecraftingGUI.SetNextDefaultItemCollectionToRefresh(itemTypes);
	}

	internal static void StopCurrentThread() {
		_watchdogs.Clear();

		if (activeThread is not null) {
			CurrentlyRefreshing = false;
			activeThread.Stop();
			activeThread = null;
		}
	}

	public static void RefreshItems() {
		_refreshUI = false;
		StorageGUI.Obsolete_needRefresh() = false;

		if (IsStorageUIOpen()) {
			CraftingGUI.ResetRefreshCache();
			DecraftingGUI.ResetRefreshCache();

			StorageGUI.RefreshItems_Inner();
		} else if (IsCraftingUIOpen()) {
			StorageGUI.ResetRefreshCache();
			DecraftingGUI.ResetRefreshCache();

			CraftingGUI.RefreshItems_Inner();
		} else if (IsDecraftingUIOpen()) {
			StorageGUI.ResetRefreshCache();
			CraftingGUI.ResetRefreshCache();

			DecraftingGUI.RefreshItems();
		} else {
			StorageGUI.ResetRefreshCache();
			CraftingGUI.ResetRefreshCache();
			DecraftingGUI.ResetRefreshCache();
		}

		forceFullRefresh = false;
	}

	internal static IEntitySource GetShimmeringSpawnSource() => new EntitySource_Parent(Main.LocalPlayer);

	private static readonly ConcurrentBag<RefreshUIWatchdog> _watchdogs = new();

	public static void AddRefreshWatchdog(IRefreshUIWatchTarget target, bool? initialStateOverride = null) {
		ArgumentNullException.ThrowIfNull(target);

		_watchdogs.Add(new RefreshUIWatchdog(target, initialStateOverride ?? target.GetCurrentState()));
	}

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

		Obsolete_ClearSearchBars();
		TextInputTracker.Unload();
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

		public static readonly FieldInfo PlayerInput__originalMouseX = typeof(PlayerInput).GetField("_originalMouseX", BindingFlags.NonPublic | BindingFlags.Static);
		public static readonly FieldInfo PlayerInput__originalMouseY = typeof(PlayerInput).GetField("_originalMouseY", BindingFlags.NonPublic | BindingFlags.Static);
		public static readonly FieldInfo PlayerInput__originalLastMouseX = typeof(PlayerInput).GetField("_originalLastMouseX", BindingFlags.NonPublic | BindingFlags.Static);
		public static readonly FieldInfo PlayerInput__originalLastMouseY = typeof(PlayerInput).GetField("_originalLastMouseY", BindingFlags.NonPublic | BindingFlags.Static);

		private MouseCache() {
			oldMouseX = Main.mouseX;
			oldMouseY = Main.mouseY;
			oldMouseLeft = Main.mouseLeft;
			oldMouseLeftRelease = Main.mouseLeftRelease;
			oldMouseRight = Main.mouseRight;
			oldMouseRightRelease = Main.mouseRightRelease;
			oldPIMouseX = PlayerInput.MouseX;
			oldPIMouseY = PlayerInput.MouseY;
			oldPIOrigMouseX = (int)PlayerInput__originalMouseX.GetValue(null);
			oldPIOrigMouseY = (int)PlayerInput__originalMouseY.GetValue(null);
			oldPIOrigLastMouseX = (int)PlayerInput__originalLastMouseX.GetValue(null);
			oldPIOrigLastMouseY = (int)PlayerInput__originalLastMouseY.GetValue(null);
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
			PlayerInput__originalMouseX.SetValue(null, -1);
			PlayerInput__originalMouseY.SetValue(null, -1);
			PlayerInput__originalLastMouseX.SetValue(null, -1);
			PlayerInput__originalLastMouseY.SetValue(null, -1);
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
			PlayerInput__originalMouseX.SetValue(null, c.oldPIOrigMouseX);
			PlayerInput__originalMouseY.SetValue(null, c.oldPIOrigMouseY);
			PlayerInput__originalLastMouseX.SetValue(null, c.oldPIOrigLastMouseX);
			PlayerInput__originalLastMouseY.SetValue(null, c.oldPIOrigLastMouseY);
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
							Main.instance.MouseText(mouseText);

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
			if (craftingUI is CraftingUIState cUI)
				cUI.pendingUIChange = true;

			if (storageUI is StorageUIState sUI)
				sUI.pendingUIChange = true;

			if (environmentUI is EnvironmentUIState eUI)
				eUI.pendingUIChange = true;
			
			pendingUIChangeForAnyReason = false;
		}

		TEStorageHeart heart = StoragePlayer.LocalPlayer.GetStorageHeart();
		bool viewingStorage = heart is not null;

		if (viewingStorage) {
			foreach (var module in heart.GetModules())
				module.PreUpdateUI();
		}

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
		TextInputTracker.Update(lastGameTime);
	}

	[Obsolete]
	private static void Obsolete_UpdateSearchBars() {
		foreach (var searchBar in UISearchBar.SearchBars)
			searchBar.Update(lastGameTime);
	}

	internal static void OpenUI() {
		if (uiInterface.CurrentState is not null)
			return;  //UI is already open

		Player player = Main.LocalPlayer;
		StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
		Point16 storageAccess = modPlayer.ViewingStorage();
		if (!Main.playerInventory || storageAccess.X < 0 || storageAccess.Y < 0)
			return;

		ModTile modTile = TileLoader.GetTile(Main.tile[storageAccess.X, storageAccess.Y].TileType);
		if (modTile is not StorageAccess access)
			return;

		TEStorageHeart heart = access.GetHeart(storageAccess.X, storageAccess.Y);
		if (heart is null)
			return;

		if (access is EnvironmentAccess)
			uiInterface.SetState(environmentUI);
		else if (access is CraftingAccess)
			uiInterface.SetState(craftingUI);
		else if (access is DecraftingAccess)
			uiInterface.SetState(decraftingUI);
		else
			uiInterface.SetState(storageUI);
	}

	internal static void CloseUI() {
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
