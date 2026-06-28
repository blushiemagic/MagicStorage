using MagicStorage.Common.Players;
using MagicStorage.Common.Systems.Debugging;
using MagicStorage.Components;
using MagicStorage.Items;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.Auditing {
	internal class AuditSystem : ModSystem {
		private static DateTime _lastSaveTime;
		public static TimeSpan SaveInterval { get; set; } = TimeSpan.FromMinutes(5);

		private static bool _loading, _writing, _printing, _clearing;
		private static CancellationTokenSource _cancelSource = new();

		private static AuditFile _file;
		private static ConcurrentQueue<AuditEntry> _queue = [];

		private static string _lastKnownAuditPath;
		public static string AuditPath => Main.ActiveWorldFileData is null ? null : Path.ChangeExtension(Main.ActiveWorldFileData.Path, ".ms.audit");

		// NOTE: PostUpdateWorld only runs if clients are present on the server
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				Main.OnTickForThirdPartySoftwareOnly += CheckForAudits;
		}

		public override void OnWorldUnload() {
			if (Main.netMode != NetmodeID.Server)
				return;

			// NOTE: ModSystem.Unload seems to not run when exiting on a server, but OnWorldUnload does
			Main.OnTickForThirdPartySoftwareOnly -= CheckForAudits;

			while (_loading || _printing)
				Thread.Yield();

			CheckForAudits();
			
			if (_file is not null && _lastKnownAuditPath is not null) {
				// Ensure that the file is completely saved
				_writing = true;
				SaveAuditFile();
			}

			// NOTE: OnWorldUnload only runs once on servers, so resetting e.g. "_lastKnownAuditPath" isn't necessary
		}

		private static void CheckForAudits() {
			if (Main.netMode != NetmodeID.Server || _loading || _writing || _printing || _clearing || _file is null)
				return;

			bool debug = _queue.Count > 0;

			if (debug) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "[AUDIT] {0} audit entries have been queued, adding to file object now...", _queue.Count);
				DebugMessage.Indent();
			}

			while (_queue.TryDequeue(out var entry)) {
				_file.AddEntry(entry);

				DebugMessage.Report(false, entry.NetRepresentation());
			}

			if (debug)
				DebugMessage.EndReportGroup();

			CheckSave();
		}

		internal const byte COMMAND_TRANSLATE = byte.MaxValue;
		internal const byte COMMAND_CLEAR = COMMAND_TRANSLATE - 1;
		internal const byte COMMAND_FILE_CONTENT = COMMAND_CLEAR - 1;
		internal const byte COMMAND_FILE_CONTENT_END = COMMAND_FILE_CONTENT - 1;

		private const int NUM_COMMANDS = 4;

		private static void ReportByteCommand(byte msg, int sender) {
			if (TryGetByteCommandName(msg, out string name))
				DebugMessage.Report(false, "Received command: {0}", name);
			else
				DebugMessage.Report(false, "Received unknown command {0}", msg);
		}

		internal static bool TryGetByteCommandName(byte msg, out string name) {
			switch (msg) {
				case COMMAND_FILE_CONTENT_END:
					name = nameof(COMMAND_FILE_CONTENT_END);
					return true;
				case COMMAND_FILE_CONTENT:
					name = nameof(COMMAND_FILE_CONTENT);
					return true;
				case COMMAND_CLEAR:
					name = nameof(COMMAND_CLEAR);
					return true;
				case COMMAND_TRANSLATE:
					name = nameof(COMMAND_TRANSLATE);
					return true;
				default:
					name = null;
					return false;
			}
		}

		internal static string GetByteCommandName(byte msg)
			=> TryGetByteCommandName(msg, out string name) ? name : throw new ArgumentOutOfRangeException(nameof(msg), $"Byte command ID ({msg}) was outside the range of expected values");

		internal static void HandlePacket(BinaryReader reader, int sender) {
			byte msg = reader.ReadByte();

			// Not actions, but rather commands
			if (msg >= byte.MaxValue - NUM_COMMANDS + 1) {
				ReportByteCommand(msg, sender);

				switch (msg) {
					case COMMAND_FILE_CONTENT_END:
						ReceiveEndOfFileContent(reader);
						break;
					case COMMAND_FILE_CONTENT:
						ReceiveFileContent(reader);
						break;
					case COMMAND_CLEAR:
						ClearAudits();
						break;
					case COMMAND_TRANSLATE:
						ReceivePrettifyAuditFileRequest(reader, sender);
						break;
				}

				return;
			}


			AuditAction action = (AuditAction)msg;
			int playerWhoAmI = reader.ReadByte();

			using var debugging = DebugMessage.CreateIf(DebugControls.Names.IncomingNetcodePackets);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Action: {0} ({1})", action, (byte)action)
					.Report(false, "Actor player: {0}", playerWhoAmI)
					.Indent();
			}

			switch (action) {
				case AuditAction.DepositOne:
					ReceiveItemDepositOne(reader, playerWhoAmI);
					break;
				case AuditAction.DepositMany:
					ReceiveItemDepositMany(reader, playerWhoAmI);
					break;
				case AuditAction.WithdrawOne:
					ReceiveItemWithdrawOne(reader, playerWhoAmI);
					break;
				case AuditAction.WithdrawMany:
					ReceiveItemWithdrawMany(reader, playerWhoAmI);
					break;
				case AuditAction.UnitDeactivate:
					ReceiveStorageUnitDeactivation(reader, playerWhoAmI);
					break;
				case AuditAction.UnitActivate:
					ReceiveStorageUnitActivation(reader, playerWhoAmI);
					break;
				case AuditAction.UnitCoreRemove:
					ReceiveStorageUnitCoreRemoval(reader, playerWhoAmI);
					break;
				case AuditAction.UnitCoreInsert:
					ReceiveStorageUnitCoreInsertion(reader, playerWhoAmI);
					break;
				case AuditAction.SellItems:
					ReceiveMassItemSell(reader, playerWhoAmI);
					break;
				case AuditAction.DestroyItem:
					ReceiveItemDeletion(reader, playerWhoAmI);
					break;
				case AuditAction.CraftRequest:
					NetReceiveCraftRequest(reader, playerWhoAmI);
					break;
				case AuditAction.ControlDeleteUnloadedItems:
					ReceiveControlDeleteUnloadedItems(reader, playerWhoAmI);
					break;
				case AuditAction.ControlDeleteUnloadedData:
					ReceiveControlDeleteUnloadedData(reader, playerWhoAmI);
					break;
				case AuditAction.LinkRemoteAccess:
					ReceiveRemoteAccessLink(reader, playerWhoAmI);
					break;
				case AuditAction.LinkPortableAccess:
					ReceivePortableAccessLink(reader, playerWhoAmI);
					break;
				case AuditAction.SecurityNetworkAssignment:
					ReceiveSecurityNetworkAssignment(reader, playerWhoAmI);
					break;
				case AuditAction.SecurityNetworkModification:
					ReceiveSecurityNetworkModification(reader, playerWhoAmI);
					break;
				case AuditAction.SecurityNetworkDelete:
					ReceiveSecurityNetworkDeletion(reader, playerWhoAmI);
					break;
				case AuditAction.SecurityNetworkJoin:
					ReceiveSecurityNetworkJoin(reader, playerWhoAmI);
					break;
				case AuditAction.ControlCompactCoins:
					ReceiveControlCoinCompacting(reader, playerWhoAmI);
					break;
				case AuditAction.StatusServerAdmin:
					ReceiveAdministratorStatusAssignment(reader, playerWhoAmI);
					break;
				case AuditAction.StatusServerOperatorGranted:
					ReceiveOperatorStatusAssignment(reader, playerWhoAmI);
					break;
				case AuditAction.StatusServerOperatorRemoved:
					ReceiveOperatorStatusRemoval(reader, playerWhoAmI);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(action));
			}
		}

		private static void Report(AuditEntry entry) {
			if (Main.netMode != NetmodeID.Server || Main.ActiveWorldFileData is null)
				return;

			CheckLoad();

			_queue.Enqueue(entry);
		}

		public static void ReportItemDeposit(Player player, TEStorageHeart heart, Item item) => Report(new DepositOne(player, heart, item));
		public static void ReportItemDeposit(int playerWhoAmI, TEStorageHeart heart, Item item) => ReportItemDeposit(Main.player[playerWhoAmI], heart, item);
		public static void ReportItemDeposit(Player player, TEStorageHeart heart, ReducedItem item) => Report(new DepositOne(player, heart, item));
		public static void ReportItemDeposit(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) => ReportItemDeposit(Main.player[playerWhoAmI], heart, item);
		
		public static void NetReportItemDepositOne(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.DepositOne, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write(item);
			packet.Send();

			using var debugging = DebugMessage.CreateIf(
				DebugControls.Combine()
					.Get(DebugControls.Names.OutgoingNetcodePackets)
					.AndAny(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.ItemDepositAuditsNetcode)
			);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent audit packet {0} to the server", AuditAction.DepositOne)
					.Indent()
					.Report(false, "Heart position: {0}", heart.Position.DebugString())
					.Report(false, "Item: {0}", item.IdentifierAndStack());
			}
		}

		private static void ReceiveItemDepositOne(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			ReducedItem item = reader.ReadReducedItem();

			using var debugging = DebugMessage.ChainIf(
				DebugControls.Combine()
					.Get(DebugControls.Names.IncomingNetcodePackets)
					.AndAny(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.ItemDepositAuditsNetcode)
			);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read position: {0}", location.DebugString())
					.Report(false, "Read item: {0}", item.IdentifierAndStack());
			}

			if (NetHelper.TryGetEntityFromLocation($"{MessageType.AuditSystemMessage}.{AuditAction.DepositOne}", location, out TEStorageHeart heart))
				ReportItemDeposit(playerWhoAmI, heart, item);
		}

		public static void ReportItemDeposit(Player player, TEStorageHeart heart, Item[] items) => Report(new DepositMany(player, heart, items));
		public static void ReportItemDeposit(int playerWhoAmI, TEStorageHeart heart, Item[] items) => ReportItemDeposit(Main.player[playerWhoAmI], heart, items);
		public static void ReportItemDeposit(Player player, TEStorageHeart heart, ReducedItem[] items) => Report(new DepositMany(player, heart, items));
		public static void ReportItemDeposit(int playerWhoAmI, TEStorageHeart heart, ReducedItem[] items) => ReportItemDeposit(Main.player[playerWhoAmI], heart, items);

		public static void NetReportItemDepositMany(int playerWhoAmI, TEStorageHeart heart, ReadOnlySpan<ReducedItem> items) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.DepositMany, playerWhoAmI);
			packet.Write(heart.Position);

			packet.Write7BitEncodedInt(items.Length);
			foreach (var item in items)
				packet.Write(item);

			packet.Send();

			using var debugging = DebugMessage.CreateIf(
				DebugControls.Combine()
					.Get(DebugControls.Names.OutgoingNetcodePackets)
					.AndAny(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.ItemDepositAuditsNetcode)
			);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent audit packet {0} to the server", AuditAction.DepositMany)
					.Indent()
					.Report(false, "Heart position: {0}", heart.Position.DebugString())
					.Report(false, "Item count: {0}", items.Length);
			}
		}

		private static void ReceiveItemDepositMany(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			int count = reader.Read7BitEncodedInt();
			
			ReducedItem[] items = new ReducedItem[count];
			for (int i = 0; i < count; i++)
				items[i] = reader.ReadReducedItem();

			using var debugging = DebugMessage.ChainIf(
				DebugControls.Combine()
					.Get(DebugControls.Names.IncomingNetcodePackets)
					.AndAny(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.ItemDepositAuditsNetcode)
			);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read position: {0}", location.DebugString())
					.Report(false, "Read item count: {0}", count);
			}

			if (NetHelper.TryGetEntityFromLocation($"{MessageType.AuditSystemMessage}.{AuditAction.DepositMany}", location, out TEStorageHeart heart))
				ReportItemDeposit(playerWhoAmI, heart, items);
		}

		public static void ReportItemWithdraw(Player player, TEStorageHeart heart, Item item) => Report(new WithdrawOne(player, heart, item));
		public static void ReportItemWithdraw(int playerWhoAmI, TEStorageHeart heart, Item item) => ReportItemWithdraw(Main.player[playerWhoAmI], heart, item);
		public static void ReportItemWithdraw(Player player, TEStorageHeart heart, ReducedItem item) => Report(new WithdrawOne(player, heart, item));
		public static void ReportItemWithdraw(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) => ReportItemWithdraw(Main.player[playerWhoAmI], heart, item);

		public static void NetReportItemWithdrawOne(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.WithdrawOne, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write(item);
			packet.Send();

			using var debugging = DebugMessage.CreateIf(
				DebugControls.Combine()
					.Get(DebugControls.Names.OutgoingNetcodePackets)
					.AndAny(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.ItemWithdrawalAuditsNetcode)
			);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent audit packet {0} to the server", AuditAction.WithdrawOne)
					.Indent()
					.Report(false, "Heart position: {0}", heart.Position.DebugString())
					.Report(false, "Item: {0}", item.IdentifierAndStack());
			}
		}

		private static void ReceiveItemWithdrawOne(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			ReducedItem item = reader.ReadReducedItem();

			using var debugging = DebugMessage.ChainIf(
				DebugControls.Combine()
					.Get(DebugControls.Names.IncomingNetcodePackets)
					.AndAny(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.ItemWithdrawalAuditsNetcode)
			);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read position: {0}", location.DebugString())
					.Report(false, "Read item: {0}", item.IdentifierAndStack());
			}

			if (NetHelper.TryGetEntityFromLocation($"{MessageType.AuditSystemMessage}.{AuditAction.WithdrawOne}", location, out TEStorageHeart heart))
				ReportItemWithdraw(playerWhoAmI, heart, item);
		}

		public static void ReportItemWithdraw(Player player, TEStorageHeart heart, Item[] items) => Report(new WithdrawMany(player, heart, items));
		public static void ReportItemWithdraw(int playerWhoAmI, TEStorageHeart heart, Item[] items) => ReportItemWithdraw(Main.player[playerWhoAmI], heart, items);
		public static void ReportItemWithdraw(Player player, TEStorageHeart heart, ReducedItem[] items) => Report(new WithdrawMany(player, heart, items));
		public static void ReportItemWithdraw(int playerWhoAmI, TEStorageHeart heart, ReducedItem[] items) => ReportItemWithdraw(Main.player[playerWhoAmI], heart, items);

		public static void NetReportItemWithdrawMany(int playerWhoAmI, TEStorageHeart heart, ReducedItem[] items) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.WithdrawMany, playerWhoAmI);
			packet.Write(heart.Position);

			packet.Write7BitEncodedInt(items.Length);
			foreach (var item in items)
				packet.Write(item);

			packet.Send();

			using var debugging = DebugMessage.CreateIf(
				DebugControls.Combine()
					.Get(DebugControls.Names.OutgoingNetcodePackets)
					.AndAny(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.ItemWithdrawalAuditsNetcode)
			);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent audit packet {0} to the server", AuditAction.WithdrawMany)
					.Indent()
					.Report(false, "Heart position: {0}", heart.Position.DebugString())
					.Report(false, "Item count: {0}", items.Length);
			}
		}

		private static void ReceiveItemWithdrawMany(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			int count = reader.Read7BitEncodedInt();

			ReducedItem[] items = new ReducedItem[count];
			for (int i = 0; i < count; i++)
				items[i] = reader.ReadReducedItem();

			using var debugging = DebugMessage.ChainIf(
				DebugControls.Combine()
					.Get(DebugControls.Names.IncomingNetcodePackets)
					.AndAny(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.ItemWithdrawalAuditsNetcode)
			);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, "Read position: {0}", location.DebugString())
					.Report(false, "Read item count: {0}", count);
			}

			if (NetHelper.TryGetEntityFromLocation($"{MessageType.AuditSystemMessage}.{AuditAction.WithdrawMany}", location, out TEStorageHeart heart))
				ReportItemWithdraw(playerWhoAmI, heart, items);
		}

		public static void ReportStorageUnitDeactivation(Player player, TEStorageUnit unit) => Report(new StorageUnitDeactivation(player, unit));
		public static void ReportStorageUnitDeactivation(int playerWhoAmI, TEStorageUnit unit) => ReportStorageUnitDeactivation(Main.player[playerWhoAmI], unit);

		public static void NetReportStorageUnitDeactivation(int playerWhoAmI, TEStorageUnit unit) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.UnitDeactivate, playerWhoAmI);
			packet.Write(unit.Position);
			packet.Send();

			using var debugging = DebugMessage.CreateIf(
				DebugControls.Combine()
					.Get(DebugControls.Names.OutgoingNetcodePackets)
					.AndAny(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.UnitActivationAuditsNetcode)
			);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent audit packet {0} to the server", AuditAction.UnitDeactivate)
					.Indent()
					.Report(false, "Unit position: (X: {0}, Y: {1})", unit.Position.X, unit.Position.Y);
			}
		}

		private static void ReceiveStorageUnitDeactivation(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();

			using var debugging = DebugMessage.ChainIf(
				DebugControls.Combine()
					.Get(DebugControls.Names.IncomingNetcodePackets)
					.AndAny(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.UnitActivationAuditsNetcode)
			);

			if (debugging.IsDebugging)
				debugging.Report(false, "Read position: {0}", location.DebugString());

			if (NetHelper.TryGetEntityFromLocation($"{MessageType.AuditSystemMessage}.{AuditAction.UnitDeactivate}", location, out TEStorageUnit unit))
				ReportStorageUnitDeactivation(playerWhoAmI, unit);
		}

		public static void ReportStorageUnitActivation(Player player, TEStorageUnit unit) => Report(new StorageUnitActivation(player, unit));
		public static void ReportStorageUnitActivation(int playerWhoAmI, TEStorageUnit unit) => ReportStorageUnitActivation(Main.player[playerWhoAmI], unit);

		public static void NetReportStorageUnitActivation(int playerWhoAmI, TEStorageUnit unit) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.UnitActivate, playerWhoAmI);
			packet.Write(unit.Position);
			packet.Send();

			using var debugging = DebugMessage.CreateIf(
				DebugControls.Combine()
					.Get(DebugControls.Names.OutgoingNetcodePackets)
					.AndAny(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.UnitActivationAuditsNetcode)
			);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Sent audit packet {0} to the server", AuditAction.UnitActivate)
					.Indent()
					.Report(false, "Unit position: (X: {0}, Y: {1})", unit.Position.X, unit.Position.Y);
			}
		}

		private static void ReceiveStorageUnitActivation(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();

			using var debugging = DebugMessage.ChainIf(
				DebugControls.Combine()
					.Get(DebugControls.Names.IncomingNetcodePackets)
					.AndAny(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.UnitActivationAuditsNetcode)
			);

			if (debugging.IsDebugging)
				debugging.Report(false, "Read position: {0}", location.DebugString());

			if (NetHelper.TryGetEntityFromLocation($"{MessageType.AuditSystemMessage}.{AuditAction.UnitActivate}", location, out TEStorageUnit unit))
				ReportStorageUnitActivation(playerWhoAmI, unit);
		}

		public static void ReportStorageUnitCoreRemoval(Player player, TEStorageUnit unit, BaseStorageCore core) => Report(new StorageUnitCoreRemoval(player, unit, core));
		public static void ReportStorageUnitCoreRemoval(int playerWhoAmI, TEStorageUnit unit, BaseStorageCore core) => ReportStorageUnitCoreRemoval(Main.player[playerWhoAmI], unit, core);
		public static void ReportStorageUnitCoreRemoval(Player player, TEStorageUnit unit, ReducedItem item) => Report(new StorageUnitCoreRemoval(player, unit, item));
		public static void ReportStorageUnitCoreRemoval(int playerWhoAmI, TEStorageUnit unit, ReducedItem item) => ReportStorageUnitCoreRemoval(Main.player[playerWhoAmI], unit, item);

		public static void NetReportStorageUnitCoreRemoval(int playerWhoAmI, TEStorageUnit unit, BaseStorageCore core) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.UnitCoreRemove, playerWhoAmI);
			packet.Write(unit.Position);
			packet.Write(new ReducedItem(core.Item));
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageCoreAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.UnitCoreRemove);
				DebugMessage.Indent();
				DebugMessage.Report(false, "Unit position: (X: {0}, Y: {1})", unit.Position.X, unit.Position.Y);
				DebugMessage.Report(false, "Item: {0}", core.Item.IdentifierAndStack());
				DebugMessage.EndReportGroup();
			}
		}

		private static void ReceiveStorageUnitCoreRemoval(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			ReducedItem item = reader.ReadReducedItem();

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageCoreAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(false, "Read position: {0}", location.DebugString());
				DebugMessage.Report(false, "Read item: {0}", item.IdentifierAndStack());
				DebugMessage.EndReportGroup();
			}

			if (!NetHelper.TryGetEntityFromLocation($"{MessageType.AuditSystemMessage}.{AuditAction.UnitCoreRemove}", location, out TEStorageUnit unit)) {
				NetHelper.PrintBadArgsFallback();
				return;
			}

			ReportStorageUnitCoreRemoval(playerWhoAmI, unit, item);
		}

		public static void ReportStorageUnitCoreInsertion(Player player, TEStorageUnit unit, BaseStorageCore core) => Report(new StorageUnitCoreInsertion(player, unit, core));
		public static void ReportStorageUnitCoreInsertion(int playerWhoAmI, TEStorageUnit unit, BaseStorageCore core) => ReportStorageUnitCoreInsertion(Main.player[playerWhoAmI], unit, core);
		public static void ReportStorageUnitCoreInsertion(Player player, TEStorageUnit unit, ReducedItem item) => Report(new StorageUnitCoreInsertion(player, unit, item));
		public static void ReportStorageUnitCoreInsertion(int playerWhoAmI, TEStorageUnit unit, ReducedItem item) => ReportStorageUnitCoreInsertion(Main.player[playerWhoAmI], unit, item);

		public static void NetReportStorageUnitCoreInsertion(int playerWhoAmI, TEStorageUnit unit, BaseStorageCore core) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.UnitCoreInsert, playerWhoAmI);
			packet.Write(unit.Position);
			packet.Write(new ReducedItem(core.Item));
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageCoreAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.UnitCoreInsert);
				DebugMessage.Indent();
				DebugMessage.Report(false, "Unit position: (X: {0}, Y: {1})", unit.Position.X, unit.Position.Y);
				DebugMessage.Report(false, "Item: {0}", core.Item.IdentifierAndStack());
				DebugMessage.EndReportGroup();
			}
		}

		private static void ReceiveStorageUnitCoreInsertion(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			ReducedItem item = reader.ReadReducedItem();

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageCoreAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(false, "Read position: {0}", location.DebugString());
				DebugMessage.Report(false, "Read item: {0}", item.IdentifierAndStack());
				DebugMessage.EndReportGroup();
			}

			if (!NetHelper.TryGetEntityFromLocation($"{MessageType.AuditSystemMessage}.{AuditAction.UnitCoreInsert}", location, out TEStorageUnit unit)) {
				NetHelper.PrintBadArgsFallback();
				return;
			}

			ReportStorageUnitCoreInsertion(playerWhoAmI, unit, item);
		}

		public static void ReportMassItemSell(Player player, TEStorageHeart heart, int soldItemCount, long totalSellValue) => Report(new StorageControlSellItems(player, heart, soldItemCount, totalSellValue));
		public static void ReportMassItemSell(int playerWhoAmI, TEStorageHeart heart, int soldItemCount, long totalSellValue) => ReportMassItemSell(Main.player[playerWhoAmI], heart, soldItemCount, totalSellValue);

		public static void NetReportMassItemSell(int playerWhoAmI, TEStorageHeart heart, int soldItemCount, long totalSellValue) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.SellItems, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write7BitEncodedInt(soldItemCount);
			packet.Write7BitEncodedInt64(totalSellValue);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.SellDuplicatesMenuAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.SellItems);
				DebugMessage.Indent();
				DebugMessage.Report(false, "Heart position: {0}", heart.Position.DebugString());
				DebugMessage.Report(false, "Sold item count: {0}", soldItemCount);
				DebugMessage.Report(false, "Total sell value: {0} copper coins", totalSellValue);
				DebugMessage.EndReportGroup();
			}
		}

		private static void ReceiveMassItemSell(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			int soldItemCount = reader.Read7BitEncodedInt();
			long totalSellValue = reader.Read7BitEncodedInt64();

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.SellDuplicatesMenuAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(false, "Read position: {0}", location.DebugString());
				DebugMessage.Report(false, "Read sold item count: {0}", soldItemCount);
				DebugMessage.Report(false, "Read total sell value: {0} copper coins", totalSellValue);
				DebugMessage.EndReportGroup();
			}

			if (!NetHelper.TryGetEntityFromLocation($"{MessageType.AuditSystemMessage}.{AuditAction.SellItems}", location, out TEStorageHeart heart)) {
				NetHelper.PrintBadArgsFallback();
				return;
			}

			ReportMassItemSell(playerWhoAmI, heart, soldItemCount, totalSellValue);
		}

		public static void ReportItemDeletion(Player player, TEStorageHeart heart, ReducedItem item) => Report(new StorageControlDeleteItem(player, heart, item));
		public static void ReportItemDeletion(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) => ReportItemDeletion(Main.player[playerWhoAmI], heart, item);

		public static void NetReportItemDeletion(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.DestroyItem, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write(item);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageHeartOperationsAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.DestroyItem);
				DebugMessage.Indent();
				DebugMessage.Report(false, "Heart position: {0}", heart.Position.DebugString());
				DebugMessage.Report(false, "Item: {0}", item.IdentifierAndStack());
				DebugMessage.EndReportGroup();
			}
		}

		private static void ReceiveItemDeletion(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			ReducedItem item = reader.ReadReducedItem();

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageHeartOperationsAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(false, "Read position: {0}", location.DebugString());
				DebugMessage.Report(false, "Read item: {0}", item.IdentifierAndStack());
				DebugMessage.EndReportGroup();
			}

			if (!NetHelper.TryGetEntityFromLocation($"{MessageType.AuditSystemMessage}.{AuditAction.DestroyItem}", location, out TEStorageHeart heart)) {
				NetHelper.PrintBadArgsFallback();
				return;
			}

			ReportItemDeletion(playerWhoAmI, heart, item);
		}

		public static void ReportCraftRequest(Player player, TEStorageHeart heart, Item[] results, Item[] consumedMaterials) => Report(new CraftRequest(player, heart, results, consumedMaterials));
		public static void ReportCraftRequest(int playerWhoAmI, TEStorageHeart heart, Item[] results, Item[] consumedMaterials) => ReportCraftRequest(Main.player[playerWhoAmI], heart, results, consumedMaterials);
		public static void ReportCraftRequest(Player player, TEStorageHeart heart, ReducedItem[] results, ReducedItem[] consumedMaterials) => Report(new CraftRequest(player, heart, results, consumedMaterials));
		public static void ReportCraftRequest(int playerWhoAmI, TEStorageHeart heart, ReducedItem[] results, ReducedItem[] consumedMaterials) => ReportCraftRequest(Main.player[playerWhoAmI], heart, results, consumedMaterials);

		public static void NetReportCraftRequest(int playerWhoAmI, TEStorageHeart heart, ReadOnlySpan<ReducedItem> results, ReadOnlySpan<ReducedItem> consumedMaterials) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.CraftRequest, playerWhoAmI);
			packet.Write(heart.Position);

			packet.Write7BitEncodedInt(results.Length);
			foreach (var item in results)
				packet.Write(item);

			packet.Write7BitEncodedInt(consumedMaterials.Length);
			foreach (var item in consumedMaterials)
				packet.Write(item);

			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.CraftRequestAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.CraftRequest);
				DebugMessage.Indent();
				DebugMessage.Report(false, "Heart position: {0}", heart.Position.DebugString());
				DebugMessage.Report(false, "Results count: {0}", results.Length);
				DebugMessage.Report(false, "Consumed materials count: {0}", consumedMaterials.Length);
				DebugMessage.EndReportGroup();
			}
		}

		private static void NetReceiveCraftRequest(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			int resultsCount = reader.Read7BitEncodedInt();
			int materialsCount = reader.Read7BitEncodedInt();

			ReducedItem[] results = new ReducedItem[resultsCount];
			for (int i = 0; i < resultsCount; i++)
				results[i] = reader.ReadReducedItem();

			ReducedItem[] materials = new ReducedItem[materialsCount];
			for (int i = 0; i < materialsCount; i++)
				materials[i] = reader.ReadReducedItem();

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.CraftRequestAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(false, "Read position: {0}", location.DebugString());
				DebugMessage.Report(false, "Read results count: {0}", resultsCount);
				DebugMessage.Report(false, "Read consumed materials count: {0}", materialsCount);
				DebugMessage.EndReportGroup();
			}

			if (!NetHelper.TryGetEntityFromLocation($"{MessageType.AuditSystemMessage}.{AuditAction.CraftRequest}", location, out TEStorageHeart heart)) {
				NetHelper.PrintBadArgsFallback();
				return;
			}

			ReportCraftRequest(playerWhoAmI, heart, results, materials);
		}

		public static void ReportControlDeleteUnloadedItems(Player player, TEStorageHeart heart, int itemsAffected) => Report(new StorageControlDeleteUnloadedItems(player, heart, itemsAffected));
		public static void ReportControlDeleteUnloadedItems(int playerWhoAmI, TEStorageHeart heart, int itemsAffected) => ReportControlDeleteUnloadedItems(Main.player[playerWhoAmI], heart, itemsAffected);

		public static void NetReportControlDeleteUnloadedItems(int playerWhoAmI, TEStorageHeart heart, int itemsAffected) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.ControlDeleteUnloadedItems, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write7BitEncodedInt(itemsAffected);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageHeartOperationsAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.ControlDeleteUnloadedItems);
				DebugMessage.Indent();
				DebugMessage.Report(false, "Heart position: {0}", heart.Position.DebugString());
				DebugMessage.Report(false, "Number of affected items: {0}", itemsAffected);
				DebugMessage.EndReportGroup();
			}
		}

		private static void ReceiveControlDeleteUnloadedItems(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			int itemsAffected = reader.Read7BitEncodedInt();

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageHeartOperationsAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(false, "Read position: {0}", location.DebugString());
				DebugMessage.Report(false, "Read number of affected items: {0}", itemsAffected);
				DebugMessage.EndReportGroup();
			}

			if (!NetHelper.TryGetEntityFromLocation($"{MessageType.AuditSystemMessage}.{AuditAction.ControlDeleteUnloadedItems}", location, out TEStorageHeart heart)) {
				NetHelper.PrintBadArgsFallback();
				return;
			}

			ReportControlDeleteUnloadedItems(playerWhoAmI, heart, itemsAffected);
		}

		public static void ReportControlDeleteUnloadedData(Player player, TEStorageHeart heart, int itemsAffected) => Report(new StorageControlDeleteUnloadedData(player, heart, itemsAffected));
		public static void ReportControlDeleteUnloadedData(int playerWhoAmI, TEStorageHeart heart, int itemsAffected) => ReportControlDeleteUnloadedData(Main.player[playerWhoAmI], heart, itemsAffected);

		public static void NetReportControlDeleteUnloadedData(int playerWhoAmI, TEStorageHeart heart, int itemsAffected) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.ControlDeleteUnloadedData, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write7BitEncodedInt(itemsAffected);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageHeartOperationsAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.ControlDeleteUnloadedData);
				DebugMessage.Indent();
				DebugMessage.Report(false, "Heart position: {0}", heart.Position.DebugString());
				DebugMessage.Report(false, "Number of affected items: {0}", itemsAffected);
				DebugMessage.EndReportGroup();
			}
		}

		private static void ReceiveControlDeleteUnloadedData(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			int itemsAffected = reader.Read7BitEncodedInt();

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageHeartOperationsAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(false, "Read position: {0}", location.DebugString());
				DebugMessage.Report(false, "Read number of affected items: {0}", itemsAffected);
				DebugMessage.EndReportGroup();
			}

			if (!NetHelper.TryGetEntityFromLocation($"{MessageType.AuditSystemMessage}.{AuditAction.ControlDeleteUnloadedData}", location, out TEStorageHeart heart)) {
				NetHelper.PrintBadArgsFallback();
				return;
			}

			ReportControlDeleteUnloadedData(playerWhoAmI, heart, itemsAffected);
		}

		public static void ReportControlCoinCompacting(Player player, TEStorageHeart heart) => Report(new StorageControlCompactCoins(player, heart));
		public static void ReportControlCoinCompacting(int playerWhoAmI, TEStorageHeart heart) => ReportControlCoinCompacting(Main.player[playerWhoAmI], heart);

		public static void NetReportControlCoinCompacting(int playerWhoAmI, TEStorageHeart heart) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.ControlCompactCoins, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageHeartOperationsAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.ControlCompactCoins);
				DebugMessage.Indent();
				DebugMessage.Report(false, "Heart position: {0}", heart.Position.DebugString());
				DebugMessage.EndReportGroup();
			}
		}

		private static void ReceiveControlCoinCompacting(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageHeartOperationsAuditsNetcode))
				DebugMessage.Report(false, "Read position: {0}", location.DebugString());

			if (!NetHelper.TryGetEntityFromLocation($"{MessageType.AuditSystemMessage}.{AuditAction.ControlCompactCoins}", location, out TEStorageHeart heart)) {
				NetHelper.PrintBadArgsFallback();
				return;
			}

			ReportControlCoinCompacting(playerWhoAmI, heart);
		}

		public static void ReportRemoteAccessLink(Player player, TEStorageHeart heart, TERemoteAccess access) => Report(new LinkRemoteAccess(player, heart, access));
		public static void ReportRemoteAccessLink(int playerWhoAmI, TEStorageHeart heart, TERemoteAccess access) => ReportRemoteAccessLink(Main.player[playerWhoAmI], heart, access);

		public static void NetReportRemoteAccessLink(int playerWhoAmI, TEStorageHeart heart, TERemoteAccess access) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.LinkRemoteAccess, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write(access.Position);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageHeartOperationsAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.LinkRemoteAccess);
				DebugMessage.Indent();
				DebugMessage.Report(false, "Heart position: {0}", heart.Position.DebugString());
				DebugMessage.Report(false, "Linking component position: {0}", access.Position.DebugString());
				DebugMessage.EndReportGroup();
			}
		}

		private static void ReceiveRemoteAccessLink(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			Point16 accessLocation = reader.ReadPoint16();

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageHeartOperationsAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(false, "Read heart position: {0}", location.DebugString());
				DebugMessage.Report(false, "Read linking component position: {0}", accessLocation.DebugString());
				DebugMessage.EndReportGroup();
			}

			var packetSource = $"{MessageType.AuditSystemMessage}.{AuditAction.LinkRemoteAccess}";

			if (!NetHelper.TryGetEntityFromLocation(packetSource, location, out TEStorageHeart heart)) {
				NetHelper.PrintBadArgsFallback();
				return;
			}

			if (!NetHelper.TryGetEntityFromLocation(packetSource, accessLocation, out TERemoteAccess access)) {
				NetHelper.PrintBadArgsFallback();
				return;
			}

			ReportRemoteAccessLink(playerWhoAmI, heart, access);
		}

		public static void ReportPortableAccessLink(Player player, TEStorageHeart heart, PortableAccess item) => Report(new LinkPortableAccess(player, heart, item));
		public static void ReportPortableAccessLink(int playerWhoAmI, TEStorageHeart heart, PortableAccess item) => ReportPortableAccessLink(Main.player[playerWhoAmI], heart, item);
		public static void ReportPortableAccessLink(Player player, TEStorageHeart heart, ReducedItem item) => Report(new LinkPortableAccess(player, heart, item));
		public static void ReportPortableAccessLink(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) => ReportPortableAccessLink(Main.player[playerWhoAmI], heart, item);
		public static void ReportPortableAccessLink(Player player, TECraftingAccess access, PortableCraftingAccess item) => Report(new LinkPortableAccess(player, access, item));
		public static void ReportPortableAccessLink(int playerWhoAmI, TECraftingAccess access, PortableCraftingAccess item) => ReportPortableAccessLink(Main.player[playerWhoAmI], access, item);
		public static void ReportPortableAccessLink(Player player, TECraftingAccess access, ReducedItem item) => Report(new LinkPortableAccess(player, access, item));
		public static void ReportPortableAccessLink(int playerWhoAmI, TECraftingAccess access, ReducedItem item) => ReportPortableAccessLink(Main.player[playerWhoAmI], access, item);

		public static void NetReportPortableAccessLink(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.LinkPortableAccess, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write(item);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageHeartOperationsAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.LinkPortableAccess);
				DebugMessage.Indent();
				DebugMessage.Report(false, "Heart position: {0}", heart.Position.DebugString());
				DebugMessage.Report(false, "Item: {0}", item.IdentifierAndStack());
				DebugMessage.EndReportGroup();
			}
		}

		public static void NetReportPortableAccessLink(int playerWhoAmI, TECraftingAccess access, ReducedItem item) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.LinkPortableAccess, playerWhoAmI);
			packet.Write(access.Position);
			packet.Write(item);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageHeartOperationsAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.LinkPortableAccess);
				DebugMessage.Indent();
				DebugMessage.Report(false, "Crafting Interface position: {0}", access.Position.DebugString());
				DebugMessage.Report(false, "Item: {0}", item.IdentifierAndStack());
				DebugMessage.EndReportGroup();
			}
		}

		private static void ReceivePortableAccessLink(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			ReducedItem item = reader.ReadReducedItem();

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.StorageHeartOperationsAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(false, "Read position: {0}", location.DebugString());
				DebugMessage.Report(false, "Read item: {0}", item.IdentifierAndStack());
				DebugMessage.EndReportGroup();
			}

			var packetSource = $"{MessageType.AuditSystemMessage}.{AuditAction.LinkPortableAccess}";

			if (!NetHelper.TryFindEntity(packetSource, location, out TileEntity entity))
			{
				NetHelper.PrintBadArgsFallback();
				return;
			}

			if (entity is TEStorageHeart heart)
				ReportPortableAccessLink(playerWhoAmI, heart, item);
			else if (entity is TECraftingAccess access)
				ReportPortableAccessLink(playerWhoAmI, access, item);
			else {
				if (DebugControls.Get(DebugControls.Names.InvalidNetcodeValues))
					NetHelper.PrintInvalidTileEntityReport<TEStorageHeart, TECraftingAccess>(packetSource, entity, location);
				else
					NetHelper.PrintBadArgsFallback();
			}
		}

		public static void ReportSecurityNetworkAssignment(Player player, TEStorageHeart heart, int assignedNetworkID) => Report(new SecurityNetworkAssignment(player, heart, assignedNetworkID));
		public static void ReportSecurityNetworkAssignment(int playerWhoAmI, TEStorageHeart heart, int assignedNetworkID) => ReportSecurityNetworkAssignment(Main.player[playerWhoAmI], heart, assignedNetworkID);

		public static void NetReportSecurityNetworkAssignment(int playerWhoAmI, TEStorageHeart heart, int assignedNetworkID) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.SecurityNetworkAssignment, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write(assignedNetworkID);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.SecurityNetworkAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.SecurityNetworkAssignment);
				DebugMessage.Indent();
				DebugMessage.Report(false, "Heart position: {0}", heart.Position.DebugString());
				DebugMessage.Report(false, "Assigned network ID: {0}", assignedNetworkID);
				DebugMessage.EndReportGroup();
			}
		}

		private static void ReceiveSecurityNetworkAssignment(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			int assignedNetworkID = reader.ReadInt32();

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.SecurityNetworkAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(false, "Read position: {0}", location.DebugString());
				DebugMessage.Report(false, "Read network ID: {0}", assignedNetworkID);
				DebugMessage.EndReportGroup();
			}

			if (!NetHelper.TryGetEntityFromLocation($"{MessageType.AuditSystemMessage}.{AuditAction.SecurityNetworkAssignment}", location, out TEStorageHeart heart)) {
				NetHelper.PrintBadArgsFallback();
				return;
			}

			ReportSecurityNetworkAssignment(playerWhoAmI, heart, assignedNetworkID);
		}

		public static void ReportSecurityNetworkModification(Player player, int networkID, string oldPassword, bool oldRestricted, string newPassword, bool newRestricted) => Report(new SecurityNetworkModification(player, networkID, oldPassword, newPassword, oldRestricted, newRestricted));
		public static void ReportSecurityNetworkModification(int playerWhoAmI, int networkID, string oldPassword, bool oldRestricted, string newPassword, bool newRestricted) => ReportSecurityNetworkModification(Main.player[playerWhoAmI], networkID, oldPassword, oldRestricted, newPassword, newRestricted);

		public static void NetReportSecurityNetworkModification(int playerWhoAmI, int networkID, string oldPassword, bool oldRestricted, string newPassword, bool newRestricted) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.SecurityNetworkModification, playerWhoAmI);
			packet.Write(networkID);

			bool hasOldPassword = oldPassword is not null, hasNewPassword = newPassword is not null;
			packet.Write(new BitsByte(hasOldPassword, oldRestricted, hasNewPassword, newRestricted));

			if (hasOldPassword) {
				byte[] scrambled = StringScrambling.Scramble(oldPassword);
				packet.Write7BitEncodedInt(scrambled.Length);
				packet.Write(scrambled);
			}

			if (hasNewPassword) {
				byte[] scrambled = StringScrambling.Scramble(newPassword);
				packet.Write7BitEncodedInt(scrambled.Length);
				packet.Write(scrambled);
			}

			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.SecurityNetworkAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.SecurityNetworkModification);
				DebugMessage.Indent();
				DebugMessage.Report(false, "Network ID: {0}", networkID);

				if (Main.LocalPlayer.GetModPlayer<OperatorPlayer>().IsAdministrator)
				{
					DebugMessage.Report(false, "Old password: {0}", oldPassword ?? "<null>");
					DebugMessage.Report(false, "Old restricted status: {0}", oldRestricted);
					DebugMessage.Report(false, "New password: {0}", newPassword ?? "<null>");
					DebugMessage.Report(false, "New restricted status: {0}", newRestricted);
				}

				DebugMessage.EndReportGroup();
			}
		}

		private static void ReceiveSecurityNetworkModification(BinaryReader reader, int playerWhoAmI) {
			int networkID = reader.ReadInt32();

			BitsByte bb = reader.ReadByte();
			bool hasOldPassword = false, oldRestricted = false, hasNewPassword = false, newRestricted = false;
			bb.Retrieve(ref hasOldPassword, ref oldRestricted, ref hasNewPassword, ref newRestricted);

			string oldPassword = null, newPassword = null;
			if (hasOldPassword)
				oldPassword = StringScrambling.Unscramble(reader.ReadBytes(reader.Read7BitEncodedInt()));

			if (hasNewPassword)
				newPassword = StringScrambling.Unscramble(reader.ReadBytes(reader.Read7BitEncodedInt()));

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.SecurityNetworkAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(false, "Read network ID: {0}", networkID);
				DebugMessage.Report(false, "Read old password: {0}", oldPassword ?? "<null>");
				DebugMessage.Report(false, "Read old restricted status: {0}", oldRestricted);
				DebugMessage.Report(false, "Read new password: {0}", newPassword ?? "<null>");
				DebugMessage.Report(false, "Read new restricted status: {0}", newRestricted);
				DebugMessage.EndReportGroup();
			}

			ReportSecurityNetworkModification(playerWhoAmI, networkID, oldPassword, oldRestricted, newPassword, newRestricted);
		}

		public static void ReportSecurityNetworkDeletion(Player player, int networkID) => Report(new SecurityNetworkDeletion(player, networkID));
		public static void ReportSecurityNetworkDeletion(int playerWhoAmI, int networkID) => ReportSecurityNetworkDeletion(Main.player[playerWhoAmI], networkID);

		public static void NetReportSecurityNetworkDeletion(int playerWhoAmI, int networkID) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.SecurityNetworkDelete, playerWhoAmI);
			packet.Write(networkID);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.SecurityNetworkAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.SecurityNetworkDelete);
				DebugMessage.Indent();
				DebugMessage.Report(false, "Network ID: {0}", networkID);
				DebugMessage.EndReportGroup();
			}
		}

		private static void ReceiveSecurityNetworkDeletion(BinaryReader reader, int playerWhoAmI) {
			int networkID = reader.ReadInt32();

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.SecurityNetworkAuditsNetcode))
				DebugMessage.Report(false, "Read network ID: {0}", networkID);

			ReportSecurityNetworkDeletion(playerWhoAmI, networkID);
		}

		public static void ReportSecurityNetworkJoin(Player player, int networkID) => Report(new SecurityNetworkJoin(player, networkID));
		public static void ReportSecurityNetworkJoin(int playerWhoAmI, int networkID) => ReportSecurityNetworkJoin(Main.player[playerWhoAmI], networkID);

		public static void NetReportSecurityNetworkJoin(int playerWhoAmI, int networkID) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.SecurityNetworkJoin, playerWhoAmI);
			packet.Write(networkID);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.SecurityNetworkAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.SecurityNetworkJoin);
				DebugMessage.Indent();
				DebugMessage.Report(false, "Network ID: {0}", networkID);
				DebugMessage.EndReportGroup();
			}
		}

		private static void ReceiveSecurityNetworkJoin(BinaryReader reader, int playerWhoAmI) {
			int networkID = reader.ReadInt32();

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.SecurityNetworkAuditsNetcode))
				DebugMessage.Report(false, "Read network ID: {0}", networkID);

			ReportSecurityNetworkJoin(playerWhoAmI, networkID);
		}

		public static void ReportAdministratorStatusAssignment(Player player) => Report(new StatusAdministratorAssignment(player));
		public static void ReportAdministratorStatusAssignment(int playerWhoAmI) => ReportAdministratorStatusAssignment(Main.player[playerWhoAmI]);

		public static void NetReportAdministratorStatusAssignment(int playerWhoAmI) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.StatusServerAdmin, playerWhoAmI);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.CommandAuditsNetcode))
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.StatusServerAdmin);
		}

		private static void ReceiveAdministratorStatusAssignment(BinaryReader reader, int playerWhoAmI) => ReportAdministratorStatusAssignment(playerWhoAmI);

		public static void ReportOperatorStatusAssignment(Player player) => Report(new StatusOperatorAssignment(player));
		public static void ReportOperatorStatusAssignment(int playerWhoAmI) => ReportOperatorStatusAssignment(Main.player[playerWhoAmI]);

		public static void NetReportOperatorStatusAssignment(int playerWhoAmI) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.StatusServerOperatorGranted, playerWhoAmI);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.CommandAuditsNetcode))
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.StatusServerOperatorGranted);
		}

		private static void ReceiveOperatorStatusAssignment(BinaryReader reader, int playerWhoAmI) => ReportOperatorStatusAssignment(playerWhoAmI);

		public static void ReportOperatorStatusRemoval(Player player) => Report(new StatusOperatorRemoval(player));
		public static void ReportOperatorStatusRemoval(int playerWhoAmI) => ReportOperatorStatusRemoval(Main.player[playerWhoAmI]);

		public static void NetReportOperatorStatusRemoval(int playerWhoAmI) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.StatusServerOperatorRemoved, playerWhoAmI);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.CommandAuditsNetcode))
				DebugMessage.Report(true, "Sent audit packet {0} to the server", AuditAction.StatusServerOperatorRemoved);
		}

		private static void ReceiveOperatorStatusRemoval(BinaryReader reader, int playerWhoAmI) => ReportOperatorStatusRemoval(playerWhoAmI);

		private static ModPacket PreparePacket(AuditAction action, int player) {
			var packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.AuditSystemMessage);
			packet.Write((byte)action);
			packet.Write((byte)player);
			return packet;
		}

		private static string GetLocalizedMessage(string key) => MagicStorageMod.Instance.GetLocalization("AuditLogging." + key).Value;

		private static string GetLocalizedMessage(string key, params object[] values) => MagicStorageMod.Instance.GetLocalization("AuditLogging." + key).Format(values);

		private static void PrintInfo(string localizationKey) {
			string msg = GetLocalizedMessage(localizationKey);
			Utility.PrettyWriteLineToConsole(msg, ConsoleColor.Yellow, ConsoleColor.Black);
			MagicStorageMod.Instance.Logger.Info(msg);
		}

		private static void PrintSuccess(string localizationKey) {
			string msg = GetLocalizedMessage(localizationKey);
			Utility.PrettyWriteLineToConsole(msg, ConsoleColor.Green, ConsoleColor.Black);
			MagicStorageMod.Instance.Logger.Info(msg);
		}

		private static void PrintSuccess(string localizationKey, params object[] values) {
			string msg = GetLocalizedMessage(localizationKey, values);
			Utility.PrettyWriteLineToConsole(msg, ConsoleColor.Green, ConsoleColor.Black);
			MagicStorageMod.Instance.Logger.Info(msg);
		}

		private static void PrintError(string localizationKey) {
			string msg = GetLocalizedMessage(localizationKey);
			Utility.PrettyWriteLineToConsole(msg, ConsoleColor.Red, ConsoleColor.Black);
			MagicStorageMod.Instance.Logger.Warn(msg);
		}

		private static void PrintError(string localizationKey, Exception ex) {
			string msg = GetLocalizedMessage(localizationKey);
			Utility.PrettyWriteLineToConsole(msg, ConsoleColor.Red, ConsoleColor.Black);
			MagicStorageMod.Instance.Logger.Error(msg, ex);
		}

		private static string SetCurrentThreadName(string name) {
			if (!AssetRepository.IsMainThread) {
				string oldName = Thread.CurrentThread.Name;
				Thread.CurrentThread.Name = name;
				return oldName;
			}

			// If this is the main thread, we don't change the name
			return null;
		}

		private static void CheckLoad() {
			if (_file is null && !_loading) {
				if (DebugControls.Get(DebugControls.Names.AuditFile))
					DebugMessage.Report(true, "[AUDIT] Audit file is not loaded, attempting to load...");

				_loading = true;
				new Task(LoadAuditFile, _cancelSource.Token, TaskCreationOptions.LongRunning).Start();
			}
		}

		private static void LoadAuditFile() {
			// Since CancellationTokenSource can only be cancelled once, the variable is reset after cancelling
			// Copying the reference here allows for monitoring the object even after the variable has been reassigned
			var localSource = _cancelSource;

			// NOTE: other operations wait, but loading has the highest priority so it won't wait for them

			string oldName = SetCurrentThreadName("MagicStorage Auditing");

			string path = AuditPath;
			AuditFile file = null;

			bool debug = DebugControls.Get(DebugControls.Names.AuditFile);

			if (debug) {
				DebugMessage.ReserveThreadContext();
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "[AUDIT] Loading audit file from: {0}", path);
				DebugMessage.Indent();
				DebugMessage.RememberCurrentGroup();
			}

			try {
				if (File.Exists(path)) {
					if (debug) {
						DebugMessage.BeginReportGroup();
						DebugMessage.Report(false, "File exists, attempting to load...");
						DebugMessage.Indent();
					}

					// Attempt to load the file
					using FileStream stream = File.OpenRead(path);
					using BinaryReader reader = new(stream);

					file = new();
					AuditFile.DeserializeOne(reader, ref file);

					if (debug)
						DebugMessage.EndReportGroup();

					PrintSuccess("Messages.LoadSuccess", path);
				} else {
					if (debug)
						DebugMessage.Report(false, "File does not exist, skipping load with new AuditFile object");
				}
			} catch when (localSource.IsCancellationRequested) {
				// File loading was cancelled due to the file being cleared, ignore this exception
				if (debug) {
					DebugMessage.RecallSavedGroup();
					DebugMessage.Report(true, "File loading was cancelled");
					DebugMessage.RememberCurrentGroup();
				}
			} catch (Exception ex) {
				if (debug) {
					DebugMessage.RecallSavedGroup();
					DebugMessage.Report(true, "Exception occurred while loading audit file, audit data will be cleared");
					DebugMessage.RememberCurrentGroup();
				}

				PrintError("Errors.LoadFailed", ex);
				file = null;
			} finally {
				if (debug) {
					DebugMessage.RecallSavedGroup();
					DebugMessage.EndReportGroup();
					DebugMessage.FreeThreadContext();
				}

				_lastKnownAuditPath = path;
				_loading = false;

				if (!_cancelSource.IsCancellationRequested) {
					_lastSaveTime = DateTime.UtcNow;
					_file = file ?? new();
				}

				SetCurrentThreadName(oldName);
			}
		}

		private static void CheckSave() {
			DateTime now = DateTime.UtcNow;

			if (_file is not null && !_loading && _file.HasChanges && !_writing && !_printing && now - _lastSaveTime >= SaveInterval) {
				if (DebugControls.Get(DebugControls.Names.AuditFile))
					DebugMessage.Report(true, "[AUDIT] Audit file has changes, saving...");

				_writing = true;
				_lastSaveTime = now;
				new Task(SaveAuditFile, _cancelSource.Token, TaskCreationOptions.LongRunning).Start();
			}
		}

		private static void SaveAuditFile() {
			// Since CancellationTokenSource can only be cancelled once, the variable is reset after cancelling
			// Copying the reference here allows for monitoring the object even after the variable has been reassigned
			var localSource = _cancelSource;

			// Wait for any ongoing operations to finish
			while (_loading || _printing || _clearing)
				Thread.Yield();

			string oldName = SetCurrentThreadName("MagicStorage Auditing");

			bool debug = DebugControls.Get(DebugControls.Names.AuditFile);

			if (debug) {
				DebugMessage.ReserveThreadContext();
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "[AUDIT] Saving audit file to: {0}", _lastKnownAuditPath);
				DebugMessage.Indent();
				DebugMessage.RememberCurrentGroup();
			}

			try {
				using FileStream stream = File.Create(_lastKnownAuditPath);
				using BinaryWriter writer = new(stream);

				_file.Serialize(writer);

				PrintSuccess("Messages.SaveSuccess", _lastKnownAuditPath);
			} catch when (localSource.IsCancellationRequested) {
				// Saving was cancelled due to the file being cleared, ignore this exception
				if (debug) {
					DebugMessage.RecallSavedGroup();
					DebugMessage.Report(true, "File saving was cancelled");
					DebugMessage.RememberCurrentGroup();
				}
			} catch (Exception ex) {
				if (debug) {
					DebugMessage.RecallSavedGroup();
					DebugMessage.Report(true, "Exception occurred while saving audit file, any pending changes will be ignored");
					DebugMessage.RememberCurrentGroup();
				}

				PrintError("Errors.SaveFailed", ex);
				_file.ForceNoChanges();
			} finally {
				if (debug) {
					DebugMessage.RecallSavedGroup();
					DebugMessage.EndReportGroup();
					DebugMessage.FreeThreadContext();
				}

				_writing = false;

				SetCurrentThreadName(oldName);
			}
		}

		public static void TranslateAuditFile() {
			if (Main.netMode != NetmodeID.Server)
				return;

			if (_file is null && !_printing) {
				// Force the file to load
				if (DebugControls.Get(DebugControls.Names.AuditFile))
					DebugMessage.Report(true, "[AUDIT] Audit file translation was requested, but the file is not loaded");

				PrintInfo("Messages.TranslationFileNotLoaded");

				_loading = true;
				_printing = true;
				new Task(LoadThenPrettifyAuditFile, _cancelSource.Token, TaskCreationOptions.LongRunning).Start();
			} else if (!_printing) {
				if (DebugControls.Get(DebugControls.Names.AuditFile))
					DebugMessage.Report(true, "[AUDIT] Audit file translation was requested, starting translation...");

				PrintInfo("Messages.TranslationStarted");

				_printing = true;
				new Task(ServerPrettifyAuditFile, _cancelSource.Token, TaskCreationOptions.LongRunning).Start();
			} else {
				if (DebugControls.Get(DebugControls.Names.AuditFile))
					DebugMessage.Report(true, "[AUDIT] Audit file translation was requested, but a translation is already running");

				PrintError("Errors.TranslationAlreadyRunning");
			}
		}

		private static void LoadThenPrettifyAuditFile() {
			LoadAuditFile();
			ServerPrettifyAuditFile();
		}

		public static void RequestTranslatedAuditFile() {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			if (!Main.LocalPlayer.GetModPlayer<OperatorPlayer>().IsAdministrator) {
				Main.NewText(GetLocalizedMessage("CommandInfo.AdminOnly"), Color.Red);
				return;
			}

			var packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.AuditSystemMessage);
			packet.Write(COMMAND_TRANSLATE);
			packet.Write(Environment.NewLine);  // The server could have a different architecture than the requesting client
			packet.Write(_contentRequest);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.CommandAuditsNetcode))
				DebugMessage.Report(true, "Sent audit command packet {0} to the server with request ID {1}", GetByteCommandName(COMMAND_TRANSLATE), _contentRequest);

			Main.NewText(GetLocalizedMessage("Messages.Client.RequestSent"), Color.Yellow);
		}

		private static bool PrettifyAuditFile<T>(in T writer) where T : IWriteIntersceptor {
			// Since CancellationTokenSource can only be cancelled once, the variable is reset after cancelling
			// Copying the reference here allows for monitoring the object even after the variable has been reassigned
			var localSource = _cancelSource;
			string alternatePath = _lastKnownAuditPath + ".txt";

			string oldName = SetCurrentThreadName("MagicStorage Auditing");

			// Wait for any ongoing operations to finish
			while (_loading || _writing || _clearing)
				Thread.Yield();

			bool debug = DebugControls.Get(DebugControls.Names.AuditFile);

			if (debug) {
				DebugMessage.ReserveThreadContext();
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "[AUDIT] Starting translation of audit file with {0} entries", _file?.EntryCount ?? 0);
				DebugMessage.Indent();
				DebugMessage.RememberCurrentGroup();
			}

			try {
				writer.WriteLine("Magic Storage Audit Log");
				writer.WriteLine($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				writer.WriteLine($"World: {Main.ActiveWorldFileData?.Name ?? "Unknown"}");
				writer.WriteLine("=========================================================");
				writer.WriteLine();

				if (debug) {
					DebugMessage.Report(false, "Translating entries...");
					DebugMessage.Indent();
				}

				int i = 0;
				string align = null;
				string format = null;

				if (debug) {
					align = $"{{0,{_file.EntryCount.ToString().Length}}}";
					format = $"{align}: {{1}}";
				}

				foreach (var entry in _file.Entries) {
					entry.EvaluateParameters();

					if (debug) {
						DebugMessage.Report(false, format, i, entry.NetRepresentation());
						i++;
					}

					writer.WriteLine(entry.ToString());
				}

				writer.Flush();

				// Translation has finished
				PrintSuccess("Messages.TranslationSuccess", alternatePath);

				return true;
			} catch when (localSource.IsCancellationRequested) {
				// Translating was cancelled due to the file being cleared
				if (debug) {
					DebugMessage.RecallSavedGroup();
					DebugMessage.Report(true, "Audit file translation was cancelled");
					DebugMessage.RememberCurrentGroup();
				}

				try {
					File.Delete(alternatePath);
				} catch { }

				return false;
			} catch (Exception ex) {
				if (debug) {
					DebugMessage.RecallSavedGroup();
					DebugMessage.Report(true, "Exception occurred while translating audit file, translation failed will not be generated");
					DebugMessage.RememberCurrentGroup();
				}

				PrintError("Errors.TranslationFailed", ex);

				return false;
			} finally {
				if (debug) {
					DebugMessage.RecallSavedGroup();
					DebugMessage.EndReportGroup();
					DebugMessage.FreeThreadContext();
				}

				_printing = false;

				SetCurrentThreadName(oldName);
			}
		}

		private static void ServerPrettifyAuditFile() {
			StreamWriter writer = null;

			try {
				writer = new StreamWriter(_lastKnownAuditPath + ".txt", false, Encoding.UTF8);
				bool success = PrettifyAuditFile(new StreamWriterIntersceptor(writer));

				if (success && DebugControls.Get(DebugControls.Names.AuditFile)) {
					DebugMessage.Report(false, "Translated file is located at: {0}", _lastKnownAuditPath + ".txt");
					DebugMessage.EndReportGroup();
				}
			} finally {
				writer?.Dispose();
			}
		}

		private static void ReceivePrettifyAuditFileRequest(BinaryReader reader, int sender) {
			if (Main.netMode != NetmodeID.Server)
				return;

			string newline = reader.ReadString();
			int requestID = reader.ReadInt32();

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.CommandAuditsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(false, "Read environment newline: {0}", newline.Replace("\n", "LF").Replace("\r", "CR"));
				DebugMessage.Report(false, "Read request ID: {0}", requestID);
				DebugMessage.EndReportGroup();
			}

			if (_file is not null)
				new Task(LoadThenNetPrettifyAuditFile, new PacketIntersceptor(8192, newline, sender, requestID), _cancelSource.Token, TaskCreationOptions.LongRunning).Start();
		}

		private static void LoadThenNetPrettifyAuditFile(object state) {
			LoadAuditFile();
			bool success = PrettifyAuditFile((PacketIntersceptor)state);

			if (success && DebugControls.Get(DebugControls.Names.AuditFile)) {
				DebugMessage.Report(false, "Finished translating audit file for network transmission");
				DebugMessage.EndReportGroup();
			}
		}

		private static List<char[]> _contentBuffers;
		private static int _contentRequest;

		private static void ReceiveFileContent(BinaryReader reader) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			int incomingRequest = reader.ReadInt32();
			int bufferIndex = reader.ReadUInt16();
			int length = reader.ReadUInt16();
			char[] buffer = reader.ReadChars(length);

			bool debug = NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.CommandsNetcode);

			// Only use the data if no other request has been made since this packet's request
			if (_contentRequest == incomingRequest) {
				if (debug) {
					DebugMessage.BeginReportGroup();
					DebugMessage.Report(false, "Read request ID: {0}", incomingRequest);
					DebugMessage.Report(false, "Read buffer index: {0}", bufferIndex);
					DebugMessage.Report(false, "Read character count: {0}", length);
					DebugMessage.EndReportGroup();
				}

				_contentBuffers ??= [];

				while (_contentBuffers.Count <= bufferIndex)
					_contentBuffers.Add(null);  // Pad the list to ensure the index exists

				_contentBuffers[bufferIndex] = buffer;
			} else {
				if (debug)
					DebugMessage.Report(false, "Incoming buffer ID ({0}) does not match current request ID ({1}), ignoring buffer", incomingRequest, _contentRequest);
			}
		}

		private static void ReceiveEndOfFileContent(BinaryReader reader) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			int expectedBuffers = reader.ReadUInt16();
			string sourceWorldFile = reader.ReadString();

			if (NetHelper.CanDebugIncomingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.CommandsNetcode)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(false, "Read expected buffer count: {0}", expectedBuffers);
				DebugMessage.Report(false, "Read source world file name: {0}", sourceWorldFile);
				DebugMessage.EndReportGroup();
			}

			// The buffers have to exist and contain data
			if (_contentBuffers is null)
				throw new InvalidOperationException(GetLocalizedMessage("Errors.Client.NoBuffers"));
			if (_contentBuffers.Any(static b => b is null))
				throw new InvalidOperationException(GetLocalizedMessage("Errors.Client.IncompleteBuffers"));
			if (_contentBuffers.Count != expectedBuffers)
				throw new InvalidOperationException(GetLocalizedMessage("Errors.Client.MismatchedBufferCount", _contentBuffers.Count, expectedBuffers));

			// Write the content to the expected file
			string path = Path.Combine(Main.SavePath, "Worlds", sourceWorldFile + ".ms.audit.txt");

			using StreamWriter writer = new(path, false, Encoding.UTF8);

			foreach (var buffer in _contentBuffers)
				writer.Write(buffer);

			writer.Flush();

			Main.NewText(GetLocalizedMessage("Messages.Client.TranslationSuccess"), Color.Green);
			Main.NewText(path);

			// Indicate that the request has finished
			_contentBuffers = null;
			_contentRequest++;
		}

		public static void ClearAudits() {
			if (Main.netMode != NetmodeID.Server)
				return;

			if (!_clearing) {
				PrintInfo("Messages.ClearingStarted");

				if (DebugControls.Get(DebugControls.Names.AuditFile))
					DebugMessage.Report(true, "[AUDIT] Clearing audit file...");

				_clearing = true;
				_cancelSource.Cancel();
				_cancelSource = new CancellationTokenSource();  // Reassign the source to allow it to be potentially cancelled again

				new Task(ClearAuditFile, TaskCreationOptions.LongRunning).Start();
			} else
				PrintError("Errors.ClearingAlreadyRunning");
		}

		private static void ClearAuditFile() {
			// Wait for any ongoing operations to finish
			while (_loading || _writing || _printing)
				Thread.Yield();

			_file ??= new();

			string oldName = SetCurrentThreadName("MagicStorage Auditing");

			try {
				_file.ClearEverything();
				_lastSaveTime = DateTime.UtcNow - SaveInterval;  // Force a save
				_lastKnownAuditPath ??= AuditPath;  // Ensure that the path is set

				PrintSuccess("Messages.ClearingSuccess");
			} finally {
				_clearing = false;

				SetCurrentThreadName(oldName);
			}
		}

		public static void RequestAuditFileClear() {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			if (!Main.LocalPlayer.GetModPlayer<OperatorPlayer>().IsAdministrator) {
				Main.NewText(GetLocalizedMessage("CommandInfo.AdminOnly"), Color.Red);
				return;
			}

			var packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.AuditSystemMessage);
			packet.Write(COMMAND_CLEAR);
			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Any(DebugControls.Names.AnyAuditsNetcode, DebugControls.Names.CommandAuditsNetcode))
				DebugMessage.Report(true, "Sent audit command packet {0} to the server", GetByteCommandName(COMMAND_CLEAR));

			_contentBuffers = null;
			_contentRequest++;

			Main.NewText(GetLocalizedMessage("Messages.Client.RequestSent"), Color.Yellow);
		}
	}
}
