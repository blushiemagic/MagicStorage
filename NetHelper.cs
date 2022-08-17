using System;
using System.Collections.Generic;
using System.IO;
using MagicStorage.Components;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using MagicStorage.UI.States;
using System.Text;

namespace MagicStorage
{
	public static class NetHelper
	{
		private static bool queueUpdates;
		private static readonly Queue<int> updateQueue = new();
		private static readonly HashSet<int> updateQueueContains = new();

		[Conditional("NETPLAY")]
		public static void Report(bool reportTime, string message) {
			if (Main.netMode != NetmodeID.Server) {
				if (reportTime)
					Main.NewText("Time: " + DateTime.Now.Ticks);

				Main.NewTextMultiline(message, c: Color.White);
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
				message = "Time: " + DateTime.Now.Ticks + "\n" + message;

			MagicStorageMod.Instance.Logger.Debug(message);
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

			Report(true, "Received message " + type + " from player " + sender);

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
					ReceiveResetCompactStage(reader, sender);
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
				case MessageType.TransferItems:
					ReceiveClientRequestItemTransfer(reader, sender);
					break;
				case MessageType.TransferItemsResult:
					RecieveTransferItemsResult(reader);
					break;
				case MessageType.RequestCoinCompact:
					ReceiveCoinCompactRequest(reader, sender);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static void SyncStorageUnit(Point16 position)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.SyncStorageUnit);
				packet.Write(position.X);
				packet.Write(position.Y);
				packet.Send();

				Report(true, MessageType.SyncStorageUnit + " packet sent from client " + Main.myPlayer);
			}
		}

		public static void ServerReciveSyncStorageUnit(BinaryReader reader, int remoteClient)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				//byte remoteClient = reader.ReadByte();
				Point16 position = new(reader.ReadInt16(), reader.ReadInt16());

				if (!TileEntity.ByPosition.TryGetValue(position, out TileEntity tileEntity)) {
					Report(true, MessageType.ResetCompactStage + " packet had a data mismatch");
					Report(false, "  A Tile Entity at location (X: " + position.X + ", Y: " + position.Y + ") does not exist on the server");
					return;
				}

				if (tileEntity is not TEStorageUnit storageUnit) {
					Report(true, MessageType.ResetCompactStage + " received a position for a Tile Entity that isn't a TEStorageUnit: (X: " + position.X + ", Y: " + position.Y + ")");
					Report(false, "  Tile Entity type was actually " + tileEntity.GetType().FullName);
					return;
				}

				storageUnit.FullySync();

				using (MemoryStream packetStream = new(65536))
				using (BinaryWriter BWriter = new BinaryWriter(packetStream))
				{
					TileEntity.Write(BWriter, tileEntity, true);
					BWriter.Flush();

					ModPacket packet = MagicStorageMod.Instance.GetPacket();
					packet.Write((byte)MessageType.SyncStorageUnitToClinet);
					packet.Write(packetStream.GetBuffer(), 0, (int)packetStream.Length);
					packet.Send(remoteClient);
				}

