using System;
using System.Collections.Generic;
using System.IO;
using MagicStorage.Components;
using MagicStorage.Edits;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage
{
	public static class NetHelper
	{
		private static bool queueUpdates;
		private static readonly Queue<int> updateQueue = new();
		private static readonly HashSet<int> updateQueueContains = new();

		public static void HandlePacket(BinaryReader reader, int sender)
		{
			MessageType type = (MessageType) reader.ReadByte();

			/*
			if (Main.netMode == NetmodeID.MultiplayerClient)
				Main.NewText($"Receiving Message Type \"{Enum.GetName(type)}\"");
			else if(Main.netMode == NetmodeID.Server)
				Console.WriteLine($"Receiving Message Type \"{Enum.GetName(type)}\"");
			*/

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
					ReceiveRefreshNetworkItems(reader);
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
				case MessageType.NetWorkaround:
					ReceiveNetWorkaround(reader);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static void SendComponentPlace(int i, int j, int type)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				NetMessage.SendTileSquare(Main.myPlayer, i, j, 2, 2);
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
				ModPacket packet = MagicStorage.Instance.GetPacket();
				packet.Write((byte) MessageType.SearchAndRefreshNetwork);
				packet.Write((short) i);
				packet.Write((short) j);
				packet.Send();
			}
		}

		private static void ReceiveSearchAndRefresh(BinaryReader reader)
		{
			Point16 point = new(reader.ReadInt16(), reader.ReadInt16());
			TEStorageComponent.SearchAndRefreshNetwork(point);
		}

		private static ModPacket PrepareStorageOperation(int ent, byte op)
		{
			ModPacket packet = MagicStorage.Instance.GetPacket();
			packet.Write((byte) MessageType.TryStorageOperation);
			packet.Write(ent);
			packet.Write(op);
			return packet;
		}

		private static ModPacket PrepareOperationResult(byte op)
		{
			ModPacket packet = MagicStorage.Instance.GetPacket();
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
			int ent = reader.ReadInt32();
			byte op = reader.ReadByte();

			/*
			if (Main.netMode == NetmodeID.Server)
				Console.WriteLine($"Receiving Storage Operation {op} from entity {ent}");
			else
				Main.NewText($"Receiving Storage Operation {op} from entity {ent}");
			*/

			if (Main.netMode != NetmodeID.Server)
			{
				//The data still needs to be read for exceptions to not be thrown...
				if (op == 0)
				{
					_ = ItemIO.Receive(reader, true, true);
				}
				else if (op == 1 || op == 3)
				{
					_ = reader.ReadBoolean();
					_ = ItemIO.Receive(reader, true, true);
				}
				else if (op == 2)
				{
					int count = reader.ReadByte();
					for (int i = 0; i < count; i++)
						_ = ItemIO.Receive(reader, true, true);
				}

				return;
			}

			if (!TileEntity.ByID.TryGetValue(ent, out TileEntity te) || te is not TEStorageHeart heart)
				return;

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
				List<Item> items = new();
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
			byte op = reader.ReadByte();

			/*
			if (Main.netMode == NetmodeID.Server)
				Console.WriteLine($"Receiving Operation Result {op}");
			else
				Main.NewText($"Receiving Operation Result {op}");
			*/

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				//The data still needs to be read for exceptions to not be thrown...
				if (op == 0 || op == 1 || op == 3)
				{
					_ = ItemIO.Receive(reader, true, true);
				}
				else if (op == 2)
				{
					int count = reader.ReadByte();
					for (int i = 0; i < count; i++)
						_ = ItemIO.Receive(reader, true, true);
				}

				return;
			}

			if (op == 0 || op == 1 || op == 3)
			{
				Item item = ItemIO.Receive(reader, true, true);

				/*
				Main.NewText($"Item Read: [type: {item.type}, prefix: {item.prefix}, stack: {item.stack}]");
				Main.NewText($"Reader Position: {reader.BaseStream.Position}");
				*/

				StoragePlayer.GetItem(item, op != 3);
			}
			else if (op == 2)
			{
				int count = reader.ReadByte();
				for (int k = 0; k < count; k++)
				{
					Item item = ItemIO.Receive(reader, true, true);

					/*
					Main.NewText($"Item Read: [type: {item.type}, prefix: {item.prefix}, stack: {item.stack}]");
					Main.NewText($"Reader Position: {reader.BaseStream.Position}");
					*/

					StoragePlayer.GetItem(item, false);
				}
			}
		}

		public static void SendRefreshNetworkItems(int ent)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				ModPacket packet = MagicStorage.Instance.GetPacket();
				packet.Write((byte) MessageType.RefreshNetworkItems);
				packet.Write(ent);
				packet.Send();
			}
		}

		private static void ReceiveRefreshNetworkItems(BinaryReader reader)
		{
			int ent = reader.ReadInt32();
			if (Main.netMode == NetmodeID.Server)
				return;

			if (TileEntity.ByID.ContainsKey(ent))
				StorageGUI.RefreshItems();
		}

		public static void ClientSendTEUpdate(int id)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorage.Instance.GetPacket();
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
				TileEntity ent = TileEntity.Read(reader, true);
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
			else if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				//Still need to read the data
				_ = reader.ReadInt32();
				// TODO does TileEntity need to be read?
				//_ = TileEntity.Read(reader, true);
			}
		}

		private static ModPacket PrepareStationOperation(int ent, byte op)
		{
			ModPacket packet = MagicStorage.Instance.GetPacket();
			packet.Write((byte) MessageType.TryStationOperation);
			packet.Write(ent);
			packet.Write(op);
			return packet;
		}

		private static ModPacket PrepareStationResult(byte op)
		{
			ModPacket packet = MagicStorage.Instance.GetPacket();
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
			int ent = reader.ReadInt32();
			byte op = reader.ReadByte();

			if (Main.netMode != NetmodeID.Server)
			{
				//Still need to read the data for exceptions to not be thrown...
				if (op == 0)
				{
					_ = ItemIO.Receive(reader, true, true);
				}
				else if (op == 1)
				{
					_ = reader.ReadByte();
				}
				else if (op == 2)
				{
					_ = ItemIO.Receive(reader, true, true);
					_ = reader.ReadByte();
				}

				return;
			}

			if (!TileEntity.ByID.TryGetValue(ent, out TileEntity te) || te is not TECraftingAccess access)
				return;

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
			StorageAccess modTile = TileLoader.GetTile(Main.tile[pos.X, pos.Y].type) as StorageAccess;
			TEStorageHeart heart = modTile?.GetHeart(pos.X, pos.Y);
			if (heart is not null)
				SendRefreshNetworkItems(heart.ID);
		}

		public static void ReceiveStationResult(BinaryReader reader)
		{
			//Still need to read the data for exceptions to not be thrown...
			byte op = reader.ReadByte();
			Item item = ItemIO.Receive(reader, true, true);

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Player player = Main.LocalPlayer;
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
				item = player.GetItem(Main.myPlayer, item, GetItemSettings.InventoryEntityToPlayerInventorySettings);
				if (!item.IsAir)
					player.QuickSpawnClonedItem(item, item.stack);
			}
		}

		public static void SendResetCompactStage(int ent)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorage.Instance.GetPacket();
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
				if (TileEntity.ByID.TryGetValue(ent, out var te) && te is TEStorageHeart heart)
					heart.ResetCompactStage();
			}
			else if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				reader.ReadInt32();
			}
		}

		public static void SendCraftRequest(int heart, List<Item> toWithdraw, List<Item> results)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorage.Instance.GetPacket();
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
			int ent = reader.ReadInt32();
			int withdrawCount = reader.ReadInt32();

			if (Main.netMode != NetmodeID.Server)
			{
				//Still need to read the data for exceptions to not be thrown
				for (int i = 0; i < withdrawCount; i++)
					_ = ItemIO.Receive(reader, true, true);


				int count = reader.ReadInt32();
				for (int i = 0; i < count; i++)
					_ = ItemIO.Receive(reader, true, true);

				return;
			}

			if (!TileEntity.ByID.TryGetValue(ent, out TileEntity te) || te is not TEStorageHeart heart)
				return;

			List<Item> toWithdraw = new();
			for (int k = 0; k < withdrawCount; k++)
				toWithdraw.Add(ItemIO.Receive(reader, true, true));

			int resultsCount = reader.ReadInt32();
			List<Item> results = new();
			for (int k = 0; k < resultsCount; k++)
				results.Add(ItemIO.Receive(reader, true, true));

			List<Item> items = CraftingGUI.DoCraft(heart, toWithdraw, results);
			if (items.Count > 0)
			{
				ModPacket packet = MagicStorage.Instance.GetPacket();
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
				ModPacket packet = MagicStorage.Instance.GetPacket();
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
				Point16 coords = new(reader.ReadInt16(), reader.ReadInt16());
				RemoteClient.CheckSection(sender, coords.ToWorldCoordinates());
			}
		}

		public static void ReceiveNetWorkaround(BinaryReader reader)
		{
			EditsLoader.MessageTileEntitySyncing = false;

			int entityCount = reader.ReadUInt16();
			for (int i = 0; i < entityCount; i++)
				/*
					long entStart = reader.BaseStream.Position;
	
					if (Main.netMode == NetmodeID.MultiplayerClient)
					{
						byte type = reader.ReadByte();
						reader.BaseStream.Seek(-1, SeekOrigin.Current);
						MagicStorage.Instance.Logger.Debug($"Reading entity data of type {type}");
					}
					*/

				TileEntity.Read(reader, true);
			/*
				if (Main.netMode == NetmodeID.MultiplayerClient)
					MagicStorage.Instance.Logger.Debug($"Bytes read (#{i + 1}): {reader.BaseStream.Position - entStart} (total: {reader.BaseStream.Position})");
				*/

			/*
			long end = reader.BaseStream.Position;

			if (Main.netMode == NetmodeID.MultiplayerClient && entityCount > 0)
				MagicStorage.Instance.Logger.Debug($"Received netmessage workaround - {entityCount} entities read, {end} bytes read");
			*/
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
		SectionRequest,
		NetWorkaround
	}
}
