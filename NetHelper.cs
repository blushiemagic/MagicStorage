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
using System.Linq;
using Terraria.Audio;
using MagicStorage.Common.Global;
using MagicStorage.Common.Systems;
using static MagicStorage.Common.Systems.StringScrambling;
using MagicStorage.Common.Players;
using MagicStorage.UI;
using System.Threading;
using ReLogic.Content;
using MagicStorage.Common.Systems.Shimmering;

namespace MagicStorage
{
	public static class NetHelper
	{
		private static bool queueUpdates;
		private static readonly Queue<int> updateQueue = new();
		private static readonly HashSet<int> updateQueueContains = new();

		[Conditional("NETPLAY")]
		public static void Report(bool reportTime, string message) {
			if (!AssetRepository.IsMainThread) {
				// Local capturing
				bool report = reportTime;
				string msg = message;
				DateTime now = DateTime.Now;

				Main.QueueMainThreadAction(() => Report_Inner(report, msg, now));
			} else
				Report_Inner(reportTime, message, DateTime.Now);
		}

		[Conditional("NETPLAY")]
		private static void Report_Inner(bool reportTime, string message, DateTime now) {
			StringBuilder sb = new();

			if (reportTime)
				sb.Append("Time: " + now.Ticks + " ");

			sb.Append(message);

			if (Main.netMode != NetmodeID.Server) {
				#if NETPLAY
				if (MagicStorageBetaConfig.PrintTextToChat)
				#endif
					Main.NewTextMultiline(sb.ToString(), c: Color.White);
			} else if (Main.dedServ) {
				if (reportTime) {
					ConsoleColor fg = Console.ForegroundColor;
					ConsoleColor bg = Console.BackgroundColor;

					Console.ForegroundColor = ConsoleColor.Red;
					Console.BackgroundColor = ConsoleColor.Black;

					Console.WriteLine("Time: " + now.Ticks);

					Console.ForegroundColor = fg;
					Console.BackgroundColor = bg;
				}

				Console.WriteLine(message);
			}

			MagicStorageMod.Instance.Logger.Debug(sb.ToString());
		}

		public static void PrintToServerLogAndConsole(bool reportTime, string message) {
			if (!Main.dedServ)
				return;

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

			StringBuilder sb = new();

			if (reportTime)
				sb.Append("Time: " + DateTime.Now.Ticks + " ");

			sb.Append(message);

			MagicStorageMod.Instance.Logger.Debug(sb.ToString());
		}

		public static void PrintClientRequest(int sender, string requestName, Vector2 worldCoordinates) {
			if (!MagicStorageMod.UsingPrivateBeta && MagicStorageServerConfig.ReportClientStorageUsage) {
				Utility.ConvertToGPSCoordinates(worldCoordinates, out string compassText, out string depthText);
				PrintToServerLogAndConsole(true, $"Client \"{Netplay.Clients[sender].Name}\" requested action \"{requestName}\" at location: {compassText} | {depthText}");
			}
		}

