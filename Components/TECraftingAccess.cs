using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
	public class TECraftingAccess : TEStorageComponent
	{
	    public const int Rows = 3;
	    public const int ItemsPerRow = 15;
	    public const int ItemsTotal = Rows * ItemsPerRow;

		public Item[] stations = new Item[ItemsTotal];

		public TECraftingAccess()
		{
			for (int k = 0; k < ItemsTotal; k++)
			{
				stations[k] = new Item();
			}
		}

		public override bool ValidTile(Tile tile)
		{
			return tile.type == mod.TileType("CraftingAccess") && tile.frameX == 0 && tile.frameY == 0;
		}

		public void TryDepositStation(Item item)
		{
			if (Main.netMode == 1)
			{
				return;
			}
			foreach (Item station in stations)
			{
				if (station.type == item.type)
				{
					return;
				}
			}
			for (int k = 0; k < stations.Length; k++)
			{
				if (stations[k].IsAir)
				{
					stations[k] = item.Clone();
					stations[k].stack = 1;
					item.stack--;
					if (item.stack <= 0)
					{
						item.SetDefaults(0);
					}
					NetHelper.SendTEUpdate(ID, Position);
					return;
				}
			}
		}

		public Item TryWithdrawStation(int slot)
		{
			if (Main.netMode == 1)
			{
				return new Item();
			}
			if (!stations[slot].IsAir)
			{
				Item item = stations[slot];
				stations[slot] = new Item();
				NetHelper.SendTEUpdate(ID, Position);
				return item;
			}
			return new Item();
		}

		public Item DoStationSwap(Item item, int slot)
		{
			if (Main.netMode == 1)
			{
				return new Item();
			}
			if (!item.IsAir)
			{
				for (int k = 0; k < stations.Length; k++)
				{
					if (k != slot && stations[k].type == item.type)
					{
						return item;
					}
				}
			}
			if ((item.IsAir || item.stack == 1) && !stations[slot].IsAir)
			{
				Item temp = item;
				item = stations[slot];
				stations[slot] = temp;
				NetHelper.SendTEUpdate(ID, Position);
				return item;
			}
			else if (!item.IsAir && stations[slot].IsAir)
			{
				stations[slot] = item.Clone();
				stations[slot].stack = 1;
				item.stack--;
				if (item.stack <= 0)
				{
					item.SetDefaults(0);
				}
				NetHelper.SendTEUpdate(ID, Position);
				return item;
			}
			return item;
		}

		public override TagCompound Save()
		{
			TagCompound tag = new TagCompound();
			IList<TagCompound> listStations = new List<TagCompound>();
			foreach (Item item in stations)
			{
				listStations.Add(ItemIO.Save(item));
			}
			tag["Stations"] = listStations;
			return tag;
		}

		public override void Load(TagCompound tag)
		{
			IList<TagCompound> listStations = tag.GetList<TagCompound>("Stations");
			if (listStations != null && listStations.Count > 0)
			{
				for (int k = 0; k < stations.Length; k++)
				{
                    if (k < listStations.Count)
                        stations[k] = ItemIO.Load(listStations[k]);
                    else
                        stations[k] = new Item();
                }
			}
		}

		public override void NetSend(BinaryWriter writer, bool lightSend)
		{
			foreach (Item item in stations)
			{
				ItemIO.Send(item, writer, true, true);
			}
		}

		public override void NetReceive(BinaryReader reader, bool lightReceive)
		{
			for (int k = 0; k < stations.Length; k++)
			{
				stations[k] = ItemIO.Receive(reader, true, true);
			}
		}
	}
}