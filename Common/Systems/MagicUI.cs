using System.Collections.Generic;
using MagicStorage.Components;
using MagicStorage.UI;
using MagicStorage.UI.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.Common.Systems;

public class MagicUI : ModSystem
{
	internal static UserInterface uiInterface;

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
	}

	public override void Unload() {
		uiInterface = null;
		craftingUI = null;
		storageUI = null;
		environmentUI = null;

		UISearchBar.ClearList();
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int inventoryIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Inventory");
		if (inventoryIndex != -1) {
			layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer("MagicStorage: StorageAccess",
				() => {
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

	public override void UpdateUI(GameTime gameTime) {
		CanUpdateSearchBars = false;
		lastGameTime = gameTime;

		//Some UI elements couldn't easily be updated to the UIElement API, so these two fields still need to be updated
		StorageGUI.oldMouse = StorageGUI.curMouse;
		StorageGUI.curMouse = Mouse.GetState();
		
		BlockItemSlotActionsDetour = true;

		uiInterface?.Update(gameTime);

		BlockItemSlotActionsDetour = false;

		Main.instance.MouseText(mouseText);
	}

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
	}

	internal static void CloseUI() {
		uiInterface.SetState(null);
	}
}
