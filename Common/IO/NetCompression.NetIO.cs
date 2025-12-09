using MagicStorage.Items.ErrorDisplay;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.IO {
	partial class NetCompression {
		public const int VERSION_UNCHECKED_STACK_OVERFLOW = 0;
		public const int VERSION_OVERFLOW_SUPPORT = 1;

		internal static readonly LengthCompressor<uint> lengthTiers;

		static NetCompression() {
			// Optimized for smaller lengths
			var tier0 = EncodingTier.CreateZero        (prefix: 0b_00, 2, size: 16u);
			var tier1 = tier0.CreateSuccessive         (prefix: 0b_01, 2, size: 64u);
			var tier2 = tier1.CreateSuccessive         (prefix: 0b_10, 2, size: 256u);
			var tier3 = tier2.CreateSuccessive         (prefix: 0b_11, 2, size: 4096u);
			var tier4 = tier3.CreateSuccessive         (prefix: 0b011, 3, size: 131072u);
			var tier5 = tier4.CreateSuccessiveUnbounded(prefix: 0b111, 3);

			lengthTiers = new LengthCompressor<uint>(tier0, tier1, tier2, tier3, tier4, tier5);
		}

		public static void SendItem(Item item, BinaryWriter writer, bool writeStack = true, bool writeFavorite = true) {
			ValueWriter valueWriter = new(writer);
			SendItem(item, valueWriter, writeStack, writeFavorite);
			valueWriter.Flush();
		}

		public static void SendItem(Item item, ValueWriter writer, bool writeStack, bool writeFavorite) {
			using (writer.CreateScope(lengthTiers, optimizeForBytes: false)) {
				ModContent.GetInstance<ItemTypeTracker>().Send(item, writer);
				ModContent.GetInstance<ItemPrefixTracker>().Send(item, writer);

				if (writeStack && item.maxStack > 1) {
					bool partialOrFull = item.stack <= item.maxStack;

					writer.Write(partialOrFull);

					int stack = item.stack;

					if (!partialOrFull) {
						writer.Write7BitEncodedInt(item.stack / item.maxStack);
						stack = item.stack % item.maxStack;
					}
				
					writer.Write((uint)stack, GetBitSize(item.maxStack));
				}

				if (writeFavorite)
					writer.Write(item.favorited);
			}

			writer.WriteModData(item, lengthTiers);
			writer.WriteGlobalModData(item, lengthTiers);
		}

		public static void SendItems(List<Item> items, BinaryWriter writer, bool writeStacks = true, bool writeFavorites = true, int? listCountBitSizeOverride = null) {
			ValueWriter valueWriter = new(writer);
			SendItems(items, valueWriter, writeStacks, writeFavorites, listCountBitSizeOverride);
			valueWriter.Flush();
		}

		public static void SendItems(List<Item> items, ValueWriter writer, bool writeStacks = true, bool writeFavorites = true, int? listCountBitSizeOverride = null) {
			writer.Write((uint)items.Count, listCountBitSizeOverride ?? BitBuffer128.MAX_INT);
			foreach (Item item in items) {
				using (writer.CreateScope(lengthTiers, optimizeForBytes: false))
					SendItem(item, writer, writeStacks, writeFavorites);
			}
		}

		public static Item ReceiveItem(BinaryReader reader, bool readStack = true, bool readFavorite = true) {
			ValueReader valueReader = new(reader);
			return ReceiveItem(valueReader, readStack, readFavorite);
		}

		public static Item ReceiveItem(ValueReader reader, bool readStack, bool readFavorite) => ReceiveItem(reader, VERSION_OVERFLOW_SUPPORT, true, readStack, readFavorite, out _);

		private static Item ReceiveItem(ValueReader reader, int serializationVersion, bool isolated, bool readStack, bool readFavorite, out DeserializedNetItem readData) {
			if (serializationVersion == VERSION_UNCHECKED_STACK_OVERFLOW)
				return ReceiveItem_0(reader, readStack, readFavorite, out readData);
			else
				return ReceiveItem_1(reader, readStack, readFavorite, isolated, out readData);
		}

		// Legacy v0.7.0.11 format
		private static Item ReceiveItem_0(ValueReader reader, bool readStack, bool readFavorite, out DeserializedNetItem readData) {
			Item item = new Item();
			readData = new();

			ModContent.GetInstance<ItemTypeTracker>().Receive(ref item, reader);
			readData.type = item.type;

			ModContent.GetInstance<ItemPrefixTracker>().Receive(ref item, reader);
			readData.prefix = item.prefix;
			
			if (readStack && item.maxStack > 1)
				item.stack = readData.stack = (int)reader.ReadUInt32(GetBitSize(item.maxStack));

			if (readFavorite)
				item.favorited = readData.favorite = reader.ReadBoolean();

			using MemoryStream modData = new MemoryStream(reader.ReadBytes());
			using (BinaryReader modReader = new BinaryReader(modData)) {
				bool failed = false;

				try {
					Utility.UnsafelyReceiveModData(item, modReader);
				} catch (Exception ex) {
					MagicStorageMod.Instance.Logger.Error($"Error reading item from compressed stream caused by {item.ModItem.Name} from the {item.ModItem.Mod.Name} mod.", ex);
					failed = true;
				}

				GlobalItem lastReadGlobal = null;
				try {
					Utility.UnsafelyReceiveGlobalModData(item, readData, modReader, out lastReadGlobal);
				} catch (Exception ex) {
					MagicStorageMod.Instance.Logger.Error($"Error reading item from compressed stream caused by {lastReadGlobal.Name} from the {lastReadGlobal.Mod.Name} mod.", ex);
					failed = true;
				}

				if (failed)
					item = Utility.PrepareFailureItem(BaseErrorDummyItem.NetReadFailItemType, null, readData);
			}

			return item;
		}

		private static Item ReceiveItem_1(ValueReader reader, bool readStack, bool readFavorite, bool isolated, out DeserializedNetItem readData) {
			Item item = new Item();
			readData = new();

			Exception error = null;
			IDisposable scope = null;

			try {
				scope = reader.ReadScope(lengthTiers, optimizeForBytes: false);
				ReceiveItemMetadata(reader, ref item, readStack, readFavorite, ref readData);
			} catch (Exception ex) {
				error = ex;
			} finally {
				try {
					scope?.Dispose();
				} catch (Exception ex) {
					error = error is null ? ex : new AggregateException(error, ex);
				}
			}

			try {
				reader.ReadModData(item, lengthTiers);
			} catch (Exception ex) {
				error = error is null ? ex : new AggregateException(error, ex);
			}

			try {
				reader.ReadGlobalModData(item, lengthTiers, readData);
			} catch (Exception ex) {
				error = error is null ? ex : new AggregateException(error, ex);
			}

			if (error is not null) {
				if (error is AggregateException aggregate)
					error = aggregate.Flatten();

				if (isolated) {
					MagicStorageMod.Instance.Logger.Error($"Error reading item \"{item.IdentifierAndStack()}\" from compressed stream", error);

					item = Utility.PrepareFailureItem(BaseErrorDummyItem.NetReadFailItemType, null, readData);
				} else
					throw error;
			}

			return item;
		}

		private static void ReceiveItemMetadata(ValueReader reader, ref Item item, bool readStack, bool readFavorite, ref DeserializedNetItem readData) {
			ModContent.GetInstance<ItemTypeTracker>().Receive(ref item, reader);
			readData.type = item.type;

			ModContent.GetInstance<ItemPrefixTracker>().Receive(ref item, reader);
			readData.prefix = item.prefix;
			
			if (readStack && item.maxStack > 1) {
				bool partialOrFull = reader.ReadBoolean();

				int overflow = 0;
				if (!partialOrFull) {
					int fullStacks = reader.Read7BitEncodedInt();
					overflow = fullStacks * item.maxStack;
				}

				readData.stack = overflow + (int)reader.ReadUInt32(GetBitSize(item.maxStack));
			}

			if (readFavorite)
				item.favorited = readData.favorite = reader.ReadBoolean();
		}

		public static List<Item> ReceiveItems(BinaryReader reader, bool readStacks = true, bool readFavorites = true, int? listCountBitSizeOverride = null) {
			ValueReader valueReader = new(reader);
			return ReceiveItems(valueReader, readStacks, readFavorites, listCountBitSizeOverride);
		}

		internal static List<Item> ReceiveItems(BinaryReader reader, int serializationVersion, bool readStacks = true, bool readFavorites = true, int? listCountBitSizeOverride = null) {
			ValueReader valueReader = new(reader);
			return ReceiveItems(valueReader, serializationVersion, readStacks, readFavorites, listCountBitSizeOverride);
		}

		public static List<Item> ReceiveItems(ValueReader reader, bool readStacks = true, bool readFavorites = true, int? listCountBitSizeOverride = null) {
			return ReceiveItems(reader, VERSION_OVERFLOW_SUPPORT, readStacks, readFavorites, listCountBitSizeOverride);
		}

		private static List<Item> ReceiveItems(ValueReader reader, int serializationVersion, bool readStacks = true, bool readFavorites = true, int? listCountBitSizeOverride = null) {
			if (serializationVersion == VERSION_UNCHECKED_STACK_OVERFLOW)
				return ReceiveItems_0(reader, readStacks, readFavorites, listCountBitSizeOverride);
			else
				return ReceiveItems_1(reader, readStacks, readFavorites, listCountBitSizeOverride);
		}

		// Legacy v0.7.0.11 format
		private static List<Item> ReceiveItems_0(ValueReader reader, bool readStacks, bool readFavorites, int? listCountBitSizeOverride) {
			int count = (int)reader.ReadUInt32(listCountBitSizeOverride ?? BitBuffer128.MAX_INT);
			List<Item> items = new(count);
			for (int k = 0; k < count; k++)
				items.Add(ReceiveItem_0(reader, readStacks, readFavorites, out _));
			return items;
		}

		private static List<Item> ReceiveItems_1(ValueReader reader, bool readStacks, bool readFavorites, int? listCountBitSizeOverride) {
			int count = (int)reader.ReadUInt32(listCountBitSizeOverride ?? BitBuffer128.MAX_INT);
			List<Item> items = new(count);

			for (int k = 0; k < count; k++) {
				IDisposable scope = null;
				Item item = null;
				DeserializedNetItem readData = null;

				bool failed = false;
				Exception error = null;

				try {
					scope = reader.ReadScope(lengthTiers, optimizeForBytes: false);
					item = ReceiveItem(reader, VERSION_OVERFLOW_SUPPORT, false, readStacks, readFavorites, out readData);
				} catch (Exception ex) {
					failed = true;
					error = ex;
				} finally {
					try {
						scope?.Dispose();
					} catch (Exception ex) {
						failed = true;
						error = error is null ? ex : new AggregateException(error, ex);
					}
				}

				if (failed) {
					if (error is AggregateException aggregate)
						error = aggregate.Flatten();

					if (item is not null)
						MagicStorageMod.Instance.Logger.Error($"Error reading item \"{item.IdentifierAndStack()}\" from compressed stream", error);
					else
						MagicStorageMod.Instance.Logger.Error("Error reading unknown item from compressed stream", error);

					item = Utility.PrepareFailureItem(BaseErrorDummyItem.NetReadFailItemType, null, readData ?? new());
				}

				if (item is not null)
					items.Add(item);
			}

			return items;
		}
	}
}
