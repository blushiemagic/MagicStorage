using MagicStorage.Common;
using System;
using System.IO;
using Terraria.DataStructures;

namespace MagicStorage {
	partial class Utility {
		public static Point16 ReadPoint16(this BinaryReader reader) => new(reader.ReadInt16(), reader.ReadInt16());

		public static Guid ReadGuid(this BinaryReader reader) {
			if (reader.ReadByte() != 16)
				throw new InvalidDataException("Expected a byte array of Guid data");

			return new Guid(reader.ReadBytes(16));
		}

		public static string ReadStringSafely(this BinaryReader reader) => reader.ReadBoolean() ? reader.ReadString() : null;

		public static void ReadStringsSafely(this BinaryReader reader, out string s0, out string s1) {
			PackedStrings.Read(reader).Retrieve(out s0, out s1);
		}

		public static void ReadStringsSafely(this BinaryReader reader, out string s0, out string s1, out string s2) {
			PackedStrings.Read(reader).Retrieve(out s0, out s1, out s2);
		}

		public static void ReadStringsSafely(this BinaryReader reader, out string s0, out string s1, out string s2, out string s3) {
			PackedStrings.Read(reader).Retrieve(out s0, out s1, out s2, out s3);
		}

		public static void ReadStringsSafely(this BinaryReader reader, out string s0, out string s1, out string s2, out string s3, out string s4) {
			PackedStrings.Read(reader).Retrieve(out s0, out s1, out s2, out s3, out s4);
		}

		public static void ReadStringsSafely(this BinaryReader reader, out string s0, out string s1, out string s2, out string s3, out string s4, out string s5) {
			PackedStrings.Read(reader).Retrieve(out s0, out s1, out s2, out s3, out s4, out s5);
		}

		public static void ReadStringsSafely(this BinaryReader reader, out string s0, out string s1, out string s2, out string s3, out string s4, out string s5, out string s6) {
			PackedStrings.Read(reader).Retrieve(out s0, out s1, out s2, out s3, out s4, out s5, out s6);
		}

		public static void ReadStringsSafely(this BinaryReader reader, out string s0, out string s1, out string s2, out string s3, out string s4, out string s5, out string s6, out string s7) {
			PackedStrings.Read(reader).Retrieve(out s0, out s1, out s2, out s3, out s4, out s5, out s6, out s7);
		}
	}
}
