using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MagicStorage.Edits;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
	public partial class TEStorageUnit : TEAbstractStorageUnit
	{
		private readonly Queue<UnitOperation> netQueue = new();
		private HashSet<ItemData> hasItem = new();
		private HashSet<int> hasItemNoPrefix = new();

		//metadata
		private HashSet<ItemData> hasSpaceInStack = new();
		private IList<Item> items = new List<Item>();
		private bool receiving;

		public int Capacity
		{
			get
			{
				int style = Main.tile[Position.X, Position.Y].frameY / 36;
				if (style == 8)
					return 4;
				if (style > 1)
					style--;
				int capacity = style + 1;
				if (capacity > 4)
					capacity++;
				if (capacity > 6)
					capacity++;
				if (capacity > 8)
					capacity += 7;
				return 40 * capacity;
			}
		}

		public override bool IsFull => items.Count >= Capacity;

		public bool IsEmpty => items.Count == 0;

		public int NumItems => items.Count;

		public override bool ValidTile(Tile tile) => tile.type == ModContent.TileType<StorageUnit>() && tile.frameX % 36 == 0 && tile.frameY % 36 == 0;

		public override bool HasSpaceInStackFor(Item check, bool locked = false)
		{
			if (Main.netMode == NetmodeID.Server && !locked)
				GetHeart().EnterReadLock();
			try
			{
				ItemData data = new(check);
				return hasSpaceInStack.Contains(data);
			}
			finally
			{
				if (Main.netMode == NetmodeID.Server && !locked)
					GetHeart().ExitReadLock();
			}
		}

		public bool HasSpaceFor(Item check, bool locked = false) => !IsFull || HasSpaceInStackFor(check, locked);

		public override bool HasItem(Item check, bool locked = false, bool ignorePrefix = false)
		{
			if (Main.netMode == NetmodeID.Server && !locked)
				GetHeart().EnterReadLock();
			try
			{
				if (ignorePrefix)
					return hasItemNoPrefix.Contains(check.type);
				ItemData data = new(check);
				return hasItem.Contains(data);
			}
			finally
			{
				if (Main.netMode == NetmodeID.Server && !locked)
					GetHeart().ExitReadLock();
			}
		}

		public override IEnumerable<Item> GetItems() => items;

		public override void DepositItem(Item toDeposit, bool locked = false)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
				return;
			if (Main.netMode == NetmodeID.Server && !locked)
				GetHeart().EnterWriteLock();
			try
			{
				if (CraftingGUI.IsTestItem(toDeposit))
					return;
				Item original = toDeposit.Clone();
				bool finished = false;
				bool hasChange = false;
				foreach (Item item in items)
					if (ItemData.Matches(toDeposit, item) && item.stack < item.maxStack)
					{
						int total = item.stack + toDeposit.stack;
						int newStack = total;
						if (newStack > item.maxStack)
							newStack = item.maxStack;
						item.stack = newStack;

						if (toDeposit.favorited)
							item.favorited = true;
						if (toDeposit.newAndShiny)
							item.newAndShiny = MagicStorageConfig.GlowNewItems;

						hasChange = true;
						toDeposit.stack = total - newStack;
						if (toDeposit.stack <= 0)
						{
							toDeposit.SetDefaults(0, true);
							finished = true;
							break;
						}
					}

				if (!finished && !IsFull)
				{
					Item item = toDeposit.Clone();
					items.Add(item);
					toDeposit.SetDefaults(0, true);
					hasChange = true;
					finished = true;
				}

				if (hasChange && Main.netMode != NetmodeID.MultiplayerClient)
				{
					if (Main.netMode == NetmodeID.Server)
						netQueue.Enqueue(UnitOperation.Deposit.Create(original));
					PostChangeContents();
				}
			}
			finally
			{
				if (Main.netMode == NetmodeID.Server && !locked)
					GetHeart().ExitWriteLock();
			}
		}

		public override Item TryWithdraw(Item lookFor, bool locked = false, bool keepOneIfFavorite = false)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
				return new Item();
			if (Main.netMode == NetmodeID.Server && !locked)
				GetHeart().EnterWriteLock();
			try
			{
				Item original = lookFor.Clone();
				Item result = lookFor.Clone();
				result.stack = 0;
				for (int k = items.Count - 1; k >= 0; k--)
				{
					Item item = items[k];
					if (ItemData.Matches(lookFor, item))
					{
						int maxToTake = item.stack;
						if (item.stack > 0 && item.favorited && keepOneIfFavorite)
							maxToTake -= 1;
						int withdraw = Math.Min(lookFor.stack, maxToTake);
						item.stack -= withdraw;
						if (item.stack <= 0)
							items.RemoveAt(k);
						result.stack += withdraw;
						lookFor.stack -= withdraw;
						if (lookFor.stack <= 0)
						{
							if (Main.netMode != NetmodeID.MultiplayerClient)
							{
								if (Main.netMode == NetmodeID.Server)
								{
									WithdrawOperation op = UnitOperation.Withdraw.Create(original);
									op.KeepOneIfFavorite = keepOneIfFavorite;
									netQueue.Enqueue(op);
								}

								PostChangeContents();
							}

							return result;
						}
					}
				}

				if (result.stack == 0)
					return new Item();
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					if (Main.netMode == NetmodeID.Server)
					{
						WithdrawOperation op = UnitOperation.Withdraw.Create(original);
						op.KeepOneIfFavorite = keepOneIfFavorite;
						netQueue.Enqueue(op);
					}

					PostChangeContents();
				}

				return result;
			}
			finally
			{
				if (Main.netMode == NetmodeID.Server && !locked)
					GetHeart().ExitWriteLock();
			}
		}

		public bool UpdateTileFrame(bool locked = false)
		{
			Tile topLeft = Main.tile[Position.X, Position.Y];
			int oldFrame = topLeft.frameX;
			int style;
			if (Main.netMode == NetmodeID.Server && !locked)
				GetHeart().EnterReadLock();
			if (IsEmpty)
				style = 0;
			else if (IsFull)
				style = 2;
			else
				style = 1;
			if (Main.netMode == NetmodeID.Server && !locked)
				GetHeart().ExitReadLock();
			if (Inactive)
				style += 3;
			style *= 36;
			topLeft.frameX = (short) style;
			Main.tile[Position.X, Position.Y + 1].frameX = (short) style;
			Main.tile[Position.X + 1, Position.Y].frameX = (short) (style + 18);
			Main.tile[Position.X + 1, Position.Y + 1].frameX = (short) (style + 18);
			return oldFrame != style;
		}

		public void UpdateTileFrameWithNetSend(bool locked = false)
		{
			if (UpdateTileFrame(locked))
				NetMessage.SendTileSquare(-1, Position.X, Position.Y, 2, 2);
		}

		//precondition: lock is already taken
		internal static void SwapItems(TEStorageUnit unit1, TEStorageUnit unit2)
		{
			(unit1.items, unit2.items) = (unit2.items, unit1.items);
			(unit1.hasSpaceInStack, unit2.hasSpaceInStack) = (unit2.hasSpaceInStack, unit1.hasSpaceInStack);
			(unit1.hasItem, unit2.hasItem) = (unit2.hasItem, unit1.hasItem);
			(unit1.hasItemNoPrefix, unit2.hasItemNoPrefix) = (unit2.hasItemNoPrefix, unit1.hasItemNoPrefix);
			if (Main.netMode == NetmodeID.Server)
			{
				unit1.netQueue.Clear();
				unit2.netQueue.Clear();
				unit1.netQueue.Enqueue(UnitOperation.FullSync.Create());
				unit2.netQueue.Enqueue(UnitOperation.FullSync.Create());
			}

			unit1.PostChangeContents();
			unit2.PostChangeContents();
		}

		//precondition: lock is already taken
		internal Item WithdrawStack()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
				return new Item();

			Item item = items[items.Count - 1];
			items.RemoveAt(items.Count - 1);
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				if (Main.netMode == NetmodeID.Server)
					netQueue.Enqueue(UnitOperation.WithdrawStack.Create());
				PostChangeContents();
			}

			return item;
		}

		public override void SaveData(TagCompound tag)
		{
			base.SaveData(tag);
			List<TagCompound> tagItems = items.Select(ItemIO.Save).ToList();
			tag.Set("Items", tagItems);
		}

		public override void LoadData(TagCompound tag)
		{
			base.LoadData(tag);
			ClearItemsData();
			foreach (Item item in tag.GetList<TagCompound>("Items").Select(ItemIO.Load))
			{
				items.Add(item);
				ItemData data = new(item);
				if (item.stack < item.maxStack)
					hasSpaceInStack.Add(data);
				hasItem.Add(data);
				hasItemNoPrefix.Add(data.Type);
			}

			if (Main.netMode == NetmodeID.Server)
				netQueue.Enqueue(UnitOperation.FullSync.Create());
		}

		public override void NetSend(BinaryWriter trueWriter)
		{
			//If the workaround is active, then the entity isn't being sent via the NetWorkaround packet or is being saved to a world file
			if (EditsLoader.MessageTileEntitySyncing)
			{
				trueWriter.Write(true);
				base.NetSend(trueWriter);
				return;
			}

			trueWriter.Write(false);

			/* Recreate a BinaryWriter writer */
			using MemoryStream buffer = new(65536);
			using BinaryWriter writer = new(buffer);

			/* Original code */
			base.NetSend(writer);

			// too many updates, at this point just fully sync
			if (netQueue.Count > Capacity / 2 || !EditsLoader.LightSend)
			{
				netQueue.Clear();
				netQueue.Enqueue(UnitOperation.FullSync.Create());
			}

			writer.Write((ushort) netQueue.Count);
			while (netQueue.Count > 0)
				netQueue.Dequeue().Send(writer, this);

			/* Forces data to be flushed into the buffer */
			writer.Flush();

			byte[] data;
			/* compress buffer data */
			using (MemoryStream memoryStream = new())
			{
				using (DeflateStream deflateStream = new(memoryStream, CompressionMode.Compress))
				{
					deflateStream.Write(buffer.GetBuffer());
				}

				data = memoryStream.ToArray();
			}

			/* Sends the buffer through the network */
			trueWriter.Write((ushort) data.Length);
			trueWriter.Write(data.ToArray());

			/* Compression stats and debugging code (server side) */
			/*
			if (false)
			{
				using MemoryStream decompressedBuffer = new MemoryStream(65536);
				using DeflateStream decompressor = new DeflateStream(buffer, CompressionMode.Decompress, true);
				decompressor.CopyTo(decompressedBuffer);
				decompressor.Close();

				Console.WriteLine("Magic Storage Data Compression Stats: " + decompressedBuffer.Length + " => " + buffer.Length);
			}
			*/
		}

		public override void NetReceive(BinaryReader trueReader)
		{
			//If the workaround is active, then the entity isn't being sent via the NetWorkaround packet
			bool workaround = trueReader.ReadBoolean();

			if (EditsLoader.MessageTileEntitySyncing || workaround)
			{
				base.NetReceive(trueReader);
				return;
			}

			/* Reads the buffer off the network */
			using MemoryStream buffer = new(65536);
			buffer.Write(trueReader.ReadBytes(trueReader.ReadUInt16()));
			buffer.Position = 0;

			/* Recreate the BinaryReader reader */
			using DeflateStream decompressor = new(buffer, CompressionMode.Decompress, true);
			using BinaryReader reader = new(decompressor);

			/* Original code */
			base.NetReceive(reader);

			if (ByPosition.TryGetValue(Position, out TileEntity te) && te is TEStorageUnit otherUnit)
			{
				items = otherUnit.items;
				hasSpaceInStack = otherUnit.hasSpaceInStack;
				hasItem = otherUnit.hasItem;
			}

			receiving = true;
			int count = reader.ReadUInt16();

			bool repairMetadata = false;
			for (int k = 0; k < count; k++)
				repairMetadata |= UnitOperation.Receive(reader, this);

			if (repairMetadata)
				RepairMetadata();
			receiving = false;
		}

		private void ClearItemsData()
		{
			items.Clear();
			hasSpaceInStack.Clear();
			hasItem.Clear();
			hasItemNoPrefix.Clear();
		}

		private void RepairMetadata()
		{
			hasSpaceInStack.Clear();
			hasItem.Clear();
			hasItemNoPrefix.Clear();
			foreach (Item item in items)
			{
				ItemData data = new(item);
				if (item.stack < item.maxStack)
					hasSpaceInStack.Add(data);
				hasItem.Add(data);
				hasItemNoPrefix.Add(data.Type);
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
