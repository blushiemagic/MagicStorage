using System;
using System.Collections.Generic;
using System.IO;
using MagicStorageExtra.Components;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorageExtra
{
	public static class NetHelper
	{
		private static bool queueUpdates;
		private static readonly Queue<int> updateQueue = new Queue<int>();
		private static readonly HashSet<int> updateQueueContains = new HashSet<int>();

		public static void HandlePacket(BinaryReader reader, int sender)
		{
			var type = (MessageType) reader.ReadByte();
			switch (type)
			{
				case MessageType.SearchAndRefreshNetwork:
					ReceiveSearchAndRefresh(reader);
					break;
				case MessageType.TryStorageOperation:
					ReceiveStorageOperation(reader, sender);
					break;
				case MessageType.StorageOperationResult:
					ReceiveOperationResult(reader);
					break;
				case MessageType.RefreshNetworkItems:
					StorageGUI.RefreshItems();
					break;
				case MessageType.ClientSendTEUpdate:
					ReceiveClientSendTEUpdate(reader, sender);
					break;
				case MessageType.TryStationOperation:
					ReceiveStationOperation(reader, sender);
					break;
				case MessageType.StationOperationResult:
					ReceiveStationResult(reader);
					break;
				case MessageType.ResetCompactStage:
					ReceiveResetCompactStage(reader);
					break;
				case MessageType.CraftRequest:
					ReceiveCraftRequest(reader, sender);
					break;
				case MessageType.CraftResult:
					ReceiveCraftResult(reader);
					break;
				case MessageType.SectionRequest:
					ReceiveClientRequestSection(reader, sender);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static void SendComponentPlace(int i, int j, int type)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				NetMessage.SendTileRange(Main.myPlayer, i, j, 2, 2);
				NetMessage.SendData(MessageID.TileEntityPlacement, -1, -1, null, i, j, type);
			}
		}

		public static void StartUpdateQueue()
		{
			queueUpdates = true;
		}

		public static void SendTEUpdate(int id, Point16 position)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
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
				NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, id, position.X, position.Y);
			}
		}

		public static void ProcessUpdateQueue()
		{
			if (queueUpdates)
			{
				queueUpdates = false;
				while (updateQueue.Count > 0)
					NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, updateQueue.Dequeue());
				updateQueueContains.Clear();
			}
		}

		public static void SendSearchAndRefresh(int i, int j)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorageExtra.Instance.GetPacket();
				packet.Write((byte) MessageType.SearchAndRefreshNetwork);
				packet.Write((short) i);
				packet.Write((short) j);
				packet.Send();
			}
		}

		private static void ReceiveSearchAndRefresh(BinaryReader reader)
		{
			var point = new Point16(reader.ReadInt16(), reader.ReadInt16());
			TEStorageComponent.SearchAndRefreshNetwork(point);
		}

		private static ModPacket PrepareStorageOperation(int ent, byte op)
		{
			ModPacket packet = MagicStorageExtra.Instance.GetPacket();
			packet.Write((byte) MessageType.TryStorageOperation);
			packet.Write(ent);
			packet.Write(op);
			return packet;
		}

		private static ModPacket PrepareOperationResult(byte op)
		{
			ModPacket packet = MagicStorageExtra.Instance.GetPacket();
			packet.Write((byte) MessageType.StorageOperationResult);
			packet.Write(op);
			return packet;
		}

		public static void SendDeposit(int ent, Item item)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = PrepareStorageOperation(ent, 0);
				ItemIO.Send(item, packet, true, true);
				packet.Send();
			}
		}

		public static void SendWithdraw(int ent, Item item, bool toInventory = false, bool keepOneIfFavorite = false)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = PrepareStorageOperation(ent, (byte) (toInventory ? 3 : 1));
				packet.Write(keepOneIfFavorite);
				ItemIO.Send(item, packet, true, true);
				packet.Send();
			}
		}

		public static void SendDepositAll(int ent, List<Item> items)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = PrepareStorageOperation(ent, 2);
				packet.Write((byte) items.Count);
				foreach (Item item in items)
					ItemIO.Send(item, packet, true, true);
				packet.Send();
			}
		}

		public static void ReceiveStorageOperation(BinaryReader reader, int sender)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			int ent = reader.ReadInt32();
			if (!TileEntity.ByID.ContainsKey(ent) || !(TileEntity.ByID[ent] is TEStorageHeart heart))
				return;
			byte op = reader.ReadByte();
			if (op == 0)
			{
				Item item = ItemIO.Receive(reader, true, true);
				heart.DepositItem(item);
				if (!item.IsAir)
				{
					ModPacket packet = PrepareOperationResult(op);
					ItemIO.Send(item, packet, true, true);
					packet.Send(sender);
				}
			}
			else if (op == 1 || op == 3)
			{
				bool keepOneIfFavorite = reader.ReadBoolean();
				Item item = ItemIO.Receive(reader, true, true);
				item = heart.TryWithdraw(item, keepOneIfFavorite);
				if (!item.IsAir)
				{
					ModPacket packet = PrepareOperationResult(op);
					ItemIO.Send(item, packet, true, true);
					packet.Send(sender);
				}
			}
			else if (op == 2)
			{
				int count = reader.ReadByte();
				var items = new List<Item>();
				StartUpdateQueue();
				for (int k = 0; k < count; k++)
				{
					Item item = ItemIO.Receive(reader, true, true);
					heart.DepositItem(item);
					if (!item.IsAir)
						items.Add(item);
				}

				ProcessUpdateQueue();
				if (items.Count > 0)
				{
					ModPacket packet = PrepareOperationResult(op);
					packet.Write((byte) items.Count);
					foreach (Item item in items)
						ItemIO.Send(item, packet, true, true);
					packet.Send(sender);
				}
			}

			SendRefreshNetworkItems(ent);
		}

		public static void ReceiveOperationResult(BinaryReader reader)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;
			byte op = reader.ReadByte();
			if (op == 0 || op == 1 || op == 3)
			{
				Item item = ItemIO.Receive(reader, true, true);
				StoragePlayer.GetItem(item, op != 3);
			}
			else if (op == 2)
			{
				int count = reader.ReadByte();
				for (int k = 0; k < count; k++)
				{
					Item item = ItemIO.Receive(reader, true, true);
					StoragePlayer.GetItem(item, false);
				}
			}
		}

		public static void SendRefreshNetworkItems(int ent)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				ModPacket packet = MagicStorageExtra.Instance.GetPacket();
				packet.Write((byte) MessageType.RefreshNetworkItems);
				packet.Write(ent);
				packet.Send();
			}
		}

		public static void ClientSendTEUpdate(int id)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorageExtra.Instance.GetPacket();
				packet.Write((byte) MessageType.ClientSendTEUpdate);
				packet.Write(id);
				TileEntity.Write(packet, TileEntity.ByID[id], true);
				packet.Send();
			}
		}

		public static void ReceiveClientSendTEUpdate(BinaryReader reader, int sender)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				int id = reader.ReadInt32();
				var ent = TileEntity.Read(reader, true);
				ent.ID = id;
				TileEntity.ByID[id] = ent;
				TileEntity.ByPosition[ent.Position] = ent;
				if (ent is TEStorageUnit storageUnit)
				{
					TEStorageHeart heart = storageUnit.GetHeart();
					heart?.ResetCompactStage();
				}

				NetMessage.SendData(MessageID.TileEntitySharing, -1, sender, null, id, ent.Position.X, ent.Position.Y);
			}
		}

		private static ModPacket PrepareStationOperation(int ent, byte op)
		{
			ModPacket packet = MagicStorageExtra.Instance.GetPacket();
			packet.Write((byte) MessageType.TryStationOperation);
			packet.Write(ent);
			packet.Write(op);
			return packet;
		}

		private static ModPacket PrepareStationResult(byte op)
		{
			ModPacket packet = MagicStorageExtra.Instance.GetPacket();
			packet.Write((byte) MessageType.StationOperationResult);
			packet.Write(op);
			return packet;
		}

		public static void SendDepositStation(int ent, Item item)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = PrepareStationOperation(ent, 0);
				ItemIO.Send(item, packet, true, true);
				packet.Send();
			}
		}

		public static void SendWithdrawStation(int ent, int slot)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = PrepareStationOperation(ent, 1);
				packet.Write((byte) slot);
				packet.Send();
			}
		}

		public static void SendStationSlotClick(int ent, Item item, int slot)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = PrepareStationOperation(ent, 2);
				ItemIO.Send(item, packet, true, true);
				packet.Write((byte) slot);
				packet.Send();
			}
		}

		public static void ReceiveStationOperation(BinaryReader reader, int sender)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			int ent = reader.ReadInt32();
			if (!TileEntity.ByID.ContainsKey(ent) || !(TileEntity.ByID[ent] is TECraftingAccess access))
				return;
			byte op = reader.ReadByte();
			if (op == 0)
			{
				Item item = ItemIO.Receive(reader, true, true);
				access.TryDepositStation(item);
				if (item.stack > 0)
				{
					ModPacket packet = PrepareStationResult(op);
					ItemIO.Send(item, packet, true, true);
					packet.Send(sender);
				}
			}
			else if (op == 1)
			{
				int slot = reader.ReadByte();
				Item item = access.TryWithdrawStation(slot);
				if (!item.IsAir)
				{
					ModPacket packet = PrepareStationResult(op);
					ItemIO.Send(item, packet, true, true);
					packet.Send(sender);
				}
			}
			else if (op == 2)
			{
				Item item = ItemIO.Receive(reader, true, true);
				int slot = reader.ReadByte();
				item = access.DoStationSwap(item, slot);
				if (!item.IsAir)
				{
					ModPacket packet = PrepareStationResult(op);
					ItemIO.Send(item, packet, true, true);
					packet.Send(sender);
				}
			}

			Point16 pos = access.Position;
			var modTile = TileLoader.GetTile(Main.tile[pos.X, pos.Y].type) as StorageAccess;
			TEStorageHeart heart = modTile?.GetHeart(pos.X, pos.Y);
			if (heart != null)
				SendRefreshNetworkItems(heart.ID);
		}

		public static void ReceiveStationResult(BinaryReader reader)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;
			Player player = Main.LocalPlayer;
			byte op = reader.ReadByte();
			Item item = ItemIO.Receive(reader, true, true);
			if (op == 2 && Main.playerInventory && Main.mouseItem.IsAir)
			{
				Main.mouseItem = item;
				item = new Item();
			}
			else if (op == 2 && Main.playerInventory && Main.mouseItem.type == item.type)
			{
				int total = Main.mouseItem.stack + item.stack;
				if (total > Main.mouseItem.maxStack)
					total = Main.mouseItem.maxStack;
				Main.mouseItem.stack = total;
				item.stack -= total;
			}

			if (item.stack > 0)
			{
				item = player.GetItem(Main.myPlayer, item, false, true);
				if (!item.IsAir)
					player.QuickSpawnClonedItem(item, item.stack);
			}
		}

		public static void SendResetCompactStage(int ent)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorageExtra.Instance.GetPacket();
				packet.Write((byte) MessageType.ResetCompactStage);
				packet.Write(ent);
				packet.Send();
			}
		}

		public static void ReceiveResetCompactStage(BinaryReader reader)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				int ent = reader.ReadInt32();
				if (TileEntity.ByID[ent] is TEStorageHeart heart)
					heart.ResetCompactStage();
			}
		}

		public static void SendCraftRequest(int heart, List<Item> toWithdraw, List<Item> results)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorageExtra.Instance.GetPacket();
				packet.Write((byte) MessageType.CraftRequest);
				packet.Write(heart);
				packet.Write(toWithdraw.Count);
				foreach (Item item in toWithdraw)
					ItemIO.Send(item, packet, true, true);
				packet.Write(results.Count);
				foreach (Item result in results)
					ItemIO.Send(result, packet, true, true);
				packet.Send();
			}
		}

		public static void ReceiveCraftRequest(BinaryReader reader, int sender)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			int ent = reader.ReadInt32();
			if (!TileEntity.ByID.ContainsKey(ent) || !(TileEntity.ByID[ent] is TEStorageHeart heart))
				return;

			int withdrawCount = reader.ReadInt32();
			var toWithdraw = new List<Item>();
			for (int k = 0; k < withdrawCount; k++)
				toWithdraw.Add(ItemIO.Receive(reader, true, true));

			int resultsCount = reader.ReadInt32();
			var results = new List<Item>();
			for (int k = 0; k < resultsCount; k++)
				results.Add(ItemIO.Receive(reader, true, true));

			List<Item> items = CraftingGUI.DoCraft(heart, toWithdraw, results);
			if (items.Count > 0)
			{
				ModPacket packet = MagicStorageExtra.Instance.GetPacket();
				packet.Write((byte) MessageType.CraftResult);
				packet.Write(items.Count);
				foreach (Item item in items)
					ItemIO.Send(item, packet, true, true);
				packet.Send(sender);
			}

			SendRefreshNetworkItems(ent);
		}

		public static void ReceiveCraftResult(BinaryReader reader)
		{
			Player player = Main.LocalPlayer;
			int count = reader.ReadInt32();
			for (int k = 0; k < count; k++)
			{
				Item item = ItemIO.Receive(reader, true, true);
				player.QuickSpawnClonedItem(item, item.stack);
			}
		}

		public static void ClientRequestSection(Point16 coords)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorageExtra.Instance.GetPacket();
				packet.Write((byte) MessageType.SectionRequest);

				packet.Write(coords.X);
				packet.Write(coords.Y);

				packet.Send();
			}
		}

		public static void ReceiveClientRequestSection(BinaryReader reader, int sender)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				var coords = new Point16(reader.ReadInt16(), reader.ReadInt16());
				RemoteClient.CheckSection(sender, coords.ToWorldCoordinates());
			}
		}
	}

	internal enum MessageType : byte
	{
		SearchAndRefreshNetwork,
		TryStorageOperation,
		StorageOperationResult,
		RefreshNetworkItems,
		ClientSendTEUpdate,
		TryStationOperation,
		StationOperationResult,
		ResetCompactStage,
		CraftRequest,
		CraftResult,
		SectionRequest
	}
}