		public static void PrintClientRequest(int sender, string requestName, Point16 tileCoordinates) {
			PrintClientRequest(sender, requestName, tileCoordinates.ToWorldCoordinates());
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

			switch (type) {
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
				case MessageType.RequestCoinCompact:
					ReceiveCoinCompactRequest(reader, sender);
					break;
				case MessageType.MassDuplicateSellRequest:
					ReceiveDuplicateSellingRequest(reader, sender);
					break;
				case MessageType.MassDuplicateSellResult:
					ClientReceiveDuplicateSellingResult(reader);
					break;
				case MessageType.RequestStorageUnitStyle:
					ReceiveStorageUnitStyle(reader, sender);
					break;
				case MessageType.ServerQuickStackToStorageResult:
					ClientReceiveQuickStackToNearbyStorageResult(reader);
					break;
				case MessageType.GolemHelpTextUpdate:
					ClientReceiveGolemTextUpdate();
					break;
				case MessageType.ClientRequestServerOp:
					ServerReceiveOperatorRequest(sender);
					break;
				case MessageType.ServerOpResponse:
					ClientReceiveOperatorReponse();
					break;
				case MessageType.ClientRequestServerOpConfirmation:
					ServerReceiveOperatorKeyFromClient(reader, sender);
					break;
				case MessageType.ServerOpConfirmationResult:
					ClientReceiveOperatorConformationResult(reader);
					break;
				case MessageType.PlayerHasServerOp:
					ReceivePlayerHasOperator(reader);
					break;
				case MessageType.ClientRequestPlayerBankDeposit:
					ServerReceiveDepositFromBankRequest(reader, sender);
					break;
				case MessageType.PlayerBankDepositResult:
					ClientReceiveDepositFromBankResult(reader);
					break;
				case MessageType.ComponentPlacement:
					ServerReceiveComponentPlacement(reader, sender);
					break;
				case MessageType.ComponentDestruction:
					ServerReceiveComponentDestruction(reader, sender);
					break;
				case MessageType.ClientLockStorageHeart:
				case MessageType.ClientUnlockStorageHeart:
					ReceiveStorageHeartUsage(reader, sender, type == MessageType.ClientLockStorageHeart);
					break;
				case MessageType.DeleteSpecificItem:
					ServerReceiveExactItemDeletionRequest(reader);
					break;
				case MessageType.RequestShimmerItemInStorage:
					ServerReceiveItemShimmeringRequest(reader, sender);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type));
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
				else if (op == TEStorageHeart.Operation.WithdrawThenTryModuleInventory || op == TEStorageHeart.Operation.WithdrawToInventoryThenTryModuleInventory)
				{
					_ = ItemIO.Receive(reader, true, true);
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
				if (op == TEStorageHeart.Operation.Withdraw || op == TEStorageHeart.Operation.WithdrawToInventory || op == TEStorageHeart.Operation.Deposit || op == TEStorageHeart.Operation.WithdrawThenTryModuleInventory || op == TEStorageHeart.Operation.WithdrawToInventoryThenTryModuleInventory)
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
				else if (op == TEStorageHeart.Operation.WithdrawThenTryModuleInventory || op == TEStorageHeart.Operation.WithdrawToInventoryThenTryModuleInventory)
				{
					_ = ItemIO.Receive(reader, true, true);
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
			else if (op == TEStorageHeart.Operation.WithdrawThenTryModuleInventory || op == TEStorageHeart.Operation.WithdrawToInventoryThenTryModuleInventory)
			{
				Item item  = ItemIO.Receive(reader, true, true);
				var heart = StoragePlayer.LocalPlayer.GetStorageHeart();

				if (item.IsAir)
					item = CraftingGUI.TryToWithdrawFromModuleItems(item, wasAlreadyCloned: true);

				StoragePlayer.GetItem(new EntitySource_TileEntity(heart), item, op != TEStorageHeart.Operation.WithdrawToInventoryThenTryModuleInventory);
			}

			Report(true, MessageType.ServerStorageResult + " packet received by client " + Main.myPlayer);
			Report(false, "Operation: " + op);
		}

		public static void SendRefreshNetworkItems(Point16 position, bool forceFullRefresh = false, IEnumerable<int> typesToRefresh = null)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.RefreshNetworkItems);
				packet.Write(position.X);
				packet.Write(position.Y);

				if (typesToRefresh is null || !typesToRefresh.Any())
					packet.Write((ushort)0);
				else {
					List<int> types = typesToRefresh.ToList();
					packet.Write((ushort)types.Count);

					foreach (int id in types)
						packet.Write(id);
				}

				packet.Write(forceFullRefresh);

				packet.Send();

