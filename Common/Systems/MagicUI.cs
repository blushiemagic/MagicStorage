using System.Collections.Generic;
using System.Reflection;
using MagicStorage.Components;
using MagicStorage.Edits;
using MagicStorage.UI;
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

	public static BaseStorageUI craftingUI, storageUI, environmentUI;

	//Assign text to this value instead of using Main.instance.MouseText() in the MouseOver and MouseOut events
	internal static string mouseText;

	internal static bool BlockItemSlotActionsDetour { get; set; }

	public override void Load() {
		if (Main.dedServ)
			return;

		uiInterface = new();
		craftingUI = new CraftingUIState();
		storageUI = new StorageUIState();
		environmentUI = new EnvironmentUIState();

		Main.OnResolutionChanged += PendingResolutionChange;
	}

	public override void Unload() {
		if (!Main.dedServ) {
			(craftingUI as CraftingUIState)?.history?.Clear();

			Main.OnResolutionChanged -= PendingResolutionChange;
		}

		uiInterface = null;
		craftingUI = null;
		storageUI = null;
		environmentUI = null;

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
			oldMouseRightRelease = Main.mouseRight;
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

						BlockItemSlotActionsDetour = true;

						uiInterface.Draw(Main.spriteBatch, new GameTime());

						BlockItemSlotActionsDetour = false;
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
		
		BlockItemSlotActionsDetour = true;

		if (pendingUIChangeForAnyReason) {
			if (craftingUI is CraftingUIState cUI)
				cUI.pendingUIChange = true;

			if (storageUI is StorageUIState sUI)
				sUI.pendingUIChange = true;

			if (environmentUI is EnvironmentUIState eUI)
				eUI.pendingUIChange = true;
			
			pendingUIChangeForAnyReason = false;
		}

		uiInterface?.Use();
		uiInterface?.Update(gameTime);

		if (pendingClose) {
			Main.NewTextMultiline(Language.GetTextValue("Mods.MagicStorage.PanelTooSmol"), c: Color.Red);
			StoragePlayer.LocalPlayer.CloseStorage();
			pendingClose = false;
		}

		BlockItemSlotActionsDetour = false;

		if (CanUpdateMouseText())
			Main.instance.MouseText(mouseText);
	}

	private static bool CanUpdateMouseText()
		=> uiInterface.CurrentState is not null && !object.ReferenceEquals(uiInterface.CurrentState.GetElementAt(new Vector2(Main.mouseX, Main.mouseY)), uiInterface.CurrentState);

	public override void PostUpdateInput() {
		CanUpdateSearchBars = true;

		if (Main.dedServ)
			return;

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
		else
			uiInterface.SetState(storageUI);

		StorageGUI.needRefresh = true;
	}

	internal static void CloseUI() {
		uiInterface.SetState(null);

		mouseText = "";
	}

	internal static void CloseUIDueToHeightLimit() => pendingClose = true;
}
