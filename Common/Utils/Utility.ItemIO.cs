using MagicStorage.Items.ErrorDisplay;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage {
	partial class Utility {
		public static Item SafelyLoadItem(TagCompound tag) {
			Item loadedItem = null;

			try {
				loadedItem = ItemIO.Load(tag);
			} catch (KeyNotFoundException) {
				// Item was malformed
			} catch (Exception ex) {
				MagicStorageMod.Instance.Logger.Error("Error loading item from tag", ex);
			} finally {
				loadedItem ??= new Item(BaseErrorDummyItem.NetReadFailItemType, tag.Get<int?>("stack").GetValueOrDefault(1));
			}

			return loadedItem;
		}

		public static Item SafelyReadItem(BinaryReader reader, bool readStack = false, bool readFavorite = false) {
			int numBytes = reader.Read7BitEncodedInt();
			byte[] data = reader.ReadBytes(numBytes);

			using MemoryStream ms = new MemoryStream(data);
			using BinaryReader actualReader = new BinaryReader(ms);

			Item readItem = null;

			try {
				readItem = ItemIO.Receive(reader, readStack, readFavorite);
			} catch (Exception ex) {
				// Could not load the item
				MagicStorageMod.Instance.Logger.Error("Error reading item from stream", ex);
			} finally {
				readItem ??= new Item(BaseErrorDummyItem.NetReadFailItemType);
			}

			return readItem;
		}

		// Copies of ItemIO.ReceiveModData() that allows the exception to propagate instead of being caught and logged
		internal static void UnsafelyReceiveModData(Item item, BinaryReader reader) {
			if (item.IsAir)
				return;

			// Local capturing
			Item i = item;
			reader.SafeRead(r => i.ModItem?.NetReceive(r));
		}

		internal static void UnsafelyReceiveGlobalModData(Item item, BinaryReader reader, out GlobalItem lastReadGlobal) {
			if (item.IsAir) {
				lastReadGlobal = null;
				return;
			}

			foreach (var globalItem in ItemLoader.HookNetReceive.Enumerate(item)) {
				lastReadGlobal = globalItem;

				// Local capturing
				Item i = item;
				GlobalItem g = globalItem;
				reader.SafeRead(r => g.NetReceive(i, r));
			}

			lastReadGlobal = null;
		}

		public static void SafelyWriteItem(Item item, BinaryWriter writer, bool writeStack = false, bool writeFavorite = false) {
			byte[] data;
			using (MemoryStream ms = new MemoryStream()) {
				using (BinaryWriter actualWriter = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
					ItemIO.Send(item, actualWriter, writeStack, writeFavorite);

				data = ms.ToArray();
			}

			writer.Write7BitEncodedInt(data.Length);
			writer.Write(data);
		}

		public static byte[] ToByteArrayNoCompression(Item item) {
			MemoryStream ms = new MemoryStream();
			TagIO.ToStream(ItemIO.Save(item), ms, false);
			return ms.ToArray();
		}

		public static ReadOnlySpan<byte> ToByteSpanNoCompression(Item item) => ToByteArrayNoCompression(item);

		public static string ToBase64NoCompression(Item item) => Convert.ToBase64String(ToByteArrayNoCompression(item));

		public static byte[] ToByteArrayNoCompression(TagCompound tag) {
			MemoryStream ms = new MemoryStream();
			TagIO.ToStream(tag, ms, false);
			return ms.ToArray();
		}

		public static ReadOnlySpan<byte> ToByteSpanNoCompression(TagCompound tag) => ToByteArrayNoCompression(tag);

		public static string ToBase64NoCompression(TagCompound tag) => Convert.ToBase64String(ToByteArrayNoCompression(tag));

		public static Item FromByteArrayNoCompression(byte[] bytes) {
			MemoryStream ms = new MemoryStream(bytes);
			return SafelyLoadItem(TagIO.FromStream(ms, false));
		}

		public static Item FromByteSpanNoCompression(ReadOnlySpan<byte> bytes) => FromByteArrayNoCompression(bytes.ToArray());

		public static Item FromBase64NoCompression(string base64) => FromByteArrayNoCompression(Convert.FromBase64String(base64));
	}
}
