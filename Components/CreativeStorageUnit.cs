using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MagicStorage.Components
{
	public class CreativeStorageUnit : StorageComponent
	{
		public override ModTileEntity GetTileEntity()
		{
			return mod.GetTileEntity("TECreativeStorageUnit");
		}

		public override int ItemType(int frameX, int frameY)
		{
			return mod.ItemType("CreativeStorageUnit");
		}
	}
}