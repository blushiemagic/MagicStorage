using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Collections.Concurrent;
using System.Reflection;
using Terraria.ModLoader.Default;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using System;
using MagicStorage.Common.Systems;
using System.Collections;
using MagicStorage.Common;
using System.Runtime.CompilerServices;
using MagicStorage.Common.Players;
using MagicStorage.Common.Systems.Auditing;
using System.Runtime.InteropServices;
using MagicStorage.UI.States;
using MagicStorage.CrossMod;
using MagicStorage.UI;

namespace MagicStorage.Components
{
	public class TEStorageHeart : TEStorageCenter
	{
		public enum Operation : byte
		{
			Withdraw,
			WithdrawToInventory,
			Deposit,
			DepositAll,
			WithdrawAllAndDestroy,  //Withdraws without actually putting the items in the player's inventory, effectively destroying the items
			DeleteUnloadedGlobalItemData,
			WithdrawThenTryModuleInventory,
			WithdrawToInventoryThenTryModuleInventory
		}

		private class NetOperation
		{
			public NetOperation(Operation _type, Item _item, bool _keepOneInFavorite, int _client)
			{
				type = _type;
				item = _item;
				keepOneInFavorite = _keepOneInFavorite;
				client = _client;
			}

			public NetOperation(Operation _type, Item _item, int _client = -1)
			{
				type = _type;
				item = _item;
				client = _client;
			}

			public NetOperation(Operation _type, List<Item> _items, int _client)
			{
				type = _type;
				items = _items;
				client = _client;
			}

			public Operation type { get; }
			public Item item { get; }
			public List<Item> items { get; }
			public bool keepOneInFavorite { get; }
			public int client { get; }

			public int? AccessingPlayer { get; set; }
		}

		ConcurrentQueue<NetOperation> clientOpQ = new ConcurrentQueue<NetOperation>();
		internal bool compactCoins = false;
	//	private const int UNIQUE_ITEM_HISTORY_SIZE = StorageGUI.RECENT_FILTER_ITEM_COUNT + 30;
	//	private readonly ItemTypeOrderedSet _uniqueItemsPutHistory = new("UniqueItemsPutHistory") { MemoryLimit = UNIQUE_ITEM_HISTORY_SIZE };
		private readonly ItemTypeOrderedSet _uniqueItemsPutHistory = new("UniqueItemsPutHistory");
		private int compactStage;

		[Obsolete("Use ComponentManager.GetRemoteAccesses() instead", true)]
		public HashSet<Point16> remoteAccesses = new();
		[Obsolete]
		internal List<Point16> Obsolete_remoteAccesses() => storageUnits;

		[Obsolete("Use ComponentManager.GetEnvironmentAccesses() instead", true)]
		public HashSet<Point16> environmentAccesses = new();
		[Obsolete]
		internal List<Point16> Obsolete_environmentAccesses() => storageUnits;
		
		private int updateTimer = 60;

		internal bool[] clientUsingHeart = new bool[Main.maxPlayers];

		public bool IsAlive { get; private set; } = true;

		public string storageName;

		internal bool netcodeUpdate;
		internal int netDesync;

		public IEnumerable<Item> UniqueItemsPutHistory => _uniqueItemsPutHistory.Items;
		private int requestingHistory;
		private List<int[]> _workingHistory;
		internal bool hasDepositHistory;
		internal bool requestingDepositHistory;

		public override void OnKill()
		{
			base.OnKill();  // NOTE: very important!  TEStorageCenter.OnKill() handles disconnecting the components in the network
			IsAlive = false;
		}

		public override bool ValidTile(in Tile tile) => tile.TileType == ModContent.TileType<StorageHeart>() && tile.TileFrameX == 0 && tile.TileFrameY == 0;

		public override TEStorageHeart GetHeart() => this;

		public bool AnyClientUsingThis() {
			for (int i = 0; i < Main.maxPlayers; i++) {
				if (!Main.player[i].active) {
					clientUsingHeart[i] = false;
					continue;
				}

				if (clientUsingHeart[i]) {
					NetHelper.Report(false, $"Client {i} is currently using this Storage Heart entity");
					return true;
				}
			}

			return false;
		}

		public void LockOnCurrentClient() {
			NetHelper.Report(true, $"Locking storage heart at X={Position.X}, Y={Position.Y}");

			clientUsingHeart[Main.myPlayer] = true;
			NetHelper.ClientInformStorageHeartUsage(this);
		}

		public void UnlockOnCurrentClient() {
			NetHelper.Report(true, $"Unlocking storage heart at X={Position.X}, Y={Position.Y}");

			clientUsingHeart[Main.myPlayer] = false;
			NetHelper.ClientInformStorageHeartUsage(this);
		}

