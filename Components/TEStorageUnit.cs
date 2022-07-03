using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
	public class TEStorageUnit : TEAbstractStorageUnit
	{
		internal enum NetOperations : byte
		{
			FullySync,
			Deposit,
			Withdraw,
			WithdrawStack,
		}

		private struct NetOperation
		{
			public NetOperation(NetOperations _netOPeration, Item _item = null, bool _keepOneInFavorite = false)
			{
				netOperation = _netOPeration;
				item = _item;
				keepOneInFavorite = _keepOneInFavorite;
			}

			public NetOperations netOperation { get; }
			public Item item { get; }
			public bool keepOneInFavorite { get; }
		}

		private readonly Queue<NetOperation> netOpQueue = new();
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
				int style = Main.tile[Position.X, Position.Y].TileFrameY / 36;
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

		public override bool ValidTile(in Tile tile) => tile.TileType == ModContent.TileType<StorageUnit>() && tile.TileFrameX % 36 == 0 && tile.TileFrameY % 36 == 0;

		public override bool HasSpaceInStackFor(Item check)
		{
			ItemData data = new(check);
			return hasSpaceInStack.Contains(data);
		}

		public bool HasSpaceFor(Item check) => !IsFull || HasSpaceInStackFor(check);

		public override bool HasItem(Item check, bool ignorePrefix = false)
		{
			if (ignorePrefix)
				return hasItemNoPrefix.Contains(check.type);
			ItemData data = new(check);
			return hasItem.Contains(data);
		}

		public override IEnumerable<Item> GetItems() => items;

		public override void DepositItem(Item toDeposit)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
				return;

			Item original = toDeposit.Clone();
			bool finished = false;
			bool hasChange = false;
			foreach (Item item in items)
			{
				if (ItemCombining.CanCombineItems(toDeposit, item) && item.stack < item.maxStack)
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
				{
					netOpQueue.Enqueue(new NetOperation(NetOperations.Deposit, original));
				}
				PostChangeContents();
			}
		}

		public override Item TryWithdraw(Item lookFor, bool locked = false, bool keepOneIfFavorite = false)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
				return new Item();

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
								netOpQueue.Enqueue(new NetOperation(NetOperations.Withdraw, original, keepOneIfFavorite));
							}

							PostChangeContents();
						}

						return result;
					}
				}
			}

			if (result.stack == 0)
			{
				return new Item();
			}
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				if (Main.netMode == NetmodeID.Server)
				{
					netOpQueue.Enqueue(new NetOperation(NetOperations.Withdraw, original, keepOneIfFavorite));
				}

				PostChangeContents();
			}

			return result;
		}

		public bool UpdateTileFrame()
		{
			Tile topLeft = Main.tile[Position.X, Position.Y];
			int oldFrame = topLeft.TileFrameX;
			int style;
			if (IsEmpty)
				style = 0;
			else if (IsFull)
				style = 2;
			else
				style = 1;
			if (Inactive)
				style += 3;
			style *= 36;
			topLeft.TileFrameX = (short)style;
			Main.tile[Position.X, Position.Y + 1].TileFrameX = (short)style;
			Main.tile[Position.X + 1, Position.Y].TileFrameX = (short)(style + 18);
			Main.tile[Position.X + 1, Position.Y + 1].TileFrameX = (short)(style + 18);
			return oldFrame != style;
		}

		public void UpdateTileFrameWithNetSend()
		{
			if (UpdateTileFrame())
				NetMessage.SendTileSquare(-1, Position.X, Position.Y, 2, 2);
		}

		internal static void SwapItems(TEStorageUnit unit1, TEStorageUnit unit2)
		{
			(unit1.items, unit2.items) = (unit2.items, unit1.items);
			(unit1.hasSpaceInStack, unit2.hasSpaceInStack) = (unit2.hasSpaceInStack, unit1.hasSpaceInStack);
			(unit1.hasItem, unit2.hasItem) = (unit2.hasItem, unit1.hasItem);
			(unit1.hasItemNoPrefix, unit2.hasItemNoPrefix) = (unit2.hasItemNoPrefix, unit1.hasItemNoPrefix);
			if (Main.netMode == NetmodeID.Server)
			{
				unit1.netOpQueue.Clear();
				unit2.netOpQueue.Clear();
				unit1.netOpQueue.Enqueue(new NetOperation(NetOperations.FullySync));
				unit2.netOpQueue.Enqueue(new NetOperation(NetOperations.FullySync));
			}

			unit1.PostChangeContents();
			unit2.PostChangeContents();
		}

		internal Item WithdrawStack()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
				return new Item();

			Item item = items[items.Count - 1];
			items.RemoveAt(items.Count - 1);
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				if (Main.netMode == NetmodeID.Server)
				{
					netOpQueue.Enqueue(new NetOperation(NetOperations.WithdrawStack));
				}
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
		}

		public void FullySync()
		{
			netOpQueue.Enqueue(new NetOperation(NetOperations.FullySync));
		}

		public override void NetSend(BinaryWriter trueWriter)
		{
			using MemoryStream buffer = new(65536);
			using BinaryWriter writer = new(buffer);

			base.NetSend(writer);

			// too many updates at this point just fully sync
			if (netOpQueue.Count > Capacity / 2)
			{
				netOpQueue.Clear();
				netOpQueue.Enqueue(new NetOperation(NetOperations.FullySync));
			}

			writer.Write((ushort)items.Count);
			writer.Write((ushort)netOpQueue.Count);
			while (netOpQueue.Count > 0)
			{
				NetOperation netOp = netOpQueue.Dequeue();
				writer.Write((byte)netOp.netOperation);
				switch (netOp.netOperation)
				{
					case NetOperations.FullySync:
						writer.Write(items.Count);
						foreach (Item item in items)
						{
							ItemIO.Send(item, writer, true, true);
						}
						break;
					case NetOperations.Withdraw:
						writer.Write(netOp.keepOneInFavorite);
						ItemIO.Send(netOp.item, writer, true, true);
						break;
					case NetOperations.WithdrawStack:
						break;
					case NetOperations.Deposit:
						ItemIO.Send(netOp.item, writer, true, true);
						break;
					default:
						break;
				}
			}

			/* Forces data to be flushed into the buffer */
			writer.Flush();

			byte[] data = null;
			/* compress buffer data */
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
				{
					deflateStream.Write(buffer.GetBuffer(), 0, (int)buffer.Length);
				}
				data = memoryStream.ToArray();
			}

			/* Sends the buffer through the network */
			trueWriter.Write((ushort)data.Length);
			trueWriter.Write(data.ToArray());
		}

		public override void NetReceive(BinaryReader trueReader)
		{
			/* Reads the buffer off the network */
			using MemoryStream buffer = new();

			ushort bufferLen = trueReader.ReadUInt16();
			buffer.Write(trueReader.ReadBytes(bufferLen));
			buffer.Position = 0;

			/* Recreate the BinaryReader reader */
			using DeflateStream decompressor = new(buffer, CompressionMode.Decompress, true);
			using BinaryReader reader = new(decompressor);

			base.NetReceive(reader);

			int serverItemsCount = reader.ReadInt16();
			int opCount = reader.ReadUInt16();
			if (opCount > 0)
			{
				if (ByPosition.TryGetValue(Position, out TileEntity te) && te is TEStorageUnit otherUnit)
				{
					items = otherUnit.items;
					hasSpaceInStack = otherUnit.hasSpaceInStack;
					hasItem = otherUnit.hasItem;
				}

				receiving = true;
				bool repairMetaData = true;
				for (int i = 0; i < opCount; i++)
				{
					byte netOp = reader.ReadByte();
					if (Enum.IsDefined(typeof(NetOperations), netOp))
					{
						switch ((NetOperations)netOp)
						{
							case NetOperations.FullySync:
								repairMetaData = false;
								ClearItemsData();
								int itemsCount = reader.ReadInt32();
								for (int j = 0; j < itemsCount; j++)
								{
									Item item = ItemIO.Receive(reader, true, true);
									items.Add(item);
									ItemData data = new(item);
									if (item.stack < item.maxStack)
										hasSpaceInStack.Add(data);
									hasItem.Add(data);
									hasItemNoPrefix.Add(data.Type);
								}
								break;
							case NetOperations.Withdraw:
								bool keepOneIfFavorite = reader.ReadBoolean();
								TryWithdraw(ItemIO.Receive(reader, true, true), keepOneIfFavorite: keepOneIfFavorite);
								break;
							case NetOperations.WithdrawStack:
								WithdrawStack();
								break;
							case NetOperations.Deposit:
								DepositItem(ItemIO.Receive(reader, true, true));
								break;
							default:
								break;
						}
					}
					else
					{
						Main.NewText($"NetRecive Bad OP: {netOp}", Microsoft.Xna.Framework.Color.Red);
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine($"NetRecive Bad OP: {netOp}");
						Console.ResetColor();
					}
				}

				if (repairMetaData)
					RepairMetadata();
				receiving = false;
			}
			else if (serverItemsCount != items.Count) // if there is mismatch between the server and the client then send a sync request
			{
				NetHelper.SyncStorageUnit(ID);
			}
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

		public void PostChangeContents()
		{
			RepairMetadata();
			UpdateTileFrameWithNetSend();
			NetHelper.SendTEUpdate(ID, Position);
		}
	}
}
