using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace MagicStorage
{
	public class MagicStorage : Mod
	{
		public static MagicStorage Instance;

		public static readonly Version requiredVersion = new Version(0, 9, 2, 2);

		public override void Load()
		{
			if (ModLoader.version < requiredVersion)
			{
				throw new Exception("Magic storage requires a tModLoader version of at least " + requiredVersion);
			}
			Instance = this;
			InterfaceHelper.Initialize();
		}

		public override void PostSetupContent()
		{
			
		}

		public override void AddRecipeGroups()
		{
			RecipeGroup group = new RecipeGroup(() => Lang.misc[37] + " Craftable Wood Chest", ItemID.Chest, ItemID.EbonwoodChest, ItemID.RichMahoganyChest, ItemID.PearlwoodChest, ItemID.ShadewoodChest, ItemID.PalmWoodChest, ItemID.BorealWoodChest, ItemID.SpookyChest);
			RecipeGroup.RegisterGroup("MagicStorage:WoodChest", group);
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
			CraftingGUI.Update(null);
		}
	}
}

