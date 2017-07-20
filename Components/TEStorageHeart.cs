using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
	public class TEStorageHeart : TEStorageCenter
	{
		private ReaderWriterLockSlim itemsLock = new ReaderWriterLockSlim();
		public List<Point16> remoteAccesses = new List<Point16>();
		private int updateTimer = 60;
		private int compactStage = 0;

		public override bool ValidTile(Tile tile)
		{
			return tile.type == mod.TileType("StorageHeart") && tile.frameX == 0 && tile.frameY == 0;
		}

		public override TEStorageHeart GetHeart()
		{
			return this;
		}

		public IEnumerable<TEAbstractStorageUnit> GetStorageUnits()
		{
			return storageUnits.Concat(remoteAccesses.Where(remoteAccess => TileEntity.ByPosition.ContainsKey(remoteAccess) && TileEntity.ByPosition[remoteAccess] is TERemoteAccess)
				.SelectMany(remoteAccess => ((TERemoteAccess)TileEntity.ByPosition[remoteAccess]).storageUnits))
				.Where(storageUnit => TileEntity.ByPosition.ContainsKey(storageUnit) && TileEntity.ByPosition[storageUnit] is TEAbstractStorageUnit)
				.Select(storageUnit => (TEAbstractStorageUnit)TileEntity.ByPosition[storageUnit]);
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
			for (int k = 0; k < remoteAccesses.Count; k++)
			{
				if (!TileEntity.ByPosition.ContainsKey(remoteAccesses[k]) || !(TileEntity.ByPosition[remoteAccesses[k]] is TERemoteAccess))
				{
					remoteAccesses.RemoveAt(k);
					k--;
				}
			}
			if (Main.netMode == 1)
			{
				return;
			}
			updateTimer++;
			if (updateTimer >= 60)
			{
				updateTimer = 0;
				if (Main.netMode != 2 || itemsLock.TryEnterWriteLock(2))
				{
					try
					{
						CompactOne();
					}
					finally
					{
						if (Main.netMode == 2)
						{
							itemsLock.ExitWriteLock();
						}
					}
				}
			}
		}

		//precondition: lock is already taken
		public void CompactOne()
		{
			if (compactStage == 0)
			{
				EmptyInactive();
			}
			else if (compactStage == 1)
			{
				Defragment();
			}
			else if (compactStage == 2)
			{
				PackItems();
			}
		}

		//precondition: lock is already taken
		public bool EmptyInactive()
		{
			TEStorageUnit inactiveUnit = null;
			foreach (TEAbstractStorageUnit abstractStorageUnit in GetStorageUnits())
			{
				if (!(abstractStorageUnit is TEStorageUnit))
				{
					continue;
				}
				TEStorageUnit storageUnit = (TEStorageUnit)abstractStorageUnit;
				if (storageUnit.Inactive && !storageUnit.IsEmpty)
				{
					inactiveUnit = storageUnit;
				}
			}
			if (inactiveUnit == null)
			{
				compactStage++;
				return false;
			}
			foreach (TEAbstractStorageUnit abstractStorageUnit in GetStorageUnits())
			{
				if (!(abstractStorageUnit is TEStorageUnit) || abstractStorageUnit.Inactive)
				{
					continue;
				}
				TEStorageUnit storageUnit = (TEStorageUnit)abstractStorageUnit;
				if (storageUnit.IsEmpty && inactiveUnit.NumItems <= storageUnit.Capacity)
				{
					TEStorageUnit.SwapItems(inactiveUnit, storageUnit);
					NetHelper.SendRefreshNetworkItems(ID);
					return true;
				}
			}
			bool hasChange = false;
			NetHelper.StartUpdateQueue();
			Item tryMove = inactiveUnit.WithdrawStack();
			foreach (TEAbstractStorageUnit abstractStorageUnit in GetStorageUnits())
			{
				if (!(abstractStorageUnit is TEStorageUnit) || abstractStorageUnit.Inactive)
				{
					continue;
				}
				TEStorageUnit storageUnit = (TEStorageUnit)abstractStorageUnit;
				while (storageUnit.HasSpaceFor(tryMove, true) && !tryMove.IsAir)
				{
					storageUnit.DepositItem(tryMove, true);
					if (tryMove.IsAir && !inactiveUnit.IsEmpty)
					{
						tryMove = inactiveUnit.WithdrawStack();
					}
					hasChange = true;
				}
			}
			if (!tryMove.IsAir)
			{
				inactiveUnit.DepositItem(tryMove, true);
			}
			NetHelper.ProcessUpdateQueue();
			if (hasChange)
			{
				NetHelper.SendRefreshNetworkItems(ID);
			}
			else
			{
				compactStage++;
			}
			return hasChange;
		}

		//precondition: lock is already taken
		public bool Defragment()
		{
			TEStorageUnit emptyUnit = null;
			foreach (TEAbstractStorageUnit abstractStorageUnit in GetStorageUnits())
			{
				if (!(abstractStorageUnit is TEStorageUnit))
				{
					continue;
				}
				TEStorageUnit storageUnit = (TEStorageUnit)abstractStorageUnit;
				if (emptyUnit == null && storageUnit.IsEmpty && !storageUnit.Inactive)
				{
					emptyUnit = storageUnit;
				}
				else if (emptyUnit != null && !storageUnit.IsEmpty && storageUnit.NumItems <= emptyUnit.Capacity)
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
				if (!(abstractStorageUnit is TEStorageUnit))
				{
					continue;
				}
				TEStorageUnit storageUnit = (TEStorageUnit)abstractStorageUnit;
				if (unitWithSpace == null && !storageUnit.IsFull && !storageUnit.Inactive)
				{
					unitWithSpace = storageUnit;
				}
				else if (unitWithSpace != null && !storageUnit.IsEmpty)
				{
					NetHelper.StartUpdateQueue();
					while (!unitWithSpace.IsFull && !storageUnit.IsEmpty)
					{
						Item item = storageUnit.WithdrawStack();
						unitWithSpace.DepositItem(item, true);
						if (!item.IsAir)
						{
							storageUnit.DepositItem(item, true);
						}
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
			{
				compactStage = stage;
			}
		}

		public void DepositItem(Item toDeposit)
		{
			if (Main.netMode == 2)
			{
				EnterWriteLock();
			}
			int oldStack = toDeposit.stack;
			try
			{
				foreach (TEAbstractStorageUnit storageUnit in GetStorageUnits())
				{
					if (!storageUnit.Inactive && storageUnit.HasSpaceInStackFor(toDeposit, true))
					{
						storageUnit.DepositItem(toDeposit, true);
						if (toDeposit.IsAir)
						{
							return;
						}
					}
				}
				foreach (TEAbstractStorageUnit storageUnit in GetStorageUnits())
				{
					if (!storageUnit.Inactive && !storageUnit.IsFull)
					{
						storageUnit.DepositItem(toDeposit, true);
						if (toDeposit.IsAir)
						{
							return;
						}
					}
				}
			}
			finally
			{
				if (oldStack != toDeposit.stack)
				{
					ResetCompactStage();
				}
				if (Main.netMode == 2)
				{
					ExitWriteLock();
				}
			}
		}

		public Item TryWithdraw(Item lookFor)
		{
			if (Main.netMode == 1)
			{
				return new Item();
			}
			if (Main.netMode == 2)
			{
				EnterWriteLock();
			}
			try
			{
				Item result = new Item();
				foreach (TEAbstractStorageUnit storageUnit in GetStorageUnits())
				{
					if (storageUnit.HasItem(lookFor, true))
					{
						Item withdrawn = storageUnit.TryWithdraw(lookFor, true);
						if (!withdrawn.IsAir)
						{
							if (result.IsAir)
							{
								result = withdrawn;
							}
							else
							{
								result.stack += withdrawn.stack;
							}
							if (lookFor.stack <= 0)
							{
								ResetCompactStage();
								return result;
							}
						}
					}
				}
				if (result.stack > 0)
				{
					ResetCompactStage();
				}
				return result;
			}
			finally
			{
				if (Main.netMode == 2)
				{
					ExitWriteLock();
				}
			}
		}

		public override TagCompound Save()
		{
			TagCompound tag = base.Save();
			List<TagCompound> tagRemotes = new List<TagCompound>();
			foreach (Point16 remoteAccess in remoteAccesses)
			{
				TagCompound tagRemote = new TagCompound();
				tagRemote.Set("X", remoteAccess.X);
				tagRemote.Set("Y", remoteAccess.Y);
				tagRemotes.Add(tagRemote);
			}
			tag.Set("RemoteAccesses", tagRemotes);
			return tag;
		}

		public override void Load(TagCompound tag)
		{
			base.Load(tag);
			foreach (TagCompound tagRemote in tag.GetList<TagCompound>("RemoteAccesses"))
			{
				remoteAccesses.Add(new Point16(tagRemote.GetShort("X"), tagRemote.GetShort("Y")));
			}
		}

		public override void NetSend(BinaryWriter writer, bool lightSend)
		{
			base.NetSend(writer, lightSend);
			writer.Write((short)remoteAccesses.Count);
			foreach (Point16 remoteAccess in remoteAccesses)
			{
				writer.Write(remoteAccess.X);
				writer.Write(remoteAccess.Y);
			}
		}

		public override void NetReceive(BinaryReader reader, bool lightReceive)
		{
			base.NetReceive(reader, lightReceive);
			int count = reader.ReadInt16();
			for (int k = 0; k < count; k++)
			{
				remoteAccesses.Add(new Point16(reader.ReadInt16(), reader.ReadInt16()));
			}
		}
	}
}