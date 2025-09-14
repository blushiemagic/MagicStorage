using MagicStorage.Common;
using System;
using System.IO;
using Terraria.DataStructures;

namespace MagicStorage {
	partial class Utility {
		public static void Write(this BinaryWriter writer, Point16 position) {
			writer.Write(position.X);
			writer.Write(position.Y);
		}

		public static void Write(this BinaryWriter writer, Guid guid) {
			writer.Write((byte)16);
			writer.Write(guid.ToByteArray());
		}

		public static void WriteStringSafely(this BinaryWriter writer, string value) {
			writer.Write(value is not null);
			if (value is not null)
				writer.Write(value);
		}

		public static void WriteStringsSafely(this BinaryWriter writer, string s0, string s1) {
			new PackedStrings(s0, s1).Write(writer);
		}

		public static void WriteStringsSafely(this BinaryWriter writer, string s0, string s1, string s2) {
			new PackedStrings(s0, s1, s2).Write(writer);
		}

		public static void WriteStringsSafely(this BinaryWriter writer, string s0, string s1, string s2, string s3) {
			new PackedStrings(s0, s1, s2, s3).Write(writer);
		}

		public static void WriteStringsSafely(this BinaryWriter writer, string s0, string s1, string s2, string s3, string s4) {
			new PackedStrings(s0, s1, s2, s3, s4).Write(writer);
		}

		public static void WriteStringsSafely(this BinaryWriter writer, string s0, string s1, string s2, string s3, string s4, string s5) {
			new PackedStrings(s0, s1, s2, s3, s4, s5).Write(writer);
		}

		public static void WriteStringsSafely(this BinaryWriter writer, string s0, string s1, string s2, string s3, string s4, string s5, string s6) {
			new PackedStrings(s0, s1, s2, s3, s4, s5, s6).Write(writer);
		}

		public static void WriteStringsSafely(this BinaryWriter writer, string s0, string s1, string s2, string s3, string s4, string s5, string s6, string s7) {
			new PackedStrings(s0, s1, s2, s3, s4, s5, s6, s7).Write(writer);
		}
	}
}
