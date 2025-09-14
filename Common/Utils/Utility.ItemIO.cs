using System;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;

namespace MagicStorage {
	partial class Utility {
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
