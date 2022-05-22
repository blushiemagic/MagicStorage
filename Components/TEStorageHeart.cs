using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Collections.Concurrent;

namespace MagicStorage.Components
{
	public class TEStorageHeart : TEStorageCenter
	{
		public enum Operation : byte
		{
			Withdraw,
			WithdrawToInventory,
			Deposit,
			DepositAll
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
		bool compactCoins = false;
		private readonly ItemTypeOrderedSet _uniqueItemsPutHistory = new("UniqueItemsPutHistory");
		private int compactStage;
		public HashSet<Point16> remoteAccesses = new();
		private int updateTimer = 60;

		public bool IsAlive { get; private set; } = true;

		public IEnumerable<Item> UniqueItemsPutHistory => _uniqueItemsPutHistory.Items;

		public override void OnKill()
		{
			IsAlive = false;
		}

		public override bool ValidTile(in Tile tile) => tile.TileType == ModContent.TileType<StorageHeart>() && tile.TileFrameX == 0 && tile.TileFrameY == 0;

		public override TEStorageHeart GetHeart() => this;

		public IEnumerable<TEAbstractStorageUnit> GetStorageUnits()
		{
			IEnumerable<Point16> remoteStorageUnits = remoteAccesses.Select(remoteAccess => ByPosition.TryGetValue(remoteAccess, out TileEntity te) ? te : null)
				.OfType<TERemoteAccess>()
				.SelectMany(remoteAccess => remoteAccess.storageUnits);

			return storageUnits.Concat(remoteStorageUnits)
				.Select(storageUnit => ByPosition.TryGetValue(storageUnit, out TileEntity te) ? te : null)
				.OfType<TEAbstractStorageUnit>();
		}

		public IEnumerable<Item> GetStoredItems()
		{
			return GetStorageUnits().SelectMany(storageUnit => storageUnit.GetItems());
		}

		public override void Update()
		{
			foreach (Point16 remoteAccess in remoteAccesses)
				if (!ByPosition.TryGetValue(remoteAccess, out TileEntity te) || te is not TERemoteAccess)
					remoteAccesses.Remove(remoteAccess);

			if (Main.netMode == NetmodeID.Server && processClientOperations())
			{
				NetHelper.SendRefreshNetworkItems(ID);
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

		private bool processClientOperations()
		{
			int opCount = clientOpQ.Count;
			bool networkRefresh = false;
			for (int i = 0; i < opCount; ++i)
			{
				NetOperation op;
				if (clientOpQ.TryDequeue(out op))
				{
					networkRefresh = true;
					if (op.type == Operation.Withdraw || op.type == Operation.WithdrawToInventory)
					{
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
				}
			}
			return networkRefresh;
		}

		public void QClientOperation(BinaryReader reader, Operation op, int client)
		{
			if (op == Operation.Withdraw || op == Operation.WithdrawToInventory)
			{
				bool keepOneIfFavorite = reader.ReadBoolean();
				Item item = ItemIO.Receive(reader, true, true);
				clientOpQ.Enqueue(new NetOperation(op, item, keepOneIfFavorite, client));
			}
			else if (op == Operation.Deposit)
			{
				Item item = ItemIO.Receive(reader, true, true);
				clientOpQ.Enqueue(new NetOperation(op, item, client));
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
			}
		}

		private static ModPacket PrepareServerResult(Operation op)
		{
			ModPacket packet = MagicStorage.Instance.GetPacket();
			packet.Write((byte)MessageType.ServerStorageResult);
			packet.Write((byte)op);
			return packet;
		}

		private ModPacket PrepareClientRequest(Operation op)
		{
			ModPacket packet = MagicStorage.Instance.GetPacket();
			packet.Write((byte)MessageType.ClinetStorageOperation);
			packet.Write(ID);
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
					NetHelper.SendRefreshNetworkItems(ID);
					return true;
				}

			bool hasChange = false;
			NetHelper.StartUpdateQueue();
			Item tryMove = inactiveUnit.WithdrawStack();
			foreach (TEStorageUnit storageUnit in GetStorageUnits().OfType<TEStorageUnit>().Where(unit => !unit.Inactive))
				while (storageUnit.HasSpaceFor(tryMove) && !tryMove.IsAir)
				{
					storageUnit.DepositItem(tryMove);
					if (tryMove.IsAir && !inactiveUnit.IsEmpty)
						tryMove = inactiveUnit.WithdrawStack();
					hasChange = true;
				}

			if (!tryMove.IsAir)
				inactiveUnit.DepositItem(tryMove);
			NetHelper.ProcessUpdateQueue();
			if (hasChange)
				NetHelper.SendRefreshNetworkItems(ID);
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
					NetHelper.SendRefreshNetworkItems(ID);
					return true;
				}
			}

			compactStage++;
			return false;
		}

		public bool PackItems()
		{
			TEStorageUnit unitWithSpace = null;
			foreach (TEAbstractStorageUnit abstractStorageUnit in GetStorageUnits())
			{
				if (abstractStorageUnit is not TEStorageUnit storageUnit)
					continue;
				if (unitWithSpace is null && !storageUnit.IsFull && !storageUnit.Inactive)
				{
					unitWithSpace = storageUnit;
				}
				else if (unitWithSpace is not null && !storageUnit.IsEmpty)
				{
					NetHelper.StartUpdateQueue();
					while (!unitWithSpace.IsFull && !storageUnit.IsEmpty)
					{
						Item item = storageUnit.WithdrawStack();
						unitWithSpace.DepositItem(item);
						if (!item.IsAir)
							storageUnit.DepositItem(item);
					}

					NetHelper.ProcessUpdateQueue();
					NetHelper.SendRefreshNetworkItems(ID);
					return true;
				}
			}

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
			int oldStack = toDeposit.stack;
			if (toDeposit.IsACoin)
			{
				compactCoins = true;
			}
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
			foreach (TEAbstractStorageUnit storageUnit in GetStorageUnits().Reverse())
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
			Item item = new Item();
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = PrepareClientRequest((toInventory ? Operation.WithdrawToInventory : Operation.Withdraw));
				packet.Write(keepOneIfFavorite);
				ItemIO.Send(lookFor, packet, true, true);
				packet.Send();
			}
			else
			{
				item = Withdraw(lookFor, keepOneIfFavorite);
			}
			return item;
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
			_uniqueItemsPutHistory.Save(tag);
		}

		public override void LoadData(TagCompound tag)
		{
			base.LoadData(tag);
			foreach (TagCompound tagRemote in tag.GetList<TagCompound>("RemoteAccesses"))
				remoteAccesses.Add(new Point16(tagRemote.GetShort("X"), tagRemote.GetShort("Y")));
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
		}

		public override void NetReceive(BinaryReader reader)
		{
			base.NetReceive(reader);
			int count = reader.ReadInt16();
			for (int k = 0; k < count; k++)
				remoteAccesses.Add(new Point16(reader.ReadInt16(), reader.ReadInt16()));
		}
	}
}
