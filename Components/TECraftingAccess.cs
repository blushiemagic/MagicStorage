using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Collections.Concurrent;
using Terraria.DataStructures;

namespace MagicStorage.Components
{
	public class TECraftingAccess : TEStorageComponent
	{
		public enum Operation : byte
		{
			Withdraw,
			WithdrawToInventory,
			Deposit,
		}

		private class NetOperation
		{
			public NetOperation(Operation _type, Item _item, int _client)
			{
				type = _type;
				item = _item;
				client = _client;
			}

			public NetOperation(Operation _type, int _slot, int _client)
			{
				type = _type;
				slot = _slot;
				client = _client;
			}

			public Operation type { get; }
			public int slot { get; }
			public Item item { get; }
			public int client { get; }
		}
		ConcurrentQueue<NetOperation> clientOpQ = new ConcurrentQueue<NetOperation>();

		public const int Rows = 3;
		public const int Columns = 15;
		public const int ItemsTotal = Rows * Columns;

		//public Item[] stations = new Item[ItemsTotal];
		public List<Item> stations = new List<Item>();

		public TECraftingAccess()
		{
		}

		public override void Update()
		{
			if (Main.netMode == NetmodeID.Server)
			{
				processClientOperations();
			}
		}

		private void processClientOperations()
		{
			int opCount = clientOpQ.Count;
			if (opCount > 0)
			{
				for (int i = 0; i < opCount; ++i)
				{
					NetOperation op;
					if (clientOpQ.TryDequeue(out op))
					{
						if (op.type == Operation.Withdraw || op.type == Operation.WithdrawToInventory)
						{
							Item item = WithdrawStation(op.slot);
							if (!item.IsAir)
							{
								ModPacket packet = PrepareServerResult(op.type);
								ItemIO.Send(item, packet, true, true);
								packet.Send(op.client);
							}
						}
						else
						{
							Item item = DepositStation(op.item);
							if (item.stack > 0)
							{
								ModPacket packet = PrepareServerResult(op.type);
								ItemIO.Send(item, packet, true, true);
								packet.Send(op.client);
							}
						}
						NetHelper.SendTEUpdate(ID, Position);
					}
				}

				Point16 pos = Position;
				StorageAccess modTile = TileLoader.GetTile(Main.tile[pos.X, pos.Y].TileType) as StorageAccess;
				TEStorageHeart heart = modTile?.GetHeart(pos.X, pos.Y);
				if (heart is not null)
					NetHelper.SendRefreshNetworkItems(heart.Position);
			}
		}

		public void QClientOperation(BinaryReader reader, Operation op, int client)
		{
			if (op == Operation.Withdraw || op == Operation.WithdrawToInventory)
			{
				byte slot = reader.ReadByte();
				clientOpQ.Enqueue(new NetOperation(op, slot, client));

				NetHelper.PrintClientRequest(client, "Item Withdraw", Position);
			}
			else
			{
				Item item = ItemIO.Receive(reader, true, true);
				clientOpQ.Enqueue(new NetOperation(op, item, client));

				NetHelper.PrintClientRequest(client, "Item Deposit", Position);
			}
		}

		private static ModPacket PrepareServerResult(Operation op)
		{
			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ServerStationOperationResult);
			packet.Write((byte)op);
			return packet;
		}

		private ModPacket PrepareClientRequest(Operation op)
		{
			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ClientStationOperation);
			packet.Write(Position);
			packet.Write((byte)op);
			return packet;
		}

		public override bool ValidTile(in Tile tile) => tile.TileType == ModContent.TileType<CraftingAccess>() && tile.TileFrameX == 0 && tile.TileFrameY == 0;

		private Item DepositStation(Item item)
		{
			if (stations.Count < ItemsTotal)
			{
				bool foundSame = false;
				foreach (Item station in stations)
				{
					if (station.type == item.type)
					{
						foundSame = true;
						break;
					}
				}

				if (!foundSame)
				{
					Item nItem = item.Clone();
					nItem.stack = 1;
					stations.Add(nItem);
					item.stack--;
					if (item.stack <= 0)
						item.SetDefaults();

					UpdateRecipesFromStationAction(nItem.createTile);
				}
			}

			return item;
		}

		public Item TryDepositStation(Item item)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = PrepareClientRequest(Operation.Deposit);
				ItemIO.Send(item, packet, true, true);
				packet.Send();
				item.SetDefaults(0, true);
			}
			else
			{
				DepositStation(item);
			}

			return item;
		}

		private Item WithdrawStation(int slot)
		{
			if (slot >= stations.Count)
				return new Item();

			var item = stations[slot];
			stations.RemoveAt(slot);

			UpdateRecipesFromStationAction(item.createTile);

			return item;
		}

		private static void UpdateRecipesFromStationAction(int stationTile) {
			bool[] adjTiles = (bool[])Main.LocalPlayer.adjTile.Clone();

			TileLoader.AdjTiles(Main.LocalPlayer, stationTile);

			CraftingGUI.SetNextDefaultRecipeCollectionToRefreshFromTile(Main.LocalPlayer.adjTile.Select(static (b, i) => b ? i : -1).Where(static i => i >= 0).Prepend(stationTile));

			Main.LocalPlayer.adjTile = adjTiles;
		}

		public Item TryWithdrawStation(int slot, bool toInventory = false)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = PrepareClientRequest(toInventory ? Operation.WithdrawToInventory : Operation.Withdraw);
				packet.Write((byte) slot);
				packet.Send();

				return new Item();
			}

			var item = WithdrawStation(slot);
			StoragePlayer.GetItem(new EntitySource_TileEntity(this), item, !toInventory);

			return item;
		}

		public override void SaveData(TagCompound tag)
		{
			tag["Stations"] = stations.Select(ItemIO.Save).ToList();
		}

		public override void LoadData(TagCompound tag)
		{
			IList<TagCompound> listStations = tag.GetList<TagCompound>("Stations");
			if (listStations is not null && listStations.Count > 0)
			{
				foreach (TagCompound stationTag in listStations)
				{
					Item item = ItemIO.Load(stationTag);
					if (!item.IsAir)
					{
						stations.Add(ItemIO.Load(stationTag));
					}
				}
			}
		}

		public override void NetSend(BinaryWriter writer)
		{
			writer.Write(stations.Count);
			foreach (Item item in stations)
				ItemIO.Send(item, writer, true, true);
		}

		public override void NetReceive(BinaryReader reader)
		{
			int stationsCount = reader.ReadInt32();
			stations = new List<Item>();
			for (int k = 0; k < stationsCount; k++)
				stations.Add(ItemIO.Receive(reader, true, true));
		}
	}
}