				Report(true, MessageType.RefreshNetworkItems + " packet sent from client " + Main.myPlayer);
			}
		}

		private static void ReceiveRefreshNetworkItems(BinaryReader reader)
		{
			Point16 position = new(reader.ReadInt16(), reader.ReadInt16());
			int count = reader.ReadUInt16();

			List<int> types = new();
			for (int i = 0; i < count; i++)
				types.Add(reader.ReadInt32());

			bool forceFullRefresh = reader.ReadBoolean();

			if (Main.netMode == NetmodeID.Server)
				return;

			if (TileEntity.ByPosition.ContainsKey(position)) {
				MagicUI.ForceNextRefreshToBeFull = forceFullRefresh;
				MagicUI.SetNextCollectionsToRefresh(types);
			}

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

				PrintClientRequest(sender, $"{(inActive ? "Deactivate" : "Activate")} Unit", position);
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

			if (Main.netMode != NetmodeID.MultiplayerClient) {
				if (op == TECraftingAccess.Operation.Deposit)
					_ = reader.ReadUInt16();

				return;
			}

			if (op == TECraftingAccess.Operation.Withdraw || op == TECraftingAccess.Operation.WithdrawToInventory)
			{
				var heart = StoragePlayer.LocalPlayer.GetStorageHeart();
				StoragePlayer.GetItem(new EntitySource_TileEntity(heart), item, op == TECraftingAccess.Operation.Withdraw);
					
				TECraftingAccess.UpdateRecipesFromStationAction(item);
			}
			else // deposit operation
			{
				Main.mouseItem = item;

				int oldType = reader.ReadUInt16();
				TECraftingAccess.UpdateRecipesFromStationAction(new Item(oldType));
			}

			Report(true, "Station operation " + op + " packet received by client " + Main.myPlayer);
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

			PrintClientRequest(sender, "Craft", position);

			if (!TileEntity.ByPosition.TryGetValue(position, out TileEntity te) || te is not TEStorageHeart heart)
				return;

			HashSet<int> typesToUpdate = new();

			List<Item> toWithdraw = new();
			for (int k = 0; k < withdrawCount; k++) {
				Item withdrawn = ItemIO.Receive(reader, true, true);
				toWithdraw.Add(withdrawn);
				typesToUpdate.Add(withdrawn.type);
			}

			int resultsCount = reader.ReadInt32();
			List<Item> results = new();
			for (int k = 0; k < resultsCount; k++) {
				Item result = ItemIO.Receive(reader, true, true);
				results.Add(result);
				typesToUpdate.Add(result.type);
			}

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

			SendRefreshNetworkItems(position, false, typesToUpdate);
		}

		public static void ReceiveCraftResult(BinaryReader reader)
		{
			Player player = Main.LocalPlayer;
			int count = reader.ReadInt32();
			for (int k = 0; k < count; k++)
			{
				Item item  = ItemIO.Receive(reader, true, true);
				var  heart = StoragePlayer.LocalPlayer.GetStorageHeart();

				player.QuickSpawnItem(new EntitySource_TileEntity(heart), item, item.stack);
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

				PrintClientRequest(sender, "Refresh UI", storage);
			} else if (Main.netMode == NetmodeID.MultiplayerClient) {
				if (StoragePlayer.LocalPlayer.GetStorageHeart() is TEStorageHeart heart && heart.Position == storage && StoragePlayer.IsStorageCrafting()) {
					MagicUI.RefreshItems();

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

			AttemptItemTransferAndSendResult(unitDestination, unitSource, out _);
		}

		public static bool AttemptItemTransferAndSendResult(TEStorageUnit destination, TEStorageUnit source, out List<Item> transferredItems, bool netQueue = true) {
			transferredItems = null;

			if (Main.netMode != NetmodeID.Server)
				return false;

			Report(true, $"Performing AttemptItemTransferAndSendResult on source unit (X: {source.Position.X}, Y: {source.Position.Y}) and destination unit (X: {destination.Position.X}, Y: {destination.Position.Y})...");

			TEStorageUnit.AttemptItemTransfer(destination, source, out transferredItems);

			if (transferredItems.Count == 0) {
				//Nothing to do
				Report(false, "No items were transferred");
				return false;
			}

			Report(false, transferredItems.Count + " items were transferred");

			if (netQueue) {
				StartUpdateQueue();

				destination.GetHeart()?.ResetCompactStage();
			}

			destination.FullySync();
			source.FullySync();

			destination.PostChangeContents();
			source.PostChangeContents();

			if (netQueue)
				ProcessUpdateQueue();

			if (destination.GetHeart() is TEStorageHeart heart)
				SendRefreshNetworkItems(heart.Position, false, transferredItems.Select(static i => i.type).Distinct());

			return true;
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

				PrintClientRequest(sender, "Compact Coins", position);
			} else if (Main.netMode == NetmodeID.MultiplayerClient) {
				reader.ReadPoint16();

				Report(true, MessageType.RequestCoinCompact + " packet recevied by client " + Main.myPlayer);
			}
		}

		public static void RequestDuplicateSelling(Point16 heart, int sellOption) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.MassDuplicateSellRequest);
			packet.Write(heart);
			packet.Write((byte)sellOption);
			packet.Send();

			Report(true, MessageType.MassDuplicateSellRequest + " packet sent to the server");
		}

		public static void ReceiveDuplicateSellingRequest(BinaryReader reader, int sender) {
			if (Main.netMode == NetmodeID.Server) {
				Point16 position = reader.ReadPoint16();
				int sellOption = reader.ReadByte();

				if (TileEntity.ByPosition.TryGetValue(position, out var te) && te is TEStorageHeart heart) {
					StorageUIState.ControlsPage.DoSell(heart, sellOption, out long coppersEarned, out var withdrawnItems);

					int sold = withdrawnItems.Values.Select(l => l.Count).Sum();

					Report(false, $"{sold} items were sold for {coppersEarned} copper coins");

					StorageUIState.ControlsPage.DuplicateSellingResult(heart, sold, coppersEarned, reportText: false, depositCoins: true);

					ModPacket packet = MagicStorageMod.Instance.GetPacket();
					packet.Write((byte)MessageType.MassDuplicateSellResult);
					packet.Write((short)sender);
					packet.Write(position);
					packet.Write7BitEncodedInt64(coppersEarned);
					packet.Write7BitEncodedInt(sold);

					packet.Send();
				}

				Report(false, MessageType.MassDuplicateSellRequest + " packet received by server from client " + sender);
				Report(false, "Entity read: (X: " + position.X + ", Y: " + position.Y + ")");

				PrintClientRequest(sender, "Sell Duplicates", position);
			} else if (Main.netMode == NetmodeID.MultiplayerClient) {
				reader.ReadPoint16();
				reader.ReadInt32();

				Report(true, MessageType.MassDuplicateSellRequest + " packet recevied by client " + Main.myPlayer);
			}
		}

		public static void ClientReceiveDuplicateSellingResult(BinaryReader reader) {
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				//Read the data, but do nothing with it
				_ = reader.ReadInt16();
				_ = reader.ReadPoint16();
				_ = reader.Read7BitEncodedInt64();

				return;
			}

			short sender = reader.ReadInt16();
			Point16 heart = reader.ReadPoint16();
			long coppersEarned = reader.Read7BitEncodedInt64();

			int sold = reader.Read7BitEncodedInt();

			if (!TileEntity.ByPosition.TryGetValue(heart, out TileEntity heartEntity) || heartEntity is not TEStorageHeart storageHeart) {
				Report(true, MessageType.MassDuplicateSellResult + " packet was malformed: Storage Heart location did not have a Storage Heart");
				return;
			}

			Report(true, $"{sold} items were sold/destroyed at heart (X: {heart.X}, Y: {heart.Y}) for {coppersEarned} copper coins");

			Report(false, MessageType.MassDuplicateSellResult + " packet recevied by client " + Main.myPlayer);

			if (sender == Main.myPlayer)
				StorageUIState.ControlsPage.DuplicateSellingResult(storageHeart, sold, coppersEarned, reportText: true, depositCoins: false);
		}

		public static void RequestStorageUnitStyle(Point16 unit) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.RequestStorageUnitStyle);
				packet.Write(unit);
				packet.Send();

				Report(true, MessageType.RequestStorageUnitStyle + " packet sent to the server");
			}
		}

		public static void ReceiveStorageUnitStyle(BinaryReader reader, int sender) {
			if (Main.netMode != NetmodeID.Server) {
				_ = reader.ReadPoint16();

				return;
			}

			Point16 unit = reader.ReadPoint16();

			//Safeguard:  Ensure that the map section exists before sending data
			RemoteClient.CheckSection(sender, unit.ToWorldCoordinates());

			PrintClientRequest(sender, "Update Unit Type", unit);

			if (!TileEntity.ByPosition.TryGetValue(unit, out TileEntity entity) || entity is not TEStorageUnit storageUnit) {
				Report(true, MessageType.RequestStorageUnitStyle + " packet was malformed: Storage Unit location did not have a Storage Unit");
				return;
			}

			storageUnit.UpdateTileFrameWithNetSend();

			Report(false, MessageType.RequestStorageUnitStyle + " packet received by server from client " + sender);
		}

		public static void ClientReceiveQuickStackToNearbyStorageResult(BinaryReader reader) {
			bool playSound = reader.ReadBoolean();
			int origType = reader.ReadInt32();

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			// NOTE: 1.4.4 does not play a sound
			/*
			if (playSound)
				SoundEngine.PlaySound(SoundID.Grab);
			*/

			if (origType > 0)
				MagicUI.SetNextCollectionsToRefresh(origType);
		}

		public static void SendGolemTextUpdate() {
			if (Main.netMode != NetmodeID.Server)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.GolemHelpTextUpdate);
			packet.Send();
		}

		public static void ClientReceiveGolemTextUpdate() {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			GolemTextTracking.SetPendingText();
		}

		public static void ClientRequestServerOperator() {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ClientRequestServerOp);
			packet.Send();

			Report(true, MessageType.ClientRequestServerOp + " packet sent to the server");
		}

		public static void ServerReceiveOperatorRequest(int sender) {
			if (Main.netMode != NetmodeID.Server)
				return;

			bool print = !Netcode.KeyIsGenerated;

			string key = Netcode.ServerOperatorKey;

			Report(false, MessageType.ClientRequestServerOp + " packet received by server from client " + sender);

			if (print) {
				ConsoleColor fg = Console.ForegroundColor;
				ConsoleColor bg = Console.BackgroundColor;

				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.BackgroundColor = ConsoleColor.Black;

				string keyMsg = "=====\n" +
					"THIS MESSAGE WILL ONLY BE DISPLAYED ONCE!\n" +
					"Server Operator Key: " + key + "\n" +
					"=====";

				Console.WriteLine(keyMsg);
				// Send the text to the client log as well
				MagicStorageMod.Instance.Logger.Info("\n" + keyMsg);

				Console.ForegroundColor = fg;
				Console.BackgroundColor = bg;
			}

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ServerOpResponse);
			packet.Send(toClient: sender);

			Report(false, MessageType.ServerOpResponse + " packet sent to client " + sender);
		}

		public static void ClientReceiveOperatorReponse() {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Report(false, MessageType.ServerOpResponse + " packet received by client " + Main.myPlayer);

			Main.NewText("=== ENTER THE KEY PRINTED TO THE SERVER'S CONSOLE ===", Color.Yellow);

			Netcode.RequestingOperatorKey = true;
		}

		public static void ClientSendOperatorKey(string key) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Netcode.RequestingOperatorKey = false;

			if (!Netcode.IsKeyValidForConfirmationMessage(key)) {
				//Bail immediately since the key couldn't be valid in the first place
				Netcode.ClientPrintKeyReponse(valid: false);
				return;
			}

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ClientRequestServerOpConfirmation);
			byte[] bytes = ToBytes(Scramble(key));
			packet.Write((byte)bytes.Length);
			packet.Write(bytes);
			packet.Send();

			Report(true, MessageType.ClientRequestServerOpConfirmation + " packet sent to the server");
		}

		public static void ServerReceiveOperatorKeyFromClient(BinaryReader reader, int sender) {
			byte count = reader.ReadByte();
			byte[] bytes = reader.ReadBytes(count);

			if (Main.netMode != NetmodeID.Server)
				return;

			string key = Unscramble(FromBytes(bytes));

			bool valid = key == Netcode.ServerOperatorKey;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ServerOpConfirmationResult);
			packet.Write(valid);
			packet.Send(toClient: sender);

			Report(false, MessageType.ServerOpConfirmationResult + " packet sent to client " + sender);

			if (valid)
				PrintClientRequest(sender, "Enable Server Oprator", Main.player[sender].Center);
		}

		public static void ClientReceiveOperatorConformationResult(BinaryReader reader) {
			bool valid = reader.ReadBoolean();

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Report(false, MessageType.ServerOpConfirmationResult + " packet received by client " + Main.myPlayer);

			Netcode.ClientPrintKeyReponse(valid);

			if (valid) {
				var mp = Main.LocalPlayer.GetModPlayer<OperatorPlayer>();

				mp.manualOp = mp.hasOp = true;

				ClientSendPlayerHasOp(Main.myPlayer);
			}
		}

		public static void ClientSendPlayerHasOp(int plr) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.PlayerHasServerOp);
			packet.Write((byte)plr);

			var mp = Main.LocalPlayer.GetModPlayer<OperatorPlayer>();
			BitsByte bb = new(mp.hasOp, mp.manualOp);

			packet.Write(bb);
			packet.Send(ignoreClient: Main.myPlayer);

			Report(true, MessageType.PlayerHasServerOp + " packet sent to the server");
		}

		public static void ReceivePlayerHasOperator(BinaryReader reader) {
			byte plr = reader.ReadByte();
			BitsByte opFlags = reader.ReadByte();

			var mp = Main.player[plr].GetModPlayer<OperatorPlayer>();

			opFlags.Retrieve(ref mp.hasOp, ref mp.manualOp);

			if (Main.netMode != NetmodeID.Server) {
				Report(true, MessageType.PlayerHasServerOp + " packet received by client " + Main.myPlayer);
				return;
			}

			//Forward the result
			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.PlayerHasServerOp);
			packet.Write(plr);

			BitsByte bb = new(mp.hasOp, mp.manualOp);

			packet.Write(bb);
			packet.Send(ignoreClient: plr);

			Report(true, MessageType.PlayerHasServerOp + " packet sent to all clients");
		}

		public static void ClientRequestDepositFromBank(Item[] inventory, Point16 heart, Action<Player, Item[]> netResult) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			UIStorageControlDepositPlayerInventoryButton.PendingResultAction = netResult;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ClientRequestPlayerBankDeposit);

			packet.Write(heart);

			packet.Write((ushort)inventory.Length);

			for (int i = 0; i < inventory.Length; i++)
				ItemIO.Send(inventory[i], packet, true, true);

			packet.Send();

			Report(true, MessageType.ClientRequestPlayerBankDeposit + " packet sent to the server");
		}

		public static void ServerReceiveDepositFromBankRequest(BinaryReader reader, int sender) {
			Point16 heart = reader.ReadPoint16();

			int count = reader.ReadUInt16();

			Item[] inventory = new Item[count];

			for (int i = 0; i < count; i++)
				inventory[i] = ItemIO.Receive(reader, true, true);

			if (!TileEntity.ByPosition.TryGetValue(heart, out TileEntity heartEntity) || heartEntity is not TEStorageHeart storageHeart) {
				Report(true, MessageType.ClientRequestPlayerBankDeposit + " packet was malformed: Storage Heart location did not have a Storage Heart");
				return;
			}

			if (Main.netMode != NetmodeID.Server) {
				Report(true, MessageType.ClientRequestPlayerBankDeposit + " packet received by client " + Main.myPlayer);
				return;
			}

			UIStorageControlDepositPlayerInventoryButton.TryDepositItems(inventory, storageHeart, false, out bool changed);

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.PlayerBankDepositResult);
			packet.Write(changed);

			packet.Write((ushort)inventory.Length);

			for (int i = 0; i < inventory.Length; i++)
				ItemIO.Send(inventory[i], packet, true, true);

			packet.Send(toClient: sender);

			Report(false, MessageType.PlayerBankDepositResult + " packet sent to client " + sender);

			PrintClientRequest(sender, "Deposit Items from Bank/Safe/Forge", heart);
		}

		public static void ClientReceiveDepositFromBankResult(BinaryReader reader) {
			bool changed = reader.ReadBoolean();
			int count = reader.ReadUInt16();

			Item[] inventory = new Item[count];

			for (int i = 0; i < count; i++)
				inventory[i] = ItemIO.Receive(reader, true, true);

			if (Main.netMode != NetmodeID.MultiplayerClient) {
				Report(true, MessageType.PlayerBankDepositResult + " packet received by the server");
				return;
			}

			Interlocked.Exchange(ref UIStorageControlDepositPlayerInventoryButton.PendingResultAction, null)?.Invoke(Main.LocalPlayer, inventory);

			if (changed)
				SoundEngine.PlaySound(SoundID.Grab);

			Report(true, MessageType.PlayerBankDepositResult + " packet received by client " + Main.myPlayer);
		}

		public static void SendComponentPlacement(Point16 position) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ComponentPlacement);
			packet.Write(position);
			packet.Send();

			Report(true, MessageType.ComponentPlacement + " packet sent to the server");
		}

		public static void ServerReceiveComponentPlacement(BinaryReader reader, int sender) {
			Point16 position = reader.ReadPoint16();

			if (Main.netMode != NetmodeID.Server)
				return;

			PrintClientRequest(sender, "Component Placement", position);
			Report(false, MessageType.ComponentPlacement + " packet received by server from client " + sender);
		}

		public static void SendComponentDestruction(Point16 position) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ComponentDestruction);
			packet.Write(position);
			packet.Send();

			Report(true, MessageType.ComponentDestruction + " packet sent to the server");
		}

		public static void ServerReceiveComponentDestruction(BinaryReader reader, int sender) {
			Point16 position = reader.ReadPoint16();

			if (Main.netMode != NetmodeID.Server)
				return;

			PrintClientRequest(sender, "Component Destruction", position);
			Report(false, MessageType.ComponentDestruction + " packet received by server from client " + sender);
		}

		public static void ClientInformStorageHeartUsage(TEStorageHeart heart) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var msg = heart.clientUsingHeart[Main.myPlayer] ? MessageType.ClientLockStorageHeart : MessageType.ClientUnlockStorageHeart;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)msg);
			packet.Write((byte)Main.myPlayer);
			packet.Write(heart.Position);
			packet.Send(ignoreClient: Main.myPlayer);
			packet.Send();

			Report(true, msg + " packet sent to the server");
		}

		public static void ReceiveStorageHeartUsage(BinaryReader reader, int sender, bool inUse) {
			byte player = reader.ReadByte();
			Point16 position = reader.ReadPoint16();

			var msg = inUse ? MessageType.ClientLockStorageHeart : MessageType.ClientUnlockStorageHeart;

			if (!TileEntity.ByPosition.TryGetValue(position, out TileEntity entity) || entity is not TEStorageHeart heart)
				return;

			heart.clientUsingHeart[player] = inUse;

			if (Main.netMode == NetmodeID.Server) {
				// Forward to other clients
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)msg);
				packet.Write(player);
				packet.Write(position);
				packet.Send(ignoreClient: sender);

				Report(true, msg + " packet sent from server from client " + sender);
			} else
				Report(true, msg + " packet received by client " + Main.myPlayer);
		}

		public static void ClientRequestExactItemDeletion(TEStorageHeart heart, Item item) {
			if (Main.netMode != NetmodeID.MultiplayerClient || !Main.LocalPlayer.GetModPlayer<OperatorPlayer>().hasOp)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.DeleteSpecificItem);
			packet.Write(heart.Position);
			packet.Write(Utility.ToBase64NoCompression(item));
			packet.Send();
		}

		public static void ServerReceiveExactItemDeletionRequest(BinaryReader reader) {
			Point16 point = reader.ReadPoint16();
			string item = reader.ReadString();

			if (Main.netMode != NetmodeID.Server)
				return;

			if (!TileEntity.ByPosition.TryGetValue(point, out TileEntity entity) || entity is not TEStorageHeart heart)
				return;

			heart.TryDeleteExactItem(item);
		}

		public static void RequestItemShimmering(int itemType, int toShimmer, StorageIntermediary storage, List<IShimmerResult> results) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.RequestShimmerItemInStorage);
			packet.Write(itemType);
			packet.Write(toShimmer);
			storage.Send(packet);
			ShimmerMetrics.SendShimmerResults(packet, results);
			packet.Send();

			Report(true, MessageType.RequestShimmerItemInStorage + " packet sent to the server");
		}

		public static void ServerReceiveItemShimmeringRequest(BinaryReader reader, int sender) {
			int itemType = reader.ReadInt32();
			int toShimmer = reader.ReadInt32();

			var storage = StorageIntermediary.Receive(reader);

			var results = ShimmerMetrics.ReceiveShimmerResults(reader);

			if (Main.netMode != NetmodeID.Server || storage is null)
				return;

			Item shimmeringItem = new Item(itemType, toShimmer);
			int iconicItem = MagicCache.ShimmerInfos[itemType].iconicItem;

			foreach (var result in results)
				result?.OnShimmer(shimmeringItem, iconicItem, storage, false);

			List<Item> items = CraftingGUI.HandleCraftWithdrawAndDeposit(storage.heart, storage.toWithdraw, storage.toDeposit);

			Report(true, MessageType.RequestShimmerItemInStorage + " packet received by server from client " + sender);

			if (items.Count > 0) {
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.CraftResult);
				packet.Write(items.Count);
				foreach (Item item in items)
					ItemIO.Send(item, packet, true, true);
				packet.Send(sender);

				Report(false, MessageType.CraftResult + " packet sent to all clients");
			}

			SendRefreshNetworkItems(storage.heart.Position, false);
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
		RequestCoinCompact,
		MassDuplicateSellRequest,
		MassDuplicateSellResult,
		RequestStorageUnitStyle,
		ServerQuickStackToStorageResult,
		GolemHelpTextUpdate,
		ClientRequestServerOp,
		ServerOpResponse,
		ClientRequestServerOpConfirmation,
		ServerOpConfirmationResult,
		PlayerHasServerOp,
		ClientRequestPlayerBankDeposit,
		PlayerBankDepositResult,
		ComponentPlacement,
		ComponentDestruction,
		ClientLockStorageHeart,
		ClientUnlockStorageHeart,
		DeleteSpecificItem,
		RequestShimmerItemInStorage
	}
}
