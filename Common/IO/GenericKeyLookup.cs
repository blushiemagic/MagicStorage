using System.Collections.Generic;

namespace MagicStorage.Common.IO {
	internal class GenericKeyLookup {
		private readonly Dictionary<string, int> _keyToIndex = [];
		private readonly List<string> _keys = [];

		public int KeyCount => _keys.Count;

		public GenericKeyLookup() {
			// The root object has an empty name
			AddKey("");
		}

		public int AddKey(string key) {
			if (!_keyToIndex.TryGetValue(key, out int keyIndex)) {
				keyIndex = _keys.Count;
				_keys.Add(key);
				_keyToIndex[key] = keyIndex;
			}

			return keyIndex;
		}

		public int GetKeyIndex(string key) => _keyToIndex[key];

		public void WriteKeyIndex(ValueWriter writer, string key) {
			WriteKeyIndex(writer, GetKeyIndex(key));
		}

		public void WriteKeyIndex(ValueWriter writer, int index) {
			writer.Write((ushort)index, NetCompression.GetBitSize(KeyCount));
		}

		public string ReadKey(ValueReader reader) {
			ushort keyIndex = reader.ReadUInt16(NetCompression.GetBitSize(KeyCount));
			return _keys[keyIndex];
		}

		public void SaveTo(ValueWriter writer) {
			writer.Write((ushort)_keys.Count, BitBuffer128.MAX_SHORT);
			foreach (string key in _keys)
				StringCompressor.WriteTo(writer, key);
		}

		public void LoadFrom(ValueReader reader) {
			_keyToIndex.Clear();
			_keys.Clear();

			ushort keyCount = reader.ReadUInt16(BitBuffer128.MAX_SHORT);
			for (int i = 0; i < keyCount; i++) {
				string key = StringCompressor.ReadFrom(reader);
				_keys.Add(key);
				_keyToIndex[key] = i;
			}
		}
	}
}
