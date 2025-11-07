using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Common.IO {
	partial class NetCompression {
		public const int VERSION_UNCHECKED_STACK_OVERFLOW = 0;
		public const int VERSION_OVERFLOW_SUPPORT = 1;

		public static void SendItem(Item item, BinaryWriter writer, bool writeStack = true, bool writeFavorite = true) {
			ValueWriter valueWriter = new(writer);
			SendItem(item, valueWriter, writeStack, writeFavorite);
			valueWriter.Flush();
		}

		public static void SendItem(Item item, ValueWriter writer, bool writeStack, bool writeFavorite) {
			if (ValueWriter.LogWrites)
				MagicStorageMod.Instance.Logger.Info("WRITE START [SendItem]");

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

			using MemoryStream modData = new MemoryStream();
			using (BinaryWriter modWriter = new BinaryWriter(modData))
				ItemIO.SendModData(item, modWriter);

			byte[] data = modData.ToArray();
			writer.WriteBytes(data);

			if (ValueWriter.LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE FINISH [SendItem]: {ItemID.Search.GetName(item.type)} (stack {item.stack}, prefix {item.prefix}, favorited {item.favorited}, modData {data.Length} bytes)");
		}

		public static void SendItems(List<Item> items, BinaryWriter writer, bool writeStacks = true, bool writeFavorites = true, int? listCountBitSizeOverride = null) {
			ValueWriter valueWriter = new(writer);
			SendItems(items, valueWriter, writeStacks, writeFavorites, listCountBitSizeOverride);
			valueWriter.Flush();
		}

		public static void SendItems(List<Item> items, ValueWriter writer, bool writeStacks = true, bool writeFavorites = true, int? listCountBitSizeOverride = null) {
			writer.Write((uint)items.Count, listCountBitSizeOverride ?? BitBuffer128.MAX_INT);
			foreach (Item item in items)
				SendItem(item, writer, writeStacks, writeFavorites);
		}

		public static Item ReceiveItem(BinaryReader reader, bool readStack = true, bool readFavorite = true) {
			ValueReader valueReader = new(reader);
			return ReceiveItem(valueReader, readStack, readFavorite);
		}

		public static Item ReceiveItem(ValueReader reader, bool readStack, bool readFavorite) => ReceiveItem(reader, VERSION_OVERFLOW_SUPPORT, readStack, readFavorite);

		private static Item ReceiveItem(ValueReader reader, int serializationVersion, bool readStack, bool readFavorite) {
			if (ValueReader.LogReads)
				MagicStorageMod.Instance.Logger.Info("READ START [ReceiveItem]");

			Item item = new Item();
			DeserializedNetItem readData = new();

			ModContent.GetInstance<ItemTypeTracker>().Receive(ref item, reader);
			readData.type = item.type;

			ModContent.GetInstance<ItemPrefixTracker>().Receive(ref item, reader);
			readData.prefix = item.prefix;
			
			if (readStack && item.maxStack > 1) {
				if (serializationVersion == VERSION_UNCHECKED_STACK_OVERFLOW) {
					// Legacy v0.7.0.11 format
					item.stack = readData.stack = (int)reader.ReadUInt32(GetBitSize(item.maxStack));
				} else if (serializationVersion == VERSION_OVERFLOW_SUPPORT) {
					bool partialOrFull = reader.ReadBoolean();

					int overflow = 0;
					if (!partialOrFull) {
						int fullStacks = reader.Read7BitEncodedInt();
						overflow = fullStacks * item.maxStack;
					}

					readData.stack = overflow + (int)reader.ReadUInt32(GetBitSize(item.maxStack));
				}
			}

			if (readFavorite)
				item.favorited = readData.favorite = reader.ReadBoolean();

			using MemoryStream modData = new MemoryStream(reader.ReadBytes());
			using (BinaryReader modReader = new BinaryReader(modData)) {
				bool failed = false;

				try {
					Utility.UnsafelyReceiveModData(item, modReader);
				} catch (Exception ex) {
					LogThenPrepareErrorItem(ref item, readData, item.ModItem, ex);
					failed = true;
				}

				if (!failed) {
					GlobalItem lastReadGlobal = null;
					try {
						Utility.UnsafelyReceiveGlobalModData(item, readData, modReader, out lastReadGlobal);
					} catch (Exception ex) {
						LogThenPrepareErrorItem(ref item, readData, lastReadGlobal, ex);
					}
				}

				if (ValueReader.LogReads)
					MagicStorageMod.Instance.Logger.Info($"READ FINISH [ReceiveItem]: {ItemID.Search.GetName(item.type)} (stack {item.stack}, prefix {item.prefix}, favorited {item.favorited}, modData {modData.Length} bytes)");
			}

			return item;
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

		internal static List<Item> ReceiveItems(ValueReader reader, int serializationVersion, bool readStacks = true, bool readFavorites = true, int? listCountBitSizeOverride = null) {
			int count = (int)reader.ReadUInt32(listCountBitSizeOverride ?? BitBuffer128.MAX_INT);
			List<Item> items = new(count);
			for (int k = 0; k < count; k++)
				items.Add(ReceiveItem(reader, serializationVersion, readStacks, readFavorites));
			return items;
		}
	}
}