				Report(true, MessageType.SyncStorageUnit + " packet received by server from client " + remoteClient);
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
				if (updateQueue.Count > 0)
					Report(true, "Tile Entity update queue had " + updateQueue.Count + " values");

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
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.SearchAndRefreshNetwork);
				packet.Write((short)i);
				packet.Write((short)j);
				packet.Send();

				Report(true, MessageType.SearchAndRefreshNetwork + " packet sent from client " + Main.myPlayer);
				Report(false, "Refresh origin: (" + i + ", " + j + ")");
			}
		}

		private static void ReceiveSearchAndRefresh(BinaryReader reader)
		{
			Point16 point = new(reader.ReadInt16(), reader.ReadInt16());
			TEStorageComponent.SearchAndRefreshNetwork(point);

			Report(true, MessageType.SearchAndRefreshNetwork + " packet received by client " + Main.myPlayer);
			Report(false, "Refresh origin: (" + point.X + ", " + point.Y + ")");
		}

		public static void ReciveClientStorageOperation(BinaryReader reader, int sender)
		{
			Point16 position = new(reader.ReadInt16(), reader.ReadInt16());
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
				else if (op == TEStorageHeart.Operation.WithdrawAllAndDestroy)
				{
					_ = reader.ReadInt32();
				}

				return;
			}

			if (!TileEntity.ByPosition.TryGetValue(position, out TileEntity te) || te is not TEStorageHeart heart)
				return;

			heart.QClientOperation(reader, op, sender);

			Report(true, MessageType.ClinetStorageOperation + " packet recieved by client " + Main.myPlayer);
			Report(false, "Operation: " + op);
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
				else if (op == TEStorageHeart.Operation.WithdrawAllAndDestroy)
				{
					_ = reader.ReadInt32();
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
			else if (op == TEStorageHeart.Operation.WithdrawAllAndDestroy)
			{
				int type = reader.ReadInt32();

				var heart = StoragePlayer.LocalPlayer.GetStorageHeart();

				heart.WithdrawManyAndDestroy(type, net: true);
			}
			else if (op == TEStorageHeart.Operation.DeleteUnloadedGlobalItemData)
			{
				var heart = StoragePlayer.LocalPlayer.GetStorageHeart();

				heart.DestroyUnloadedGlobalItemData(net: true);
			}

			Report(true, MessageType.ServerStorageResult + " packet received by client " + Main.myPlayer);
			Report(false, "Operation: " + op);
		}

		public static void SendRefreshNetworkItems(Point16 position)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.RefreshNetworkItems);
				packet.Write(position.X);
				packet.Write(position.Y);
				packet.Send();

				Report(true, MessageType.RefreshNetworkItems + " packet sent from client " + Main.myPlayer);
			}
		}

		private static void ReceiveRefreshNetworkItems(BinaryReader reader)
		{
			Point16 position = new(reader.ReadInt16(), reader.ReadInt16());
			if (Main.netMode == NetmodeID.Server)
				return;

			if (TileEntity.ByPosition.ContainsKey(position))
				StorageGUI.RefreshItems();

			Report(true, MessageType.RefreshNetworkItems + " packet received by client " + Main.myPlayer);
		}

		public static void ClientSendDeactivate(Point16 position, bool inActive)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.ClientSendDeactivate);
				packet.Write(position.X);
				packet.Write(position.Y);
				packet.Write(inActive);
				packet.Send();

				Report(true, MessageType.ClientSendDeactivate + " packet sent from client " + Main.myPlayer);
			}
		}

		public static void ReceiveClientDeactivate(BinaryReader reader, int sender)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				Point16 position = new(reader.ReadInt16(), reader.ReadInt16());
				bool inActive = reader.ReadBoolean();
				TileEntity ent = TileEntity.ByPosition[position];
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

				Report(true, MessageType.ClientSendDeactivate + " packet received by server from client " + sender);
			}
			else if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				//Still need to read the data
				_ = reader.ReadPoint16();
				// TODO does TileEntity need to be read?
				//_ = TileEntity.Read(reader, true);

				Report(true, MessageType.ClientSendDeactivate + " packet received by client " + Main.myPlayer);
			}
		}

		public static void ClientSendTEUpdate(Point16 position)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.ClientSendTEUpdate);
				packet.Write(position.X);
				packet.Write(position.Y);
				TileEntity.Write(packet, TileEntity.ByPosition[position], true);
				packet.Send();

				Report(true, MessageType.ClientSendTEUpdate + " packet sent from client " + Main.myPlayer);
			}
		}

		public static void ReceiveClientSendTEUpdate(BinaryReader reader, int sender)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				Point16 position = new(reader.ReadInt16(), reader.ReadInt16());
				TileEntity ent = TileEntity.Read(reader, true);
				ent.Position = position;
				TileEntity.ByID[ent.ID] = ent;
				TileEntity.ByPosition[position] = ent;
				if (ent is TEStorageUnit storageUnit)
				{
					TEStorageHeart heart = storageUnit.GetHeart();
					heart?.ResetCompactStage();
				}

				Report(true, MessageType.ClientSendTEUpdate + " packet received by server from client " + sender);

				NetMessage.SendData(MessageID.TileEntitySharing, -1, sender, null, ent.ID, ent.Position.X, ent.Position.Y);
			}
			else if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				//Still need to read the data
				_ = reader.ReadPoint16();
				// TODO does TileEntity need to be read?
				//_ = TileEntity.Read(reader, true);

				Report(true, MessageType.ClientSendTEUpdate + " packet received by client " + Main.myPlayer);
			}
		}

		private static ModPacket PrepareStationOperation(Point16 position, byte op)
		{
			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ClientStationOperation);
			packet.Write(position.X);
			packet.Write(position.Y);
			packet.Write(op);
			return packet;
		}

		private static ModPacket PrepareStationResult(byte op)
		{
			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ServerStationOperationResult);
			packet.Write(op);
			return packet;
		}

		public static void SendDepositStation(Point16 position, Item item)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = PrepareStationOperation(position, 0);
				ItemIO.Send(item, packet, true, true);
				packet.Send();

				Report(true, "SendDepositStation packet sent from client " + Main.myPlayer);
			}
		}

		public static void SendWithdrawStation(Point16 position, int slot)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = PrepareStationOperation(position, 1);
				packet.Write((byte)slot);
				packet.Send();

				Report(true, "SendWithdrawStation packet sent from client " + Main.myPlayer);
			}
		}

		public static void ReceiveClientStationOperation(BinaryReader reader, int sender)
		{
			Point16 position = new(reader.ReadInt16(), reader.ReadInt16());
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

			if (!TileEntity.ByPosition.TryGetValue(position, out TileEntity te) || te is not TECraftingAccess craftingAccess)
				return;

			craftingAccess.QClientOperation(reader, op, sender);

			Report(true, MessageType.ClientStationOperation + " packet received by server from client " + sender);
			Report(false, "Operation: " + op);
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

				Report(true, "Station operation " + op + " packet received by client " + Main.myPlayer);
			}
		}

		public static void SendResetCompactStage(Point16 heart)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.ResetCompactStage);
				packet.Write(heart.X);
				packet.Write(heart.Y);
				packet.Send();

				Report(true, MessageType.ResetCompactStage + " packet sent from client " + Main.myPlayer);
				Report(false, "Entity reset: (X: " + heart.X + ", Y: " + heart.Y + ")");
			}
		}

		public static void ReceiveResetCompactStage(BinaryReader reader, int sender)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				Point16 position = new(reader.ReadInt16(), reader.ReadInt16());
				if (TileEntity.ByPosition.TryGetValue(position, out var te) && te is TEStorageHeart heart)
					heart.ResetCompactStage();

				Report(true, MessageType.ResetCompactStage + " packet received by server from client " + sender);
				Report(false, "Entity reset: (X: " + position.X + ", Y: " + position.Y + ")");
			}
			else if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				reader.ReadPoint16();

				Report(true, MessageType.ResetCompactStage + " packet recevied by client " + Main.myPlayer);
			}
		}

		public static void SendCraftRequest(Point16 heart, List<Item> toWithdraw, List<Item> results)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.CraftRequest);
				packet.Write(heart.X);
				packet.Write(heart.Y);
				packet.Write(toWithdraw.Count);
				foreach (Item item in toWithdraw)
					ItemIO.Send(item, packet, true, true);
				packet.Write(results.Count);
				foreach (Item result in results)
					ItemIO.Send(result, packet, true, true);
				packet.Send();

				Report(true, MessageType.CraftRequest + " packet sent from client " + Main.myPlayer);
			}
		}

		public static void ReceiveCraftRequest(BinaryReader reader, int sender)
		{
			Point16 position = new(reader.ReadInt16(), reader.ReadInt16());
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

			if (!TileEntity.ByPosition.TryGetValue(position, out TileEntity te) || te is not TEStorageHeart heart)
				return;

			List<Item> toWithdraw = new();
			for (int k = 0; k < withdrawCount; k++)
				toWithdraw.Add(ItemIO.Receive(reader, true, true));

			int resultsCount = reader.ReadInt32();
			List<Item> results = new();
			for (int k = 0; k < resultsCount; k++)
				results.Add(ItemIO.Receive(reader, true, true));

			List<Item> items = CraftingGUI.HandleCraftWithdrawAndDeposit(heart, toWithdraw, results);

			Report(true, MessageType.CraftRequest + " packet received by server from client " + sender);

			if (items.Count > 0)
			{
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.CraftResult);
				packet.Write(items.Count);
				foreach (Item item in items)
					ItemIO.Send(item, packet, true, true);
				packet.Send(sender);

				Report(false, MessageType.CraftResult + " packet sent to all clients");
			}

			SendRefreshNetworkItems(position);
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

			Report(true, MessageType.CraftResult + " packet received by client " + Main.myPlayer);
			Report(false, "Item objects crafted: " + count);
		}

		public static void ClientRequestSection(Point16 coords)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.SectionRequest);

				packet.Write(coords.X);
				packet.Write(coords.Y);

				packet.Send();

				Report(false, MessageType.SectionRequest + " packet sent from client " + Main.myPlayer);
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
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.ForceCraftingGUIRefresh);

				packet.Write(heart.Position.X);
				packet.Write(heart.Position.Y);

				packet.Send(ignoreClient: Main.myPlayer);

				Report(true, MessageType.ForceCraftingGUIRefresh + " packet sent from client " + Main.myPlayer);
			}
		}

		public static void ReceiveClientForceCraftingGUIRefresh(BinaryReader reader, int sender) {
			Point16 storage = new(reader.ReadInt16(), reader.ReadInt16());

			if (Main.netMode == NetmodeID.Server) {
				//Forward the packet
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.ForceCraftingGUIRefresh);

				packet.Write(storage.X);
				packet.Write(storage.Y);

				packet.Send(ignoreClient: sender);

				Report(true, MessageType.ForceCraftingGUIRefresh + " packet sent from server from client " + sender);
			} else if (Main.netMode == NetmodeID.MultiplayerClient) {
				if (StoragePlayer.LocalPlayer.GetStorageHeart() is TEStorageHeart heart && heart.Position == storage && StoragePlayer.IsStorageCrafting()) {
					CraftingGUI.RefreshItems();

					Report(true, MessageType.ForceCraftingGUIRefresh + " packet received by client " + Main.myPlayer);
				}
			}
		}

		public static void ClientRequestItemTransfer(TEStorageUnit destination, TEStorageUnit source) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.TransferItems);
				packet.Write(destination.Position);
				packet.Write(source.Position);
				packet.Send(ignoreClient: Main.myPlayer);

				Report(true, MessageType.TransferItems + " packet sent from client " + Main.myPlayer);
			}
		}

		public static void ReceiveClientRequestItemTransfer(BinaryReader reader, int sender) {
			Point16 destination = reader.ReadPoint16();
			Point16 source = reader.ReadPoint16();

			if (Main.netMode != NetmodeID.Server)
				return;

			if (!TileEntity.ByPosition.TryGetValue(destination, out TileEntity tileEntity) || tileEntity is not TEStorageUnit unitDestination) {
				Report(true, MessageType.TransferItems + " packet failed to read on the server.\n" +
					"Reason: Destination was not a Storage Unit");
				return;
			}

			if (!TileEntity.ByPosition.TryGetValue(source, out tileEntity) || tileEntity is not TEStorageUnit unitSource) {
				Report(true, MessageType.TransferItems + " packet failed to read on the server.\n" +
					"Reason: Source was not a Storage Unit");
				return;
			}

			Report(true, MessageType.TransferItems + " packet was successfully received by server from client " + sender);

			TEStorageUnit.AttemptItemTransfer(unitDestination, unitSource, out List<Item> transferredItems);

			if (transferredItems.Count == 0)  //Nothing to do
				return;

			//Send the result to all clients
			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.TransferItemsResult);
			packet.Write(destination);
			packet.Write(source);
			packet.Write(transferredItems.Count);

			foreach (Item item in transferredItems)
				ItemIO.Send(item, packet, writeStack: true, writeFavorite: true);

			packet.Send();

			Report(true, MessageType.TransferItemsResult + " packet sent to all clients");

			unitDestination.GetHeart().ResetCompactStage();

			StartUpdateQueue();

			unitDestination.PostChangeContents();
			unitSource.PostChangeContents();

			ProcessUpdateQueue();
		}

		public static void RecieveTransferItemsResult(BinaryReader reader) {
			Point16 destination = reader.ReadPoint16();
			Point16 source = reader.ReadPoint16();

			if (!TileEntity.ByPosition.TryGetValue(destination, out TileEntity tileEntity) || tileEntity is not TEStorageUnit unitDestination) {
				Report(true, MessageType.TransferItems + " packet failed to read on client" + Main.myPlayer + ".\n" +
					"Reason: Destination was not a Storage Unit");
				return;
			}

			if (!TileEntity.ByPosition.TryGetValue(source, out tileEntity) || tileEntity is not TEStorageUnit unitSource) {
				Report(true, MessageType.TransferItems + " packet failed to read on client" + Main.myPlayer + ".\n" +
					"Reason: Source was not a Storage Unit");
				return;
			}

			int count = reader.ReadInt32();
			List<Item> transferredItems = new();
			for (int i = 0; i < count; i++)
				transferredItems.Add(ItemIO.Receive(reader, readStack: true, readFavorite: true));

			//Deposit/withdraw the transferred items
			List<Item> dest = unitDestination.GetItems() as List<Item>;
			List<Item> src = unitSource.GetItems() as List<Item>;

			foreach (Item item in transferredItems) {
				TEStorageUnit.DepositToItemCollection(dest, item.Clone(), unitDestination.Capacity, out _);
				TEStorageUnit.WithdrawFromItemCollection(src, item, out _);
			}

			Report(true, MessageType.TransferItemsResult + " packet received by client " + Main.myPlayer);
		}

		public static void SendCoinCompactRequest(Point16 heart) {
			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.RequestCoinCompact);
			packet.Write(heart);
			packet.Send();

			Report(true, MessageType.RequestCoinCompact + " packet sent to all clients");
		}

		public static void ReceiveCoinCompactRequest(BinaryReader reader, int sender) {
			if (Main.netMode == NetmodeID.Server) {
				Point16 position = reader.ReadPoint16();
				if (TileEntity.ByPosition.TryGetValue(position, out var te) && te is TEStorageHeart heart)
					heart.CompactCoins();

				Report(true, MessageType.RequestCoinCompact + " packet received by server from client " + sender);
				Report(false, "Entity read: (X: " + position.X + ", Y: " + position.Y + ")");
			} else if (Main.netMode == NetmodeID.MultiplayerClient) {
				reader.ReadPoint16();

				Report(true, MessageType.RequestCoinCompact + " packet recevied by client " + Main.myPlayer);
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
		ForceCraftingGUIRefresh,
		TransferItems,
		TransferItemsResult,
		RequestCoinCompact
	}
}
