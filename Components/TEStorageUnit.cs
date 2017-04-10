using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
	public class TEStorageUnit : TEAbstractStorageUnit
	{
		private IList<Item> items = new List<Item>();

		//metadata
		private Dictionary<ItemData, bool> hasSpaceInStack = new Dictionary<ItemData, bool>();
		private Dictionary<ItemData, bool> hasItem = new Dictionary<ItemData, bool>();

		public int Capacity
		{
			get
			{
				int style = Main.tile[Position.X, Position.Y].frameY / 36;
				if (style > 1)
				{
					style--;
				}
				int capacity = style + 1;
				if (capacity > 4)
				{
					capacity++;
				}
				if (capacity > 6)
				{
					capacity++;
				}
				if (capacity > 8)
				{
					capacity += 7;
				}
				return 40 * capacity;
			}
		}

		public override bool IsFull
		{
			get
			{
				return items.Count >= Capacity;
			}
		}

		public bool IsEmpty
		{
			get
			{
				return items.Count == 0;
			}
		}

		public int NumItems
		{
			get
			{
				return items.Count;
			}
		}

		public override bool ValidTile(Tile tile)
		{
			return tile.type == mod.TileType("StorageUnit") && tile.frameX % 36 == 0 && tile.frameY % 36 == 0;
		}

		public override bool HasSpaceInStackFor(Item check, bool locked = false)
		{
			if (Main.netMode == 2 && !locked)
			{
				GetHeart().EnterReadLock();
			}
			try
			{
				ItemData data = new ItemData(check);
				return hasSpaceInStack.ContainsKey(data) && hasSpaceInStack[data];
			}
			finally
			{
				if (Main.netMode == 2 && !locked)
				{
					GetHeart().ExitReadLock();
				}
			}
		}

		public override bool HasItem(Item check, bool locked = false)
		{
			if (Main.netMode == 2 && !locked)
			{
				GetHeart().EnterReadLock();
			}
			try
			{
				ItemData data = new ItemData(check);
				return hasItem.ContainsKey(data) && hasItem[data];
			}
			finally
			{
				if (Main.netMode == 2 && !locked)
				{
					GetHeart().ExitReadLock();
				}
			}
		}

		public override IEnumerable<Item> GetItems()
		{
			return items;
		}

		public override void DepositItem(Item toDeposit, bool locked = false)
		{
			if (Main.netMode == 1)
			{
				return;
			}
			if (Main.netMode == 2 && !locked)
			{
				GetHeart().EnterWriteLock();
			}
			try
			{
				bool finished = false;
				bool hasChange = false;
				foreach (Item item in items)
				{
					if (ItemData.Matches(toDeposit, item) && item.stack < item.maxStack)
					{
						int total = item.stack + toDeposit.stack;
						int newStack = total;
						if (newStack > item.maxStack)
						{
							newStack = item.maxStack;
						}
						item.stack = newStack;
						hasChange = true;
						toDeposit.stack = total - newStack;
						if (toDeposit.stack <= 0)
						{
							toDeposit.SetDefaults(0);
							finished = true;
							break;
						}
					}
				}
				if (!finished && !IsFull)
				{
					Item item = toDeposit.Clone();
					item.newAndShiny = false;
					items.Add(item);
					toDeposit.SetDefaults(0);
					hasChange = true;
					finished = true;
				}
				if (hasChange)
				{
					PostChangeContents();
				}
			}
			finally
			{
				if (Main.netMode == 2 && !locked)
				{
					GetHeart().ExitWriteLock();
				}
			}
		}

		public override Item TryWithdraw(Item lookFor, bool locked = false)
		{
			if (Main.netMode == 1)
			{
				return new Item();
			}
			if (Main.netMode == 2 && !locked)
			{
				GetHeart().EnterWriteLock();
			}
			try
			{
				Item result = lookFor.Clone();
				result.stack = 0;
				for (int k = 0; k < items.Count; k++)
				{
					Item item = items[k];
					if (ItemData.Matches(lookFor, item))
					{
						int withdraw = Math.Min(lookFor.stack, item.stack);
						item.stack -= withdraw;
						if (item.stack <= 0)
						{
							items.RemoveAt(k);
							k--;
						}
						result.stack += withdraw;
						lookFor.stack -= withdraw;
						if (lookFor.stack <= 0)
						{
							PostChangeContents();
							return result;
						}
					}
				}
				if (result.stack == 0)
				{
					return new Item();
				}
				PostChangeContents();
				return result;
			}
			finally
			{
				if (Main.netMode == 2 && !locked)
				{
					GetHeart().ExitWriteLock();
				}
			}
		}

		public bool UpdateTileFrame(bool locked = false)
		{
			Tile topLeft = Main.tile[Position.X, Position.Y];
			int oldFrame = topLeft.frameX;
			int style;
			if (Main.netMode == 2 && !locked)
			{
				GetHeart().EnterReadLock();
			}
			if (IsEmpty)
			{
				style = 0;	
			}
			else if (IsFull)
			{
				style = 2;
			}
			else
			{
				style = 1;
			}
			if (Main.netMode == 2 && !locked)
			{
				GetHeart().ExitReadLock();
			}
			if (Inactive)
			{
				style += 3;
			}
			style *= 36;
			topLeft.frameX = (short)style;
			Main.tile[Position.X, Position.Y + 1].frameX = (short)style;
			Main.tile[Position.X + 1, Position.Y].frameX = (short)(style + 18);
			Main.tile[Position.X + 1, Position.Y + 1].frameX = (short)(style + 18);
			return oldFrame != style;
		}

		public void UpdateTileFrameWithNetSend(bool locked = false)
		{
			if (UpdateTileFrame(locked))
			{
				NetMessage.SendTileRange(-1, Position.X, Position.Y, 2, 2);
			}
		}

		//precondition: lock is already taken
		internal static void SwapItems(TEStorageUnit unit1, TEStorageUnit unit2)
		{
			IList<Item> items = unit1.items;
			unit1.items = unit2.items;
			unit2.items = items;
			Dictionary<ItemData, bool> dict = unit1.hasSpaceInStack;
			unit1.hasSpaceInStack = unit2.hasSpaceInStack;
			unit2.hasSpaceInStack = dict;
			dict = unit1.hasItem;
			unit1.hasItem = unit2.hasItem;
			unit2.hasItem = dict;
			unit1.PostChangeContents();
			unit2.PostChangeContents();
		}

		//precondition: lock is already taken
		internal Item WithdrawStack()
		{
			Item item = items[items.Count - 1];
			items.RemoveAt(items.Count - 1);
			PostChangeContents();
			return item;
		}

		public override TagCompound Save()
		{
			TagCompound tag = base.Save();
			List<TagCompound> tagItems = new List<TagCompound>();
			foreach (Item item in items)
			{
				tagItems.Add(ItemIO.Save(item));
			}
			tag.Set("Items", tagItems);
			return tag;
		}

		public override void Load(TagCompound tag)
		{
			base.Load(tag);
			ClearItemsData();
			foreach (TagCompound tagItem in tag.GetList<TagCompound>("Items"))
			{
				Item item = ItemIO.Load(tagItem);
				items.Add(item);
				ItemData data = new ItemData(item);
				if (item.stack < item.maxStack)
				{
					hasSpaceInStack[data] = true;
				}
				hasItem[data] = true;
			}
		}

		public override void NetSend(BinaryWriter writer, bool lightSend)
		{
			base.NetSend(writer, lightSend);
			writer.Write(items.Count);
			foreach (Item item in items)
			{
				ItemIO.Send(item, writer, true, false);
			}
		}

		public override void NetReceive(BinaryReader reader, bool lightReceive)
		{
			base.NetReceive(reader, lightReceive);
			ClearItemsData();
			int count = reader.ReadInt32();
			for (int k = 0; k < count; k++)
			{
				Item item = ItemIO.Receive(reader, true, false);
				items.Add(item);
				ItemData data = new ItemData(item);
				if (item.stack < item.maxStack)
				{
					hasSpaceInStack[data] = true;
				}
				hasItem[data] = true;
			}
		}

		private void ClearItemsData()
		{
			items.Clear();
			hasSpaceInStack.Clear();
			hasItem.Clear();
		}

		private void RepairMetadata()
		{
			hasSpaceInStack.Clear();
			hasItem.Clear();
			foreach (Item item in items)
			{
				ItemData data = new ItemData(item);
				if (item.stack < item.maxStack)
				{
					hasSpaceInStack[data] = true;
				}
				hasItem[data] = true;
			}
		}

		private void PostChangeContents()
		{
			RepairMetadata();
			UpdateTileFrameWithNetSend(true);
			NetHelper.SendTEUpdate(ID, Position);
		}
	}
}