using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using MagicStorage.Edits;
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

		private readonly Queue<NetOperation> netOpQueue = new Queue<NetOperation>();
		private IList<Item> items = new List<Item>();
		private bool receiving = false;

		//metadata
		private HashSet<ItemData> hasSpaceInStack = new HashSet<ItemData>();
		private HashSet<ItemData> hasItem = new HashSet<ItemData>();

		public int Capacity
		{
			get
			{
				int style = Main.tile[Position.X, Position.Y].frameY / 36;
				if (style == 8)
				{
					return 4;
				}
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

		public override bool HasSpaceInStackFor(Item check)
		{
			ItemData data = new ItemData(check);
			return hasSpaceInStack.Contains(data);
		}

		public bool HasSpaceFor(Item check) => !IsFull || HasSpaceInStackFor(check);

		public override bool HasItem(Item check)
		{
			ItemData data = new ItemData(check);
			return hasItem.Contains(data);
		}

		public override IEnumerable<Item> GetItems()
		{
			return items;
		}

		public override void DepositItem(Item toDeposit)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
				return;

			Item original = toDeposit.Clone();
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

					if (toDeposit.favorited)
						item.favorited = true;

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
				item.newAndShiny = false;
				item.favorited = false;
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

		public override Item TryWithdraw(Item lookFor, bool keepOneIfFavorite = false)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
				return new Item();

			Item original = lookFor.Clone();
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
			int oldFrame = topLeft.frameX;
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
			topLeft.frameX = (short)style;
			Main.tile[Position.X, Position.Y + 1].frameX = (short)style;
			Main.tile[Position.X + 1, Position.Y].frameX = (short)(style + 18);
			Main.tile[Position.X + 1, Position.Y + 1].frameX = (short)(style + 18);
			return oldFrame != style;
		}

		public void UpdateTileFrameWithNetSend(bool locked = false)
		{
			if (UpdateTileFrame())
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
			HashSet<ItemData> dict = unit1.hasSpaceInStack;
			unit1.hasSpaceInStack = unit2.hasSpaceInStack;
			unit2.hasSpaceInStack = dict;
			dict = unit1.hasItem;
			unit1.hasItem = unit2.hasItem;
			unit2.hasItem = dict;
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
				{
					netOpQueue.Enqueue(new NetOperation(NetOperations.WithdrawStack));
				}
				PostChangeContents();
			}
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
					hasSpaceInStack.Add(data);
				}
				hasItem.Add(data);
			}
		}

		public void FullySync()
		{
			netOpQueue.Enqueue(new NetOperation(NetOperations.FullySync));
		}

		public override void NetSend(BinaryWriter trueWriter, bool lightSend)
		{
			using (MemoryStream buffer = new MemoryStream(65536))
			{
				using (BinaryWriter writer = new BinaryWriter(buffer))
				{
					base.NetSend(writer, lightSend);

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
					trueWriter.Write(data);
				}
			}
		}

		public override void NetReceive(BinaryReader trueReader, bool lightSend)
		{
			/* Reads the buffer off the network */
			using (MemoryStream buffer = new MemoryStream())
			{
				ushort bufferLen = trueReader.ReadUInt16();
				buffer.Write(trueReader.ReadBytes(bufferLen), 0, bufferLen);
				buffer.Position = 0;

				/* Recreate the BinaryReader reader */
				using (DeflateStream decompressor = new DeflateStream(buffer, CompressionMode.Decompress, true))
				{
					using (BinaryReader reader = new BinaryReader(decompressor))
					{
						base.NetReceive(reader, lightSend);

						int serverItemsCount = reader.ReadUInt16();
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
												ItemData data = new ItemData(item);
												if (item.stack < item.maxStack)
													hasSpaceInStack.Add(data);
												hasItem.Add(data);
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
				}
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
					hasSpaceInStack.Add(data);
				}
				hasItem.Add(data);
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
