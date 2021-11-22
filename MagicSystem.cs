using System.Collections.Generic;
using System.IO;
using MagicStorage.Edits;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage
{
	// TODO: think of a better name
	public class MagicSystem : ModSystem
	{
		public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
		{
			return false;
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			InterfaceHelper.ModifyInterfaceLayers(layers);
		}

		public override void PostUpdateInput()
		{
			if (!Main.instance.IsActive)
				return;

			StorageGUI.Update(null);
			CraftingGUI.Update(null);
		}
	}
}
