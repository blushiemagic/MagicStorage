using System;
using System.Runtime.CompilerServices;

namespace MagicStorage.Common.IO {
	internal static class StringCompressor {
		private static readonly LengthCompressor<ushort> _tiers;

		static StringCompressor() {
			var tier0 = EncodingTier.CreateZero<ushort>(prefix: 0b00, 2, size: 32);
			var tier1 = tier0.CreateSuccessive         (prefix: 0b01, 2, size: 128);
			var tier2 = tier1.CreateSuccessive         (prefix: 0b10, 2, size: 512);
			var tier3 = tier2.CreateSuccessiveUnbounded(prefix: 0b11, 2);

			_tiers = new LengthCompressor<ushort>(tier0, tier1, tier2, tier3);
		}

		/// <summary>
		/// Writes a compressed string to the given <see cref="ValueWriter"/><br/>
		/// This method is optimized for short (length &lt; 32) ASCII strings
		/// </summary>
		public static void WriteTo(ValueWriter writer, string value) {
			// Metadata for the string is included in a prefix of bits
			// Metadata is optimized for short ASCII strings

			int strLength = value.Length;
			bool ascii = HasOnlyASCIIChars(value);

			// Write the metadata prefix
			writer.Write(ascii);
			_tiers.WriteTo(writer, (ushort)strLength);

			// Write the characters
			if (ascii) {
				foreach (char c in value)
					writer.Write((byte)c, 7);
			} else {
				foreach (char c in value)
					writer.Write((ushort)c, 16);
			}
		}

		/// <summary>
		/// Reads a compressed string from the given <see cref="ValueReader"/><br/>
		/// This method is optimized for short (length &lt; 32) ASCII strings
		/// </summary>
		public static string ReadFrom(ValueReader reader) {
			// Metadata for the string is included in a prefix of bits
			// Metadata is optimized for short ASCII strings

			bool ascii = reader.ReadBoolean();
			int strLength = _tiers.ReadFrom(reader);

			if (strLength == 0)
				return string.Empty;

			Span<char> chars = strLength < 2048 ? stackalloc char[strLength] : new char[2048];
			ref char c = ref chars[0];

			if (ascii) {
				for (int i = 0; i < strLength; i++, c = ref Unsafe.Add(ref c, 1))
					c = (char)reader.ReadByte(7);
			} else {
				for (int i = 0; i < strLength; i++, c = ref Unsafe.Add(ref c, 1))
					c = (char)reader.ReadUInt16(16);
			}

			return new string(chars);
		}

		private static bool HasOnlyASCIIChars(string str) {
			foreach (char c in str) {
				if (c > 127)
					return false;
			}

			return true;
		}
	}
}
