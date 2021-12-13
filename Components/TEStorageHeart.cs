using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System;

namespace MagicStorage.Components
{
	public class TEStorageHeart : TEStorageCenter
	{
		bool compactCoins = false;
		private readonly ItemTypeOrderedSet _uniqueItemsPutHistory = new("UniqueItemsPutHistory");
		private readonly ReaderWriterLockSlim itemsLock = new();
		private int compactStage;
		public HashSet<Point16> remoteAccesses = new();
		private int updateTimer = 60;

		public bool IsAlive { get; private set; } = true;

		public IEnumerable<Item> UniqueItemsPutHistory => _uniqueItemsPutHistory.Items;

		public override void OnKill()
		{
			IsAlive = false;
		}

		public override bool ValidTile(Tile tile) => tile.type == ModContent.TileType<StorageHeart>() && tile.frameX == 0 && tile.frameY == 0;

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

		public void EnterReadLock()
		{
			itemsLock.EnterReadLock();
		}

		public void ExitReadLock()
		{
			itemsLock.ExitReadLock();
		}

		public void EnterWriteLock()
		{
			itemsLock.EnterWriteLock();
		}

		public void ExitWriteLock()
		{
			itemsLock.ExitWriteLock();
		}

		public override void Update()
		{
			foreach (Point16 remoteAccess in remoteAccesses)
				if (!ByPosition.TryGetValue(remoteAccess, out TileEntity te) || te is not TERemoteAccess)
					remoteAccesses.Remove(remoteAccess);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			updateTimer++;
			if (updateTimer >= 60)
			{
				updateTimer = 0;
				if (compactCoins)
				{
					CompactCoins();
					compactCoins = false;
				}

				if (Main.netMode != NetmodeID.Server || itemsLock.TryEnterWriteLock(2))
					try
					{
						CompactOne();
					}
					finally
					{
						if (Main.netMode == NetmodeID.Server)
							itemsLock.ExitWriteLock();
					}
			}
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
				if (coinQty >= 100)
				{
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

		//precondition: lock is already taken
		public void CompactOne()
		{
			if (compactStage == 0)
				EmptyInactive();
			else if (compactStage == 1)
				Defragment();
			else if (compactStage == 2)
				PackItems();
		}

		//precondition: lock is already taken
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
				while (storageUnit.HasSpaceFor(tryMove, true) && !tryMove.IsAir)
				{
					storageUnit.DepositItem(tryMove, true);
					if (tryMove.IsAir && !inactiveUnit.IsEmpty)
						tryMove = inactiveUnit.WithdrawStack();
					hasChange = true;
				}

			if (!tryMove.IsAir)
				inactiveUnit.DepositItem(tryMove, true);
			NetHelper.ProcessUpdateQueue();
			if (hasChange)
				NetHelper.SendRefreshNetworkItems(ID);
			else
				compactStage++;
			return hasChange;
		}

		//precondition: lock is already taken
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

		//precondition: lock is already taken
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
						unitWithSpace.DepositItem(item, true);
						if (!item.IsAir)
							storageUnit.DepositItem(item, true);
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
			if (Main.netMode == NetmodeID.Server)
				EnterWriteLock();
			int oldStack = toDeposit.stack;
			if (toDeposit.IsACoin)
			{
				compactCoins = true;
			}
			try
			{
				int remember = toDeposit.type;
				foreach (TEAbstractStorageUnit storageUnit in GetStorageUnits())
					if (!storageUnit.Inactive && storageUnit.HasSpaceInStackFor(toDeposit, true))
					{
						storageUnit.DepositItem(toDeposit, true);
						if (toDeposit.IsAir)
							return;
					}

				bool prevNewAndShiny = toDeposit.newAndShiny;
				toDeposit.newAndShiny = MagicStorageConfig.GlowNewItems && !_uniqueItemsPutHistory.Contains(toDeposit);
				foreach (TEAbstractStorageUnit storageUnit in GetStorageUnits())
					if (!storageUnit.Inactive && !storageUnit.IsFull)
					{
						storageUnit.DepositItem(toDeposit, true);
						if (toDeposit.IsAir)
						{
							_uniqueItemsPutHistory.Add(remember);
							return;
						}
					}

				toDeposit.newAndShiny = prevNewAndShiny;
			}
			finally
			{
				if (oldStack != toDeposit.stack)
					ResetCompactStage();
				if (Main.netMode == NetmodeID.Server)
					ExitWriteLock();
			}
		}

		public Item TryWithdraw(Item lookFor, bool keepOneIfFavorite)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return new Item();
			if (Main.netMode == NetmodeID.Server)
				EnterWriteLock();
			try
			{
				Item result = new();
				foreach (TEAbstractStorageUnit storageUnit in GetStorageUnits().Reverse())
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

				if (result.stack > 0)
					ResetCompactStage();
				return result;
			}
			finally
			{
				if (Main.netMode == NetmodeID.Server)
					ExitWriteLock();
			}
		}

		public bool HasItem(Item lookFor, bool ignorePrefix = false)
		{
			if (Main.netMode == NetmodeID.Server)
				EnterReadLock();
			try
			{
				foreach (TEAbstractStorageUnit storageUnit in GetStorageUnits())
					if (storageUnit.HasItem(lookFor, true, ignorePrefix))
						return true;
				return false;
			}
			finally
			{
				if (Main.netMode == NetmodeID.Server)
					ExitReadLock();
			}
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
