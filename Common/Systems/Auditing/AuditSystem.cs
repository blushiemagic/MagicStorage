using MagicStorage.Components;
using MagicStorage.Items;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.Auditing {
	internal class AuditSystem : ModSystem {
		private static bool _loading, _writing, _printing;
		private static AuditFile _file;
		private static ConcurrentQueue<AuditEntry> _queue = [];

		public static string AuditPath => Main.ActiveWorldFileData is null ? null : Path.ChangeExtension(Main.ActiveWorldFileData.Path, ".ms.audit");

		public override void PostUpdateWorld() {
			if (_loading || _writing || _printing)
				return;

			while (_queue.TryDequeue(out var entry))
				_file.AddEntry(entry);

			CheckSave();
		}

		internal static void HandlePacket(BinaryReader reader, int sender) {
			AuditAction action = (AuditAction)reader.ReadByte();
			int playerWhoAmI = reader.ReadByte();

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

		public static void ReportItemDeposit(Player player, TEStorageHeart heart, Item item) => Report(new DepositOne(_file, player, heart, item));
		public static void ReportItemDeposit(int playerWhoAmI, TEStorageHeart heart, Item item) => ReportItemDeposit(Main.player[playerWhoAmI], heart, item);
		public static void ReportItemDeposit(Player player, TEStorageHeart heart, ReducedItem item) => Report(new DepositOne(_file, player, heart, item));
		public static void ReportItemDeposit(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) => ReportItemDeposit(Main.player[playerWhoAmI], heart, item);
		
		public static void NetReportItemDepositOne(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.DepositOne, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write(item);
			packet.Send();
		}

		private static void ReceiveItemDepositOne(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			ReducedItem item = reader.ReadReducedItem();

			if (location.ResolveToTileEntity() is TEStorageHeart heart)
				ReportItemDeposit(playerWhoAmI, heart, item);
		}

		public static void ReportItemDeposit(Player player, TEStorageHeart heart, ReadOnlySpan<Item> items) => Report(new DepositMany(_file, player, heart, items));
		public static void ReportItemDeposit(int playerWhoAmI, TEStorageHeart heart, ReadOnlySpan<Item> items) => ReportItemDeposit(Main.player[playerWhoAmI], heart, items);
		public static void ReportItemDeposit(Player player, TEStorageHeart heart, ReadOnlySpan<ReducedItem> items) => Report(new DepositMany(_file, player, heart, items));
		public static void ReportItemDeposit(int playerWhoAmI, TEStorageHeart heart, ReadOnlySpan<ReducedItem> items) => ReportItemDeposit(Main.player[playerWhoAmI], heart, items);

		public static void NetReportItemDepositMany(int playerWhoAmI, TEStorageHeart heart, ReadOnlySpan<ReducedItem> items) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.DepositMany, playerWhoAmI);
			packet.Write(heart.Position);

			packet.Write7BitEncodedInt(items.Length);
			foreach (var item in items)
				packet.Write(item);

			packet.Send();
		}

		private static void ReceiveItemDepositMany(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			int count = reader.Read7BitEncodedInt();
			
			ReducedItem[] items = new ReducedItem[count];
			for (int i = 0; i < count; i++)
				items[i] = reader.ReadReducedItem();

			if (location.ResolveToTileEntity() is TEStorageHeart heart)
				ReportItemDeposit(playerWhoAmI, heart, items);
		}

		public static void ReportItemWithdraw(Player player, TEStorageHeart heart, Item item) => Report(new WithdrawOne(_file, player, heart, item));
		public static void ReportItemWithdraw(int playerWhoAmI, TEStorageHeart heart, Item item) => ReportItemWithdraw(Main.player[playerWhoAmI], heart, item);
		public static void ReportItemWithdraw(Player player, TEStorageHeart heart, ReducedItem item) => Report(new WithdrawOne(_file, player, heart, item));
		public static void ReportItemWithdraw(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) => ReportItemWithdraw(Main.player[playerWhoAmI], heart, item);

		public static void NetReportItemWithdrawOne(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.WithdrawOne, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write(item);
			packet.Send();
		}

		private static void ReceiveItemWithdrawOne(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			ReducedItem item = reader.ReadReducedItem();

			if (location.ResolveToTileEntity() is TEStorageHeart heart)
				ReportItemWithdraw(playerWhoAmI, heart, item);
		}

		public static void ReportItemWithdraw(Player player, TEStorageHeart heart, ReadOnlySpan<Item> items) => Report(new WithdrawMany(_file, player, heart, items));
		public static void ReportItemWithdraw(int playerWhoAmI, TEStorageHeart heart, ReadOnlySpan<Item> items) => ReportItemWithdraw(Main.player[playerWhoAmI], heart, items);
		public static void ReportItemWithdraw(Player player, TEStorageHeart heart, ReadOnlySpan<ReducedItem> items) => Report(new WithdrawMany(_file, player, heart, items));
		public static void ReportItemWithdraw(int playerWhoAmI, TEStorageHeart heart, ReadOnlySpan<ReducedItem> items) => ReportItemWithdraw(Main.player[playerWhoAmI], heart, items);

		public static void NetReportItemWithdrawMany(int playerWhoAmI, TEStorageHeart heart, ReadOnlySpan<ReducedItem> items) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.WithdrawMany, playerWhoAmI);
			packet.Write(heart.Position);

			packet.Write7BitEncodedInt(items.Length);
			foreach (var item in items)
				packet.Write(item);

			packet.Send();
		}

		private static void ReceiveItemWithdrawMany(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			int count = reader.Read7BitEncodedInt();

			ReducedItem[] items = new ReducedItem[count];
			for (int i = 0; i < count; i++)
				items[i] = reader.ReadReducedItem();

			if (location.ResolveToTileEntity() is TEStorageHeart heart)
				ReportItemWithdraw(playerWhoAmI, heart, items);
		}

		public static void ReportStorageUnitDeactivation(Player player, TEStorageUnit unit) => Report(new StorageUnitDeactivation(_file, player, unit));
		public static void ReportStorageUnitDeactivation(int playerWhoAmI, TEStorageUnit unit) => ReportStorageUnitDeactivation(Main.player[playerWhoAmI], unit);

		public static void NetReportStorageUnitDeactivation(int playerWhoAmI, TEStorageUnit unit) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.UnitDeactivate, playerWhoAmI);
			packet.Write(unit.Position);
			packet.Send();
		}

		private static void ReceiveStorageUnitDeactivation(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();

			if (location.ResolveToTileEntity() is TEStorageUnit unit)
				ReportStorageUnitDeactivation(playerWhoAmI, unit);
		}

		public static void ReportStorageUnitActivation(Player player, TEStorageUnit unit) => Report(new StorageUnitActivation(_file, player, unit));
		public static void ReportStorageUnitActivation(int playerWhoAmI, TEStorageUnit unit) => ReportStorageUnitActivation(Main.player[playerWhoAmI], unit);

		public static void NetReportStorageUnitActivation(int playerWhoAmI, TEStorageUnit unit) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.UnitActivate, playerWhoAmI);
			packet.Write(unit.Position);
			packet.Send();
		}

		private static void ReceiveStorageUnitActivation(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();

			if (location.ResolveToTileEntity() is TEStorageUnit unit)
				ReportStorageUnitActivation(playerWhoAmI, unit);
		}

		public static void ReportStorageUnitCoreRemoval(Player player, TEStorageUnit unit, BaseStorageCore core) => Report(new StorageUnitCoreRemoval(_file, player, unit, core));
		public static void ReportStorageUnitCoreRemoval(int playerWhoAmI, TEStorageUnit unit, BaseStorageCore core) => ReportStorageUnitCoreRemoval(Main.player[playerWhoAmI], unit, core);
		public static void ReportStorageUnitCoreRemoval(Player player, TEStorageUnit unit, ReducedItem item) => Report(new StorageUnitCoreRemoval(_file, player, unit, item));
		public static void ReportStorageUnitCoreRemoval(int playerWhoAmI, TEStorageUnit unit, ReducedItem item) => ReportStorageUnitCoreRemoval(Main.player[playerWhoAmI], unit, item);

		public static void NetReportStorageUnitCoreRemoval(int playerWhoAmI, TEStorageUnit unit, BaseStorageCore core) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;
			var packet = PreparePacket(AuditAction.UnitCoreRemove, playerWhoAmI);
			packet.Write(unit.Position);
			packet.Write(new ReducedItem(core.Item));
			packet.Send();
		}

		private static void ReceiveStorageUnitCoreRemoval(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			ReducedItem item = reader.ReadReducedItem();

			if (location.ResolveToTileEntity() is TEStorageUnit unit)
				ReportStorageUnitCoreRemoval(playerWhoAmI, unit, item);
		}

		public static void ReportStorageUnitCoreInsertion(Player player, TEStorageUnit unit, BaseStorageCore core) => Report(new StorageUnitCoreInsertion(_file, player, unit, core));
		public static void ReportStorageUnitCoreInsertion(int playerWhoAmI, TEStorageUnit unit, BaseStorageCore core) => ReportStorageUnitCoreInsertion(Main.player[playerWhoAmI], unit, core);
		public static void ReportStorageUnitCoreInsertion(Player player, TEStorageUnit unit, ReducedItem item) => Report(new StorageUnitCoreInsertion(_file, player, unit, item));
		public static void ReportStorageUnitCoreInsertion(int playerWhoAmI, TEStorageUnit unit, ReducedItem item) => ReportStorageUnitCoreInsertion(Main.player[playerWhoAmI], unit, item);

		public static void NetReportStorageUnitCoreInsertion(int playerWhoAmI, TEStorageUnit unit, BaseStorageCore core) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.UnitCoreInsert, playerWhoAmI);
			packet.Write(unit.Position);
			packet.Write(new ReducedItem(core.Item));
			packet.Send();
		}

		private static void ReceiveStorageUnitCoreInsertion(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			ReducedItem item = reader.ReadReducedItem();

			if (location.ResolveToTileEntity() is TEStorageUnit unit)
				ReportStorageUnitCoreInsertion(playerWhoAmI, unit, item);
		}

		public static void ReportMassItemSell(Player player, TEStorageHeart heart, int soldItemCount, long totalSellValue) => Report(new StorageControlSellItems(_file, player, heart, soldItemCount, totalSellValue));
		public static void ReportMassItemSell(int playerWhoAmI, TEStorageHeart heart, int soldItemCount, long totalSellValue) => ReportMassItemSell(Main.player[playerWhoAmI], heart, soldItemCount, totalSellValue);

		public static void NetReportMassItemSell(int playerWhoAmI, TEStorageHeart heart, int soldItemCount, long totalSellValue) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.SellItems, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write7BitEncodedInt(soldItemCount);
			packet.Write7BitEncodedInt64(totalSellValue);
			packet.Send();
		}

		private static void ReceiveMassItemSell(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			int soldItemCount = reader.Read7BitEncodedInt();
			long totalSellValue = reader.Read7BitEncodedInt64();

			if (location.ResolveToTileEntity() is TEStorageHeart heart)
				ReportMassItemSell(playerWhoAmI, heart, soldItemCount, totalSellValue);
		}

		public static void ReportItemDeletion(Player player, TEStorageHeart heart, ReducedItem item) => Report(new StorageControlDeleteItem(_file, player, heart, item));
		public static void ReportItemDeletion(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) => ReportItemDeletion(Main.player[playerWhoAmI], heart, item);

		public static void NetReportItemDeletion(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.DestroyItem, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write(item);
			packet.Send();
		}

		private static void ReceiveItemDeletion(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			ReducedItem item = reader.ReadReducedItem();

			if (location.ResolveToTileEntity() is TEStorageHeart heart)
				ReportItemDeletion(playerWhoAmI, heart, item);
		}

		public static void ReportCraftRequest(Player player, TEStorageHeart heart, ReadOnlySpan<Item> results, ReadOnlySpan<Item> consumedMaterials) => Report(new CraftRequest(_file, player, heart, results, consumedMaterials));
		public static void ReportCraftRequest(int playerWhoAmI, TEStorageHeart heart, ReadOnlySpan<Item> results, ReadOnlySpan<Item> consumedMaterials) => ReportCraftRequest(Main.player[playerWhoAmI], heart, results, consumedMaterials);
		public static void ReportCraftRequest(Player player, TEStorageHeart heart, ReadOnlySpan<ReducedItem> results, ReadOnlySpan<ReducedItem> consumedMaterials) => Report(new CraftRequest(_file, player, heart, results, consumedMaterials));
		public static void ReportCraftRequest(int playerWhoAmI, TEStorageHeart heart, ReadOnlySpan<ReducedItem> results, ReadOnlySpan<ReducedItem> consumedMaterials) => ReportCraftRequest(Main.player[playerWhoAmI], heart, results, consumedMaterials);

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

			if (location.ResolveToTileEntity() is TEStorageHeart heart)
				ReportCraftRequest(playerWhoAmI, heart, results, materials);
		}

		public static void ReportControlDeleteUnloadedItems(Player player, TEStorageHeart heart, int itemsAffected) => Report(new StorageControlDeleteUnloadedItems(_file, player, heart, itemsAffected));
		public static void ReportControlDeleteUnloadedItems(int playerWhoAmI, TEStorageHeart heart, int itemsAffected) => ReportControlDeleteUnloadedItems(Main.player[playerWhoAmI], heart, itemsAffected);

		public static void NetReportControlDeleteUnloadedItems(int playerWhoAmI, TEStorageHeart heart, int itemsAffected) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.ControlDeleteUnloadedItems, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write7BitEncodedInt(itemsAffected);
			packet.Send();
		}

		private static void ReceiveControlDeleteUnloadedItems(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			int itemsAffected = reader.Read7BitEncodedInt();

			if (location.ResolveToTileEntity() is TEStorageHeart heart)
				ReportControlDeleteUnloadedItems(playerWhoAmI, heart, itemsAffected);
		}

		public static void ReportControlDeleteUnloadedData(Player player, TEStorageHeart heart, int itemsAffected) => Report(new StorageControlDeleteUnloadedData(_file, player, heart, itemsAffected));
		public static void ReportControlDeleteUnloadedData(int playerWhoAmI, TEStorageHeart heart, int itemsAffected) => ReportControlDeleteUnloadedData(Main.player[playerWhoAmI], heart, itemsAffected);

		public static void NetReportControlDeleteUnloadedData(int playerWhoAmI, TEStorageHeart heart, int itemsAffected) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.ControlDeleteUnloadedData, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write7BitEncodedInt(itemsAffected);
			packet.Send();
		}

		private static void ReceiveControlDeleteUnloadedData(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			int itemsAffected = reader.Read7BitEncodedInt();

			if (location.ResolveToTileEntity() is TEStorageHeart heart)
				ReportControlDeleteUnloadedData(playerWhoAmI, heart, itemsAffected);
		}

		public static void ReportControlCoinCompacting(Player player, TEStorageHeart heart) => Report(new StorageControlCompactCoins(_file, player, heart));
		public static void ReportControlCoinCompacting(int playerWhoAmI, TEStorageHeart heart) => ReportControlCoinCompacting(Main.player[playerWhoAmI], heart);

		public static void NetReportControlCoinCompacting(int playerWhoAmI, TEStorageHeart heart) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.ControlCompactCoins, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Send();
		}

		private static void ReceiveControlCoinCompacting(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();

			if (location.ResolveToTileEntity() is TEStorageHeart heart)
				ReportControlCoinCompacting(playerWhoAmI, heart);
		}

		public static void ReportRemoteAccessLink(Player player, TEStorageHeart heart, TERemoteAccess access) => Report(new LinkRemoteAccess(_file, player, heart, access));
		public static void ReportRemoteAccessLink(int playerWhoAmI, TEStorageHeart heart, TERemoteAccess access) => ReportRemoteAccessLink(Main.player[playerWhoAmI], heart, access);

		public static void NetReportRemoteAccessLink(int playerWhoAmI, TEStorageHeart heart, TERemoteAccess access) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.LinkRemoteAccess, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write(access.Position);
			packet.Send();
		}

		private static void ReceiveRemoteAccessLink(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			Point16 accessLocation = reader.ReadPoint16();

			if (location.ResolveToTileEntity() is TEStorageHeart heart && accessLocation.ResolveToTileEntity() is TERemoteAccess access)
				ReportRemoteAccessLink(playerWhoAmI, heart, access);
		}

		public static void ReportPortableAccessLink(Player player, TEStorageHeart heart, PortableAccess item) => Report(new LinkPortableAccess(_file, player, heart, item));
		public static void ReportPortableAccessLink(int playerWhoAmI, TEStorageHeart heart, PortableAccess item) => ReportPortableAccessLink(Main.player[playerWhoAmI], heart, item);
		public static void ReportPortableAccessLink(Player player, TEStorageHeart heart, ReducedItem item) => Report(new LinkPortableAccess(_file, player, heart, item));
		public static void ReportPortableAccessLink(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) => ReportPortableAccessLink(Main.player[playerWhoAmI], heart, item);
		public static void ReportPortableAccessLink(Player player, TECraftingAccess access, PortableCraftingAccess item) => Report(new LinkPortableAccess(_file, player, access, item));
		public static void ReportPortableAccessLink(int playerWhoAmI, TECraftingAccess access, PortableCraftingAccess item) => ReportPortableAccessLink(Main.player[playerWhoAmI], access, item);
		public static void ReportPortableAccessLink(Player player, TECraftingAccess access, ReducedItem item) => Report(new LinkPortableAccess(_file, player, access, item));
		public static void ReportPortableAccessLink(int playerWhoAmI, TECraftingAccess access, ReducedItem item) => ReportPortableAccessLink(Main.player[playerWhoAmI], access, item);

		public static void NetReportPortableAccessLink(int playerWhoAmI, TEStorageHeart heart, ReducedItem item) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.LinkPortableAccess, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write(item);
			packet.Send();
		}

		public static void NetReportPortableAccessLink(int playerWhoAmI, TECraftingAccess access, ReducedItem item) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.LinkPortableAccess, playerWhoAmI);
			packet.Write(access.Position);
			packet.Write(item);
			packet.Send();
		}

		private static void ReceivePortableAccessLink(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			ReducedItem item = reader.ReadReducedItem();

			var entity = location.ResolveToTileEntity();
			if (entity is TEStorageHeart heart)
				ReportPortableAccessLink(playerWhoAmI, heart, item);
			else if (entity is TECraftingAccess access)
				ReportPortableAccessLink(playerWhoAmI, access, item);
		}

		public static void ReportSecurityNetworkAssignment(Player player, TEStorageHeart heart, int assignedNetworkID) => Report(new SecurityNetworkAssignment(_file, player, heart, assignedNetworkID));
		public static void ReportSecurityNetworkAssignment(int playerWhoAmI, TEStorageHeart heart, int assignedNetworkID) => ReportSecurityNetworkAssignment(Main.player[playerWhoAmI], heart, assignedNetworkID);

		public static void NetReportSecurityNetworkAssignment(int playerWhoAmI, TEStorageHeart heart, int assignedNetworkID) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.SecurityNetworkAssignment, playerWhoAmI);
			packet.Write(heart.Position);
			packet.Write(assignedNetworkID);
			packet.Send();
		}

		private static void ReceiveSecurityNetworkAssignment(BinaryReader reader, int playerWhoAmI) {
			Point16 location = reader.ReadPoint16();
			int assignedNetworkID = reader.ReadInt32();

			if (location.ResolveToTileEntity() is TEStorageHeart heart)
				ReportSecurityNetworkAssignment(playerWhoAmI, heart, assignedNetworkID);
		}

		public static void ReportSecurityNetworkModification(Player player, int networkID, string oldPassword, bool oldRestricted, string newPassword, bool newRestricted) => Report(new SecurityNetworkModification(_file, player, networkID, oldPassword, newPassword, oldRestricted, newRestricted));
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

			ReportSecurityNetworkModification(playerWhoAmI, networkID, oldPassword, oldRestricted, newPassword, newRestricted);
		}

		public static void ReportSecurityNetworkDeletion(Player player, int networkID) => Report(new SecurityNetworkDeletion(_file, player, networkID));
		public static void ReportSecurityNetworkDeletion(int playerWhoAmI, int networkID) => ReportSecurityNetworkDeletion(Main.player[playerWhoAmI], networkID);

		public static void NetReportSecurityNetworkDeletion(int playerWhoAmI, int networkID) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.SecurityNetworkDelete, playerWhoAmI);
			packet.Write(networkID);
			packet.Send();
		}

		private static void ReceiveSecurityNetworkDeletion(BinaryReader reader, int playerWhoAmI) {
			int networkID = reader.ReadInt32();

			ReportSecurityNetworkDeletion(playerWhoAmI, networkID);
		}

		public static void ReportSecurityNetworkJoin(Player player, int networkID) => Report(new SecurityNetworkJoin(_file, player, networkID));
		public static void ReportSecurityNetworkJoin(int playerWhoAmI, int networkID) => ReportSecurityNetworkJoin(Main.player[playerWhoAmI], networkID);

		public static void NetReportSecurityNetworkJoin(int playerWhoAmI, int networkID) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.SecurityNetworkJoin, playerWhoAmI);
			packet.Write(networkID);
			packet.Send();
		}

		private static void ReceiveSecurityNetworkJoin(BinaryReader reader, int playerWhoAmI) {
			int networkID = reader.ReadInt32();

			ReportSecurityNetworkJoin(playerWhoAmI, networkID);
		}

		public static void ReportAdministratorStatusAssignment(Player player) => Report(new StatusAdministratorAssignment(_file, player));
		public static void ReportAdministratorStatusAssignment(int playerWhoAmI) => ReportAdministratorStatusAssignment(Main.player[playerWhoAmI]);

		public static void NetReportAdministratorStatusAssignment(int playerWhoAmI) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.StatusServerAdmin, playerWhoAmI);
			packet.Send();
		}

		private static void ReceiveAdministratorStatusAssignment(BinaryReader reader, int playerWhoAmI) => ReportAdministratorStatusAssignment(playerWhoAmI);

		public static void ReportOperatorStatusAssignment(Player player) => Report(new StatusOperatorAssignment(_file, player));
		public static void ReportOperatorStatusAssignment(int playerWhoAmI) => ReportOperatorStatusAssignment(Main.player[playerWhoAmI]);

		public static void NetReportOperatorStatusAssignment(int playerWhoAmI) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.StatusServerOperatorGranted, playerWhoAmI);
			packet.Send();
		}

		private static void ReceiveOperatorStatusAssignment(BinaryReader reader, int playerWhoAmI) => ReportOperatorStatusAssignment(playerWhoAmI);

		public static void ReportOperatorStatusRemoval(Player player) => Report(new StatusOperatorRemoval(_file, player));
		public static void ReportOperatorStatusRemoval(int playerWhoAmI) => ReportOperatorStatusRemoval(Main.player[playerWhoAmI]);

		public static void NetReportOperatorStatusRemoval(int playerWhoAmI) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			var packet = PreparePacket(AuditAction.StatusServerOperatorRemoved, playerWhoAmI);
			packet.Send();
		}

		private static void ReceiveOperatorStatusRemoval(BinaryReader reader, int playerWhoAmI) => ReportOperatorStatusRemoval(playerWhoAmI);

		private static ModPacket PreparePacket(AuditAction action, int player) {
			var packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.AuditSystemMessage);
			packet.Write((byte)action);
			packet.Write((byte)player);
			return packet;
		}

		private static void CheckLoad() {
			if (_file is null && !_loading) {
				_loading = true;
				new Task(LoadAuditFile, TaskCreationOptions.LongRunning).Start();
			}
		}

		private static void LoadAuditFile() {
			string path = AuditPath;
			AuditFile file = null;

			try {
				if (File.Exists(path)) {
					// Attempt to load the file
					using FileStream stream = File.OpenRead(path);
					using BinaryReader reader = new(stream);

					file = new();
					AuditFile.DeserializeOne(reader, ref file);
				}
			} catch (Exception ex) {
				MagicStorageMod.Instance.Logger.Error("Failed to load audit file", ex);
				file = null;
			} finally {
				_file = file ?? new();
				_loading = false;
			}
		}

		private static void CheckSave() {
			if (_file is not null && !_loading && _file.HasChanges && !_writing && !_printing) {
				_writing = true;
				new Task(SaveAuditFile, TaskCreationOptions.LongRunning).Start();
			}
		}

		private static void SaveAuditFile() {
			try {
				using FileStream stream = File.Create(AuditPath);
				using BinaryWriter writer = new(stream);

				_file.Serialize(writer);
			} catch (Exception ex) {
				MagicStorageMod.Instance.Logger.Error("Failed to save audit file", ex);
				_file.ForceNoChanges();
			} finally {
				_writing = false;
			}
		}

		public static void DeconstructAuditFile() {
			if (_file is not null) {
				_printing = true;
				new Task(PrettifyAuditFile, TaskCreationOptions.LongRunning).Start();
			}
		}

		private static void PrettifyAuditFile() {
			while (_loading)
				Thread.Yield();

			while (_writing)
				Thread.Yield();

			try {
				string alternatePath = Path.ChangeExtension(AuditPath, ".ms.audit.txt");
				using StreamWriter writer = new(alternatePath, false, Encoding.UTF8);

				writer.WriteLine("Magic Storage Audit Log");
				writer.WriteLine($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				writer.WriteLine($"World: {Main.ActiveWorldFileData?.Name ?? "Unknown"}");
				writer.WriteLine("=========================================================");
				writer.WriteLine();

				foreach (var entry in _file.Entries)
					writer.WriteLine(entry);
			} catch (Exception ex) {
				MagicStorageMod.Instance.Logger.Error("Failed to write deconstructed audit file", ex);
			} finally {
				_printing = false;
			}
		}
	}
}
