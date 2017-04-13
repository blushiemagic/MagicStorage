using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Microsoft.Xna.Framework;
using MagicStorage.Items;

namespace MagicStorage.Components
{
	public class CraftingAccess : StorageAccess
	{
		public override ModTileEntity GetTileEntity()
		{
			return mod.GetTileEntity("TECraftingAccess");
		}

		public override int ItemType(int frameX, int frameY)
		{
			return mod.ItemType("CraftingAccess");
		}

		public override TEStorageHeart GetHeart(int i, int j)
		{
			Point16 point = TEStorageComponent.FindStorageCenter(new Point16(i, j));
			if (point.X < 0 || point.Y < 0 || !TileEntity.ByPosition.ContainsKey(point))
			{
				return null;
			}
			TileEntity heart = TileEntity.ByPosition[point];
			if (!(heart is TEStorageCenter))
			{
				return null;
			}
			return ((TEStorageCenter)heart).GetHeart();
		}
	}
}