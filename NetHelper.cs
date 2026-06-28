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
using System.Text;
using System.Linq;
using Terraria.Audio;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Players;
using MagicStorage.UI;
using System.Threading;
using ReLogic.Content;
using MagicStorage.Common.Systems.Shimmering;
using MagicStorage.UI.Selling;
using Terraria.Localization;
using MagicStorage.Items;
using MagicStorage.Common.Systems.Auditing;
using MagicStorage.NPCs;
using MagicStorage.Common;
using MagicStorage.Common.Systems.Debugging;

namespace MagicStorage
{
	public static partial class NetHelper
	{
		private static bool queueUpdates;
		private static readonly Queue<int> updateQueue = new();
		private static readonly HashSet<int> updateQueueContains = new();

		[Conditional("NETPLAY")]
		[Obsolete("This method has been replaced by the DebugMessage APIs", error: true)]
		public static void Report(bool reportTime, string message) => DebugMessage.Report(reportTime, message);

		public static void HandlePacket(BinaryReader reader, int sender)
		{
			MessageType type = (MessageType)reader.ReadByte();

			/*
			if (Main.netMode == NetmodeID.MultiplayerClient)
				Main.NewText($"Receiving Message Type \"{Enum.GetName(type)}\"");
			else if(Main.netMode == NetmodeID.Server)
				Console.WriteLine($"Receiving Message Type \"{Enum.GetName(type)}\"");
			*/

			using var debugging = DebugMessage.CreateIf(DebugControls.Names.IncomingNetcodePackets);

			if (debugging.IsDebugging) {
				if (Main.netMode == NetmodeID.Server)
					debugging.Report(true, "Handling packet {0} from client {1}", type, sender);
				else
					debugging.Report(true, "Handling packet {0} from the server", type);

				debugging.Indent();
			}

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
					ClientReceiveGolemTextUpdate(reader);
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
					PlayerInventoryTeller.ServerReceiveDepositToStorageRequest(reader, sender);
					break;
				case MessageType.PlayerBankDepositResult:
					PlayerInventoryTeller.ClientReceiveDepositToStorageResponse(reader);
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
					ServerReceiveExactItemDeletionRequest(reader, sender);
					break;
				case MessageType.RequestShimmerItemInStorage:
					ServerReceiveItemShimmeringRequest(reader, sender);
					break;
				case MessageType.RenameStorageHeart:
					ReceiveStorageHeartName(reader, sender);
					break;
				case MessageType.SyncDepositHistory:
					Obsolete_ReceiveStorageDepositHistory(reader, sender);
					break;
				case MessageType.ClientSendCoreRemoval:
					ReceiveCoreRemoval(reader, sender);
					break;
				case MessageType.ClientSendCoreInsertion:
					ReceiveCoreInsertion(reader, sender);
					break;
				case MessageType.SecurityNetworkCreation:
					ReceiveSecurityNetworkCreation(reader, sender);
					break;
				case MessageType.SecurityNetworkRemoval:
					ReceiveSecurityNetworkRemoval(reader, sender);
					break;
				case MessageType.SecurityNetworkJoin:
					ReceiveSecurityNetworkJoinAttempt(reader, sender);
					break;
				case MessageType.SecurityNetworkAccessible:
					ReceiveSecurityNetworkAccessAttempt(reader, sender);
					break;
				case MessageType.SecurityNetworkModification:
					ReceiveSecurityNetworkChange(reader, sender);
					break;
				case MessageType.RequestSecurityNetworkList:
					ReceiveSecurityNetworkList(reader, sender);
					break;
				case MessageType.SecurityPlayerSync:
					ReceiveSecurityPlayerSync(reader, sender);
					break;
				case MessageType.StorageHeartNetwork:
					ReceiveStorageComponentNetwork(reader, sender);
					break;
				case MessageType.StorageHeartNetworkAssignment:
					ReceiveStorageHeartNetworkAssignmentRequest(reader, sender);
					break;
				case MessageType.DefaultAccessibleNetworks:
					RecieveAccessibleNetworksByDefaultRequest(reader, sender);
					break;
				case MessageType.SecurityNetworkPassword:
					ReceiveNetworkPasswordRequest(reader, sender);
					break;
				case MessageType.AuditSystemMessage:
					AuditSystem.HandlePacket(reader, sender);
					break;
				case MessageType.SyncPityDropsPlayer:
					ReceivePityDropsPlayerSync(reader, sender);
					break;
				case MessageType.ClientRequestDepositHistoryChunks:
					ServerReceiveDepositHistoryChunksRequest(reader);
					break;
				case MessageType.ServerResponseDepositHistoryChunks:
					ClientReceiveDepositHistoryChunk(reader);
					break;
				case MessageType.UpdateDepositHistory:
					ClientReceiveDepositHistoryUpdate(reader);
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

				using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageSyncingNetcode);

