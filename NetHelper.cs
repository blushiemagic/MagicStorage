using System;
using System.IO;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using MagicStorage.Components;

namespace MagicStorage
{
	public static class NetHelper
	{
		private static bool queueUpdates = false;
		private static Queue<int> updateQueue = new Queue<int>();
		private static HashSet<int> updateQueueContains = new HashSet<int>();

		public static void HandlePacket(BinaryReader reader, int sender)
		{
			MessageType type = (MessageType)reader.ReadByte();
			if (type == MessageType.SearchAndRefreshNetwork)
			{
				ReceiveSearchAndRefresh(reader);
			}
			else if (type == MessageType.TryStorageOperation)
			{
				ReceiveStorageOperation(reader, sender);
			}
			else if (type == MessageType.StorageOperationResult)
			{
				ReceiveOperationResult(reader);
			}
			else if (type == MessageType.RefreshNetworkItems)
			{
				StorageGUI.RefreshItems();
			}
			else if (type == MessageType.ClientSendTEUpdate)
			{
				ReceiveClientSendTEUpdate(reader, sender);
			}
			else if (type == MessageType.TryStationOperation)
			{
				ReceiveStationOperation(reader, sender);
			}
			else if (type == MessageType.StationOperationResult)
			{
				ReceiveStationResult(reader);
			}
		}

		public static void SendComponentPlace(int i, int j, int type)
		{
			if (Main.netMode == 1)
			{
				NetMessage.SendTileRange(Main.myPlayer, i, j, 2, 2);
				NetMessage.SendData(MessageID.TileEntityPlacement, -1, -1, "", i, j, type);
			}
		}

		public static void StartUpdateQueue()
		{
			queueUpdates = true;
		}

		public static void SendTEUpdate(int id, Point16 position)
		{
			if (Main.netMode != 2)
			{
				return;
			}
			if (queueUpdates)
			{
				if (!updateQueueContains.Contains(id))
				{
					updateQueue.Enqueue(id);
					updateQueueContains.Add(id);
				}
			}
			else
			{
				NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, "", id, position.X, position.Y);
			}
		}

