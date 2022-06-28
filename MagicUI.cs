using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage;

public class MagicUI : ModSystem
{
	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		InterfaceHelper.ModifyInterfaceLayers(layers);
	}

	public override void PostUpdateInput()
	{
		if (!Main.instance.IsActive)
			return;

		if (!Main.gameMenu && StoragePlayer.LocalPlayer.ViewingStorage().X >= 0)
			Main.hidePlayerCraftingMenu = true;

		StorageGUI.Update(null);
		CraftingGUI.Update(null);
	}
}