				if (debugging.IsDebugging)
					debugging.Report(false, "Sent packet {0} to the server", MessageType.SyncStorageUnit);
			}
		}

		private static void ServerReciveSyncStorageUnit(BinaryReader reader, int remoteClient)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				//byte remoteClient = reader.ReadByte();
				Point16 position = new(reader.ReadInt16(), reader.ReadInt16());

				using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageSyncingNetcode);

				if (debugging.IsDebugging)
					debugging.Report(false, "Read position: {0}", position.DebugString());

				if (!TryGetEntityFromLocation(MessageType.SyncStorageUnit, position, out TEStorageUnit storageUnit))
					return;

				storageUnit.FullySync();

				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.SyncStorageUnitToClinet);
				TileEntity.Write(packet, storageUnit, true);
				packet.Send(remoteClient);

				if (debugging.IsDebugging)
					debugging.Report(false, "Sent packet {0} to client {1}", MessageType.SyncStorageUnitToClinet, remoteClient);
			}
		}

		private static void ClientReciveStorageSync(BinaryReader reader)
		{
			// TileEntity.Read(reader, true);

			byte type = reader.ReadByte();
			int id = reader.ReadInt32();

			Point16 position = reader.ReadPoint16();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageSyncingNetcode);

			if (debugging.IsDebugging) {
				debugging.Chain()
					.Report(false, "Read position: {0}", position.DebugString())
					.Report(false, "Read entity type: {0}", type)
					.Report(false, "Read entity ID: {0}", id);
			}

			if (!TryGetEntityFromLocation(MessageType.SyncStorageUnitToClinet, position, out TEStorageUnit storageUnit)) {
				// Use a dummy instance to read the rest of the data
				TileEntity dummy = TileEntity.manager.GenerateInstance(type);
				dummy.type = type;
				dummy.ID = id;
				dummy.Position = position;
				dummy.NetReceive(reader);
				return;
			}

			storageUnit.NetReceive(reader);
		}

		[Obsolete("This method has been renamed to " + nameof(SendComponentTilesAndEntityPlacement), error: true)]
		public static void SendComponentPlace(int i, int j, int type) => SendComponentTilesAndEntityPlacement(i, j, type);

		public static void SendComponentTilesAndEntityPlacement(int i, int j, int type)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				NetMessage.SendTileSquare(Main.myPlayer, i, j, 2, 2);

				using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageComponentPlacement);

				if (debugging.IsDebugging) {
					debugging
						.Report(true, "Sent vanilla method packet SendTileSquare to the server")
						.Indent()
						.Report(false, "Coordinates: (X: {0}, Y: {1})", i, j)
						.Report(false, "Type: {0}", type)
						.Unindent();
				}

				NetMessage.SendData(MessageID.TileEntityPlacement, -1, -1, null, i, j, type);

				if (debugging.IsDebugging) {
					debugging
						.Report(true, "Sent vanilla ID packet TileEntityPlacement to the server")
						.Indent()
						.Report(false, "Coordinates: (X: {0}, Y: {1})", i, j)
						.Report(false, "Type: {0}", type)
						.Unindent();
				}
			}
		}

		public static void StartUpdateQueue()
		{
			queueUpdates = true;
		}

		[Obsolete("Tile entity syncing no longer requires a position; use the other overload of this method instead.", error: true)]
		public static void SendTEUpdate(int id, Point16 position)
		{
			SendTEUpdate(id);
		}

		public static void SendTEUpdate(int id)
		{
			if (Main.netMode != NetmodeID.Server)
				return;

			if (queueUpdates)
			{
				if (!updateQueueContains.Contains(id))
				{
					updateQueue.Enqueue(id);
					updateQueueContains.Add(id);

					using var debugging = DebugMessage.CreateIf(DebugControls.Names.TileEntityUpdateQueueNetcode);

					if (debugging.IsDebugging)
						debugging.Report(true, "Queueing tile entity update (ID: {0})", id);
				}
			}
			else
			{
				NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, id);

				using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.TileEntityUpdatesNetcode);

				if (debugging.IsDebugging) {
					debugging
						.Report(true, "Sent vanilla ID packet TileEntitySharing to all clients")
						.Indent()
						.Report(false, "ID: {0}", id);
				}
			}
		}

		public static void ProcessUpdateQueue()
		{
			if (queueUpdates)
			{
				int count = updateQueue.Count;

				queueUpdates = false;
				while (updateQueue.Count > 0)
					NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, updateQueue.Dequeue());
				updateQueueContains.Clear();

				using var debugging = DebugMessage.CreateIf(
					DebugControls.Combine()
						.Set(count > 0)
						.And(DebugControls.Names.OutgoingNetcodePackets)
						.AndAny(DebugControls.Names.TileEntityUpdatesNetcode, DebugControls.Names.TileEntityUpdateQueueNetcode)
				);

				if (debugging.IsDebugging)
					debugging.Report(true, "Sent vanilla ID packet TileEntitySharing for {0} entities to all clients", count);
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

				using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageNetworkRecalculate);

				if (debugging.IsDebugging) {
					debugging
						.Report(false, "Sent packet {0} to the server", MessageType.SearchAndRefreshNetwork)
						.Indent()
						.Report(false, "Refresh origin: (X: {0}, Y: {1})", i, j);
				}
			}
		}

		private static void ReceiveSearchAndRefresh(BinaryReader reader)
		{
			Point16 point = new(reader.ReadInt16(), reader.ReadInt16());

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageNetworkRecalculate);

			if (debugging.IsDebugging)
				debugging.Report(false, "Read position: {0}", point.DebugString());

			TEStorageComponent.SearchAndRefreshNetwork(point);
		}

		private static void ReciveClientStorageOperation(BinaryReader reader, int sender)
		{
			Point16 position = new(reader.ReadInt16(), reader.ReadInt16());
			TEStorageHeart.Operation op = (TEStorageHeart.Operation)reader.ReadByte();

			bool hasContext = reader.ReadSecurityAccess(out var context);

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageHeartClientOperations);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read position: {0}", position.DebugString())
					.Report(false, "Read operation: {0}", op)
					.Report(false, "Read accessing player: {0}", context.Player);
			}

			if (TryGetEntityFromLocation(MessageType.ClinetStorageOperation, position, out TEStorageHeart heart)) {
				// NOTE: If not the server, the data will be read but not enqueued
				heart.QClientOperation(reader, op, sender);
			}

			if (hasContext)
				context.Dispose();
		}

		private static void ReciveServerStorageResult(BinaryReader reader)
		{
			TEStorageHeart.Operation op = (TEStorageHeart.Operation)reader.ReadByte();

			Point16 position = reader.ReadPoint16();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageHeartClientOperations);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read position: {0}", position.DebugString())
					.Report(false, "Read operation: {0}", op);
			}

			if (!TryGetEntityFromLocation(MessageType.ServerStorageResult, position, out TEStorageHeart heart))
				return;

			if (op == TEStorageHeart.Operation.Withdraw || op == TEStorageHeart.Operation.WithdrawToInventory || op == TEStorageHeart.Operation.Deposit)
			{
				Item item  = ItemIO.Receive(reader, true, true);

				if (debugging.IsDebugging)
					debugging.Report(false, "Read item: {0}", item.IdentifierAndStack());
				
				if (Main.netMode == NetmodeID.MultiplayerClient)
					StoragePlayer.GetItem(new EntitySource_TileEntity(heart), item, op != TEStorageHeart.Operation.WithdrawToInventory);
			}
			else if (op == TEStorageHeart.Operation.DepositAll)
			{
				int count = reader.ReadInt32();

				if (debugging.IsDebugging)
					debugging.Report(false, "Read item count: {0}", count);

				for (int k = 0; k < count; k++)
				{
					Item item  = ItemIO.Receive(reader, true, true);

					if (debugging.IsDebugging)
						debugging.Report(false, "Read item: {0}", item.IdentifierAndStack());

					if (Main.netMode == NetmodeID.MultiplayerClient)
						StoragePlayer.GetItem(new EntitySource_TileEntity(heart), item, false);
				}
			}
			else if (op == TEStorageHeart.Operation.WithdrawAllAndDestroy)
			{
				int type = reader.ReadInt32();

				if (debugging.IsDebugging)
					debugging.Report(false, "Read item type: {0}", type);

				if (Main.netMode == NetmodeID.MultiplayerClient)
					heart.WithdrawManyAndDestroy(type, out _, net: true);
			}
			else if (op == TEStorageHeart.Operation.DeleteUnloadedGlobalItemData)
			{
				if (Main.netMode == NetmodeID.MultiplayerClient)
					heart.DestroyUnloadedGlobalItemData(out _, net: true);
			}
			else if (op == TEStorageHeart.Operation.WithdrawThenTryModuleInventory || op == TEStorageHeart.Operation.WithdrawToInventoryThenTryModuleInventory)
			{
				Item item  = ItemIO.Receive(reader, true, true);

				if (debugging.IsDebugging)
					debugging.Report(false, "Read item: {0}", item.IdentifierAndStack());

				if (item.IsAir)
					item = CraftingGUI.TryToWithdrawFromModuleItems(heart, item, wasAlreadyCloned: true);

				if (Main.netMode == NetmodeID.MultiplayerClient)
					StoragePlayer.GetItem(new EntitySource_TileEntity(heart), item, op != TEStorageHeart.Operation.WithdrawToInventoryThenTryModuleInventory);
			}

			heart.netcodeUpdate = true;
			heart.netDesync = 0;
		}

		public static void SendRefreshNetworkItems(Point16 position, bool ignoreSpecificRefreshes = false, IEnumerable<int> typesToRefresh = null)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.RefreshNetworkItems);
				packet.Write(position.X);
				packet.Write(position.Y);

				int numTypes = 0;

				if (typesToRefresh is null || !typesToRefresh.Any())
					packet.Write((ushort)0);
				else {
					List<int> types = typesToRefresh.ToList();
					packet.Write((ushort)types.Count);

					foreach (int id in types)
						packet.Write(id);

					numTypes = types.Count;
				}

				packet.Write(ignoreSpecificRefreshes);

				packet.Send();

				using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.RefreshingUI);

				if (debugging.IsDebugging) {
					debugging
						.Report(true, "Sent packet {0} to all clients", MessageType.RefreshNetworkItems)
						.Indent()
						.Report(false, "Heart position: {0}", position.DebugString())
						.Report(false, "Force full zone refresh: {0}", ignoreSpecificRefreshes);

					if (numTypes > 0)
						debugging.Report(false, "Target item count: {0}", numTypes);
				}
			}
		}

		private static void ReceiveRefreshNetworkItems(BinaryReader reader)
		{
			Point16 position = new(reader.ReadInt16(), reader.ReadInt16());
			int count = reader.ReadUInt16();

			List<int> types = new();
			for (int i = 0; i < count; i++)
				types.Add(reader.ReadInt32());

			bool ignoreSpecificRefreshes = reader.ReadBoolean();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.RefreshingUI);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read position: {0}", position.DebugString())
					.Report(false, "Read forced full refresh: {0}", ignoreSpecificRefreshes);

				if (count > 0)
					debugging.Report(false, "Read item count: {0}", count);
			}

			if (Main.netMode == NetmodeID.Server)
				return;

			if (!TryGetEntityFromLocation(MessageType.RefreshNetworkItems, position, out TEStorageHeart heart))
				return;

			if (StoragePlayer.IsClientViewingHeart(heart)) {
				MagicUI.IgnoreSpecificZoneRefreshing = ignoreSpecificRefreshes;
				MagicUI.SetNextCollectionsToRefresh(types);

				heart.netcodeUpdate = false;
				heart.netDesync = 0;
			}
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

				using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageUnitActiveState);

				if (debugging.IsDebugging) {
					debugging
						.Report(true, "Sent packet {0} to the server", MessageType.ClientSendDeactivate)
						.Indent()
						.Report(false, "Position: {0}", position.DebugString())
						.Report(false, "Inactive: {0}", inActive);
				}
			}
		}

		private static void ReceiveClientDeactivate(BinaryReader reader, int sender)
		{
			Point16 position = new(reader.ReadInt16(), reader.ReadInt16());
			bool inActive = reader.ReadBoolean();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageUnitActiveState);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read position: {0}", position.DebugString())
					.Report(false, "Read activity state: {0}", !inActive);
			}

			if (Main.netMode == NetmodeID.Server)
			{
				if (TryGetEntityFromLocation(MessageType.ClientSendDeactivate, position, out TEStorageUnit storageUnit))
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
		}

		public static void ClientSendTEUpdate(Point16 position)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient && position.ResolveToTileEntity() is TileEntity entity)
			{
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.ClientSendTEUpdate);
				packet.Write(position.X);
				packet.Write(position.Y);
				TileEntity.Write(packet, entity, true);
				packet.Send();

				using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.TileEntityUpdatesNetcode);

				if (debugging.IsDebugging) {
					debugging
						.Report(true, "Sent packet {0} to the server", MessageType.ClientSendTEUpdate)
						.Indent()
						.Report(false, "Position: {0}", position.DebugString())
						.Report(false, "Entity: {0}", entity.GetType().FullName);
				}
			}
		}

		private static void ReceiveClientSendTEUpdate(BinaryReader reader, int sender)
		{
			Point16 position = new(reader.ReadInt16(), reader.ReadInt16());
			TileEntity ent = TileEntity.Read(reader, true);

			using var debuggingIncoming = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.TileEntityUpdatesNetcode);

			if (debuggingIncoming.IsDebugging) {
				debuggingIncoming
					.Report(false, "Read position: {0}", position.DebugString())
					.Report(false, "Read entity type: {0}", ent.GetType().FullName);
			}

			if (Main.netMode == NetmodeID.Server)
			{
				// NOTE: unlike other packets, this packet can force the existence of the entity
				ent.Position = position;
				TileEntity.ByID[ent.ID] = ent;
				TileEntity.ByPosition[position] = ent;
				if (ent is TEStorageUnit storageUnit)
				{
					TEStorageHeart heart = storageUnit.GetHeart();
					heart?.ResetCompactStage();
				}

				NetMessage.SendData(MessageID.TileEntitySharing, -1, sender, null, ent.ID, ent.Position.X, ent.Position.Y);

				using var debuggingOutgoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.TileEntityUpdatesNetcode);

				if (debuggingOutgoing.IsDebugging) {
					debuggingOutgoing
						.Report(true, "Sent vanilla ID packet TileEntitySharing to all clients")
						.Indent()
						.Report(false, "ID: {0}", ent.ID)
						.Report(false, "Position: {0}", ent.Position.DebugString());
				}
			}
		}

		private static void ReceiveClientStationOperation(BinaryReader reader, int sender)
		{
			Point16 position = new(reader.ReadInt16(), reader.ReadInt16());
			TECraftingAccess.Operation op = (TECraftingAccess.Operation)reader.ReadByte();

			using var debugging = DebugMessage.ChainIf(
				DebugControls.Combine()
					.Set(Main.netMode == NetmodeID.Server)
					.AndAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.CraftingStationSlots)
			);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read position: {0}", position.DebugString())
					.Report(false, "Read operation: {0}", op);
			}

			if (TryGetEntityFromLocation(MessageType.ClientStationOperation, position, out TECraftingAccess craftingAccess))
				craftingAccess.QClientOperation(reader, op, sender);
		}

		private static void ReceiveServerStationResult(BinaryReader reader)
		{
			TECraftingAccess.Operation op = (TECraftingAccess.Operation)reader.ReadByte();
			Item item = ItemIO.Receive(reader, true, true);

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.CraftingStationSlots);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read operation: {0}", op)
					.Report(false, "Read item: {0}", item.IdentifierAndStack());
			}

			if (op == TECraftingAccess.Operation.Withdraw || op == TECraftingAccess.Operation.WithdrawToInventory)
			{
				var heart = StoragePlayer.LocalPlayer.GetStorageHeart();

				if (Main.netMode == NetmodeID.MultiplayerClient)
				{
					StoragePlayer.GetItem(new EntitySource_TileEntity(heart), item, op == TECraftingAccess.Operation.Withdraw);
					
					TECraftingAccess.UpdateRecipesFromStationAction(item);
				}
			}
			else // deposit operation
			{
				int oldType = reader.ReadUInt16();

				if (Main.netMode == NetmodeID.MultiplayerClient)
				{
					Main.mouseItem = item;
					TECraftingAccess.UpdateRecipesFromStationAction(new Item(oldType));
				}
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

				using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageHeartResetCompactStage);

				if (debugging.IsDebugging) {
					debugging
						.Report(true, "Sent packet {0} to the server", MessageType.ResetCompactStage)
						.Indent()
						.Report(false, "Heart position: {0}", heart.DebugString());
				}
			}
		}

		private static void ReceiveResetCompactStage(BinaryReader reader, int sender)
		{
			Point16 position = new(reader.ReadInt16(), reader.ReadInt16());

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageHeartResetCompactStage);

			if (debugging.IsDebugging)
				debugging.Report(false, "Read position: {0}", position.DebugString());

			if (Main.netMode == NetmodeID.Server)
			{
				if (TryGetEntityFromLocation(MessageType.ResetCompactStage, position, out TEStorageHeart heart))
					heart.ResetCompactStage();
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

				using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.CraftingRequests);

				if (debugging.IsDebugging) {
					debugging
						.Report(true, "Sent packet {0} to the server", MessageType.CraftRequest)
						.Indent()
						.Report(false, "Heart position: {0}", heart.DebugString())
						.Report(false, "Withdrawing {0} item stacks from storage", toWithdraw.Count)
						.Report(false, "Crafting {0} item stacks", results.Count);
				}
			}
		}

		private static void ReceiveCraftRequest(BinaryReader reader, int sender)
		{
			Point16 position = new(reader.ReadInt16(), reader.ReadInt16());
			int withdrawCount = reader.ReadInt32();

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

			using var debuggingIncoming = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.CraftingRequests);

			if (debuggingIncoming.IsDebugging) {
				debuggingIncoming
					.Report(false, "Read position: {0}", position.DebugString())
					.Report(false, "Withdrawing {0} item stacks from storage", withdrawCount)
					.Report(false, "Crafting {0} item stacks", resultsCount);
			}

			if (!TryGetEntityFromLocation(MessageType.CraftRequest, position, out TEStorageHeart heart))
				return;

			List<Item> items;

			using (var debuggingWork = DebugMessage.CreateIf(DebugControls.Names.CraftingRequests)) {
				if (debuggingWork.IsDebugging)
					debuggingWork.Report(false, "Handling storage inventory changes and sending excess items...");

				using (SecuritySystem.CreateAccessContext(sender))
					items = CraftingGUI.HandleCraftWithdrawAndDeposit(heart, toWithdraw, results);
			}

			if (items.Count > 0)
			{
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.CraftResult);
				packet.Write(items.Count);
				foreach (Item item in items)
					ItemIO.Send(item, packet, true, true);
				packet.Send(sender);

				using var debuggingOutgoing = DebugMessage.ChainIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.CraftingRequests);

				if (debuggingOutgoing.IsDebugging) {
					debuggingOutgoing
						.Report(true, "Sent packet {0} to client {1}", MessageType.CraftResult, sender)
						.Indent()
						.Report(false, "Excess item count: {0}", items.Count)
						.Unindent();
				}

				AuditSystem.ReportCraftRequest(sender, heart, [.. results], [.. toWithdraw]);
			}

			SendRefreshNetworkItems(position, false, typesToUpdate);
		}

		private static void ReceiveCraftResult(BinaryReader reader)
		{
			Player player = Main.LocalPlayer;
			int count = reader.ReadInt32();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.CraftingRequests);

			if (debugging.IsDebugging)
				debugging.Report(false, "Read excess item count: {0}", count);

			for (int k = 0; k < count; k++)
			{
				Item item  = ItemIO.Receive(reader, true, true);
				var  heart = StoragePlayer.LocalPlayer.GetStorageHeart();

				player.QuickSpawnItem(new EntitySource_TileEntity(heart), item, item.stack);
			}
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

				using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.TileSectionNetcode);

				if (debugging.IsDebugging) {
					debugging
						.Report(true, "Sent packet {0} to the server", MessageType.SectionRequest)
						.Indent()
						.Report(false, "Tile coordinates: {0}", coords.DebugString());
				}
			}
		}

		private static void ReceiveClientRequestSection(BinaryReader reader, int sender)
		{
			Point16 coords = new(reader.ReadInt16(), reader.ReadInt16());

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.TileSectionNetcode);

			if (debugging.IsDebugging)
				debugging.Report(false, "Read tile coordinates: {0}", coords.DebugString());

			if (Main.netMode == NetmodeID.Server)
			{
				RemoteClient.CheckSection(sender, coords.ToWorldCoordinates());
			}
		}

		public static void ClientRequestForceCraftingGUIRefresh() {
			if (Main.netMode == NetmodeID.MultiplayerClient && StoragePlayer.LocalPlayer.GetStorageHeart() is TEStorageHeart heart) {
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.ForceCraftingGUIRefresh);

				packet.Write(heart.Position.X);
				packet.Write(heart.Position.Y);

				packet.Send();

				using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.RefreshingUI);

				if (debugging.IsDebugging) {
					debugging
						.Report(true, "Sent packet {0} to the server", MessageType.ForceCraftingGUIRefresh)
						.Indent()
						.Report(false, "Heart position: {0}", heart.Position.DebugString());
				}
			}
		}

		private static void ReceiveClientForceCraftingGUIRefresh(BinaryReader reader, int sender) {
			Point16 storage = new(reader.ReadInt16(), reader.ReadInt16());

			using var debuggingIncoming = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.RefreshingUI);

			if (debuggingIncoming.IsDebugging)
				DebugMessage.Report(false, "Read position: {0}", storage.DebugString());

			if (Main.netMode == NetmodeID.Server) {
				//Forward the packet
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.ForceCraftingGUIRefresh);

				packet.Write(storage.X);
				packet.Write(storage.Y);

				packet.Send(ignoreClient: sender);

				using var debuggingOutgoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.RefreshingUI);

				if (debuggingOutgoing.IsDebugging) {
					debuggingOutgoing
						.Report(true, "Forwarded packet {0} from client {1} to all other clients", MessageType.ForceCraftingGUIRefresh, sender)
						.Indent()
						.Report(false, "Heart position: {0}", storage.DebugString());
				}
			} else if (Main.netMode == NetmodeID.MultiplayerClient) {
				if (StoragePlayer.IsClientViewingHeart(storage) && StoragePlayer.IsStorageCrafting()) {
					MagicUI.RequestFullRefresh();
					MagicUI.IgnoreSpecificZoneRefreshing = true;
				}
			}
		}

		public static void ClientRequestItemTransfer(TEStorageUnit destination, TEStorageUnit source) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.TransferItems);
				packet.Write(destination.Position);
				packet.Write(source.Position);
				packet.Send();

				using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageUnitItemTransfer);

				if (debugging.IsDebugging) {
					debugging
						.Report(true, "Sent packet {0} to the server", MessageType.TransferItems)
						.Indent()
						.Report(false, "Source unit position: {0}", source.Position.DebugString())
						.Report(false, "Destination unit position: {0}", destination.Position.DebugString());
				}
			}
		}

		private static void ReceiveClientRequestItemTransfer(BinaryReader reader, int sender) {
			Point16 destination = reader.ReadPoint16();
			Point16 source = reader.ReadPoint16();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageUnitItemTransfer);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read destination unit position: {0}", destination.DebugString())
					.Report(false, "Read source unit position: {0}", source.DebugString());
			}

			if (Main.netMode != NetmodeID.Server)
				return;

			if (!TryGetEntityFromLocation(MessageType.TransferItems, destination, out TEStorageUnit unitDestination)) {
				if (debugging.IsDebugging) {
					debugging
						.Indent()
						.Report(false, "Could not evaluate destination unit");
				}

				return;
			}

			if (!TryGetEntityFromLocation(MessageType.TransferItems, source, out TEStorageUnit unitSource)) {
				if (debugging.IsDebugging) {
					debugging
						.Indent()
						.Report(false, "Could not evaluate source unit");
				}

				return;
			}

			AttemptItemTransferAndSendResult(unitDestination, unitSource, out _, true);
		}

		internal static bool AttemptItemTransferAndSendResult(TEStorageUnit destination, TEStorageUnit source, out List<Item> transferredItems, bool netQueue = true) {
			transferredItems = null;

			if (Main.netMode != NetmodeID.Server)
				return false;

			using var debugging = DebugMessage.CreateIf(DebugControls.Names.StorageUnitItemTransfer);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Attempting transfer of items between Storage Units")
					.Indent()
					.Report(false, "Destination unit position: {0}", destination.Position.DebugString())
					.Report(false, "Source unit position: {0}", source.Position.DebugString())
					.Unindent();
			}

			TEStorageUnit.AttemptItemTransfer(destination, source, out transferredItems);

			if (transferredItems.Count == 0) {
				//Nothing to do
				if (debugging.IsDebugging)
					debugging.Report(false, "No items were transferred");

				return false;
			}

			if (debugging.IsDebugging)
				debugging.Report(false, "{0} items were transferred", transferredItems.Count);

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
			packet.WriteSecurityAccess();
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.CompactCoins);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.RequestCoinCompact)
					.Indent()
					.Report(false, "Heart position: {0}", heart.DebugString());
			}
		}

		private static void ReceiveCoinCompactRequest(BinaryReader reader, int sender) {
			Point16 position = reader.ReadPoint16();
			bool hasContext = reader.ReadSecurityAccess(out var context);

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.CompactCoins);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read position: {0}", position.DebugString())
					.Report(false, "Read accessing player: {0}", context.Player);
			}

			if (Main.netMode == NetmodeID.Server) {
				if (TryGetEntityFromLocation(MessageType.RequestCoinCompact, position, out TEStorageHeart heart)) {
					heart.CompactCoins();
					AuditSystem.ReportControlCoinCompacting(sender, heart);
				}
			}

			if (hasContext)
				context.Dispose();
		}

		public static bool RequestDuplicateSelling(Point16 heart) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return true;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.MassDuplicateSellRequest);
			packet.Write(heart);

			if (!SellModeMetadata.NetSend(packet, consumedPacketSpace: sizeof(byte) + sizeof(short) * 2)) {
				// Packet was too large to send
				Main.NewTextMultiline(Language.GetTextValue("Mods.MagicStorage.StorageGUI.SellDuplicatesMenu.SoldItemsReport.PacketTooLarge"), c: Color.Red);
				return false;
			}
			
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SellDuplicatesMenu);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.MassDuplicateSellRequest)
					.Indent()
					.Report(false, "Heart position: {0}", heart.DebugString());
			}

			return true;
		}

		private static void ReceiveDuplicateSellingRequest(BinaryReader reader, int sender) {
			Point16 position = reader.ReadPoint16();

			using var debuggingIncoming = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SellDuplicatesMenu);

			if (debuggingIncoming.IsDebugging)
				debuggingIncoming.Report(false, "Read position: {0}", position.DebugString());

			SellModeMetadata.NetReceive(reader);

			using var debuggingWork = DebugMessage.CreateIf(DebugControls.Names.SellDuplicatesMenu);

			if (Main.netMode == NetmodeID.Server) {
				if (TryGetEntityFromLocation(MessageType.MassDuplicateSellRequest, position, out TEStorageHeart heart)) {
					int totalItemCount = SellModeMetadata.Count;
					SellModeMetadata.HandleSell(heart, out int soldItemCount, out var sellValue, Main.player[sender]);

					if (debuggingWork.IsDebugging) {
						debuggingWork
							.Report(false, "Results:")
							.Indent()
							.Report(false, "{0} / {1} items were sold", soldItemCount, totalItemCount)
							.Report(false, "Sell value: {0} copper coins", sellValue.TotalValue)
							.Unindent();
					}

					ModPacket packet = MagicStorageMod.Instance.GetPacket();
					packet.Write((byte)MessageType.MassDuplicateSellResult);
					packet.Write((short)sender);
					packet.Write(position);
					packet.Write7BitEncodedInt64(sellValue.TotalValue);
					packet.Write7BitEncodedInt(soldItemCount);
					packet.Write7BitEncodedInt(totalItemCount);

					packet.Send();

					AuditSystem.ReportMassItemSell(sender, heart, soldItemCount, sellValue.TotalValue);

					using var debuggingOutGoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SellDuplicatesMenu);

					if (debuggingOutGoing.IsDebugging)
						debuggingOutGoing.Report(true, "Sent packet {0} to client {1}", MessageType.MassDuplicateSellResult, sender);
				} else {
					// Invalid request
					SellModeMetadata.Clear();

					if (debuggingWork.IsDebugging)
						debuggingWork.Report(false, "Sell request was invalid, clearing metadata without processing");
				}
			} else if (Main.netMode == NetmodeID.MultiplayerClient) {
				SellModeMetadata.Clear();

				if (debuggingWork.IsDebugging)
					debuggingWork.Report(false, "Received sell request on client, clearing metadata without processing");
			}
		}

		private static void ClientReceiveDuplicateSellingResult(BinaryReader reader) {
			short sender = reader.ReadInt16();
			Point16 heart = reader.ReadPoint16();
			long coppersEarned = reader.Read7BitEncodedInt64();

			int sold = reader.Read7BitEncodedInt();
			int totalItemsBeforeSell = reader.Read7BitEncodedInt();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SellDuplicatesMenu);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read player: {0}", sender)
					.Report(false, "Read position: {0}", heart.DebugString())
					.Report(false, "Read coppers value: {0}", coppersEarned)
					.Report(false, "Read sold item count: {0}", sold)
					.Report(false, "Read total item count: {0}", totalItemsBeforeSell);
			}

			if (Main.netMode != NetmodeID.MultiplayerClient) {
				//Read the data, but do nothing with it
				return;
			}

			if (!TryGetEntityFromLocation(MessageType.MassDuplicateSellResult, heart, out TEStorageHeart _))
				return;

			if (sender == Main.myPlayer)
				SellModeMetadata.ClientReportSell(sold, totalItemsBeforeSell, new SellModeMetadata.Coins(coppersEarned));
		}

		public static void RequestStorageUnitStyle(Point16 unit) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.RequestStorageUnitStyle);
				packet.Write(unit);
				packet.Send();

				using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageUnitFrame);

				if (debugging.IsDebugging) {
					debugging
						.Report(true, "Sent packet {0} to the server", MessageType.RequestStorageUnitStyle)
						.Indent()
						.Report(false, "Unit position: {0}", unit.DebugString())
						.Unindent();
				}
			}
		}

		private static void ReceiveStorageUnitStyle(BinaryReader reader, int sender) {
			Point16 unit = reader.ReadPoint16();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageUnitFrame);

			if (debugging.IsDebugging)
				debugging.Report(false, "Read position: {0}", unit.DebugString());

			if (Main.netMode != NetmodeID.Server)
				return;

			//Safeguard:  Ensure that the map section exists before sending data
			RemoteClient.CheckSection(sender, unit.ToWorldCoordinates());

			if (TryGetEntityFromLocation(MessageType.RequestStorageUnitStyle, unit, out TEStorageUnit storageUnit))
				storageUnit.UpdateTileFrameWithNetSend();
		}

		private static void ClientReceiveQuickStackToNearbyStorageResult(BinaryReader reader) {
		//	bool playSound = reader.ReadBoolean();
			int origType = reader.ReadInt32();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.QuickStacking);

			if (debugging.IsDebugging) {
				debugging
				//	.Report(false, "Read play sound: {0}", playSound)
					.Report(false, "Read item type: {0}", origType);
			}

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			// NOTE: 1.4.4 does not play a sound
			/*
			if (playSound)
				SoundEngine.PlaySound(SoundID.Grab);
			*/

			if (origType > 0 && MagicUI.uiInterface.CurrentState is not null)
				MagicUI.SetNextCollectionsToRefresh(origType);
		}

		public static void SendGolemTextUpdate() {
			if (Main.netMode != NetmodeID.Server)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.GolemHelpTextUpdate);
			StorageWorld.NetSendHelpTips(packet);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.AutomatonHelpTipUpdate);

			if (debugging.IsDebugging)
				debugging.Report(true, "Sent packet {0} to all clients", MessageType.GolemHelpTextUpdate);
		}

		private static void ClientReceiveGolemTextUpdate(BinaryReader reader) {
			StorageWorld.NetReceiveHelpTips(reader);

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Golem.ReportNewTipUnlocked();
		}

		public static void ClientRequestServerOperator() {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ClientRequestServerOp);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.CommandGrantAdministrator);

			if (debugging.IsDebugging)
				debugging.Report(true, "Sent packet {0} to the server", MessageType.ClientRequestServerOp);
		}

		private static void ServerReceiveOperatorRequest(int sender) {
			if (Main.netMode != NetmodeID.Server)
				return;

			Netcode.AttemptKeyGeneration();

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ServerOpResponse);
			packet.Send(toClient: sender);

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.CommandGrantAdministrator);

			if (debugging.IsDebugging)
				debugging.Report(true, "Sent packet {0} to client {1}", MessageType.ServerOpResponse, sender);
		}

		private static void ClientReceiveOperatorReponse() {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Main.NewText(MagicStorageMod.Instance.GetLocalization("ServerOperator.CommandInfo.ClientKeyText"), Color.Yellow);

			Netcode.RequestingOperatorKey = true;
		}

		public static void ClientSendOperatorKey(string key) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Netcode.RequestingOperatorKey = false;

			using var debugging = DebugMessage.CreateIf(DebugControls.Names.CommandGrantAdministrator);

			if (debugging.IsDebugging)
				debugging.Report(true, "Chat interceptions have been removed.");

			if (!Netcode.IsKeyValidForConfirmationMessage(key)) {
				// Bail immediately since the key couldn't be valid in the first place
				Netcode.ClientPrintKeyReponse(valid: false);
				return;
			}

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ClientRequestServerOpConfirmation);
			byte[] bytes = StringScrambling.Scramble(key);
			packet.Write((byte)bytes.Length);
			packet.Write(bytes);
			packet.Send();

			using var debuggingOutgoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.CommandGrantAdministrator);

			if (debuggingOutgoing.IsDebugging)
				debuggingOutgoing.Report(true, "Sent packet {0} to the server", MessageType.ClientRequestServerOpConfirmation);
		}

		private static void ServerReceiveOperatorKeyFromClient(BinaryReader reader, int sender) {
			byte count = reader.ReadByte();
			byte[] bytes = reader.ReadBytes(count);

			if (Main.netMode != NetmodeID.Server)
				return;

			string key = StringScrambling.Unscramble(bytes);

			bool valid = key == Netcode.GetOrGenerateOperatorKey();

			using var debuggingIncoming = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.CommandGrantAdministrator);

			if (debuggingIncoming.IsDebugging) {
				debuggingIncoming
					.Report(false, "Read key: {0}", key)
					.Report(false, "Valid key? {0}", valid);
			}

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ServerOpConfirmationResult);
			packet.Write(valid);
			packet.Send(toClient: sender);

			if (valid)
				AuditSystem.ReportAdministratorStatusAssignment(sender);

			using var debuggingOutgoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.CommandGrantAdministrator);

			if (debuggingOutgoing.IsDebugging) {
				debuggingOutgoing
					.Report(true, "Sent packet {0} to client {1}", MessageType.ServerOpConfirmationResult, sender)
					.Indent()
					.Report(false, "Sent key: {0}", key)
					.Report(false, "Key was {0}", valid ? "valid" : "invalid");
			}
		}

		private static void ClientReceiveOperatorConformationResult(BinaryReader reader) {
			bool valid = reader.ReadBoolean();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.CommandGrantAdministrator);

			if (debugging.IsDebugging)
				debugging.Report(false, "Valid key? {0}", valid);

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Netcode.ClientPrintKeyReponse(valid);

			if (valid) {
				var mp = Main.LocalPlayer.GetModPlayer<OperatorPlayer>();

				mp.manualOp = mp.hasOp = true;

				ClientSendPlayerHasOp(Main.myPlayer);  // NOTE: Administrators will automatically request the full security network list
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
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.OperatorStatus);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.PlayerHasServerOp)
					.Indent()
					.Report(false, "Player: {0}", plr)
					.Report(false, "Operator status: {0}", mp.hasOp)
					.Report(false, "Administrator status: {0}", mp.IsAdministrator);
			}
		}

		private static void ReceivePlayerHasOperator(BinaryReader reader) {
			byte plr = reader.ReadByte();
			BitsByte opFlags = reader.ReadByte();

			var mp = Main.player[plr].GetModPlayer<OperatorPlayer>();

			bool wasOperator = mp.hasOp, wasAdministrator = mp.IsAdministrator;

			opFlags.Retrieve(ref mp.hasOp, ref mp.manualOp);

			using var debuggingIncoming = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.OperatorStatus);

			if (debuggingIncoming.IsDebugging) {
				debuggingIncoming
					.Report(false, "Read player: {0}", plr)
					.Report(false, "Read Operator status: {0}", mp.hasOp)
					.Report(false, "Read Administrator status: {0}", mp.IsAdministrator);
			}

			if (Main.netMode == NetmodeID.MultiplayerClient && plr == Main.myPlayer && mp.IsAdministrator)  // Force a sync of the network information
				RequestAccessibleNetworksByDefault();

			if (Main.netMode != NetmodeID.Server)
				return;

			//Forward the result
			ModPacket packet = ServerPreparePlayerHasOperatorPacket(plr, mp);
			packet.Send(ignoreClient: plr);

			using var debuggingOutgoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.OperatorStatus);

			if (debuggingOutgoing.IsDebugging)
				debuggingOutgoing.Report(true, "Forwarded packet {0} from client {1} to all other clients", MessageType.PlayerHasServerOp, plr);

			if (mp.IsAdministrator != wasAdministrator) {
				if (mp.IsAdministrator)
					AuditSystem.ReportAdministratorStatusAssignment(plr);
			} else if (mp.hasOp != wasOperator) {
				if (mp.hasOp)
					AuditSystem.ReportOperatorStatusAssignment(plr);
				else
					AuditSystem.ReportOperatorStatusRemoval(plr);
			}
		}

		private static ModPacket ServerPreparePlayerHasOperatorPacket(int plr, OperatorPlayer mp) {
			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.PlayerHasServerOp);
			packet.Write(plr);

			BitsByte bb = new(mp.hasOp, mp.manualOp);
			packet.Write(bb);
			
			return packet;
		}

		[Obsolete("This method has been renamed to " + nameof(SendNetworkConnectionsUpdateOnPlacement), error: true)]
		public static void SendComponentPlacement(Point16 position) => SendNetworkConnectionsUpdateOnPlacement(position);

		public static void SendNetworkConnectionsUpdateOnPlacement(Point16 position)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ComponentPlacement);
			packet.Write(position);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageComponentPlacement);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.ComponentPlacement)
					.Indent()
					.Report(false, "Component position: {0}", position.DebugString());
			}
		}

		public static void ServerReceiveComponentPlacement(BinaryReader reader, int sender) {
			Point16 position = reader.ReadPoint16();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageComponentPlacement);

			if (debugging.IsDebugging)
				debugging.Report(false, "Read position: {0}", position.DebugString());

			TileNetworkScanner.SmartlyConnectAdjacentNetworks(position);
		}

		[Obsolete("This method has been renamed to " + nameof(SendNetworkConnectionsUpdateOnDestruction), error: true)]
		public static void SendComponentDestruction(Point16 position) => SendNetworkConnectionsUpdateOnDestruction(position, TileNetworkScanner.GetLocalNeighbors2x2());

		public static void SendNetworkConnectionsUpdateOnDestruction(Point16 position, IEnumerable<Point16> initialLocalNeighbors) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			if (position.ResolveToTileEntity() is not TEStorageComponent component)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ComponentDestruction);
			packet.Write(component.Position);

			List<Point16> neighbors = [.. initialLocalNeighbors];

			packet.Write((byte)neighbors.Count);
			foreach (Point16 neighbor in neighbors)
				packet.Write(neighbor);

			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageComponentDestruction);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.ComponentDestruction)
					.Indent()
					.Report(false, "Component position: {0}", position.DebugString());
			}
		}

		private static void ServerReceiveComponentDestruction(BinaryReader reader, int sender) {
			Point16 position = reader.ReadPoint16();

			int neighborCount = reader.ReadByte();
			List<Point16> initialLocalNeighbors = new(neighborCount);
			for (int i = 0; i < neighborCount; i++)
				initialLocalNeighbors.Add(reader.ReadPoint16());

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageComponentDestruction);

			if (debugging.IsDebugging)
				debugging.Report(false, "Read position: {0}", position.DebugString());

			TileNetworkScanner.SmartlyDisconnectComponents(position, initialLocalNeighbors);
		}

		public static void ClientInformStorageHeartUsage(TEStorageHeart heart) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var msg = heart.clientUsingHeart[Main.myPlayer] ? MessageType.ClientLockStorageHeart : MessageType.ClientUnlockStorageHeart;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)msg);
			packet.Write((byte)Main.myPlayer);
			packet.Write(heart.Position);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageHeartUsage);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", msg)
					.Indent()
					.Report(false, "Heart position: {0}", heart.Position.DebugString());
			}
		}

		private static void ReceiveStorageHeartUsage(BinaryReader reader, int sender, bool inUse) {
			byte player = reader.ReadByte();
			Point16 position = reader.ReadPoint16();

			var msg = inUse ? MessageType.ClientLockStorageHeart : MessageType.ClientUnlockStorageHeart;

			using var debuggingIncoming = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageHeartUsage);

			if (debuggingIncoming.IsDebugging) {
				debuggingIncoming
					.Report(false, "Read player: {0}", player)
					.Report(false, "Read position: {0}", position.DebugString());
			}

			if (!TryGetEntityFromLocation(msg, position, out TEStorageHeart heart))
				return;

			using (var debuggingWork = DebugMessage.ChainIf(DebugControls.Names.StorageHeartUsage)) {
				if (debuggingWork.IsDebugging)
					debuggingWork.Report(false, "Usage state for Storage Heart at {0} has been update to {1}", position.DebugString(), inUse);

				heart.clientUsingHeart[player] = inUse;
			}

			if (Main.netMode == NetmodeID.Server) {
				// Forward to other clients
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)msg);
				packet.Write(player);
				packet.Write(position);
				packet.Send(ignoreClient: sender);

				using var debuggingOutgoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageHeartUsage);

				if (debuggingOutgoing.IsDebugging) {
					debuggingOutgoing
						.Report(true, "Forwarded packet {0} from client {1} to all other clients", msg, sender)
						.Indent()
						.Report(false, "Interacting player: {0}", player)
						.Report(false, "Heart position: {0}", position.DebugString());
				}
			}
		}

		public static void ClientRequestExactItemDeletion(TEStorageHeart heart, Item item) {
			if (Main.netMode != NetmodeID.MultiplayerClient || !Main.LocalPlayer.GetModPlayer<OperatorPlayer>().hasOp)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.DeleteSpecificItem);
			packet.Write(heart.Position);
			ReadOnlySpan<byte> data;
			using (ObjectSwitch.Create(ref item.stack, 1))
				data = Utility.ToByteSpanNoCompression(item);
			packet.Write7BitEncodedInt(data.Length);
			packet.Write(data);
			packet.Write(item.stack);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageHeartItemDeletion);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.DeleteSpecificItem)
					.Indent()
					.Report(false, "Heart position: {0}", heart.Position.DebugString())
					.Report(false, "Item: {0}", item.IdentifierAndStack())
					.Report(false, "Encoded data length: {0}", data.Length);
			}
		}

		private static void ServerReceiveExactItemDeletionRequest(BinaryReader reader, int sender) {
			Point16 point = reader.ReadPoint16();
			int dataLength = reader.Read7BitEncodedInt();
			ReadOnlySpan<byte> item = reader.ReadBytes(dataLength);
			int stack = reader.ReadInt32();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageHeartItemDeletion);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read position: {0}", point.DebugString())
					.Report(false, "Read item data length: {0}", dataLength)
					.Report(false, "Read item stack: {0}", stack);
			}

			if (Main.netMode != NetmodeID.Server)
				return;

			if (!TryGetEntityFromLocation(MessageType.DeleteSpecificItem, point, out TEStorageHeart heart))
				return;

			int toRemove = stack;
			if (heart.TryDeleteExactItem(item, out var netItem, ref toRemove))
				AuditSystem.ReportItemDeletion(sender, heart, new ReducedItem(netItem.Type, stack - toRemove));
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

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.ShimmerRequestNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.RequestShimmerItemInStorage)
					.Indent()
					.Report(false, "Item type: {0}", itemType)
					.Report(false, "Item stack to shimmer: {0}", toShimmer);
			}
		}

		private static void ServerReceiveItemShimmeringRequest(BinaryReader reader, int sender) {
			int itemType = reader.ReadInt32();
			int toShimmer = reader.ReadInt32();

			var storage = StorageIntermediary.Receive(reader);
			storage.IgnoreContentChanges = true;

			var results = ShimmerMetrics.ReceiveShimmerResults(reader);

			using var debuggingIncoming = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.ShimmerRequestNetcode);

			if (debuggingIncoming.IsDebugging) {
				debuggingIncoming
					.Report(false, "Read item type: {0}", itemType)
					.Report(false, "Read item stack to shimmer: {0}", toShimmer)
					.Report(false, "Shimmering actions count: {0}", results.Count);
			}

			if (Main.netMode != NetmodeID.Server || storage is null)
				return;

			Item shimmeringItem = new Item(itemType, toShimmer);
			int iconicItem = MagicCache.ShimmerInfos[itemType].iconicItem;

			List<Item> items;
			using(var debuggingWork = DebugMessage.CreateIf(DebugControls.Names.ShimmerRequestNetcode)) {
				if (debuggingWork.IsDebugging) {
					debuggingWork
						.Report(true, "Processing shimmer request")
						.Indent()
						.Report(false, "Shimmering item: {0}", shimmeringItem.IdentifierAndStack())
						.Unindent();
				}

				foreach (var result in results)
					result?.OnShimmer(shimmeringItem, iconicItem, storage, net: true);

				if (debuggingWork.IsDebugging) {
					debuggingWork
						.Report(false, "Attempting item withdraws/deposits")
						.Indent()
						.Report(false, "Withdrawing {0} items", storage.toWithdraw.Count)
						.Report(false, "Depositing {0} items", storage.toDeposit.Count)
						.Unindent();
				}

				using (SecuritySystem.CreateAccessContext(sender))
					items = CraftingGUI.HandleCraftWithdrawAndDeposit(storage.heart, storage.toWithdraw, storage.toDeposit);

				if (debuggingWork.IsDebugging)
					debuggingWork.Report(false, "{0} item stacks were leftover", items.Count);
			}

			if (items.Count > 0) {
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.CraftResult);
				packet.Write(items.Count);
				foreach (Item item in items)
					ItemIO.Send(item, packet, true, true);
				packet.Send(sender);

				using var debuggingOutgoing = DebugMessage.CreateIf(
					DebugControls.Combine()
						.Get(DebugControls.Names.OutgoingNetcodePackets)
						.AndAny(DebugControls.Names.ShimmerRequestNetcode, DebugControls.Names.StorageOperationsNetcode)
				);

				if (debuggingOutgoing.IsDebugging)
					debuggingOutgoing.Report(false, "Sent packet {0} to client {1}", MessageType.CraftResult, sender);
			}

			SendRefreshNetworkItems(storage.heart.Position, false);
		}

		public static void SendStorageHeartName(TEStorageHeart heart) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.RenameStorageHeart);
			packet.Write(heart.Position);
			packet.Write(heart.storageName);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageOperationsNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.RenameStorageHeart)
					.Indent()
					.Report(false, "Heart position: {0}", heart.Position.DebugString())
					.Report(false, "New name: {0}", heart.storageName);
			}
		}

		private static void ReceiveStorageHeartName(BinaryReader reader, int sender) {
			Point16 position = reader.ReadPoint16();
			string name = reader.ReadString();

			using var debuggingIncoming = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageOperationsNetcode);

			if (debuggingIncoming.IsDebugging) {
				debuggingIncoming
					.Report(false, "Read position: {0}", position.DebugString())
					.Report(false, "Read name: {0}", name);
			}

			if (!TryGetEntityFromLocation(MessageType.RenameStorageHeart, position, out TEStorageHeart heart))
				return;

			heart.storageName = name;

			if (Main.netMode != NetmodeID.Server)
				return;

			// Forward the rename to other clients
			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.RenameStorageHeart);
			packet.Write(position);
			packet.Write(name);
			packet.Send(ignoreClient: sender);

			using var debuggingOutgoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageOperationsNetcode);

			if (debuggingOutgoing.IsDebugging) {
				debuggingOutgoing
					.Report(true, "Forwarded packet {0} from client {1} to all other clients", MessageType.RenameStorageHeart, sender)
					.Indent()
					.Report(false, "Heart position: {0}", position.DebugString())
					.Report(false, "New name: {0}", name);
			}
		}

		[Obsolete("This message has been replaced by " + nameof(RequestStorageDepositHistoryChunks), error: true)]
		public static void SyncStorageDepositHistory(TEStorageHeart heart) => throw new NotSupportedException();

		[Obsolete]
		private static void Obsolete_ReceiveStorageDepositHistory(BinaryReader reader, int sender) => throw new NotSupportedException();

		[Obsolete("This message has been replaced by " + nameof(ServerReceiveDepositHistoryChunksRequest), error: true)]
		private static void ReceiveStorageDepositHistory(BinaryReader reader, int sender) => throw new NotSupportedException();

		public static void ClientSendCoreRemoval(Point16 position) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ClientSendCoreRemoval);
			packet.Write(position);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageCoreNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.ClientSendCoreRemoval)
					.Indent()
					.Report(false, "Unit position: {0}", position.DebugString());
			}
		}

		private static void ReceiveCoreRemoval(BinaryReader reader, int sender) {
			Point16 position = reader.ReadPoint16();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageCoreNetcode);

			if (debugging.IsDebugging)
				debugging.Report(false, "Read position: {0}", position.DebugString());

			if (Main.netMode != NetmodeID.Server)
				return;

			if (!TryGetEntityFromLocation(MessageType.ClientSendCoreRemoval, position, out TEStorageUnit unit))
				return;

			var types = unit.GetItems().Select(static i => i.type).Distinct().ToList();

			Item spawnedItem = unit.RemoveItemsAndSpawnCore();
			if (spawnedItem is not null)
				AuditSystem.ReportStorageUnitCoreRemoval(sender, unit, new ReducedItem(spawnedItem));

			// RemoveItemsAndSpawnCore() already sends the frame change
			//	unit.UpdateTileFrameWithNetSend();

			if (unit.GetHeart() is TEStorageHeart heart) {
				heart.ResetCompactStage();
				SendRefreshNetworkItems(heart.Position, typesToRefresh: types);
			}
		}

		public static void ClientSendCoreInsertion(Point16 position, BaseStorageCore core) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ClientSendCoreInsertion);
			packet.Write(position);
			ItemIO.Send(core.Item, packet, false, false);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageCoreNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.ClientSendCoreInsertion)
					.Indent()
					.Report(false, "Unit position: {0}", position.DebugString())
					.Report(false, "Item: {0}", core.Item.IdentifierAndStack());
			}
		}

		private static void ReceiveCoreInsertion(BinaryReader reader, int sender) {
			Point16 position = reader.ReadPoint16();
			Item item = ItemIO.Receive(reader, false, false);

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageCoreNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read position: {0}", position.DebugString())
					.Report(false, "Read item: {0}", item.IdentifierAndStack());
			}

			if (Main.netMode != NetmodeID.Server)
				return;

			if (!TryGetEntityFromLocation(MessageType.ClientSendCoreInsertion, position, out TEStorageUnit unit))
				return;

			unit.InsertCore((BaseStorageCore)item.ModItem);
			// InsertCore already sends the frame change
			//	unit.UpdateTileFrameWithNetSend();

			if (unit.GetHeart() is TEStorageHeart heart) {
				heart.ResetCompactStage();
				SendRefreshNetworkItems(heart.Position, ignoreSpecificRefreshes: true);
			}
		}

		public static void RequestSecurityNetworkCreation(string name, string password, bool restricted) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.SecurityNetworkCreation);
			packet.WriteStringsSafely(name, password);
			packet.Write(restricted);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkCreationNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.SecurityNetworkCreation)
					.Indent()
					.Report(false, "Network name: {0}", name)
					.Report(false, "Private: {0}", restricted);

				if (!string.IsNullOrWhiteSpace(password))
					debugging.Report(false, "Password: {0}", password);
			}
		}

		private static void ReceiveSecurityNetworkCreation(BinaryReader reader, int sender) {
			if (Main.netMode == NetmodeID.Server) {
				reader.ReadStringsSafely(out string name, out string password);
				bool restricted = reader.ReadBoolean();

				using var debuggingIncoming = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkCreationNetcode);

				if (debuggingIncoming.IsDebugging) {
					debuggingIncoming
						.Report(false, "Network name: {0}", name)
						.Report(false, "Private: {0}", restricted);

					if (!string.IsNullOrWhiteSpace(password))
						debuggingIncoming.Report(false, "Password: {0}", password);
				}

				var result = SecuritySystem.ServerCreateNetwork(sender, name, password, restricted, out int networkID);

				// Inform all clients of the result
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.SecurityNetworkCreation);
				packet.Write((byte)result);
				packet.Write(networkID);
				packet.Write((byte)sender);
				packet.Send();

				using var debuggingOutgoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkCreationNetcode);

				if (debuggingOutgoing.IsDebugging) {
					debuggingOutgoing
						.Report(true, "Sent packet {0} to all clients", MessageType.SecurityNetworkCreation)
						.Indent()
						.Report(false, "Result: {0}", result);

					if (result.IsSuccess()) {
						debuggingOutgoing
							.Report(false, "Network ID: {0}", networkID)
							.Report(false, "Creator: {0}", sender);
					}
				}
			} else {
				NetworkActionResult result = (NetworkActionResult)reader.ReadByte();
				int networkID = reader.ReadInt32();
				int creator = reader.ReadByte();

				using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkCreationNetcode);

				if (debugging.IsDebugging) {
					debugging
						.Report(false, "Result: {0}", result);

					if (result.IsSuccess()) {
						debugging
							.Report(false, "Network ID: {0}", networkID)
							.Report(false, "Creator: {0}", creator);
					}
				}

				if (creator == Main.myPlayer)
					SecuritySystem.ReportNetworkResult(result, NetworkReportCategory.Creation);

				SecuritySystem.HandleNetworkAccessibilityOnCreation(result, creator, Main.LocalPlayer, networkID);

				// Ensure that Administrators always know the password for the network
				if (Main.LocalPlayer.GetModPlayer<OperatorPlayer>().IsAdministrator)
					RequestPasswordForNetwork(networkID);

				RequestSecurityNetworkList();
			}
		}

		public static void RequestSecurityNetworkRemoval(int networkID, string password) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.SecurityNetworkRemoval);
			packet.Write(networkID);
			packet.WriteStringSafely(password);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkDeletionNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.SecurityNetworkRemoval)
					.Indent()
					.Report(false, "Network ID: {0}", networkID);

				if (!string.IsNullOrWhiteSpace(password))
					debugging.Report(false, "Provided password: {0}", password);
			}
		}

		private static void ReceiveSecurityNetworkRemoval(BinaryReader reader, int sender) {
			if (Main.netMode == NetmodeID.Server) {
				int networkID = reader.ReadInt32();
				string password = reader.ReadStringSafely();

				using var debuggingIncoming = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkDeletionNetcode);

				if (debuggingIncoming.IsDebugging) {
					debuggingIncoming
						.Report(false, "Network ID: {0}", networkID);

					if (!string.IsNullOrWhiteSpace(password))
						debuggingIncoming.Report(false, "Provided password: {0}", password);
				}

				var result = SecuritySystem.ServerRemoveNetwork(sender, networkID, password);

				// Inform all clients of the result
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.SecurityNetworkRemoval);
				packet.Write((byte)result);
				packet.Write(networkID);
				packet.Write((byte)sender);
				packet.Send();

				using var debuggingOutgoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkDeletionNetcode);

				if (debuggingOutgoing.IsDebugging) {
					debuggingOutgoing
						.Report(true, "Sent packet {0} to all clients", MessageType.SecurityNetworkRemoval)
						.Indent()
						.Report(false, "Result: {0}", result);

					if (result.IsSuccess()) {
						debuggingOutgoing
							.Report(false, "Network ID: {0}", networkID)
							.Report(false, "Requester: {0}", sender);
					}
				}
			} else {
				NetworkActionResult result = (NetworkActionResult)reader.ReadByte();
				int networkID = reader.ReadInt32();
				int requestingPlayer = reader.ReadByte();

				using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkDeletionNetcode);

				if (debugging.IsDebugging) {
					debugging
						.Report(false, "Result: {0}", result)
						.Report(false, "Network ID: {0}", networkID)
						.Report(false, "Requester: {0}", requestingPlayer);
				}

				if (requestingPlayer == Main.myPlayer)
					SecuritySystem.ReportNetworkResult(result, NetworkReportCategory.Removal);

				SecuritySystem.HandleNetworkAccessibilityOnRemoval(result, Main.LocalPlayer, networkID);

				RequestSecurityNetworkList();
			}
		}

		public static void RequestSecurityNetworkJoin(int networkID, string password) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			// Check if the player already has access to the network
			if (Main.LocalPlayer.GetModPlayer<SecurityPlayer>().HasJoinedNetwork(networkID))
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.SecurityNetworkJoin);
			packet.Write(networkID);
			packet.WriteStringSafely(password);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkAccessNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.SecurityNetworkJoin)
					.Indent()
					.Report(false, "Network ID: {0}", networkID);

				if (!string.IsNullOrWhiteSpace(password))
					debugging.Report(false, "Provided password: {0}", password);
			}
		}

		private static void ReceiveSecurityNetworkJoinAttempt(BinaryReader reader, int sender) {
			if (Main.netMode == NetmodeID.Server) {
				int networkID = reader.ReadInt32();
				string password = reader.ReadStringSafely();

				using var debuggingIncoming = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkAccessNetcode);

				if (debuggingIncoming.IsDebugging) {
					debuggingIncoming
						.Report(false, "Network ID: {0}", networkID);

					if (!string.IsNullOrWhiteSpace(password))
						debuggingIncoming.Report(false, "Provided password: {0}", password);
				}

				var result = SecuritySystem.ServerJoinNetwork(sender, networkID, password);

				// Inform the client of the result
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.SecurityNetworkJoin);
				packet.Write((byte)result);
				packet.Write(networkID);
				packet.WriteStringSafely(password);
				packet.Send(toClient: sender);

				using var debuggingOutgoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkAccessNetcode);

				if (debuggingOutgoing.IsDebugging) {
					debuggingOutgoing
						.Report(true, "Sent packet {0} to client {1}", MessageType.SecurityNetworkJoin, sender)
						.Indent()
						.Report(false, "Result: {0}", result)
						.Report(false, "Network ID: {0}", networkID);

					if (!string.IsNullOrWhiteSpace(password))
						debuggingOutgoing.Report(false, "Provided password: {0}", password);
				}
			} else {
				NetworkActionResult result = (NetworkActionResult)reader.ReadByte();
				int networkID = reader.ReadInt32();
				string password = reader.ReadStringSafely();

				using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkAccessNetcode);

				if (debugging.IsDebugging) {
					debugging
						.Report(false, "Result: {0}", result)
						.Report(false, "Network ID: {0}", networkID);

					if (!string.IsNullOrWhiteSpace(password))
						debugging.Report(false, "Provided password: {0}", password);
				}

				SecuritySystem.ReportNetworkResult(result, NetworkReportCategory.Join);

				SecuritySystem.HandleNetworkAccessibilityOnJoin(result, Main.LocalPlayer, networkID, password);
			}
		}

		public static void RequestSecurityNetworkAccess(int networkID) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			// Check if the player already has access to the network
			if (Main.LocalPlayer.GetModPlayer<SecurityPlayer>().HasJoinedNetwork(networkID))
				return;

			// Check for operator status, and immediately give access in that case
			if (Main.LocalPlayer.GetModPlayer<OperatorPlayer>().hasOp) {
				using var debuggingWork = DebugMessage.CreateIf(DebugControls.Names.SecurityNetworkAccessNetcode);

				if (debuggingWork.IsDebugging)
					debuggingWork.Report(true, "Local player has Operator status, granting immediate access to network {0}", networkID);

				SecuritySystem.ReportNetworkResult(NetworkActionResult.OperatorForcedSuccess, NetworkReportCategory.Access);

				SecuritySystem.HandleNetworkAccessibilityOnAccess(NetworkActionResult.OperatorForcedSuccess, Main.LocalPlayer, networkID);

				return;
			}

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.SecurityNetworkAccessible);
			packet.Write(networkID);
			packet.Send();

			using var debuggingOutgoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkAccessNetcode);

			if (debuggingOutgoing.IsDebugging) {
				debuggingOutgoing
					.Report(true, "Sent packet {0} to the server", MessageType.SecurityNetworkAccessible)
					.Indent()
					.Report(false, "Network ID: {0}", networkID);
			}
		}

		private static void ReceiveSecurityNetworkAccessAttempt(BinaryReader reader, int sender) {
			if (Main.netMode == NetmodeID.Server) {
				int networkID = reader.ReadInt32();

				using var debuggingIncoming = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkAccessNetcode);

				if (debuggingIncoming.IsDebugging)
					debuggingIncoming.Report(false, "Network ID: {0}", networkID);

				var result = SecuritySystem.ServerAccessNetwork(sender, networkID);

				SecuritySystem.HandleNetworkAccessibilityOnAccess(result, Main.player[sender], networkID);

				// Inform the client of the result
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.SecurityNetworkAccessible);
				packet.Write((byte)result);
				packet.Write(networkID);
				packet.Send(toClient: sender);

				using var debuggingOutgoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkAccessNetcode);

				if (debuggingOutgoing.IsDebugging) {
					debuggingOutgoing
						.Report(true, "Sent packet {0} to client {1}", MessageType.SecurityNetworkAccessible, sender)
						.Indent()
						.Report(false, "Result: {0}", result)
						.Report(false, "Network ID: {0}", networkID);
				}
			} else {
				NetworkActionResult result = (NetworkActionResult)reader.ReadByte();
				int networkID = reader.ReadInt32();

				using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkAccessNetcode);

				if (debugging.IsDebugging) {
					debugging
						.Report(false, "Result: {0}", result)
						.Report(false, "Network ID: {0}", networkID);
				}

				SecuritySystem.ReportNetworkResult(result, NetworkReportCategory.Access);

				SecuritySystem.HandleNetworkAccessibilityOnAccess(result, Main.LocalPlayer, networkID);
			}
		}

		public static void RequestSecurityNetworkChange(int networkID, string newName, string newPassword, bool? newRestricted) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.SecurityNetworkModification);
			packet.Write(networkID);

			BitsByte flags = new BitsByte(newName is not null, newPassword is not null, newRestricted is not null);
			if (newRestricted is bool restricted)
				flags[3] = restricted;

			packet.Write(flags);

			if (newName is not null)
				packet.Write(newName);
			if (newPassword is not null)
				packet.Write(newPassword);

			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkModificationNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.SecurityNetworkModification)
					.Indent()
					.Report(false, "Network ID: {0}", networkID);

				if (newName is not null)
					debugging.Report(false, "New name: {0}", newName);

				if (newPassword is not null)
					debugging.Report(false, "New password: {0}", newPassword);

				if (newRestricted.HasValue)
					debugging.Report(false, "New private status: {0}", newRestricted.Value);
			}
		}

		private static void ReceiveSecurityNetworkChange(BinaryReader reader, int sender) {
			if (Main.netMode == NetmodeID.Server) {
				int networkID = reader.ReadInt32();
				BitsByte flags = reader.ReadByte();

				string newName = flags[0] ? reader.ReadString() : null;
				string newPassword = flags[1] ? reader.ReadString() : null;
				bool? newRestricted = flags[2] ? flags[3] : null;

				using var debuggingIncoming = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkModificationNetcode);

				if (debuggingIncoming.IsDebugging) {
					debuggingIncoming
						.Report(false, "Network ID: {0}", networkID);

					if (newName is not null)
						debuggingIncoming.Report(false, "New name: {0}", newName);

					if (newPassword is not null)
						debuggingIncoming.Report(false, "New password: {0}", newPassword);

					if (newRestricted is bool restricted)
						debuggingIncoming.Report(false, "New private status: {0}", restricted);
				}

				var result = SecuritySystem.ServerModifyNetwork(sender, networkID, newName, newPassword, newRestricted, out bool passwordChanged, out bool privacyChanged);

				if (result.IsSuccess()) {
					bool outdatedAuthorization = passwordChanged || privacyChanged;
					var network = SecuritySystem.GetNetwork(networkID);

					foreach (var player in Main.ActivePlayers)
						SecuritySystem.HandleNetworkAccessibilityOnModification(result, player, network, outdatedAuthorization, player.whoAmI == sender);
				}

				// Inform all clients of the result
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.SecurityNetworkModification);
				packet.Write((byte)result);
				packet.Write(networkID);
				packet.Write(new BitsByte(passwordChanged, privacyChanged));
				packet.Write((byte)sender);
				packet.Send();

				using var debuggingOutgoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkModificationNetcode);

				if (debuggingOutgoing.IsDebugging) {
					debuggingOutgoing
						.Report(true, "Sent packet {0} to all clients", MessageType.SecurityNetworkModification)
						.Indent()
						.Report(false, "Result: {0}", result)
						.Report(false, "Network ID: {0}", networkID)
						.Report(false, "Password changed: {0}", passwordChanged)
						.Report(false, "Privacy changed: {0}", privacyChanged)
						.Report(false, "Requester: {0}", sender);
				}
			} else {
				NetworkActionResult result = (NetworkActionResult)reader.ReadByte();
				int networkID = reader.ReadInt32();
				BitsByte flags = reader.ReadByte();
				int requestingPlayer = reader.ReadByte();

				using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkModificationNetcode);

				if (debugging.IsDebugging) {
					debugging
						.Report(false, "Result: {0}", result)
						.Report(false, "Network ID: {0}", networkID)
						.Report(false, "Password changed: {0}", flags[0])
						.Report(false, "Privacy changed: {0}", flags[1])
						.Report(false, "Requester: {0}", requestingPlayer);
				}

				if (requestingPlayer == Main.myPlayer)
					SecuritySystem.ReportNetworkResult(result, NetworkReportCategory.Modification);

				// If the password or restricted status was changed, the client's authorization status is outdated
				var network = SecuritySystem.GetNetwork(networkID);
				SecuritySystem.HandleNetworkAccessibilityOnModification(result, Main.LocalPlayer, network, flags[0] || flags[1], requestingPlayer == Main.myPlayer);

				// Ensure that Administrators always know the password for the network
				if (flags[0] && Main.LocalPlayer.GetModPlayer<OperatorPlayer>().IsAdministrator)
					RequestPasswordForNetwork(networkID);

				RequestSecurityNetworkList();
			}
		}

		public static void RequestSecurityNetworkList() {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.RequestSecurityNetworkList);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkListNetcode);

			if (debugging.IsDebugging)
				debugging.Report(true, "Sent packet {0} to the server", MessageType.RequestSecurityNetworkList);
		}

		private static void ReceiveSecurityNetworkList(BinaryReader reader, int sender) {
			if (Main.netMode == NetmodeID.Server) {
				// Inform the client of the result
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.RequestSecurityNetworkList);
				SecuritySystem.SyncClientNetworkViews(packet);
				packet.Send(sender);

				using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkListNetcode);

				if (debugging.IsDebugging) {
					debugging
						.Report(true, "Sent packet {0} to client {1}", MessageType.RequestSecurityNetworkList, sender)
						.Indent()
						.Report(false, "Number of networks sent: {0}", SecuritySystem.NetworkCount);
				}
			} else {
				SecuritySystem.ReceiveClientNetworkViews(reader);

				using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkListNetcode);

				if (debugging.IsDebugging) {
					debugging
						.Report(false, "Security list updated with {0} networks:", SecuritySystem.NetworkCount)
						.Indent();

					foreach (var network in SecuritySystem.GetNetworks())
						debugging.Report(false, "{0} (ID: {1})", network.name, network.id);
				}
			}
		}

		private static void ReceiveSecurityPlayerSync(BinaryReader reader, int sender) {
			byte plr = reader.ReadByte();
			SecurityPlayer mp = Main.player[plr].GetModPlayer<SecurityPlayer>();
			mp.ReceiveSync(reader);

			if (Main.netMode == NetmodeID.Server) {
				// Forward the result
				mp.SyncPlayer(-1, sender, false);
			}
		}

		public static void SyncStorageComponentNetwork(TEStorageComponent component) {
			if (Main.netMode != NetmodeID.Server)
				return;

			// Inform all clients of the result
			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.StorageHeartNetwork);
			packet.Write(component.Position);
			packet.Write(component.assignedNetwork);

			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkAssignmentNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to all clients", MessageType.StorageHeartNetwork)
					.Indent()
					.Report(false, "Component position: {0}", component.Position.DebugString())
					.Report(false, "Assigned network ID: {0}", component.assignedNetwork);
			}
		}

		private static void ReceiveStorageComponentNetwork(BinaryReader reader, int sender) {
			Point16 componentPosition = reader.ReadPoint16();
			int networkID = reader.ReadInt32();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkAssignmentNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read component position: {0}", componentPosition.DebugString())
					.Report(false, "Read assigned network ID: {0}", networkID);
			}

			if (!TryGetEntityFromLocation(MessageType.StorageHeartNetwork, componentPosition, out TEStorageComponent component))
				return;

			component.assignedNetwork = networkID;

			// Checking for the security UI shouldn't be necessary, since components will set to a network either
			//   via the heart (which is handled by another netcode packet) or when destroying the heart (which would
			//   close the security UI anyway)
			/*
			Point16 viewing = Main.LocalPlayer.GetModPlayer<StoragePlayer>().ViewingStorage();

			if (viewing == componentPosition && MagicUI.IsSecurityUIOpen()) {
				// The security UI will need to update
				SecuritySystem.clientListDirty = true;
			}
			*/
		}

		public static void RequestStorageHeartNetworkAssignment(Player player, TEStorageHeart heart, int networkID) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			if (!player.GetModPlayer<OperatorPlayer>().hasOp && !player.GetModPlayer<SecurityPlayer>().HasJoinedNetwork(networkID)) {
				using var debuggingPermissions = DebugMessage.CreateIf(DebugControls.Names.SecurityNetworkAssignmentNetcode);

				if (debuggingPermissions.IsDebugging) {
					debuggingPermissions
						.Report(true, "Current player lacks the permissions to change the assigned network for a Storage Heart")
						.Indent()
						.Report(false, "Heart position: {0}", heart.Position.DebugString())
						.Report(false, "Requested network ID: {0}", networkID);
				}

				return;
			}

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.StorageHeartNetworkAssignment);
			packet.Write(heart.Position);
			packet.Write(networkID);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkAssignmentNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.StorageHeartNetworkAssignment)
					.Indent()
					.Report(false, "Heart position: {0}", heart.Position.DebugString())
					.Report(false, "Requested network ID: {0}", networkID);
			}
		}

		public static void ReceiveStorageHeartNetworkAssignmentRequest(BinaryReader reader, int sender) {
			if (Main.netMode == NetmodeID.Server) {
				Point16 heartPosition = reader.ReadPoint16();
				int networkID = reader.ReadInt32();

				using var debuggingIncoming = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkAssignmentNetcode);

				if (debuggingIncoming.IsDebugging) {
					debuggingIncoming
						.Report(false, "Read position: {0}", heartPosition.DebugString())
						.Report(false, "Read network ID: {0}", networkID);
				}

				NetworkActionResult result = SecuritySystem.ServerAssignNetwork(sender, heartPosition, networkID);

				// Inform all clients of the result
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.StorageHeartNetworkAssignment);
				packet.Write((byte)result);
				packet.Write((byte)sender);
				packet.Write(heartPosition);  // The location of the heart needs to be sent again in case the client is viewing its security list
				packet.Send();

				using var debuggingOutgoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkAssignmentNetcode);

				if (debuggingOutgoing.IsDebugging) {
					debuggingOutgoing
						.Report(true, "Sent packet {0} to all clients", MessageType.StorageHeartNetworkAssignment)
						.Indent()
						.Report(false, "Result: {0}", result)
						.Report(false, "Requester: {0}", sender)
						.Report(false, "Heart position: {0}", heartPosition.DebugString())
						.Report(false, "Requested network ID: {0}", networkID);
				}
			} else if (Main.netMode == NetmodeID.MultiplayerClient) {
				NetworkActionResult result = (NetworkActionResult)reader.ReadByte();
				int requestingPlayer = reader.ReadByte();
				Point16 heartPosition = reader.ReadPoint16();

				using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkAssignmentNetcode);

				if (debugging.IsDebugging) {
					debugging
						.Report(false, "Result: {0}", result)
						.Report(false, "Requester: {0}", requestingPlayer)
						.Report(false, "Read position: {0}", heartPosition.DebugString());
				}

				if (requestingPlayer == Main.myPlayer)
					SecuritySystem.ReportNetworkResult(result, NetworkReportCategory.NetworkChange);

				// Shouldn't be necessary, since the UI will listen for the action result
				/*
				if (result.IsSuccess() && StoragePlayer.LocalPlayer.GetStorageHeart() is TEStorageHeart heart && heart.Position == heartPosition && MagicUI.IsStorageUIOpen()) {
					// The security UI will need to update
					SecuritySystem.clientListDirty = true;
				}
				*/
			}
		}

		public static void RequestAccessibleNetworksByDefault() {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.DefaultAccessibleNetworks);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkListNetcode);

			if (debugging.IsDebugging)
				debugging.Report(true, "Sent packet {0} to the server", MessageType.DefaultAccessibleNetworks);
		}

		private static void RecieveAccessibleNetworksByDefaultRequest(BinaryReader reader, int sender) {
			if (Main.netMode == NetmodeID.Server) {
				NetworkActionResult result = SecuritySystem.ServerDefaultAccessibleNetworks(sender, out var networks);

				// Inform the client of the result
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.DefaultAccessibleNetworks);
				packet.Write((byte)result);

				if (result.IsSuccess()) {
					SecurityPlayer securityPlayer = Main.player[sender].GetModPlayer<SecurityPlayer>();

					packet.Write(networks.Length);

					if (networks.Length > 0) {
						foreach (var network in networks) {
							packet.Write(network.id);
							packet.WriteStringSafely(network.password);

							// Ensure that the server's player instance is able to access the network
							securityPlayer.JoinNetwork(network.id);
							securityPlayer.RememberPassword(network.id, network.password);
						}
					}
				}
				
				packet.Send(toClient: sender);

				using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkListNetcode);

				if (debugging.IsDebugging) {
					debugging
						.Report(true, "Sent packet {0} to client {1}", MessageType.DefaultAccessibleNetworks, sender)
						.Indent()
						.Report(false, "Result: {0}", result)
						.Report(false, "Number of networks sent: {0}", result.IsSuccess() ? networks.Length : 0);
				}
			} else if (Main.netMode == NetmodeID.MultiplayerClient) {
				NetworkActionResult result = (NetworkActionResult)reader.ReadByte();

				using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkListNetcode);

				if (debugging.IsDebugging)
					debugging.Report(false, "Result: {0}", result);

				if (result.IsSuccess()) {
					SecurityPlayer securityPlayer = Main.LocalPlayer.GetModPlayer<SecurityPlayer>();

					int count = reader.ReadInt32();

					if (debugging.IsDebugging) {
						debugging
							.Report(false, "Number of networks received: {0}", count)
							.Report(false, "Networks:");

						if (count > 0)
							debugging.Indent();
					}

					for (int i = 0; i < count; i++) {
						int id = reader.ReadInt32();
						securityPlayer.JoinNetwork(id);

						var password = reader.ReadStringSafely();
						if (password is not null) {
							securityPlayer.RememberPassword(id, password);

							if (debugging.IsDebugging)
								debugging.Report(false, "ID: {0}, Password: {1}", id, password);
						} else {
							if (debugging.IsDebugging)
								debugging.Report(false, "ID: {0}, Password: <none>", id);
						}
					}
				}
			}
		}

		public static void RequestPasswordForNetwork(int networkID) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			// Only Administrators can forcibly request the password
			if (!Main.LocalPlayer.GetModPlayer<OperatorPlayer>().IsAdministrator) {
				using var debuggingPermissions = DebugMessage.CreateIf(DebugControls.Names.SecurityNetworkAccessNetcode);

				if (debuggingPermissions.IsDebugging) {
					debuggingPermissions
						.Report(true, "Current player is not an Administrator, cannot request password")
						.Indent()
						.Report(false, "Requested network ID: {0}", networkID);
				}

				return;
			}

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.SecurityNetworkPassword);
			packet.Write(networkID);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkAccessNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.SecurityNetworkPassword)
					.Indent()
					.Report(false, "Requested network ID: {0}", networkID);
			}
		}

		private static void ReceiveNetworkPasswordRequest(BinaryReader reader, int sender) {
			if (Main.netMode == NetmodeID.Server) {
				int networkID = reader.ReadInt32();

				using var debuggingIncoming = DebugMessage.CreateIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkAccessNetcode);

				if (debuggingIncoming.IsDebugging)
					debuggingIncoming.Report(false, "Requested network ID: {0}", networkID);

				NetworkActionResult result = SecuritySystem.TryGetPassword(networkID, out string password);

				if (result.IsSuccess()) {
					// Ensure that the server's player instance is able to access the network
					SecurityPlayer securityPlayer = Main.player[sender].GetModPlayer<SecurityPlayer>();
					securityPlayer.JoinNetwork(networkID);
					securityPlayer.RememberPassword(networkID, password);
				}

				// Inform the client of the result
				ModPacket packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.SecurityNetworkPassword);
				packet.Write((byte)result);
				packet.WriteStringSafely(password);
				packet.Send(toClient: sender);

				using var debuggingOutgoing = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.SecurityNetworkAccessNetcode);

				if (debuggingOutgoing.IsDebugging) {
					debuggingOutgoing
						.Report(true, "Sent packet {0} to client {1}", MessageType.SecurityNetworkPassword, sender)
						.Indent()
						.Report(false, "Result: {0}", result);

					if (result.IsSuccess())
						debuggingOutgoing.Report(false, "Password: {0}", password ?? "<none>");
				}
			} else if (Main.netMode == NetmodeID.MultiplayerClient) {
				int networkID = reader.ReadInt32();
				NetworkActionResult result = (NetworkActionResult)reader.ReadByte();
				string password = reader.ReadStringSafely();

				// NOTE: to prevent exploits, the packet messages should only appear if the local player has the Administrator status

				using var debuggingWork = DebugMessage.CreateIf(DebugControls.Names.SecurityNetworkAccessNetcode);

				// Ensure that only Administrators can receive the password
				if (!Main.LocalPlayer.GetModPlayer<OperatorPlayer>().IsAdministrator) {
					if (debuggingWork.IsDebugging)
						debuggingWork.Report(false, "Local player is not an Administrator, ignoring received password", networkID);

					return;
				}

				using var debuggingIncoming = DebugMessage.CreateIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.SecurityNetworkAccessNetcode);

				if (debuggingIncoming.IsDebugging) {
					debuggingIncoming
						.Report(false, "Requested network ID: {0}", networkID)
						.Report(false, "Result: {0}", result);

					if (result.IsSuccess())
						debuggingIncoming.Report(false, "Password: {0}", password ?? "<none>");
				}

				if (result.IsSuccess()) {
					SecurityPlayer securityPlayer = Main.LocalPlayer.GetModPlayer<SecurityPlayer>();

					securityPlayer.JoinNetwork(networkID);
					securityPlayer.RememberPassword(networkID, password);
				}
			}
		}

		private static void ReceivePityDropsPlayerSync(BinaryReader reader, int sender) {
			byte plr = reader.ReadByte();
			PityLootDrops mp = Main.player[plr].GetModPlayer<PityLootDrops>();
			mp.ReceiveSync(reader);

			if (Main.netMode == NetmodeID.Server) {
				// Forward the result
				mp.SyncPlayer(-1, sender, false);
			}
		}

		public static void RequestStorageDepositHistoryChunks(TEStorageHeart heart) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			heart.ClearDepositHistory();
			heart.requestingDepositHistory = true;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ClientRequestDepositHistoryChunks);
			packet.Write(heart.Position);
			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageHistoryNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to the server", MessageType.ClientRequestDepositHistoryChunks)
					.Indent()
					.Report(false, "Heart position: {0}", heart.Position.DebugString());
			}
		}

		private static void ServerReceiveDepositHistoryChunksRequest(BinaryReader reader) {
			Point16 position = reader.ReadPoint16();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageHistoryNetcode);

			if (debugging.IsDebugging)
				debugging.Report(false, "Read heart position: {0}", position.DebugString());

			if (Main.netMode != NetmodeID.Server)
				return;

			if (TryGetEntityFromLocation(MessageType.ClientRequestDepositHistoryChunks, position, out TEStorageHeart heart))
				heart.SendDepositHistoryChunks();
		}

		private static void ClientReceiveDepositHistoryChunk(BinaryReader reader) {
			Point16 position = reader.ReadPoint16();

			using var debugging = DebugMessage.ChainIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageHistoryNetcode);

			if (debugging.IsDebugging)
				debugging.Report(false, "Read heart position: {0}", position.DebugString());

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			if (TryGetEntityFromLocation(MessageType.ServerResponseDepositHistoryChunks, position, out TEStorageHeart heart))
				heart.ReceiveDepositHistoryChunk(reader);
		}

		public static void SendDepositHistoryUpdate(TEStorageHeart heart, int[] additions, int[] removals) {
			if (Main.netMode != NetmodeID.Server)
				return;

			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.UpdateDepositHistory);
			packet.Write(heart.Position);

			if (additions is { Length: >0 }) {
				packet.Write7BitEncodedInt(additions.Length);
				foreach (int index in additions)
					packet.Write7BitEncodedInt(index);
			} else
				packet.Write((byte)0);

			if (removals is { Length: >0 }) {
				packet.Write7BitEncodedInt(removals.Length);
				foreach (int index in removals)
					packet.Write7BitEncodedInt(index);
			} else
				packet.Write((byte)0);

			packet.Send();

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.OutgoingNetcodePackets, DebugControls.Names.StorageHistoryNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent packet {0} to all clients", MessageType.UpdateDepositHistory)
					.Indent()
					.Report(false, "Heart position: {0}", heart.Position.DebugString())
					.Report(false, "New entry count: {0}", additions is null ? 0 : additions.Length)
					.Report(false, "Removed entry count: {0}", removals is null ? 0 : removals.Length);
			}
		}

		private static void ClientReceiveDepositHistoryUpdate(BinaryReader reader) {
			Point16 position = reader.ReadPoint16();
			int additionsCount = reader.Read7BitEncodedInt();
			
			int[] additions;
			if (additionsCount > 0) {
				additions = new int[additionsCount];
				for (int i = 0; i < additionsCount; i++)
					additions[i] = reader.Read7BitEncodedInt();
			} else
				additions = [];

			int removalsCount = reader.Read7BitEncodedInt();
			int[] removals;
			if (removalsCount > 0) {
				removals = new int[removalsCount];
				for (int i = 0; i < removalsCount; i++)
					removals[i] = reader.Read7BitEncodedInt();
			} else
				removals = [];

			using var debugging = DebugMessage.CreateIfAll(DebugControls.Names.IncomingNetcodePackets, DebugControls.Names.StorageHistoryNetcode);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read heart position: {0}", position.DebugString())
					.Report(false, "New entry count: {0}", additionsCount)
					.Report(false, "Removed entry count: {0}", removalsCount);
			}

			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			if (TryGetEntityFromLocation(MessageType.UpdateDepositHistory, position, out TEStorageHeart heart))
				heart.UpdateDepositHistory(additions, removals);
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
		RequestShimmerItemInStorage,
		RenameStorageHeart,
		SyncDepositHistory,
		ClientSendCoreRemoval,
		ClientSendCoreInsertion,
		SecurityNetworkCreation,
		SecurityNetworkRemoval,
		SecurityNetworkJoin,
		SecurityNetworkAccessible,
		SecurityNetworkModification,
		RequestSecurityNetworkList,
		SecurityPlayerSync,
		StorageHeartNetwork,
		StorageHeartNetworkAssignment,
		DefaultAccessibleNetworks,
		SecurityNetworkPassword,
		AuditSystemMessage,
		SyncPityDropsPlayer,
		ClientRequestDepositHistoryChunks,
		ServerResponseDepositHistoryChunks,
		UpdateDepositHistory
	}
}