		public static void ProcessUpdateQueue()
		{
			if (queueUpdates)
			{
				queueUpdates = false;
				while (updateQueue.Count > 0)
				{
					NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, "", updateQueue.Dequeue());
				}
				updateQueueContains.Clear();
			}
		}

		public static void SendSearchAndRefresh(int i, int j)
		{
			if (Main.netMode == 1)
			{
				ModPacket packet = MagicStorage.Instance.GetPacket();
				packet.Write((byte)MessageType.SearchAndRefreshNetwork);
				packet.Write((short)i);
				packet.Write((short)j);
				packet.Send();
			}
		}

		private static void ReceiveSearchAndRefresh(BinaryReader reader)
		{
			Point16 point = new Point16(reader.ReadInt16(), reader.ReadInt16());
			TEStorageComponent.SearchAndRefreshNetwork(point);
		}

		private static ModPacket PrepareStorageOperation(int ent, byte op)
		{
			ModPacket packet = MagicStorage.Instance.GetPacket();
			packet.Write((byte)MessageType.TryStorageOperation);
			packet.Write(ent);
			packet.Write(op);
			return packet;
		}

		private static ModPacket PrepareOperationResult(byte op)
		{
			ModPacket packet = MagicStorage.Instance.GetPacket();
			packet.Write((byte)MessageType.StorageOperationResult);
			packet.Write(op);
			return packet;
		}

		public static void SendDeposit(int ent, Item item)
		{
			if (Main.netMode == 1)
			{
				ModPacket packet = PrepareStorageOperation(ent, 0);
				ItemIO.Send(item, packet, true);
				packet.Send();
			}
		}

		public static void SendWithdraw(int ent, Item item, bool toInventory = false)
		{
			if (Main.netMode == 1)
			{
				ModPacket packet = PrepareStorageOperation(ent, (byte)(toInventory ? 3 : 1));
				ItemIO.Send(item, packet, true);
				packet.Send();
			}
		}

		public static void SendDepositAll(int ent, List<Item> items)
		{
			if (Main.netMode == 1)
			{
				ModPacket packet = PrepareStorageOperation(ent, 2);
				packet.Write((byte)items.Count);
				foreach (Item item in items)
				{
					ItemIO.Send(item, packet, true);
				}
				packet.Send();
			}
		}

		public static void ReceiveStorageOperation(BinaryReader reader, int sender)
		{
			if (Main.netMode != 2)
			{
				return;
			}
			int ent = reader.ReadInt32();
			if (!TileEntity.ByID.ContainsKey(ent) || !(TileEntity.ByID[ent] is TEStorageHeart))
			{
				return;
			}
			TEStorageHeart heart = (TEStorageHeart)TileEntity.ByID[ent];
			byte op = reader.ReadByte();
			if (op == 0)
			{
				Item item = ItemIO.Receive(reader, true);
				heart.DepositItem(item);
				if (!item.IsAir)
				{
					ModPacket packet = PrepareOperationResult(op);
					ItemIO.Send(item, packet, true);
					packet.Send(sender);
				}
			}
			else if (op == 1 || op == 3)
			{
				Item item = ItemIO.Receive(reader, true);
				item = heart.TryWithdraw(item);
				if (!item.IsAir)
				{
					ModPacket packet = PrepareOperationResult(op);
					ItemIO.Send(item, packet, true);
					packet.Send(sender);
				}
			}
			else if (op == 2)
			{
				int count = reader.ReadByte();
				List<Item> items = new List<Item>();
				StartUpdateQueue();
				for (int k = 0; k < count; k++)
				{
					Item item = ItemIO.Receive(reader, true);
					heart.DepositItem(item);
					if (!item.IsAir)
					{
						items.Add(item);
					}
				}
				ProcessUpdateQueue();
				if (items.Count > 0)
				{
					ModPacket packet = PrepareOperationResult(op);
					packet.Write((byte)items.Count);
					foreach (Item item in items)
					{
						ItemIO.Send(item, packet, true);
					}
					packet.Send(sender);
				}
			}
			SendRefreshNetworkItems(ent);
		}

		public static void ReceiveOperationResult(BinaryReader reader)
		{
			if (Main.netMode != 1)
			{
				return;
			}
			Player player = Main.player[Main.myPlayer];
			byte op = reader.ReadByte();
			if (op == 0 || op == 1 || op == 3)
			{
				Item item = ItemIO.Receive(reader, true);
				StoragePlayer.GetItem(item, op != 3);
			}
			else if (op == 2)
			{
				int count = reader.ReadByte();
				for (int k = 0; k < count; k++)
				{
					Item item = ItemIO.Receive(reader, true);
					StoragePlayer.GetItem(item, false);
				}
			}
		}

		public static void SendRefreshNetworkItems(int ent)
		{
			if (Main.netMode == 2)
			{
				ModPacket packet = MagicStorage.Instance.GetPacket();
				packet.Write((byte)MessageType.RefreshNetworkItems);
				packet.Write(ent);
				packet.Send();
			}
		}

		public static void ClientSendTEUpdate(int id)
		{
			if (Main.netMode == 1)
			{
				ModPacket packet = MagicStorage.Instance.GetPacket();
				packet.Write((byte)MessageType.ClientSendTEUpdate);
				packet.Write(id);
				TileEntity.Write(packet, TileEntity.ByID[id], true);
				packet.Send();
			}
		}

		public static void ReceiveClientSendTEUpdate(BinaryReader reader, int sender)
		{
			if (Main.netMode == 2)
			{
				int id = reader.ReadInt32();
				TileEntity ent = TileEntity.Read(reader, true);
				ent.ID = id;
				TileEntity.ByID[id] = ent;
				TileEntity.ByPosition[ent.Position] = ent;
				if (ent is TEStorageUnit)
				{
					TEStorageHeart heart = ((TEStorageUnit)ent).GetHeart();
					if (heart != null)
					{
						heart.ResetCompactStage();
					}
				}
				NetMessage.SendData(MessageID.TileEntitySharing, -1, sender, "", id, ent.Position.X, ent.Position.Y);
			}
		}

		private static ModPacket PrepareStationOperation(int ent, byte op)
		{
			ModPacket packet = MagicStorage.Instance.GetPacket();
			packet.Write((byte)MessageType.TryStationOperation);
			packet.Write(ent);
			packet.Write(op);
			return packet;
		}

		private static ModPacket PrepareStationResult(byte op)
		{
			ModPacket packet = MagicStorage.Instance.GetPacket();
			packet.Write((byte)MessageType.StationOperationResult);
			packet.Write(op);
			return packet;
		}

		public static void SendDepositStation(int ent, Item item)
		{
			if (Main.netMode == 1)
			{
				ModPacket packet = PrepareStationOperation(ent, 0);
				ItemIO.Send(item, packet, true);
				packet.Send();
			}
		}

		public static void SendWithdrawStation(int ent, int slot)
		{
			if (Main.netMode == 1)
			{
				ModPacket packet = PrepareStationOperation(ent, 1);
				packet.Write((byte)slot);
				packet.Send();
			}
		}

		public static void SendStationSlotClick(int ent, Item item, int slot)
		{
			if (Main.netMode == 1)
			{
				ModPacket packet = PrepareStationOperation(ent, 2);
				ItemIO.Send(item, packet, true);
				packet.Write((byte)slot);
				packet.Send();
			}
		}

		public static void ReceiveStationOperation(BinaryReader reader, int sender)
		{
			if (Main.netMode != 2)
			{
				return;
			}
			int ent = reader.ReadInt32();
			if (!TileEntity.ByID.ContainsKey(ent) || !(TileEntity.ByID[ent] is TECraftingAccess))
			{
				return;
			}
			TECraftingAccess access = (TECraftingAccess)TileEntity.ByID[ent];
			Item[] stations = access.stations;
			byte op = reader.ReadByte();
			if (op == 0)
			{
				Item item = ItemIO.Receive(reader, true);
				access.TryDepositStation(item);
				if (item.stack > 0)
				{
					ModPacket packet = PrepareStationResult(op);
					ItemIO.Send(item, packet, true);
					packet.Send();
				}
			}
			else if (op == 1)
			{
				int slot = reader.ReadByte();
				Item item = access.TryWithdrawStation(slot);
				if (!item.IsAir)
				{
					ModPacket packet = PrepareStationResult(op);
					ItemIO.Send(item, packet, true);
					packet.Send();
				}
			}
			else if (op == 2)
			{
				Item item = ItemIO.Receive(reader, true);
				int slot = reader.ReadByte();
				item = access.DoStationSwap(item, slot);
				if (!item.IsAir)
				{
					ModPacket packet = PrepareStationResult(op);
					ItemIO.Send(item, packet, true);
					packet.Send();
				}
			}
		}

		public static void ReceiveStationResult(BinaryReader reader)
		{
			if (Main.netMode != 1)
			{
				return;
			}
			Player player = Main.player[Main.myPlayer];
			byte op = reader.ReadByte();
			Item item = ItemIO.Receive(reader, true);
			if (op == 2 && Main.playerInventory && Main.mouseItem.IsAir)
			{
				Main.mouseItem = item;
				item = new Item();
			}
			else if (op == 2 && Main.playerInventory && Main.mouseItem.type == item.type)
			{
				int total = Main.mouseItem.stack + item.stack;
				if (total > Main.mouseItem.maxStack)
				{
					total = Main.mouseItem.maxStack;
				}
				int difference = total - Main.mouseItem.stack;
				Main.mouseItem.stack = total;
				item.stack -= total;
			}
			if (item.stack > 0)
			{
				item = player.GetItem(Main.myPlayer, item, false, true);
				if (!item.IsAir)
				{
					player.QuickSpawnClonedItem(item, item.stack);
				}
			}
		}
	}

	enum MessageType : byte
	{
		SearchAndRefreshNetwork,
		TryStorageOperation,
		StorageOperationResult,
		RefreshNetworkItems,
		ClientSendTEUpdate,
		TryStationOperation,
		StationOperationResult
	}
}