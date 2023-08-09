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
		public HashSet<Point16> remoteAccesses = new();
		public HashSet<Point16> environmentAccesses = new();
		private int updateTimer = 60;

		internal bool[] clientUsingHeart = new bool[Main.maxPlayers];

		public bool IsAlive { get; private set; } = true;

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

				if (clientUsingHeart[i])
					return true;
			}

			return false;
		}

		public void LockOnCurrentClient() {
			if (!clientUsingHeart[Main.myPlayer]) {
				clientUsingHeart[Main.myPlayer] = true;
				NetHelper.ClientInformStorageHeartUsage(this);
			}
		}

		public void UnlockOnCurrentClient() {
			if (clientUsingHeart[Main.myPlayer]) {
				clientUsingHeart[Main.myPlayer] = false;
				NetHelper.ClientInformStorageHeartUsage(this);
			}
		}

		public IEnumerable<TEAbstractStorageUnit> GetStorageUnits()
		{
			IEnumerable<Point16> remoteStorageUnits = remoteAccesses.Select(remoteAccess => ByPosition.TryGetValue(remoteAccess, out TileEntity te) ? te : null)
				.OfType<TERemoteAccess>()
				.SelectMany(remoteAccess => remoteAccess.storageUnits);

			return storageUnits.Concat(remoteStorageUnits)
				.Select(storageUnit => ByPosition.TryGetValue(storageUnit, out TileEntity te) ? te : null)
				.OfType<TEAbstractStorageUnit>();
		}

		public IEnumerable<TEEnvironmentAccess> GetEnvironmentSimulators()
			=> environmentAccesses.Select(p => TileEntity.ByPosition.TryGetValue(p, out TileEntity te) ? te : null)
				.OfType<TEEnvironmentAccess>();

		public IEnumerable<EnvironmentModule> GetModules()
			=> GetEnvironmentSimulators()
				.SelectMany(e => e.Modules)
				.DistinctBy(m => m.Type);

		public IEnumerable<Item> GetStoredItems()
		{
			return GetStorageUnits().SelectMany(storageUnit => storageUnit.GetItems());
		}

		protected override void CheckMapSections() {
			base.CheckMapSections();

			// Check for remote and environment accesses as well
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				foreach (Point16 remote in remoteAccesses.Concat(environmentAccesses).DistinctBy(p => new Point16(Netplay.GetSectionX(p.X), Netplay.GetSectionY(p.Y))))
					NetHelper.ClientRequestSection(remote);
			}
		}

		public override void Update()
		{
			foreach (Point16 remoteAccess in remoteAccesses) {
				if (!ByPosition.TryGetValue(remoteAccess, out TileEntity te) || te is not TERemoteAccess)
					remoteAccesses.Remove(remoteAccess);
			}

			foreach (Point16 environmentAccess in environmentAccesses) {
				if (!TileEntity.ByPosition.TryGetValue(environmentAccess, out TileEntity te) || te is not TEEnvironmentAccess)
					environmentAccesses.Remove(environmentAccess);
			}

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

				NetHelper.PrintClientRequest(client, "Item Withdraw", Position);
			}
			else if (op == Operation.Deposit)
			{
				Item item = ItemIO.Receive(reader, true, true);
				clientOpQ.Enqueue(new NetOperation(op, item, client));

				NetHelper.PrintClientRequest(client, "Item Deposit", Position);
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

				NetHelper.PrintClientRequest(client, "Item Withdraw", Position);
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
			if (!toDeposit.IsAir)
				MagicUI.SetNextCollectionsToRefresh(toDeposit.type);

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
						return;
					}
				}

			toDeposit.newAndShiny = prevNewAndShiny;

			if (oldStack != toDeposit.stack)
				ResetCompactStage();
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

					GlobalItem[] array = globalItems.Where(i => i is not UnloadedGlobalItem).ToArray();

					if (array.Length != globalItems.Length) {
						Item_globalItems.SetValue(item, array);
						didSomething = true;

						typesToRefresh.Add(item.type);
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

		internal void TryDeleteExactItem(string itemData) {
			Item clone = Utility.FromBase64NoCompression(itemData);

			foreach (TEStorageUnit unit in GetStorageUnits().OfType<TEStorageUnit>()) {
				if (unit.IsEmpty || !unit.HasItem(clone, ignorePrefix: true))
					continue;

				for (int i = 0; i < unit.items.Count; i++) {
					Item storage = unit.items[i];
					string storageData = Utility.ToBase64NoCompression(storage);

					// Must be an exact match
					if (itemData == storageData) {
						unit.items.RemoveAt(i);
						ResetCompactStage();

						unit.PostChangeContents();

						if (Main.netMode == NetmodeID.SinglePlayer)
							StorageGUI.SetRefresh(forceFullRefresh: true);
						else
							NetHelper.SendRefreshNetworkItems(Position, forceFullRefresh: true);

						break;
					}
				}
			}
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
			foreach (Point16 remoteAccess in remoteAccesses)
			{
				TagCompound tagRemote = new();
				tagRemote.Set("X", remoteAccess.X);
				tagRemote.Set("Y", remoteAccess.Y);
				tagRemotes.Add(tagRemote);
			}

			tag.Set("RemoteAccesses", tagRemotes);

			List<TagCompound> tagEnvironments = new();
			foreach (Point16 environmentAccess in environmentAccesses) {
				tagEnvironments.Add(new TagCompound() {
					["X"] = environmentAccess.X,
					["Y"] = environmentAccess.Y
				});
			}

			tag["EnvironmentAccesses"] = tagEnvironments;

			_uniqueItemsPutHistory.Save(tag);
		}

		public override void LoadData(TagCompound tag)
		{
			base.LoadData(tag);
			foreach (TagCompound tagRemote in tag.GetList<TagCompound>("RemoteAccesses"))
				remoteAccesses.Add(new Point16(tagRemote.GetShort("X"), tagRemote.GetShort("Y")));

			foreach (TagCompound tagEnvironment in tag.GetList<TagCompound>("EnvironmentAccesses"))
				environmentAccesses.Add(new Point16(tagEnvironment.GetShort("X"), tagEnvironment.GetShort("Y")));

			_uniqueItemsPutHistory.Load(tag);

			compactCoins = true;
		}

		public override void NetSend(BinaryWriter writer)
		{
			base.NetSend(writer);
			writer.Write((short)remoteAccesses.Count);
			foreach (Point16 remoteAccess in remoteAccesses)
			{
				writer.Write(remoteAccess.X);
				writer.Write(remoteAccess.Y);
			}

			writer.Write((short)environmentAccesses.Count);
			foreach (Point16 environmentAccess in environmentAccesses)
			{
				writer.Write(environmentAccess.X);
				writer.Write(environmentAccess.Y);
			}

			NetHelper.Report(true, "Sent tile entity data for TEStorageHeart");
		}

		public override void NetReceive(BinaryReader reader)
		{
			base.NetReceive(reader);
			int count = reader.ReadInt16();
			for (int k = 0; k < count; k++)
				remoteAccesses.Add(new Point16(reader.ReadInt16(), reader.ReadInt16()));

			count = reader.ReadInt16();
			for (int k = 0; k < count; k++)
				environmentAccesses.Add(new Point16(reader.ReadInt16(), reader.ReadInt16()));

			NetHelper.Report(true, "Received tile entity data for TEStorageHeart");
		}
	}
}
