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
using System.Linq;
using System.Diagnostics;

namespace MagicStorage
{
	public static class NetHelper
	{
		private static bool queueUpdates;
		private static readonly Queue<int> updateQueue = new();
		private static readonly HashSet<int> updateQueueContains = new();

		[Conditional("DEBUG")]
		[Conditional("NETPLAY")]
		public static void Report(bool reportTime, string message) {
			if (Main.netMode != NetmodeID.Server) {
				if (reportTime)
					Main.NewText("Time: " + DateTime.Now.Ticks);

				Main.NewText(message);
			} else if (Main.dedServ) {
				if (reportTime) {
					ConsoleColor fg = Console.ForegroundColor;
					ConsoleColor bg = Console.BackgroundColor;

					Console.ForegroundColor = ConsoleColor.Red;
					Console.BackgroundColor = ConsoleColor.Black;

					Console.WriteLine("Time: " + DateTime.Now.Ticks);

					Console.ForegroundColor = fg;
					Console.BackgroundColor = bg;
				}

				Console.WriteLine(message);
			}

			if (reportTime)
				message += "Time: " + DateTime.Now.Ticks + "\n";

			MagicStorage.Instance.Logger.Debug(message);
		}

		public static void HandlePacket(BinaryReader reader, int sender)
		{
			MessageType type = (MessageType)reader.ReadByte();

			/*
			if (Main.netMode == NetmodeID.MultiplayerClient)
				Main.NewText($"Receiving Message Type \"{Enum.GetName(type)}\"");
			else if(Main.netMode == NetmodeID.Server)
				Console.WriteLine($"Receiving Message Type \"{Enum.GetName(type)}\"");
			*/

			Report(reportTime: true, "Received message " + type);

			switch (type)
			{
				case MessageType.SearchAndRefreshNetwork:
					ReceiveSearchAndRefresh(reader);
					break;
				case MessageType.ClinetStorageOperation:
					ReciveClientStorageOperation(reader, sender);
					break;
				case MessageType.ServerStorageResult:
					ReciveServerStorageResult(reader);
					break;
				case MessageType.RefreshNetworkItems:
					ReceiveRefreshNetworkItems(reader);
					break;
				case MessageType.ClientSendTEUpdate:
					ReceiveClientSendTEUpdate(reader, sender);
					break;
				case MessageType.ClientSendDeactivate:
					ReceiveClientDeactivate(reader, sender);
					break;
				case MessageType.ClientStationOperation:
					ReceiveClientStationOperation(reader, sender);
					break;
				case MessageType.ServerStationOperationResult:
					ReceiveServerStationResult(reader);
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
				case MessageType.SyncStorageUnitToClinet:
					ClientReciveStorageSync(reader);
					break;
				case MessageType.SyncStorageUnit:
					ServerReciveSyncStorageUnit(reader, sender);
					break;
				case MessageType.ForceCraftingGUIRefresh:
					ReceiveClientForceCraftingGUIRefresh(reader, sender);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static void SyncStorageUnit(int storageUnitId)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorage.Instance.GetPacket();
				packet.Write((byte)MessageType.SyncStorageUnit);
				packet.Write(storageUnitId);
				packet.Send();
			}
		}

		public static void ServerReciveSyncStorageUnit(BinaryReader reader, int remoteClient)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				//byte remoteClient = reader.ReadByte();
				int storageUnitId = reader.ReadInt32();
				TileEntity tileEntity = TileEntity.ByID[storageUnitId];
				TEStorageUnit storageUnit = (TEStorageUnit)TileEntity.ByID[storageUnitId];
				storageUnit.FullySync();