		public IEnumerable<TEAbstractStorageUnit> GetStorageUnits()
		{
			ConnectedComponentManager manager = ComponentManager;

			IEnumerable<TEAbstractStorageUnit> remoteStorageUnits = manager.GetRemoteAccessEntities().SelectMany(remoteAccess => remoteAccess.ComponentManager.GetStorageUnitEntities());

			return manager.GetStorageUnitEntities().Concat(remoteStorageUnits);
		}

		public IEnumerable<TEEnvironmentAccess> GetEnvironmentSimulators() => ComponentManager.GetEnvironmentAccessEntities();

		public IEnumerable<EnvironmentModule> GetModules()
			=> GetEnvironmentSimulators()
				.SelectMany(e => e.Modules)
				.DistinctBy(m => m.Type);

		public IEnumerable<Item> GetStoredItems()
		{
			return GetStorageUnits().SelectMany(storageUnit => storageUnit.GetItems()).Where(static i => !i.IsAir);
		}

		protected override void OnConnectComponent(TEStorageComponent component) {
			base.OnConnectComponent(component);
			if (component is TERemoteAccess)
				Obsolete_remoteAccesses().Add(component.Position);
			else if (component is TEEnvironmentAccess)
				Obsolete_environmentAccesses().Add(component.Position);
		}

		protected override void OnDisconnectComponent(TEStorageComponent component) {
			base.OnDisconnectComponent(component);
			if (component is TERemoteAccess)
				Obsolete_remoteAccesses().Remove(component.Position);
			else if (component is TEEnvironmentAccess)
				Obsolete_environmentAccesses().Remove(component.Position);
		}

		public override void Update()
		{
			base.Update();

			if (Main.netMode == NetmodeID.Server && processClientOperations(out bool forcedRefresh, out HashSet<int> typesToRefresh))
			{
				NetHelper.SendRefreshNetworkItems(Position, forcedRefresh, typesToRefresh);
			}

			updateTimer++;
			if (updateTimer >= 60)
			{
				updateTimer = 0;
				if (compactCoins)
				{
					CompactCoins();
					compactCoins = false;
				}
				CompactOne();
			}
		}

		private bool processClientOperations(out bool forcedRefresh, out HashSet<int> typesToRefresh)
		{
			int opCount = clientOpQ.Count;
			bool networkRefresh = false;
			
			forcedRefresh = false;
			typesToRefresh = new();

			for (int i = 0; i < opCount; ++i)
			{
				NetOperation op;
				if (clientOpQ.TryDequeue(out op))
				{
					SecuritySystem.AccessContext context = default;
					bool hasContext = false;
					if (op.AccessingPlayer is int plr) {
						hasContext = true;
						context = SecuritySystem.CreateAccessContext(plr);
					}

					networkRefresh = true;
					if (op.type == Operation.Withdraw || op.type == Operation.WithdrawToInventory)
					{
						typesToRefresh.Add(op.item.type);
						Item item = Withdraw(op.item, op.keepOneInFavorite);
						if (!item.IsAir)
						{
							ModPacket packet = PrepareServerResult(op.type);
							ItemIO.Send(item, packet, true, true);
							packet.Send(op.client);

							AuditSystem.ReportItemWithdraw(op.client, this, item);
						}
					}
					else if (op.type == Operation.Deposit)
					{
						ReducedItem netItem = new(op.item);

						typesToRefresh.Add(op.item.type);
						DepositItem(op.item);
						if (!op.item.IsAir)
						{
							ModPacket packet = PrepareServerResult(op.type);
							ItemIO.Send(op.item, packet, true, true);
							packet.Send(op.client);
						}

						if (op.item.stack != netItem.Stack)
							AuditSystem.ReportItemDeposit(op.client, this, netItem.WithStack(netItem.Stack - op.item.stack));
					}
					else if (op.type == Operation.DepositAll)
					{
						NetHelper.StartUpdateQueue();
						List<Item> leftOvers = new List<Item>();
						List<ReducedItem> netItems = new List<ReducedItem>();
						foreach (Item item in op.items)
						{
							ReducedItem netItem = new(item);

							typesToRefresh.Add(item.type);
							DepositItem(item);
							if (!item.IsAir)
							{
								leftOvers.Add(item);
							}

							if (item.stack != netItem.Stack)
								netItems.Add(netItem.WithStack(netItem.Stack - item.stack));
						}
						NetHelper.ProcessUpdateQueue();

						if (leftOvers.Count > 0)
						{
							ModPacket packet = PrepareServerResult(op.type);
							packet.Write(leftOvers.Count);
							foreach (Item item in leftOvers)
							{
								ItemIO.Send(item, packet, true, true);
							}
							packet.Send(op.client);
						}

						if (netItems.Count > 0)
							AuditSystem.ReportItemDeposit(op.client, this, [.. netItems]);
					}
					else if (op.type == Operation.WithdrawAllAndDestroy)
					{
						WithdrawManyAndDestroy(op.item.type, out int itemsDestroyed);

						if (HasItem(op.item, true))
						{
							ModPacket packet = PrepareServerResult(op.type);
							packet.Write(op.item.type);
							packet.Send();

							forcedRefresh = true;
						}

						if (itemsDestroyed > 0) {
							if (op.item.type == ModContent.ItemType<UnloadedItem>())
								AuditSystem.ReportControlDeleteUnloadedItems(op.client, this, itemsDestroyed);
							else
								AuditSystem.ReportItemDeletion(op.client, this, new ReducedItem(op.item.type, itemsDestroyed));
						}
					}
					else if (op.type == Operation.DeleteUnloadedGlobalItemData)
					{
						DestroyUnloadedGlobalItemData(out int itemsAffected);

						ModPacket packet = PrepareServerResult(op.type);
						packet.Send();

						forcedRefresh = true;

						if (itemsAffected > 0)
							AuditSystem.ReportControlDeleteUnloadedData(op.client, this, itemsAffected);
					}
					else if (op.type == Operation.WithdrawThenTryModuleInventory || op.type == Operation.WithdrawToInventoryThenTryModuleInventory)
					{
						typesToRefresh.Add(op.item.type);
						int stack = op.item.stack;
						Item item = Withdraw(op.item, false);

						ModPacket packet = PrepareServerResult(op.type);
						ItemIO.Send(item, packet, true, true);
						packet.Write(stack);
						packet.Send(op.client);

						if (!item.IsAir)
							AuditSystem.ReportItemWithdraw(op.client, this, item);
					}

					if (hasContext)
						context.Dispose();
				}
			}

			if (forcedRefresh)
				typesToRefresh = null;

			return networkRefresh;
		}

