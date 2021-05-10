using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorageExtra.Components
{
	public class TEStorageUnit : TEAbstractStorageUnit
	{
		private readonly Queue<UnitOperation> netQueue = new Queue<UnitOperation>();
		private HashSet<ItemData> hasItem = new HashSet<ItemData>();
		private HashSet<int> hasItemNoPrefix = new HashSet<int>();

		//metadata
		private HashSet<ItemData> hasSpaceInStack = new HashSet<ItemData>();
		private IList<Item> items = new List<Item>();
		private bool receiving;

		public int Capacity {
			get {
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

		public override bool ValidTile(Tile tile) =>
			tile.type == ModContent.TileType<StorageUnit>() && tile.frameX % 36 == 0 && tile.frameY % 36 == 0;

		public override bool HasSpaceInStackFor(Item check, bool locked = false) {
			if (Main.netMode == NetmodeID.Server && !locked)
				GetHeart().EnterReadLock();
			try {
				var data = new ItemData(check);
				return hasSpaceInStack.Contains(data);
			}
			finally {
				if (Main.netMode == NetmodeID.Server && !locked)
					GetHeart().ExitReadLock();
			}
		}

		public bool HasSpaceFor(Item check, bool locked = false) => !IsFull || HasSpaceInStackFor(check, locked);

		public override bool HasItem(Item check, bool locked = false, bool ignorePrefix = false) {
			if (Main.netMode == NetmodeID.Server && !locked)
				GetHeart().EnterReadLock();
			try {
				if (ignorePrefix) return hasItemNoPrefix.Contains(check.type);
				var data = new ItemData(check);
				return hasItem.Contains(data);
			}
			finally {
				if (Main.netMode == NetmodeID.Server && !locked)
					GetHeart().ExitReadLock();
			}
		}

		public override IEnumerable<Item> GetItems() => items;

		public override void DepositItem(Item toDeposit, bool locked = false) {
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
				return;
			if (Main.netMode == NetmodeID.Server && !locked)
				GetHeart().EnterWriteLock();
			try {
				if (CraftingGUI.IsTestItem(toDeposit)) return;
				Item original = toDeposit.Clone();
				bool finished = false;
				bool hasChange = false;
				foreach (Item item in items)
					if (ItemData.Matches(toDeposit, item) && item.stack < item.maxStack) {
						int total = item.stack + toDeposit.stack;
						int newStack = total;
						if (newStack > item.maxStack)
							newStack = item.maxStack;
						item.stack = newStack;

						if (toDeposit.favorited) item.favorited = true;
						if (toDeposit.newAndShiny) item.newAndShiny = ModContent.GetInstance<MagicStorageConfig>().glowNewItems;

						hasChange = true;
						toDeposit.stack = total - newStack;
						if (toDeposit.stack <= 0) {
							toDeposit.SetDefaults(0, true);
							finished = true;
							break;
						}
					}
				if (!finished && !IsFull) {
					Item item = toDeposit.Clone();
					items.Add(item);
					toDeposit.SetDefaults(0, true);
					hasChange = true;
					finished = true;
				}
				if (hasChange && Main.netMode != NetmodeID.MultiplayerClient) {
					if (Main.netMode == NetmodeID.Server)
						netQueue.Enqueue(UnitOperation.Deposit.Create(original));
					PostChangeContents();
				}
			}
			finally {
				if (Main.netMode == NetmodeID.Server && !locked)
					GetHeart().ExitWriteLock();
			}
		}

		public override Item TryWithdraw(Item lookFor, bool locked = false, bool keepOneIfFavorite = false) {
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
				return new Item();
			if (Main.netMode == NetmodeID.Server && !locked)
				GetHeart().EnterWriteLock();
			try {
				Item original = lookFor.Clone();
				Item result = lookFor.Clone();
				result.stack = 0;
				for (int k = 0; k < items.Count; k++) {
					Item item = items[k];
					if (ItemData.Matches(lookFor, item)) {
						int maxToTake = item.stack;
						if (item.stack > 0 && item.favorited && keepOneIfFavorite)
							maxToTake -= 1;
						int withdraw = Math.Min(lookFor.stack, maxToTake);
						item.stack -= withdraw;
						if (item.stack <= 0) {
							items.RemoveAt(k);
							k--;
						}
						result.stack += withdraw;
						lookFor.stack -= withdraw;
						if (lookFor.stack <= 0) {
							if (Main.netMode != NetmodeID.MultiplayerClient) {
								if (Main.netMode == NetmodeID.Server) {
									var op = (WithdrawOperation)UnitOperation.Withdraw.Create(original);
									op.SendKeepOneIfFavorite = keepOneIfFavorite;
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
				if (Main.netMode != NetmodeID.MultiplayerClient) {
					if (Main.netMode == NetmodeID.Server) {
						var op = (WithdrawOperation)UnitOperation.Withdraw.Create(original);
						op.SendKeepOneIfFavorite = keepOneIfFavorite;
						netQueue.Enqueue(op);
					}
					PostChangeContents();
				}
				return result;
			}
			finally {
				if (Main.netMode == NetmodeID.Server && !locked)
					GetHeart().ExitWriteLock();
			}
		}

		public bool UpdateTileFrame(bool locked = false) {
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
			topLeft.frameX = (short)style;
			Main.tile[Position.X, Position.Y + 1].frameX = (short)style;
			Main.tile[Position.X + 1, Position.Y].frameX = (short)(style + 18);
			Main.tile[Position.X + 1, Position.Y + 1].frameX = (short)(style + 18);
			return oldFrame != style;
		}

		public void UpdateTileFrameWithNetSend(bool locked = false) {
			if (UpdateTileFrame(locked))
				NetMessage.SendTileRange(-1, Position.X, Position.Y, 2, 2);
		}

		//precondition: lock is already taken
		internal static void SwapItems(TEStorageUnit unit1, TEStorageUnit unit2) {
			IList<Item> items = unit1.items;
			unit1.items = unit2.items;
			unit2.items = items;
			HashSet<ItemData> dict = unit1.hasSpaceInStack;
			unit1.hasSpaceInStack = unit2.hasSpaceInStack;
			unit2.hasSpaceInStack = dict;
			dict = unit1.hasItem;
			unit1.hasItem = unit2.hasItem;
			unit2.hasItem = dict;
			HashSet<int> temp = unit1.hasItemNoPrefix;
			unit1.hasItemNoPrefix = unit2.hasItemNoPrefix;
			unit2.hasItemNoPrefix = temp;
			if (Main.netMode == NetmodeID.Server) {
				unit1.netQueue.Clear();
				unit2.netQueue.Clear();
				unit1.netQueue.Enqueue(UnitOperation.FullSync.Create());
				unit2.netQueue.Enqueue(UnitOperation.FullSync.Create());
			}
			unit1.PostChangeContents();
			unit2.PostChangeContents();
		}

		//precondition: lock is already taken
		internal Item WithdrawStack() {
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
				return new Item();
			Item item = items[items.Count - 1];
			items.RemoveAt(items.Count - 1);
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				if (Main.netMode == NetmodeID.Server)
					netQueue.Enqueue(UnitOperation.WithdrawStack.Create());
				PostChangeContents();
			}
			return item;
		}

		public override TagCompound Save() {
			TagCompound tag = base.Save();
			List<TagCompound> tagItems = items.Select(ItemIO.Save).ToList();
			tag.Set("Items", tagItems);
			return tag;
		}

		public override void Load(TagCompound tag) {
			base.Load(tag);
			ClearItemsData();
			foreach (Item item in tag.GetList<TagCompound>("Items").Select(ItemIO.Load)) {
				items.Add(item);
				var data = new ItemData(item);
				if (item.stack < item.maxStack)
					hasSpaceInStack.Add(data);
				hasItem.Add(data);
				hasItemNoPrefix.Add(data.Type);
			}
			if (Main.netMode == NetmodeID.Server)
				netQueue.Enqueue(UnitOperation.FullSync.Create());
		}

		public override void NetSend(BinaryWriter trueWriter, bool lightSend) {
			/* Recreate a BinaryWriter writer */
			var buffer = new MemoryStream(65536);
			var compressor = new DeflateStream(buffer, CompressionMode.Compress, true);
			var writerBuffer = new BufferedStream(compressor, 65536);
			var writer = new BinaryWriter(writerBuffer);

			/* Original code */
			base.NetSend(writer, lightSend);
			if (netQueue.Count > Capacity / 2 || !lightSend) {
				netQueue.Clear();
				netQueue.Enqueue(UnitOperation.FullSync.Create());
			}
			writer.Write((ushort)netQueue.Count);
			while (netQueue.Count > 0)
				netQueue.Dequeue().Send(writer, this);

			/* Forces data to be flushed into the compressed buffer */
			writerBuffer.Flush();
			compressor.Close();

			/* Sends the buffer through the network */
			trueWriter.Write((ushort)buffer.Length);
			trueWriter.Write(buffer.ToArray());

			/* Compression stats and debugging code (server side) */
			if (false) {
				var decompressedBuffer = new MemoryStream(65536);
				var decompressor = new DeflateStream(buffer, CompressionMode.Decompress, true);
				decompressor.CopyTo(decompressedBuffer);
				decompressor.Close();

				Console.WriteLine("Magic Storage Data Compression Stats: " + decompressedBuffer.Length + " => " + buffer.Length);
				decompressor.Dispose();
				decompressedBuffer.Dispose();
			}

			/* Dispose all objects */
			writer.Dispose();
			writerBuffer.Dispose();
			compressor.Dispose();
			buffer.Dispose();
		}

		public override void NetReceive(BinaryReader trueReader, bool lightReceive) {
			/* Reads the buffer off the network */
			var buffer = new MemoryStream(65536);
			var bufferWriter = new BinaryWriter(buffer);

			bufferWriter.Write(trueReader.ReadBytes(trueReader.ReadUInt16()));
			buffer.Position = 0;

			/* Recreate the BinaryReader reader */
			var decompressor = new DeflateStream(buffer, CompressionMode.Decompress, true);
			var reader = new BinaryReader(decompressor);

			/* Original code */
			base.NetReceive(reader, lightReceive);
			if (ByPosition.ContainsKey(Position) && ByPosition[Position] is TEStorageUnit) {
				var other = (TEStorageUnit)ByPosition[Position];
				items = other.items;
				hasSpaceInStack = other.hasSpaceInStack;
				hasItem = other.hasItem;
				hasItemNoPrefix = other.hasItemNoPrefix;
			}
			receiving = true;
			int count = reader.ReadUInt16();
			bool flag = false;
			for (int k = 0; k < count; k++)
				if (UnitOperation.Receive(reader, this))
					flag = true;
			if (flag)
				RepairMetadata();
			receiving = false;

			/* Dispose all objects */
			reader.Dispose();
			decompressor.Dispose();
			bufferWriter.Dispose();
			buffer.Dispose();
		}

		private void ClearItemsData() {
			items.Clear();
			hasSpaceInStack.Clear();
			hasItem.Clear();
			hasItemNoPrefix.Clear();
		}

		private void RepairMetadata() {
			hasSpaceInStack.Clear();
			hasItem.Clear();
			hasItemNoPrefix.Clear();
			foreach (Item item in items) {
				var data = new ItemData(item);
				if (item.stack < item.maxStack)
					hasSpaceInStack.Add(data);
				hasItem.Add(data);
				hasItemNoPrefix.Add(data.Type);
			}
		}

		private void PostChangeContents() {
			RepairMetadata();
			UpdateTileFrameWithNetSend(true);
			NetHelper.SendTEUpdate(ID, Position);
		}

		private abstract class UnitOperation
		{
			public static readonly UnitOperation FullSync = new FullSync();
			public static readonly UnitOperation Deposit = new DepositOperation();
			public static readonly UnitOperation Withdraw = new WithdrawOperation();
			public static readonly UnitOperation WithdrawStack = new WithdrawStackOperation();
			private static readonly List<UnitOperation> types = new List<UnitOperation>();
			protected Item data;

			protected byte id;

			static UnitOperation() {
				types.Add(FullSync);
				types.Add(Deposit);
				types.Add(Withdraw);
				types.Add(WithdrawStack);
				for (int k = 0; k < types.Count; k++)
					types[k].id = (byte)k;
			}

			public UnitOperation Create() => (UnitOperation)MemberwiseClone();

			public UnitOperation Create(Item item) {
				UnitOperation clone = Create();
				clone.data = item;
				return clone;
			}

			public void Send(BinaryWriter writer, TEStorageUnit unit) {
				writer.Write(id);
				SendData(writer, unit);
			}

			protected abstract void SendData(BinaryWriter writer, TEStorageUnit unit);

			public static bool Receive(BinaryReader reader, TEStorageUnit unit) {
				byte id = reader.ReadByte();
				if (id >= 0 && id < types.Count)
					return types[id].ReceiveData(reader, unit);
				return false;
			}

			protected abstract bool ReceiveData(BinaryReader reader, TEStorageUnit unit);
		}

		private class FullSync : UnitOperation
		{
			protected override void SendData(BinaryWriter writer, TEStorageUnit unit) {
				writer.Write(unit.items.Count);
				foreach (Item item in unit.items)
					ItemIO.Send(item, writer, true, true);
			}

			protected override bool ReceiveData(BinaryReader reader, TEStorageUnit unit) {
				unit.ClearItemsData();
				int count = reader.ReadInt32();
				for (int k = 0; k < count; k++) {
					Item item = ItemIO.Receive(reader, true, true);
					unit.items.Add(item);
					var data = new ItemData(item);
					if (item.stack < item.maxStack)
						unit.hasSpaceInStack.Add(data);
					unit.hasItem.Add(data);
					unit.hasItemNoPrefix.Add(data.Type);
				}
				return false;
			}
		}

		private class DepositOperation : UnitOperation
		{
			protected override void SendData(BinaryWriter writer, TEStorageUnit unit) {
				ItemIO.Send(data, writer, true, true);
			}

			protected override bool ReceiveData(BinaryReader reader, TEStorageUnit unit) {
				unit.DepositItem(ItemIO.Receive(reader, true, true));
				return true;
			}
		}

		private class WithdrawOperation : UnitOperation
		{
			public bool SendKeepOneIfFavorite { get; set; }

			protected override void SendData(BinaryWriter writer, TEStorageUnit unit) {
				writer.Write(SendKeepOneIfFavorite);
				ItemIO.Send(data, writer, true, true);
			}

			protected override bool ReceiveData(BinaryReader reader, TEStorageUnit unit) {
				bool keepOneIfFavorite = reader.ReadBoolean();
				unit.TryWithdraw(ItemIO.Receive(reader, true, true), keepOneIfFavorite: keepOneIfFavorite);
				return true;
			}
		}

		private class WithdrawStackOperation : UnitOperation
		{
			protected override void SendData(BinaryWriter writer, TEStorageUnit unit) {
			}

			protected override bool ReceiveData(BinaryReader reader, TEStorageUnit unit) {
				unit.WithdrawStack();
				return true;
			}
		}
	}
}
