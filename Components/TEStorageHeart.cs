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
		}

		ConcurrentQueue<NetOperation> clientOpQ = new ConcurrentQueue<NetOperation>();
		internal bool compactCoins = false;
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

		public IEnumerable<Item> UniqueItemsPutHistory => _uniqueItemsPutHistory.Items;

		public override void OnKill()
		{
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
			if (component is TERemoteAccess)
				Obsolete_remoteAccesses().Add(component.Position);
			else if (component is TEEnvironmentAccess)
				Obsolete_environmentAccesses().Add(component.Position);
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
						}
					}
					else if (op.type == Operation.Deposit)
					{
						typesToRefresh.Add(op.item.type);
						DepositItem(op.item);
						if (!op.item.IsAir)
						{
							ModPacket packet = PrepareServerResult(op.type);
							ItemIO.Send(op.item, packet, true, true);
							packet.Send(op.client);
						}
					}
					else if (op.type == Operation.DepositAll)
					{
						NetHelper.StartUpdateQueue();
						List<Item> leftOvers = new List<Item>();
						foreach (Item item in op.items)
						{
							typesToRefresh.Add(item.type);
							DepositItem(item);
							if (!item.IsAir)
							{
								leftOvers.Add(item);
							}
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
					}
					else if (op.type == Operation.WithdrawAllAndDestroy)
					{
						WithdrawManyAndDestroy(op.item.type);

						if (HasItem(op.item, true))
						{
							ModPacket packet = PrepareServerResult(op.type);
							packet.Write(op.item.type);
							packet.Send();

							forcedRefresh = true;
						}
					}
					else if (op.type == Operation.DeleteUnloadedGlobalItemData)
					{
						DestroyUnloadedGlobalItemData();

						ModPacket packet = PrepareServerResult(op.type);
						packet.Send();

						forcedRefresh = true;
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
					}
				}
			}

			if (forcedRefresh)
				typesToRefresh = null;

			return networkRefresh;
		}

		public void QClientOperation(BinaryReader reader, Operation op, int client)
		{
			if (op == Operation.Withdraw || op == Operation.WithdrawToInventory)
			{
				bool keepOneIfFavorite = reader.ReadBoolean();
				Item item = ItemIO.Receive(reader, true, true);
				clientOpQ.Enqueue(new NetOperation(op, item, keepOneIfFavorite, client));

			//	NetHelper.PrintClientRequest(client, "Item Withdraw", Position);
			}
			else if (op == Operation.Deposit)
			{
				Item item = ItemIO.Receive(reader, true, true);
				clientOpQ.Enqueue(new NetOperation(op, item, client));

			//	NetHelper.PrintClientRequest(client, "Item Deposit", Position);
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
				clientOpQ.Enqueue(new NetOperation(op, items, client));

				NetHelper.PrintClientRequest(client, "Deposit All", Position);
			}
			else if (op == Operation.WithdrawAllAndDestroy)
			{
				int type = reader.ReadInt32();
				clientOpQ.Enqueue(new NetOperation(op, new Item(type), client));

				NetHelper.PrintClientRequest(client, "Delete Unloaded Mod Items", Position);
			}
			else if (op == Operation.DeleteUnloadedGlobalItemData)
			{
				clientOpQ.Enqueue(new NetOperation(op, (Item)null, client));

				NetHelper.PrintClientRequest(client, "Delete Unloaded Mod Data", Position);
			}
			else if (op == Operation.WithdrawThenTryModuleInventory || op == Operation.WithdrawToInventoryThenTryModuleInventory)
			{
				Item item = ItemIO.Receive(reader, true, true);
				clientOpQ.Enqueue(new NetOperation(op, item, false, client));

			//	NetHelper.PrintClientRequest(client, "Item Withdraw", Position);
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
			return packet;
		}

		public void CompactCoins()
		{
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
			bool actualItem = !toDeposit.IsAir;
			int oldStack = toDeposit.stack;
			int remember = toDeposit.type;
			foreach (TEAbstractStorageUnit storageUnit in GetStorageUnits())
				if (!storageUnit.Inactive && storageUnit.HasSpaceInStackFor(toDeposit))
				{
					storageUnit.DepositItem(toDeposit);
					if (toDeposit.IsAir)
						return;
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
						NetHelper.SyncStorageDepositHistory(this);
						return;
					}
				}

			toDeposit.newAndShiny = prevNewAndShiny;

			if (oldStack != toDeposit.stack) {
				if (actualItem)
					MagicUI.SetNextCollectionsToRefresh(toDeposit.type);

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
			}
			else
			{
				DepositItem(item);
			}
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

		public Item Withdraw(Item lookFor, bool keepOneIfFavorite)
		{
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

				return new Item();
			}

			var item = Withdraw(lookFor, keepOneIfFavorite);

			return item;
		}

		internal void WithdrawManyAndDestroy(int type, bool net = false) {
			if (!net && Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = PrepareClientRequest(Operation.WithdrawAllAndDestroy);
				packet.Write(type);
				packet.Send();
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
							}
						}
					}

					if (result.stack > 0) {
						ResetCompactStage();

						if (Main.netMode == NetmodeID.SinglePlayer)
							MagicUI.SetNextCollectionsToRefresh(type);
					}
				}
			} catch {
				// Swallow exception and let the user know that something went wrong
				if (Main.netMode != NetmodeID.Server)
					Main.NewText(Language.GetTextValue("Mods.MagicStorage.Warnings.DeleteItemsFailed"), color: Color.Red);
			}
		}

		internal static readonly FieldInfo Item_globalItems = typeof(Item).GetField("_globals", BindingFlags.NonPublic | BindingFlags.Instance);
		internal static readonly FieldInfo UnloadedGlobalItem_data = typeof(UnloadedGlobalItem).GetField("data", BindingFlags.NonPublic | BindingFlags.Instance);

		internal void DestroyUnloadedGlobalItemData(bool net = false) {
			if (!net && Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = PrepareClientRequest(Operation.DeleteUnloadedGlobalItemData);
				packet.Send();
				return;
			}

			try {
				bool didSomething = false;

				HashSet<int> typesToRefresh = new();

				foreach (Item item in GetStorageUnits().OfType<TEStorageUnit>().SelectMany(s => s.GetItems())) {
					//Filter out air items and Unloaded Items (their data might belong to the mod they're from)
					if (item is null || item.IsAir || item.ModItem is UnloadedItem)
						continue;

					if (Item_globalItems.GetValue(item) is not GlobalItem[] globalItems || globalItems.Length == 0)
						continue;

					// NOTE: items should only have one UnloadedGlobalItem, but the class is not "sealed", so having multiple is possible
					foreach (UnloadedGlobalItem unloaded in globalItems.OfType<UnloadedGlobalItem>()) {
						var data = UnloadedGlobalItem_data.GetValue(unloaded) as IList<TagCompound>;

						// Clear the data
						data?.Clear();
					}
				}

				if (didSomething) {
					ResetCompactStage();
					
					if (Main.netMode != NetmodeID.Server)
						MagicUI.SetNextCollectionsToRefresh(typesToRefresh);
				}
			} catch {
				// Swallow exception and let the user know that something went wrong
				if (Main.netMode != NetmodeID.Server)
					Main.NewText(Language.GetTextValue("Mods.MagicStorage.Warnings.DeleteDataFailed"), color: Color.Red);
			}
		}

		internal bool TryDeleteExactItem(ReadOnlySpan<byte> itemData, int? itemStackOverride = null, ConditionalWeakTable<Item, byte[]> savedItemTagIO = null) {
			Item clone = Utility.FromByteSpanNoCompression(itemData);
			if (clone.IsAir)
				return false;

			if (itemStackOverride is { } stackOverride && clone.stack != stackOverride) {
				clone.stack = stackOverride;
				itemData = Utility.ToByteSpanNoCompression(clone);
			}

			foreach (TEStorageUnit unit in GetStorageUnits().OfType<TEStorageUnit>()) {
				if (unit.IsEmpty || !unit.HasItem(clone, ignorePrefix: true))
					continue;

				for (int i = 0; i < unit.items.Count; i++) {
					Item storage = unit.items[i];
					ReadOnlySpan<byte> storageData;

					if (savedItemTagIO.TryGetValue(storage, out var storageDataArray)) {
						// Retrieve the cached value
						storageData = storageDataArray;
					} else {
						if (itemStackOverride is { } stackOverride2) {
							using (ObjectSwitch.Create(ref storage.stack, stackOverride2))
								storageDataArray = Utility.ToByteArrayNoCompression(storage);
						} else
							storageDataArray = Utility.ToByteArrayNoCompression(storage);

						// Cache the value
						savedItemTagIO.Add(storage, storageDataArray);
						storageData = storageDataArray;
					}

					// Must be an exact match
					if (itemData.SequenceEqual(storageData)) {
						if (clone.stack >= storage.stack) {
							clone.stack -= storage.stack;

							unit.items.RemoveAt(i);
							savedItemTagIO.Remove(storage);

							i--;
						} else {
							storage.stack -= clone.stack;
							clone.stack = 0;
						}

						ResetCompactStage();

						unit.PostChangeContents();

						if (Main.netMode == NetmodeID.SinglePlayer)
							MagicUI.SetRefresh(forceFullRefresh: true);
						else
							NetHelper.SendRefreshNetworkItems(Position, forceFullRefresh: true);

						if (clone.stack <= 0)
							return true;
					}
				}
			}

			return clone.stack <= 0;
		}

		public bool HasItem(Item lookFor, bool ignorePrefix = false)
		{
			foreach (TEAbstractStorageUnit storageUnit in GetStorageUnits())
				if (storageUnit.HasItem(lookFor, ignorePrefix))
					return true;
			return false;
		}

		public override void SaveData(TagCompound tag)
		{
			base.SaveData(tag);
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

			SendHistory(writer);

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

			ReceiveHistory(reader);

			NetHelper.Report(true, "Received tile entity data for TEStorageHeart");
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