		public void QClientOperation(BinaryReader reader, Operation op, int client)
		{
			NetOperation netOp = null;

			if (op == Operation.Withdraw || op == Operation.WithdrawToInventory)
			{
				bool keepOneIfFavorite = reader.ReadBoolean();
				Item item = ItemIO.Receive(reader, true, true);
				netOp = new NetOperation(op, item, keepOneIfFavorite, client);
			}
			else if (op == Operation.Deposit)
			{
				Item item = ItemIO.Receive(reader, true, true);
				netOp = new NetOperation(op, item, client);
			}
			else if (op == Operation.DepositAll)
			{
				int count = reader.ReadByte();
				List<Item> items = new();
				for (int k = 0; k < count; k++)
				{
					Item item = ItemIO.Receive(reader, true, true);
					items.Add(item);
				}
				netOp = new NetOperation(op, items, client);
			}
			else if (op == Operation.WithdrawAllAndDestroy)
			{
				int type = reader.ReadInt32();
				Item dummy = new Item(type);
				netOp = new NetOperation(op, dummy, client);
			}
			else if (op == Operation.DeleteUnloadedGlobalItemData)
			{
				netOp = new NetOperation(op, (Item)null, client);
			}
			else if (op == Operation.WithdrawThenTryModuleInventory || op == Operation.WithdrawToInventoryThenTryModuleInventory)
			{
				Item item = ItemIO.Receive(reader, true, true);
				netOp = new NetOperation(op, item, false, client);
			}

			if (netOp is not null) {
				if (SecuritySystem.TryGetCurrentAccessContext(out var context))
					netOp.AccessingPlayer = context.Player;

				clientOpQ.Enqueue(netOp);
			}
		}

		internal static ModPacket PrepareServerResult(Operation op)
		{
			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ServerStorageResult);
			packet.Write((byte)op);
			return packet;
		}

		internal ModPacket PrepareClientRequest(Operation op)
		{
			ModPacket packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ClinetStorageOperation);
			packet.Write(Position.X);
			packet.Write(Position.Y);
			packet.Write((byte)op);
			packet.WriteSecurityAccess();

