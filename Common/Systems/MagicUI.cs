using System.Collections.Generic;
using MagicStorage.Components;
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
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int inventoryIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Inventory");
		if (inventoryIndex != -1) {
			layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer("MagicStorage: StorageAccess",
				() => {
					if (uiInterface?.CurrentState is not null) {
						Main.hidePlayerCraftingMenu = true;
						uiInterface.Draw(Main.spriteBatch, new GameTime());
					}

					return true;
				}, InterfaceScaleType.UI));
		}
	}

	public override void UpdateUI(GameTime gameTime) {
		//Some UI elements couldn't easily be updated to the UIElement API, so these two fields still need to be updated
		StorageGUI.oldMouse = StorageGUI.curMouse;
		StorageGUI.curMouse = Mouse.GetState();
		
		uiInterface?.Update(gameTime);
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
