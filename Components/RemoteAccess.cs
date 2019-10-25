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
	public class RemoteAccess : StorageAccess
	{
		public override ModTileEntity GetTileEntity()
		{
			return mod.GetTileEntity("TERemoteAccess");
		}

		public override int ItemType(int frameX, int frameY)
		{
			return mod.ItemType("RemoteAccess");
		}

		public override bool HasSmartInteract()
		{
			return true;
		}

		public override TEStorageHeart GetHeart(int i, int j)
		{
			TileEntity ent = TileEntity.ByPosition[new Point16(i, j)];
			return ((TERemoteAccess)ent).GetHeart();
		}

		public override bool NewRightClick(int i, int j)
		{
			Player player = Main.player[Main.myPlayer];
			Item item = player.inventory[player.selectedItem];
			if (item.type == mod.ItemType("Locator") || item.type == mod.ItemType("LocatorDisk"))
			{
				if (Main.tile[i, j].frameX % 36 == 18)
				{
					i--;
				}
				if (Main.tile[i, j].frameY % 36 == 18)
				{
					j--;
				}
				TERemoteAccess ent = (TERemoteAccess)TileEntity.ByPosition[new Point16(i, j)];
				Locator locator = (Locator)item.modItem;
				string message;
				if (ent.TryLocate(locator.location, out message))
				{
					if (item.type == mod.ItemType("LocatorDisk"))
					{
						locator.location = new Point16(-1, -1);
					}
					else
					{
						item.SetDefaults(0);
					}
				}
				if (player.selectedItem == 58)
				{
					Main.mouseItem = item.Clone();
				}
				Main.NewText(message);
                return true;
			}
			else
			{
				return base.NewRightClick(i, j);
			}
		}
	}
}