			return packet;
		}

		public void CompactCoins()
		{
			if (!SecuritySystem.AccessibleFromContext(assignedNetwork)) {
				NetHelper.Report(true, $"[TEStorageHeart] Access denied for accessing player {(SecuritySystem.TryGetCurrentAccessContext(out var context) ? context.Player : -1)} at {Position}");
				return;
			}

			Dictionary<int, int> coinsQty = new Dictionary<int, int>();
			coinsQty.Add(ItemID.CopperCoin, 0);
			coinsQty.Add(ItemID.SilverCoin, 0);
			coinsQty.Add(ItemID.GoldCoin, 0);
			coinsQty.Add(ItemID.PlatinumCoin, 0);
			foreach (Item item in GetStoredItems())
			{
				if (item.IsACoin && coinsQty.ContainsKey(item.type))
				{
					coinsQty[item.type] += item.stack;
				}
			}

			int[] coinTypes = coinsQty.Keys.ToArray();
			for (int i = 0; i < coinTypes.Length - 1; i++)
			{
				int coin = coinTypes[i];
				int coinQty = coinsQty[coin];
				if (coinQty >= 200)
				{
					coinQty -= 100;
					int exchangeCoin = coinTypes[i + 1];
					int exchangedQty = coinQty / 100;
					coinsQty[exchangeCoin] += exchangedQty;

					Item tempCoin = new();
					tempCoin.SetDefaults(coin);
					tempCoin.stack = exchangedQty * 100;
					TryWithdraw(tempCoin, false);

					tempCoin.SetDefaults(exchangeCoin);
					tempCoin.stack = exchangedQty;
					DepositItem(tempCoin);
				}
			}
		}

		public void CompactOne()
		{
			if (compactStage == 0)
				EmptyInactive();
			else if (compactStage == 1)
				Defragment();
			else if (compactStage == 2)
				PackItems();
		}

		public bool EmptyInactive()
		{
			TEStorageUnit inactiveUnit = GetStorageUnits().OfType<TEStorageUnit>().FirstOrDefault(unit => unit.Inactive && !unit.IsEmpty);

			if (inactiveUnit is null)
			{
				compactStage++;
				return false;
			}

			foreach (TEAbstractStorageUnit abstractStorageUnit in GetStorageUnits())
				if (abstractStorageUnit is TEStorageUnit { Inactive: false, IsEmpty: true } storageUnit && inactiveUnit.NumItems <= storageUnit.Capacity)
				{
					TEStorageUnit.SwapItems(inactiveUnit, storageUnit);
					NetHelper.SendRefreshNetworkItems(Position, false, storageUnit.items.Select(static i => i.type));
					return true;
				}

			bool hasChange = false;
			NetHelper.StartUpdateQueue();
			Item tryMove = inactiveUnit.WithdrawStack();

			HashSet<int> typesToRefresh = new();

			foreach (TEStorageUnit storageUnit in GetStorageUnits().OfType<TEStorageUnit>().Where(unit => !unit.Inactive))
				while (storageUnit.HasSpaceFor(tryMove) && !tryMove.IsAir)
				{
					typesToRefresh.Add(tryMove.type);

					storageUnit.DepositItem(tryMove);
					if (tryMove.IsAir && !inactiveUnit.IsEmpty)
						tryMove = inactiveUnit.WithdrawStack();
					hasChange = true;
				}

			if (!tryMove.IsAir) {
				typesToRefresh.Add(tryMove.type);
				inactiveUnit.DepositItem(tryMove);
			}

			NetHelper.ProcessUpdateQueue();

			if (hasChange)
				NetHelper.SendRefreshNetworkItems(Position, false, typesToRefresh);
			else
				compactStage++;

			return hasChange;
		}

		public bool Defragment()
		{
			TEStorageUnit emptyUnit = null;
			foreach (TEAbstractStorageUnit abstractStorageUnit in GetStorageUnits())
			{
				if (abstractStorageUnit is not TEStorageUnit storageUnit)
					continue;
				if (emptyUnit is null && storageUnit.IsEmpty && !storageUnit.Inactive)
				{
					emptyUnit = storageUnit;
				}
				else if (emptyUnit is not null && !storageUnit.IsEmpty && storageUnit.NumItems <= emptyUnit.Capacity)
				{
					TEStorageUnit.SwapItems(emptyUnit, storageUnit);
					NetHelper.SendRefreshNetworkItems(Position, false, storageUnit.items.Select(static i => i.type));
					return true;
				}
			}

			compactStage++;
			return false;
		}

		public bool PackItems()
		{
			//Pack items within the storage units first
			NetHelper.StartUpdateQueue();
			foreach (TEAbstractStorageUnit abstractStorageUnit in GetStorageUnits()) {
				if (abstractStorageUnit is not TEStorageUnit storageUnit)
					continue;

				storageUnit.PackItems();
			}
			NetHelper.ProcessUpdateQueue();

			NetHelper.StartUpdateQueue();
			int index = -1, index2 = -1;
			foreach (TEAbstractStorageUnit abstractStorageUnit in GetStorageUnits())
			{
				index++;

				if (abstractStorageUnit is not TEStorageUnit storageUnit)
					continue;

				//Ignore inactive units as the destination
				if (storageUnit.Inactive)
					continue;

				foreach (TEAbstractStorageUnit abstractStorageUnit2 in GetStorageUnits())
				{
					index2++;

					//Only flatten to units closer to the heart
					if (index2 < index)
						continue;

					if (abstractStorageUnit2 is not TEStorageUnit storageUnit2)
						continue;
					
					//Don't check a unit against itself
					if (storageUnit.Position == storageUnit2.Position)
						continue;

					//Ignore empty units
					if (storageUnit2.IsEmpty)
						continue;

					if (!storageUnit.FlattenFrom(storageUnit2, out List<Item> transferredItems))
						continue;

					NetHelper.Report(true, $"Items flattened between units {storageUnit.ID} and {storageUnit2.ID}");

					NetHelper.ProcessUpdateQueue();
					NetHelper.SendRefreshNetworkItems(Position, false, transferredItems.Select(static i => i.type).Distinct());
					return true;
				}

				index2 = -1;
			}

			NetHelper.ProcessUpdateQueue();
			NetHelper.SendRefreshNetworkItems(Position, forceFullRefresh: true);

			compactStage++;
			return false;
		}

		public void ResetCompactStage(int stage = 0)
		{
			if (stage < compactStage)
				compactStage = stage;
		}

		public void DepositItem(Item toDeposit)
		{
			if (!SecuritySystem.AccessibleFromContext(assignedNetwork)) {
				NetHelper.Report(true, $"[TEStorageHeart] Access denied for accessing player {(SecuritySystem.TryGetCurrentAccessContext(out var context) ? context.Player : -1)} at {Position}");
				return;
			}

			bool actualItem = !toDeposit.IsAir;
			int oldStack = toDeposit.stack;
			int remember = toDeposit.type;
			foreach (TEAbstractStorageUnit storageUnit in GetStorageUnits())
				if (!storageUnit.Inactive && storageUnit.HasSpaceInStackFor(toDeposit))
				{
					storageUnit.DepositItem(toDeposit);
					if (toDeposit.IsAir)
						goto MakeTheUIRefresh;
				}

			bool prevNewAndShiny = toDeposit.newAndShiny;
			toDeposit.newAndShiny = MagicStorageConfig.GlowNewItems && !_uniqueItemsPutHistory.Contains(toDeposit);
			foreach (TEAbstractStorageUnit storageUnit in GetStorageUnits())
				if (!storageUnit.Inactive && !storageUnit.IsFull)
				{
					storageUnit.DepositItem(toDeposit);
					if (toDeposit.IsAir)
					{
						_uniqueItemsPutHistory.Add(remember);

						if (Main.netMode == NetmodeID.Server)
							NetHelper.SendDepositHistoryUpdate(this, additions: [ remember ], removals: null);

						goto MakeTheUIRefresh;
					}
				}

			toDeposit.newAndShiny = prevNewAndShiny;

			MakeTheUIRefresh:
			if (oldStack != toDeposit.stack) {
				if (actualItem && StoragePlayer.IsClientViewingHeart(this))
					MagicUI.SetNextCollectionsToRefresh(remember);

				ResetCompactStage();
			}
		}

		public void TryDeposit(Item item)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = PrepareClientRequest(Operation.Deposit);
				ItemIO.Send(item, packet, true, true);
				packet.Send();
				item.SetDefaults(0, true);

				netcodeUpdate = true;
				netDesync = 0;
			}
			else
			{
				DepositItem(item);
			}
		}

		public void TryDeposit(Item item, Player accessingPlayer)
		{
			using var _ = SecuritySystem.CreateAccessContext(accessingPlayer.whoAmI);
			TryDeposit(item);
		}

		public bool TryDeposit(List<Item> items)
		{
			bool changed = false;
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				int size = byte.MaxValue;
				for (int i = 0; i < items.Count; i += size)
				{
					List<Item> _items = items.GetRange(i, (i + size) > items.Count ? items.Count - i : size);
					using (ModPacket packet = PrepareClientRequest(Operation.DepositAll))
					{
						packet.Write((byte)_items.Count);
						for (int j = 0; j < _items.Count; ++j)
						{
							ItemIO.Send(_items[j], packet, true, true);
						}
						packet.Send();
					}
				}

				foreach (Item item in items)
				{
					item.SetDefaults(0, true);
				}
				changed = true;

				netcodeUpdate = true;
				netDesync = 0;
			}
			else
			{
				foreach (Item item in items)
				{
					int oldStack = item.stack;
					DepositItem(item);
					if (oldStack != item.stack)
						changed = true;
				}
			}
			return changed;
		}

		public bool TryDeposit(List<Item> items, Player accessingPlayer)
		{
			using var _ = SecuritySystem.CreateAccessContext(accessingPlayer.whoAmI);
			return TryDeposit(items);
		}

		public Item Withdraw(Item lookFor, bool keepOneIfFavorite)
		{
			if (!SecuritySystem.AccessibleFromContext(assignedNetwork)) {
				NetHelper.Report(true, $"[TEStorageHeart] Access denied for accessing player {(SecuritySystem.TryGetCurrentAccessContext(out var context) ? context.Player : -1)} at {Position}");
				return new Item();
			}

			Item result = new();
			foreach (TEAbstractStorageUnit storageUnit in GetStorageUnits())
			{
				if (storageUnit.HasItem(lookFor, true))
				{
					Item withdrawn = storageUnit.TryWithdraw(lookFor, true, keepOneIfFavorite);
					if (!withdrawn.IsAir)
					{
						if (result.IsAir)
							result = withdrawn;
						else
							result.stack += withdrawn.stack;

						if (StoragePlayer.IsClientViewingHeart(this))
							MagicUI.SetNextCollectionsToRefresh(withdrawn.type);

						if (lookFor.stack <= 0)
						{
							ResetCompactStage();
							return result;
						}
					}
				}
			}

			if (result.stack > 0)
				ResetCompactStage();
			return result;
		}

		public Item TryWithdraw(Item lookFor, bool keepOneIfFavorite, bool toInventory = false)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = PrepareClientRequest(toInventory ? Operation.WithdrawToInventory : Operation.Withdraw);
				packet.Write(keepOneIfFavorite);
				ItemIO.Send(lookFor, packet, true, true);
				packet.Send();

				netcodeUpdate = true;
				netDesync = 0;

				return new Item();
			}

			var item = Withdraw(lookFor, keepOneIfFavorite);

			return item;
		}

		public Item TryWithdraw(Item lookFor, bool keepOneIfFavorite, Player accessingPlayer, bool toInventory = false)
		{
			using var _ = SecuritySystem.CreateAccessContext(accessingPlayer.whoAmI);
			return TryWithdraw(lookFor, keepOneIfFavorite, toInventory);
		}

		internal void WithdrawManyAndDestroy(int type, out int itemsDestroyed, bool net = false) {
			itemsDestroyed = 0;

			if (!net && Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = PrepareClientRequest(Operation.WithdrawAllAndDestroy);
				packet.Write(type);
				packet.Send();

				netcodeUpdate = true;
				netDesync = 0;

				return;
			}

			try {
				Item lookFor, lookForOrig = new(type) { stack = int.MaxValue }, result = new();

				if (Main.netMode != NetmodeID.SinglePlayer || HasItem(lookForOrig, true)) {
					//Clone of Withdraw, but it will keep trying to remove items, even if any were found
					foreach (TEStorageUnit storageUnit in GetStorageUnits().OfType<TEStorageUnit>()) {
						lookFor = lookForOrig;
						while (storageUnit.HasItem(lookFor, true)) {
							Item withdrawn = storageUnit.TryWithdraw(lookFor, true, false);

							if (!withdrawn.IsAir) {
								if (result.IsAir)
									result = withdrawn;
								else
									result.stack += withdrawn.stack;

								itemsDestroyed += withdrawn.stack;
							}
						}
					}

					if (result.stack > 0) {
						ResetCompactStage();

						if (StoragePlayer.IsClientViewingHeart(this))
							MagicUI.SetNextCollectionsToRefresh(type);
					}
				}
			} catch {
				// Swallow exception and let the user know that something went wrong
				if (Main.netMode != NetmodeID.Server)
					Main.NewText(Language.GetTextValue("Mods.MagicStorage.Warnings.DeleteItemsFailed"), color: Color.Red);
			}
		}

		internal void DestroyUnloadedGlobalItemData(out int itemsAffected, bool net = false) {
			itemsAffected = 0;

			if (!net && Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = PrepareClientRequest(Operation.DeleteUnloadedGlobalItemData);
				packet.Send();

				netcodeUpdate = true;
				netDesync = 0;

				return;
			}

			try {
				bool didSomething = false;

				HashSet<int> typesToRefresh = new();

				foreach (Item item in GetStorageUnits().OfType<TEStorageUnit>().SelectMany(s => s.GetItems())) {
					//Filter out air items and Unloaded Items (their data might belong to the mod they're from)
					if (item is null || item.IsAir || item.ModItem is UnloadedItem)
						continue;

					if (item._globals is not { Length: >0 } globalItems)
						continue;

					// NOTE: items should only have one UnloadedGlobalItem, but the class is not "sealed", so having multiple is possible
					foreach (UnloadedGlobalItem unloaded in globalItems.OfType<UnloadedGlobalItem>()) {
						// Clear the data
						unloaded.data?.Clear();
					}

					itemsAffected++;
				}

				if (didSomething) {
					ResetCompactStage();
					
					if (StoragePlayer.IsClientViewingHeart(this))
						MagicUI.SetNextCollectionsToRefresh(typesToRefresh);
				}
			} catch {
				// Swallow exception and let the user know that something went wrong
				if (Main.netMode != NetmodeID.Server)
					Main.NewText(Language.GetTextValue("Mods.MagicStorage.Warnings.DeleteDataFailed"), color: Color.Red);
			}
		}

		internal bool TryDeleteExactItem(ReadOnlySpan<byte> itemData, out ReducedItem detectedItem, int itemCountToDelete, ConditionalWeakTable<Item, byte[]> savedItemTagIO = null) {
			Item clone = Utility.FromByteSpanNoCompression(itemData);
			detectedItem = new(clone);
			if (clone.IsAir)
				return false;

			if (clone.stack != 1) {
				// Stack should be ignored when comparing data
				clone.stack = 1;
				itemData = Utility.ToByteSpanNoCompression(clone);
			}

			foreach (TEStorageUnit unit in GetStorageUnits().OfType<TEStorageUnit>()) {
				if (unit.IsEmpty || !unit.HasItem(clone, ignorePrefix: true))
					continue;

				for (int i = unit.items.Count - 1; i >= 0; i--) {
					Item storage = unit.items[i];
					if (storage.type != clone.type)
						continue;

					ReadOnlySpan<byte> storageData;

					if (savedItemTagIO is not null && savedItemTagIO.TryGetValue(storage, out var storageDataArray)) {
						// Retrieve the cached value
						storageData = storageDataArray;
					} else {
						// Stack should be ignored when comparing data
						using (ObjectSwitch.Create(ref storage.stack, 1))
							storageDataArray = Utility.ToByteArrayNoCompression(storage);

						// Cache the value
						savedItemTagIO?.Add(storage, storageDataArray);
						storageData = storageDataArray;
					}

					// Must be an exact match
					if (itemData.SequenceEqual(storageData)) {
						if (itemCountToDelete >= storage.stack) {
							itemCountToDelete -= storage.stack;

							unit.items.RemoveAt(i);
							savedItemTagIO?.Remove(storage);
						} else {
							storage.stack -= itemCountToDelete;
							itemCountToDelete = 0;
						}

						ResetCompactStage();

						unit.PostChangeContents();

						if (Main.netMode == NetmodeID.SinglePlayer)
							MagicUI.SetRefresh(forceFullRefresh: true);
						else
							NetHelper.SendRefreshNetworkItems(Position, forceFullRefresh: true);

						if (itemCountToDelete <= 0)
							return true;
					}
				}
			}

			return clone.stack <= 0;
		}

		public bool HasItem(Item lookFor, bool ignorePrefix = false)
		{
			if (!SecuritySystem.AccessibleFromContext(assignedNetwork)) {
				NetHelper.Report(true, $"[TEStorageHeart] Access denied for accessing player {(SecuritySystem.TryGetCurrentAccessContext(out var context) ? context.Player : -1)} at {Position}");
				return false;
			}

			foreach (TEAbstractStorageUnit storageUnit in GetStorageUnits())
				if (storageUnit.HasItem(lookFor, ignorePrefix))
					return true;
			return false;
		}

		public bool HasItem(Item lookFor, Player accessingPlayer, bool ignorePrefix = false)
		{
			using var _ = SecuritySystem.CreateAccessContext(accessingPlayer.whoAmI);
			return HasItem(lookFor, ignorePrefix);
		}

		public override void SaveData(TagCompound tag)
		{
			base.SaveData(tag);

			// FIX: v0.7.0.3 - Restore legacy data for backwards compatibility
			List<TagCompound> tagRemotes = new();
			foreach (Point16 remoteAccess in Obsolete_remoteAccesses())
			{
				TagCompound tagRemote = new();
				tagRemote.Set("X", remoteAccess.X);
				tagRemote.Set("Y", remoteAccess.Y);
				tagRemotes.Add(tagRemote);
			}

			tag.Set("RemoteAccesses", tagRemotes);

			List<TagCompound> tagEnvironments = new();
			foreach (Point16 environmentAccess in Obsolete_environmentAccesses()) {
				tagEnvironments.Add(new TagCompound() {
					["X"] = environmentAccess.X,
					["Y"] = environmentAccess.Y
				});
			}

			tag["EnvironmentAccesses"] = tagEnvironments;

			_uniqueItemsPutHistory.Save(tag);

			tag["name"] = storageName;
		}

		public override void LoadData(TagCompound tag)
		{
			base.LoadData(tag);

			ConnectedComponentManager manager = ComponentManager;

			// Legacy data
			foreach (TagCompound tagRemote in tag.GetList<TagCompound>("RemoteAccesses"))
				manager.LinkRemoteAccess(new Point16(tagRemote.GetShort("X"), tagRemote.GetShort("Y")));

			foreach (TagCompound tagEnvironment in tag.GetList<TagCompound>("EnvironmentAccesses"))
				manager.LinkEnvironmentAccess(new Point16(tagEnvironment.GetShort("X"), tagEnvironment.GetShort("Y")));

			_uniqueItemsPutHistory.Load(tag);

			storageName = tag.TryGet("name", out string nameValue) ? nameValue : string.Empty;

			compactCoins = true;
		}

		public override void NetSend(BinaryWriter writer)
		{
			base.NetSend(writer);

			// Ensure that the "in use" array stays in sync
			BitArray bits = new BitArray(clientUsingHeart);
			byte[] arr = new byte[(Main.maxPlayers >> 3) + 1];
			bits.CopyTo(arr, 0);

			writer.Write((byte)arr.Length);
			writer.Write(arr);

			writer.WriteStringSafely(storageName);

			NetHelper.Report(true, "Sent tile entity data for TEStorageHeart");
		}

		public override void NetReceive(BinaryReader reader)
		{
			base.NetReceive(reader);

			byte clientUsageLength = reader.ReadByte();
			byte[] clientUsage = reader.ReadBytes(clientUsageLength);
			BitArray bits = new BitArray(clientUsage);
			bits.Length -= 1;  // Need 255 entries, not 256
			bits.CopyTo(clientUsingHeart, 0);

			storageName = reader.ReadStringSafely();

			NetHelper.Report(true, "Received tile entity data for TEStorageHeart");
		}

		internal void ClearDepositHistory() => _uniqueItemsPutHistory.Clear();

		internal void SendDepositHistoryChunks() {
			requestingHistory++;

			// Slice up the history into 1000=count arrays
			int[] history = _uniqueItemsPutHistory.Get().ToArray();

			const int STRIDE = 1000;
			int chunkCount = (int)Math.Ceiling(history.Length / (double)STRIDE);

			for (int i = 0; i < history.Length; i += STRIDE) {
				var packet = MagicStorageMod.Instance.GetPacket();
				packet.Write((byte)MessageType.ServerResponseDepositHistoryChunks);
				packet.Write(Position);
				packet.Write(requestingHistory);
				packet.Write(chunkCount);
				packet.Write(i);

				int slice = Math.Min(STRIDE, history.Length - i);
				packet.Write(slice);
				for (int j = 0; j < slice; j++)
					packet.Write(history[i + j]);

				packet.Send();
			}

			NetHelper.Report(true, $"Sent deposit history in {chunkCount} chunks (total {history.Length} unique items) to all clients");
		}

		internal void ReceiveDepositHistoryChunk(BinaryReader reader) {
			_workingHistory ??= new();

			int packetID = reader.ReadInt32();
			if (requestingHistory < packetID) {
				// New set of packets, destroy whatever was previously stored
				_workingHistory.Clear();
				requestingHistory = packetID;

				NetHelper.Report(true, $"Receiving new deposit history (packet ID {packetID})");
			}

			int packetCount = reader.ReadInt32();
			int packetIndex = reader.ReadInt32();

			int count = reader.ReadInt32();
			int[] chunk = new int[count];
			for (int i = 0; i < count; i++)
				chunk[i] = reader.ReadInt32();

			if (packetID < requestingHistory) {
				// This is an old packet, ignore it
				NetHelper.Report(true, $"Ignoring old deposit history chunk (packet ID {packetID}, current {requestingHistory})");
				return;
			}

			while (_workingHistory.Count <= packetIndex) {
				// Ensure that the working history has a slot for this packet
				_workingHistory.Add(null);
			}

			_workingHistory[packetIndex] = chunk;

			NetHelper.Report(true, $"Received deposit history chunk {packetIndex + 1}/{packetCount} (packet ID {packetID}, {count} items)");

			if (_workingHistory.Count == packetCount) {
				// All packets have been received, merge them into the history
				_uniqueItemsPutHistory.Clear();

				foreach (var readChunk in _workingHistory) {
					if (readChunk is null)
						continue;  // Shouldn't happen, but ignore it just in case

					foreach (int type in readChunk)
						_uniqueItemsPutHistory.Add(type);
				}

				_workingHistory.Clear();

				if (Main.netMode == NetmodeID.MultiplayerClient && StoragePlayer.IsClientViewingHeart(this)) {
					// Only refresh if the applicable filtering mode is being used
					if (FilteringOptionLoader.Selected == FilteringOptionLoader.Definitions.Recent.Type)
						MagicUI.SetRefresh(forceFullRefresh: true);
				}

				hasDepositHistory = true;
				requestingDepositHistory = false;

				NetHelper.Report(true, $"Completed receiving deposit history (total {_uniqueItemsPutHistory.Count} unique items)");
			}
		}

		internal void UpdateDepositHistory(int[] additions, int[] removals) {
			bool changed = false;

			if (additions is { Length: > 0 }) {
				foreach (int type in additions) {
					if (_uniqueItemsPutHistory.Add(type))
						changed = true;
				}
			}

			if (removals is { Length: > 0 }) {
				foreach (int type in removals) {
					if (_uniqueItemsPutHistory.Remove(type))
						changed = true;
				}
			}

			if (changed && Main.netMode != NetmodeID.Server && StoragePlayer.IsClientViewingHeart(this)) {
				// Only refresh if the applicable filtering mode is being used
				if (FilteringOptionLoader.Selected == FilteringOptionLoader.Definitions.Recent.Type)
					MagicUI.SetRefresh(forceFullRefresh: true);
			}
		}

		public void SendHistory(BinaryWriter writer) {
			writer.Write(_uniqueItemsPutHistory.Count);

			foreach (Item item in _uniqueItemsPutHistory.Items)
				writer.Write(item.type);
		}

		public void ReceiveHistory(BinaryReader reader) {
			_uniqueItemsPutHistory.Clear();

			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
				_uniqueItemsPutHistory.Add(reader.ReadInt32());
		}
	}
}
