using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace MagicStorage
{
	public class MagicStorage : Mod
	{
		public static MagicStorage Instance;

		public override void Load()
		{
			Instance = this;
		}

		public override void PostSetupContent()
		{
			
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			NetHelper.HandlePacket(reader, whoAmI);
		}

		public override void ModifyInterfaceLayers(List<MethodSequenceListItem> layers)
		{
			InterfaceHelper.ModifyInterfaceLayers(layers);
		}

		public override void PostUpdateInput()
		{
			StorageGUI.Update(null);
		}
	}
}

