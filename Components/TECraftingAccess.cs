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
		public Item[] stations = new Item[10];

		public override bool ValidTile(Tile tile)
		{
			return tile.type == mod.TileType("CraftingAccess") && tile.frameX == 0 && tile.frameY == 0;
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
			for (int k = 0; k < stations.Length; k++)
			{
				stations[k] = ItemIO.Load(listStations[k]);
			}
		}

		public override void NetSend(BinaryWriter writer, bool lightSend)
		{
			foreach (Item item in stations)
			{
				ItemIO.Send(item, writer, true, false);
			}
		}

		public override void NetReceive(BinaryReader reader, bool lightReceive)
		{
			for (int k = 0; k < stations.Length; k++)
			{
				stations[k] = ItemIO.Receive(reader, true, false);
			}
		}
	}
}