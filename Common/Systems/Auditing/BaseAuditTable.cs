using System;
using System.Collections.Generic;
using System.IO;

namespace MagicStorage.Common.Systems.Auditing {
	internal abstract class BaseAuditTable<TSource, TKey, TEntry> : IAuditable<BaseAuditTable<TSource, TKey, TEntry>> where TEntry : IAuditable<TEntry>, IAuditableEntry<TEntry, TSource, TKey>, new() {
		private readonly List<TEntry> _entries = [];
		private readonly Dictionary<TKey, int> _keyToIndex = [];

		public int Add(TSource obj) {
			TEntry entry = TEntry.CreateFrom(obj);
			TKey key = TEntry.GetKey(entry);

			if (_keyToIndex.TryAdd(key, _entries.Count)) {
				_entries.Add(entry);
				return _entries.Count - 1;
			}

			return _keyToIndex[key];
		}

		public int FindIndex(TSource obj) => FindIndex(TEntry.GetKey(obj));

		public int FindIndex(TKey key) => _keyToIndex.TryGetValue(key, out int index) ? index : -1;

		public TKey GetKeyFromIndex(int index) => index >= 0 && index < _entries.Count ? TEntry.GetKey(_entries[index]) : default;

		public string GetNameFromIndex(int index) => index >= 0 && index < _entries.Count ? TEntry.GetName(_entries[index]) : null;

		public string GetNameFromKey(TKey key) => _keyToIndex.TryGetValue(key, out int index) ? GetNameFromIndex(index) : null;

		public static void DeserializeOne<T>(BinaryReader reader, ref T instance) where T : BaseAuditTable<TSource, TKey, TEntry> {
			try {
				int count = reader.ReadUInt16();

				for (int i = 0; i < count; i++) {
					TEntry entry = new();
					TEntry.DeserializeOne(reader, ref entry);
					instance._entries.Add(entry);
					instance._keyToIndex.Add(TEntry.GetKey(entry), i);
				}
			} catch (Exception ex) {
				MagicStorageMod.Instance.Logger.Error($"Failed to deserialize {instance.GetType().Name}", ex);
				instance._entries.Clear();
				instance._keyToIndex.Clear();
			}
		}

		public void Serialize(BinaryWriter writer) {
			try {
				writer.Write((ushort)_entries.Count);
				foreach (TEntry entry in _entries)
					entry.Serialize(writer);
			} catch (Exception ex) {
				MagicStorageMod.Instance.Logger.Error($"Failed to serialize {GetType().Name}", ex);
			}
		}
	}
}
