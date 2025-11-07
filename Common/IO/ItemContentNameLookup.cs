using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;

namespace MagicStorage.Common.IO {
	internal class ItemContentNameLookup {
		private readonly Dictionary<string, int> _modToindex = [];
		private readonly Dictionary<string, int> _nameToIndex = [];
		private readonly List<string> _mods = [];
		private readonly List<string> _names = [];

		public int ModCount => _mods.Count;

		public int NameCount => _names.Count;

		[StackTraceHidden]
		private static void ThrowIfNotSupported<T>()
			where T : ModType
		{
			if (typeof(T) != typeof(ModItem) && typeof(T) != typeof(ModPrefix) && typeof(T) != typeof(GlobalItem))
				throw new NotSupportedException($"Unsupported ModType: {typeof(T)}");
		}

		public (int modNameIndex, int nameIndex) Add<T>(T baseInstance)
			where T : ModType
		{
			ThrowIfNotSupported<T>();

			ArgumentNullException.ThrowIfNull(baseInstance);

			return (AddMod(baseInstance.Mod.Name), AddContent(baseInstance.Name));
		}

		public int AddMod(string modName) {
			if (!_modToindex.TryGetValue(modName, out int modNameIndex)) {
				modNameIndex = _mods.Count;
				_mods.Add(modName);
				_modToindex[modName] = modNameIndex;
			}

			return modNameIndex;
		}

		public int GetModNameIndex(string modName) => _modToindex[modName];

		public int AddContent(string name) {
			if (!_nameToIndex.TryGetValue(name, out int nameIndex)) {
				nameIndex = _names.Count;
				_names.Add(name);
				_nameToIndex[name] = nameIndex;
			}

			return nameIndex;
		}

		public int GetContentNameIndex(string name) => _nameToIndex[name];

		public string GetFullName(int modNameIndex, int nameIndex) {
			if (modNameIndex < 0 || modNameIndex >= _mods.Count)
				return null;

			if (nameIndex < 0 || nameIndex >= _names.Count)
				return null;

			return $"{_mods[modNameIndex]}/{_names[nameIndex]}";
		}

		public static string GetFullName(string modName, string name) {
			if (modName is null || name is null)
				return null;

			return $"{modName}/{name}";
		}

		public int GetContentType<T>(int modNameIndex, int nameIndex)
			where T : ModType
		{
			return GetContentType<T>(GetFullName(modNameIndex, nameIndex));
		}

		public int GetContentType<T>(string modName, string name)
			where T : ModType
		{
			return GetContentType<T>(GetFullName(modName, name));
		}

		private int GetContentType<T>(string fullName)
			where T : ModType
		{
			ThrowIfNotSupported<T>();

			if (typeof(T) == typeof(GlobalItem))
				throw new InvalidOperationException("GlobalItem does not have type IDs");
			
			if (!ModContent.TryFind(fullName, out T instance)) {
				if (typeof(T) == typeof(ModItem))
					return ModContent.ItemType<UnloadedItem>();
				else if (typeof(T) == typeof(ModPrefix))
					return ModContent.PrefixType<UnloadedPrefix>();
				
				throw null;  // Code shouldn't end up here, but just in case
			}
			
			if (typeof(T) == typeof(ModItem))
				return ((ModItem)(ModType)instance).Type;
			else if (typeof(T) == typeof(ModPrefix))
				return ((ModPrefix)(ModType)instance).Type;
			
			throw null;
		}

		public void WriteNameIndices<T>(ValueWriter writer, T baseInstance)
			where T : ModType
		{
			ThrowIfNotSupported<T>();

			ArgumentNullException.ThrowIfNull(baseInstance);

			WriteModNameIndex(writer, baseInstance.Mod.Name);
			WriteContentNameIndex(writer, baseInstance.Name);
		}

		public void WriteModNameIndex(ValueWriter writer, string modName) {
			WriteModNameIndex(writer, GetModNameIndex(modName));
		}

		public void WriteModNameIndex(ValueWriter writer, int index) {
			writer.Write((ushort)index, NetCompression.GetBitSize(ModCount));
		}

		public void WriteContentNameIndex(ValueWriter writer, string name) {
			WriteContentNameIndex(writer, GetContentNameIndex(name));
		}

		public void WriteContentNameIndex(ValueWriter writer, int index) {
			writer.Write((ushort)index, NetCompression.GetBitSize(NameCount));
		}

		public (string modName, string name) ReadNames(ValueReader reader) {
			return (ReadModName(reader), ReadContentName(reader));
		}

		public (int modNameIndex, int nameIndex) ReadNameIndices(ValueReader reader) {
			return (ReadModNameIndex(reader), ReadContentNameIndex(reader));
		}

		public int ReadContentType<T>(ValueReader reader)
			where T : ModType
		{
			ThrowIfNotSupported<T>();

			int modNameIndex = ReadModNameIndex(reader);
			int nameIndex = ReadContentNameIndex(reader);
			return GetContentType<T>(modNameIndex, nameIndex);
		}

		public string ReadModName(ValueReader reader) {
			ushort modIndex = reader.ReadUInt16(NetCompression.GetBitSize(ModCount));
			return _mods[modIndex];
		}

		public int ReadModNameIndex(ValueReader reader) {
			ushort modIndex = reader.ReadUInt16(NetCompression.GetBitSize(ModCount));
			return modIndex;
		}

		public string ReadContentName(ValueReader reader) {
			ushort nameIndex = reader.ReadUInt16(NetCompression.GetBitSize(NameCount));
			return _names[nameIndex];
		}

		public int ReadContentNameIndex(ValueReader reader) {
			ushort nameIndex = reader.ReadUInt16(NetCompression.GetBitSize(NameCount));
			return nameIndex;
		}

		public void SaveTo(ValueWriter writer) {
			writer.Write((ushort)_mods.Count, BitBuffer128.MAX_SHORT);
			foreach (string mod in _mods)
				StringCompressor.WriteTo(writer, mod);

			writer.Write((ushort)_names.Count, BitBuffer128.MAX_SHORT);
			foreach (string name in _names)
				StringCompressor.WriteTo(writer, name);
		}

		public void LoadFrom(ValueReader reader) {
			_mods.Clear();
			_modToindex.Clear();
			_names.Clear();
			_nameToIndex.Clear();

			ushort modCount = reader.ReadUInt16(BitBuffer128.MAX_SHORT);
			for (int i = 0; i < modCount; i++) {
				string mod = StringCompressor.ReadFrom(reader);
				_mods.Add(mod);
				_modToindex[mod] = i;
			}

			ushort nameCount = reader.ReadUInt16(BitBuffer128.MAX_SHORT);
			for (int i = 0; i < nameCount; i++) {
				string name = StringCompressor.ReadFrom(reader);
				_names.Add(name);
				_nameToIndex[name] = i;
			}
		}
	}
}