				using (MemoryStream packetStream = new(65536))
				using (BinaryWriter BWriter = new BinaryWriter(packetStream))
				{
					TileEntity.Write(BWriter, tileEntity, true);
					BWriter.Flush();

					ModPacket packet = MagicStorage.Instance.GetPacket();
					packet.Write((byte)MessageType.SyncStorageUnitToClinet);
					packet.Write(packetStream.GetBuffer(), 0, (int)packetStream.Length);
					packet.Send(remoteClient);
				}
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
				packet.Write((byte)MessageType.SearchAndRefreshNetwork);
				packet.Write((short)i);
				packet.Write((short)j);
				packet.Send();
			}
		}

		private static void ReceiveSearchAndRefresh(BinaryReader reader)
		{
			Point16 point = new(reader.ReadInt16(), reader.ReadInt16());
			TEStorageComponent.SearchAndRefreshNetwork(point);
		}

		public static void ReciveClientStorageOperation(BinaryReader reader, int sender)
		{
			int ent = reader.ReadInt32();
			TEStorageHeart.Operation op = (TEStorageHeart.Operation)reader.ReadByte();

			if (Main.netMode != NetmodeID.Server)
			{
				//The data still needs to be read for exceptions to not be thrown...
				if (op == TEStorageHeart.Operation.Deposit)
				{
					_ = ItemIO.Receive(reader, true, true);
				}
				else if (op == TEStorageHeart.Operation.Withdraw || op == TEStorageHeart.Operation.WithdrawToInventory)
				{
					_ = reader.ReadBoolean();
					_ = ItemIO.Receive(reader, true, true);
				}
				else if (op == TEStorageHeart.Operation.DepositAll)
				{
					int count = reader.ReadByte();
					for (int i = 0; i < count; i++)
						_ = ItemIO.Receive(reader, true, true);
				}

				return;
			}

			if (!TileEntity.ByID.TryGetValue(ent, out TileEntity te) || te is not TEStorageHeart heart)
				return;

			heart.QClientOperation(reader, op, sender);
		}

		public static void ReciveServerStorageResult(BinaryReader reader)
		{
			TEStorageHeart.Operation op = (TEStorageHeart.Operation)reader.ReadByte();

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				//The data still needs to be read for exceptions to not be thrown...
				if (op == TEStorageHeart.Operation.Withdraw || op == TEStorageHeart.Operation.WithdrawToInventory || op == TEStorageHeart.Operation.Deposit)
				{
					_ = ItemIO.Receive(reader, true, true);
				}
				else if (op == TEStorageHeart.Operation.DepositAll)
				{
					int count = reader.ReadByte();
					for (int i = 0; i < count; i++)
						_ = ItemIO.Receive(reader, true, true);
				}

				return;
			}

			if (op == TEStorageHeart.Operation.Withdraw || op == TEStorageHeart.Operation.WithdrawToInventory || op == TEStorageHeart.Operation.Deposit)
			{
				Item item  = ItemIO.Receive(reader, true, true);
				var  heart = StoragePlayer.LocalPlayer.GetStorageHeart();
				StoragePlayer.GetItem(new EntitySource_TileEntity(heart), item, op != TEStorageHeart.Operation.WithdrawToInventory);
			}
			else if (op == TEStorageHeart.Operation.DepositAll)
			{
				int count = reader.ReadInt32();
				for (int k = 0; k < count; k++)
				{
					Item item  = ItemIO.Receive(reader, true, true);
					var  heart = StoragePlayer.LocalPlayer.GetStorageHeart();
					StoragePlayer.GetItem(new EntitySource_TileEntity(heart), item, false);
				}
			}
		}

		public static void SendRefreshNetworkItems(int ent)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				ModPacket packet = MagicStorage.Instance.GetPacket();
				packet.Write((byte)MessageType.RefreshNetworkItems);
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

		public static void ClientSendDeactivate(int id, bool inActive)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorage.Instance.GetPacket();
				packet.Write((byte)MessageType.ClientSendDeactivate);
				packet.Write(id);
				packet.Write(inActive);
				packet.Send();
			}
		}

		public static void ReceiveClientDeactivate(BinaryReader reader, int sender)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				int id = reader.ReadInt32();
				bool inActive = reader.ReadBoolean();
				TileEntity ent = TileEntity.ByID[id];
				if (ent is TEStorageUnit storageUnit)
				{
					storageUnit.Inactive = inActive;
					storageUnit.UpdateTileFrameWithNetSend();
					TEStorageHeart heart = storageUnit.GetHeart();
					if (heart != null)
					{
						heart.ResetCompactStage();
					}
				}
			}
			else if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				//Still need to read the data
				_ = reader.ReadInt32();
				// TODO does TileEntity need to be read?
				//_ = TileEntity.Read(reader, true);
			}
		}

		public static void ClientSendTEUpdate(int id)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
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
			packet.Write((byte)MessageType.ClientStationOperation);
			packet.Write(ent);
			packet.Write(op);
			return packet;
		}

		private static ModPacket PrepareStationResult(byte op)
		{
			ModPacket packet = MagicStorage.Instance.GetPacket();
			packet.Write((byte)MessageType.ServerStationOperationResult);
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
				packet.Write((byte)slot);
				packet.Send();
			}
		}

		public static void ReceiveClientStationOperation(BinaryReader reader, int sender)
		{
			int ent = reader.ReadInt32();
			TECraftingAccess.Operation op = (TECraftingAccess.Operation)reader.ReadByte();

			if (Main.netMode != NetmodeID.Server)
			{
				//The data still needs to be read for exceptions to not be thrown...
				if (op == TECraftingAccess.Operation.Withdraw || op == TECraftingAccess.Operation.WithdrawToInventory)
				{
					_ = reader.ReadByte();
				}
				else if (op == TECraftingAccess.Operation.Deposit)
				{
					_ = ItemIO.Receive(reader, true, true);
				}
				return;
			}

			if (!TileEntity.ByID.TryGetValue(ent, out TileEntity te) || te is not TECraftingAccess craftingAccess)
				return;

			craftingAccess.QClientOperation(reader, op, sender);
		}

		public static void ReceiveServerStationResult(BinaryReader reader)
		{
			//Still need to read the data for exceptions to not be thrown...
			TECraftingAccess.Operation op = (TECraftingAccess.Operation)reader.ReadByte();
			Item item = ItemIO.Receive(reader, true, true);

			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				if (op == TECraftingAccess.Operation.Withdraw || op == TECraftingAccess.Operation.WithdrawToInventory)
				{
					var heart = StoragePlayer.LocalPlayer.GetStorageHeart();
					StoragePlayer.GetItem(new EntitySource_TileEntity(heart), item, op == TECraftingAccess.Operation.Withdraw);
				}
				else // deposit operation
				{
					Main.mouseItem = item;
				}
			}
		}

		public static void SendResetCompactStage(int ent)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorage.Instance.GetPacket();
				packet.Write((byte)MessageType.ResetCompactStage);
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
				packet.Write((byte)MessageType.CraftRequest);
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

			List<Item> items = CraftingGUI.HandleCraftWithdrawAndDeposit(heart, toWithdraw, results);
			if (items.Count > 0)
			{
				ModPacket packet = MagicStorage.Instance.GetPacket();
				packet.Write((byte)MessageType.CraftResult);
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
				Item item  = ItemIO.Receive(reader, true, true);
				var  heart = StoragePlayer.LocalPlayer.GetStorageHeart();
				player.QuickSpawnClonedItem(new EntitySource_TileEntity(heart), item, item.stack);
			}
		}

		public static void ClientRequestSection(Point16 coords)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorage.Instance.GetPacket();
				packet.Write((byte)MessageType.SectionRequest);

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

		public static void ClientReciveStorageSync(BinaryReader reader)
		{
			TileEntity.Read(reader, true);
		}

		public static void ClientRequestForceCraftingGUIRefresh() {
			if (Main.netMode == NetmodeID.MultiplayerClient && StoragePlayer.LocalPlayer.GetStorageHeart() is TEStorageHeart heart) {
				ModPacket packet = MagicStorage.Instance.GetPacket();
				packet.Write((byte)MessageType.ForceCraftingGUIRefresh);

				packet.Write(heart.Position.X);
				packet.Write(heart.Position.Y);

				packet.Send(ignoreClient: Main.myPlayer);
			}
		}

		public static void ReceiveClientForceCraftingGUIRefresh(BinaryReader reader, int sender) {
			Point16 storage = new(reader.ReadInt16(), reader.ReadInt16());

			if (Main.netMode == NetmodeID.Server) {
				//Forward the packet
				ModPacket packet = MagicStorage.Instance.GetPacket();
				packet.Write((byte)MessageType.ForceCraftingGUIRefresh);

				packet.Write(storage.X);
				packet.Write(storage.Y);

				packet.Send(ignoreClient: sender);
			} else if (Main.netMode == NetmodeID.MultiplayerClient) {
				if (StoragePlayer.LocalPlayer.GetStorageHeart() is TEStorageHeart heart && heart.Position == storage && StoragePlayer.IsStorageCrafting())
					CraftingGUI.RefreshItems();
			}
		}
	}

	internal enum MessageType : byte
	{
		SearchAndRefreshNetwork,
		ClinetStorageOperation,
		ServerStorageResult,
		RefreshNetworkItems,
		ClientSendTEUpdate,
		ClientSendDeactivate,
		ClientStationOperation,
		ServerStationOperationResult,
		ResetCompactStage,
		CraftRequest,
		CraftResult,
		SectionRequest,
		SyncStorageUnitToClinet,
		SyncStorageUnit,
		ForceCraftingGUIRefresh
	}
}
