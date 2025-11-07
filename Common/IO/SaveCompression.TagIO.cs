using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Terraria.ModLoader.IO;

namespace MagicStorage.Common.IO {
	partial class SaveCompression {
		private const int ID_STRING = 8;
		private const int ID_LISTS = 9;
		private const int ID_BOOL = 12;
		private const int ID_TINY_INT = 13;
		private const int ID_TINY_SHORT = 14;
		private const int ID_TINY_LONG = 15;

		private const int SIZE_ID = 4;

		private static void WriteTag(ValueWriter writer, GenericKeyLookup lookup, TagCompound tag) {
			_tagKeyWriterLookup.AddOrUpdate(writer, lookup);
			WriteObject(writer, lookup, "", tag);
		}

		private static void WriteObject(ValueWriter writer, GenericKeyLookup lookup, string name, object value) {
			ArgumentNullException.ThrowIfNull(value);
			
			int id = GetPayloadId(value.GetType());

			if (value is byte b) {
				if (b <= 1) {
					// Assume boolean
					id = ID_BOOL;
					value = b == 1;
				}
			} else if (value is int i) {
				if (i >= -4096 && i < 4096)
					id = ID_TINY_INT;
			} else if (value is short s) {
				if (s >= -64 && s < 64)
					id = ID_TINY_SHORT;
			} else if (value is long l) {
				if (l >= -262144 && l < 262144)
					id = ID_TINY_LONG;
			}

			writer.Write((byte)id, numBits: SIZE_ID);
			lookup.WriteKeyIndex(writer, name);

			// Size optimization: the "<type>" key is used by types that inherit from TagSerializable, so put its parts in the lookup
			if (name == "<type>" && value is string tagSerializableType) {
				List<string> slices = [.. EnumerateSerializedTypeSlices(tagSerializableType).Where(s => !string.IsNullOrEmpty(s))];

				_collectionLengthTiers.WriteTo(writer, (uint)slices.Count);
				foreach (string slice in slices)
					lookup.WriteKeyIndex(writer, slice);

				return;
			}
			
			_payloadHandlers[id].Write(writer, value);
		}

		private static TagCompound ReadTag(ValueReader reader, GenericKeyLookup lookup) {
			_tagKeyReaderLookup.AddOrUpdate(reader, lookup);

			var obj = ReadObject(reader, lookup, out _);
			if (obj is not TagCompound tag)
				throw new InvalidOperationException("Root tag is not a TagCompound");

			return tag;
		}

		private static object ReadObject(ValueReader reader, GenericKeyLookup lookup, out string name) {
			int id = reader.ReadByte(SIZE_ID);
			if (id == 0) {
				name = null;
				return null;
			}

			name = lookup.ReadKey(reader);

			if (name == "<type>") {
				if (id != ID_STRING)
					throw new InvalidOperationException("Expected string payload for \"<type>\" key");

				int sliceCount = (int)_collectionLengthTiers.ReadFrom(reader);
				StringBuilder sb = new();

				for (int i = 0; i < sliceCount; i++)
					sb.Append(lookup.ReadKey(reader));

				return sb.ToString();
			}

			return _payloadHandlers[id].Read(reader);
		}
	}
}
