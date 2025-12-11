using MagicStorage.Items.ErrorDisplay;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;

#nullable enable
namespace MagicStorage.Common.IO {
	public static partial class SaveCompression {
		public static void SaveItem(Item item, BinaryWriter writer, bool writeStack = true, bool writeFavorite = true) {
			ValueWriter valueWriter = new(writer);
			SaveItem(item, valueWriter, writeStack, writeFavorite);
			valueWriter.Flush();
		}

		public static void SaveItem(Item item, ValueWriter writer, bool writeStack, bool writeFavorite) {
			SaveItemInternal(true, item, writer, writeStack, writeFavorite, new ItemContentNameLookup(), new GenericKeyLookup(), null, null, null);
		}

		private static void SaveItemInternal(
			bool isolated,
			Item item,
			ValueWriter writer,
			bool writeStack,
			bool writeFavorite,
			ItemContentNameLookup contentLookup,
			GenericKeyLookup tagKeyLookup,
			StackCompressor? maxStackWriter,
			TagCompound? saveData,
			List<GlobalSaveData>? globalSaveData
		) {
			// This method aims to ensure that no matter the mods loaded or unloaded, the compressed data reads correctly
			// NetCompression doesn't need to worry about this since mods can't be unloaded during gameplay, but that isn't the case for save data

			if (isolated) {
				maxStackWriter = new();
				writer.Write((uint)maxStackWriter.CommonMaxStack, BitBuffer128.MAX_INT - 1);
				
				PrepareLookupsAndData(contentLookup, tagKeyLookup, item, out saveData, out globalSaveData);

				contentLookup.SaveTo(writer);
				tagKeyLookup.SaveTo(writer);
			}

			Debug.Assert(maxStackWriter is not null);

			using (writer.CreateScope(NetCompression.lengthTiers, optimizeForBytes: false)) {
				// Write the ID of the item
				if (item.ModItem is ModItem modItem) {
					writer.Write(true);

					if (modItem is UnloadedItem unloadedItem) {
						contentLookup.WriteModNameIndex(writer, unloadedItem.ModName);
						contentLookup.WriteContentNameIndex(writer, unloadedItem.ItemName);
					} else if (modItem is BaseErrorDummyItem errorItem) {
						contentLookup.WriteModNameIndex(writer, errorItem.OriginalMod);
						contentLookup.WriteContentNameIndex(writer, errorItem.OriginalName);
					} else
						contentLookup.WriteNameIndices(writer, modItem);
				} else {
					writer.Write(false);

					// Reminder: netID can be negative!
					writer.Write(item.netID, NetCompression.GetBitSize(ItemID.Count) + 1);
				}

				// Write the prefix for the item
				int prefix = item.prefix;
				if (item.ModItem is BaseErrorDummyItem errorItemForPrefix)
					prefix = errorItemForPrefix.OriginalPrefix;

				if (PrefixLoader.GetPrefix(item.prefix) is ModPrefix modPrefix) {
					writer.Write(true);

					string prefixMod, prefixName;

					if (modPrefix is UnloadedPrefix) {
						var globalItem = item.GetGlobalItem<UnloadedGlobalItem>();
						prefixMod = globalItem.ModPrefixMod;
						prefixName = globalItem.ModPrefixName;
					} else {
						prefixMod = modPrefix.Mod.Name;
						prefixName = modPrefix.Name;
					}

					contentLookup.WriteModNameIndex(writer, prefixMod);
					contentLookup.WriteContentNameIndex(writer, prefixName);
				} else {
					writer.Write(false);

					if (item.prefix != 0 && item.prefix < PrefixID.Count) {
						writer.Write(true);
						writer.Write((byte)item.prefix, NetCompression.GetBitSize(PrefixID.Count));
					} else
						writer.Write(false);
				}

				// Write the maximum and current stack of the item
				if (writeStack) {
					bool customMaxStack = item.maxStack != maxStackWriter.CommonMaxStack;
					bool isPartialOrOverflow = item.stack != item.maxStack;

					writer.Write(customMaxStack);
					writer.Write(isPartialOrOverflow);

					if (customMaxStack)
						maxStackWriter.WriteTo(writer, item.maxStack);
					if (isPartialOrOverflow)
						maxStackWriter.WriteTo(writer, item.stack, maximum: item.maxStack);
				}

				if (writeFavorite)
					writer.Write(item.favorited);
			}

			// Write the modded data for the item
			if (saveData is { Count: > 0 }) {
				writer.Write(true);

				using (writer.CreateScope(NetCompression.lengthTiers, optimizeForBytes: false))
					WriteTag(writer, tagKeyLookup, saveData);
			} else
				writer.Write(false);

			if (globalSaveData is { Count: > 0 }) {
				writer.Write(true);

				NetCompression.lengthTiers.WriteTo(writer, (uint)globalSaveData.Count);

				foreach (var globalData in globalSaveData) {
					using (writer.CreateScope(NetCompression.lengthTiers, optimizeForBytes: false)) {
						contentLookup.WriteModNameIndex(writer, globalData.ModName);
						contentLookup.WriteContentNameIndex(writer, globalData.Name);
						WriteTag(writer, tagKeyLookup, globalData.Data);
					}
				}
			} else
				writer.Write(false);
		}

		public static void SaveItems(List<Item> items, BinaryWriter writer, bool writeStacks = true, bool writeFavorites = true, int? listCountBitSizeOverride = null) {
			ValueWriter valueWriter = new(writer);
			SaveItems(items, valueWriter, writeStacks, writeFavorites, listCountBitSizeOverride);
			valueWriter.Flush();
		}

		public static void SaveItems(List<Item> items, ValueWriter writer, bool writeStacks = true, bool writeFavorites = true, int? listCountBitSizeOverride = null) {
			StackCompressor maxStackWriter = new();
			writer.Write((uint)maxStackWriter.CommonMaxStack, BitBuffer128.MAX_INT - 1);

			// Initialize the lookup tables and data
			ItemContentNameLookup contentLookup = new();
			GenericKeyLookup tagKeyLookup = new();
			List<TagCompound?> itemSaveData = [];
			List<List<GlobalSaveData>?> globalItemSaveData = [];

			foreach (Item item in items) {
				PrepareLookupsAndData(contentLookup, tagKeyLookup, item, out var saveData, out var globalSaveData);
				itemSaveData.Add(saveData);
				globalItemSaveData.Add(globalSaveData);
			}

			// Serialize the data
			contentLookup.SaveTo(writer);
			tagKeyLookup.SaveTo(writer);

			if (listCountBitSizeOverride is { } predefinedCount)
				writer.Write((uint)items.Count, predefinedCount);
			else
				NetCompression.lengthTiers.WriteTo(writer, (uint)items.Count);

			for (int i = 0; i < items.Count; i++) {
				using (writer.CreateScope(NetCompression.lengthTiers, optimizeForBytes: false))
					SaveItemInternal(false, items[i], writer, writeStacks, writeFavorites, contentLookup, tagKeyLookup, maxStackWriter, itemSaveData[i], globalItemSaveData[i]);
			}
		}

		public static Item LoadItem(BinaryReader reader, bool readStack = true, bool readFavorite = true) {
			ValueReader valueReader = new(reader);
			return LoadItem(valueReader, readStack, readFavorite);
		}

		public static Item LoadItem(ValueReader reader, bool readStack = true, bool readFavorite = true) {
			StackCompressor maxStackReader = new();
			ItemContentNameLookup contentLookup = new();
			GenericKeyLookup tagKeyLookup = new();

			maxStackReader.SetCommonMaxStack((int)reader.ReadUInt32(BitBuffer128.MAX_INT - 1));
			contentLookup.LoadFrom(reader);
			tagKeyLookup.LoadFrom(reader);

			return LoadItemInternal(reader, readStack, readFavorite, true, contentLookup, tagKeyLookup, maxStackReader, out _);
		}

		private static Item LoadItemInternal(
			ValueReader reader,
			bool readStack,
			bool readFavorite,
			bool isolated,
			ItemContentNameLookup contentLookup,
			GenericKeyLookup tagKeyLookup,
			StackCompressor maxStackReader,
			out Exception? error
		) {
			Item? item = null;
			DeserializedNetItem readData = new();

			error = null;
			bool nbtFail = false;

			IDisposable? scope = null;

			try {
				scope = reader.ReadScope(NetCompression.lengthTiers, optimizeForBytes: false);
				item = ReadItemMetadata(reader, readStack, readFavorite, contentLookup, maxStackReader, ref readData);
			} catch (Exception ex) {
				error = ex;
			} finally {
				try {
					scope?.Dispose();
				} catch (Exception ex) {
					error = error is null ? ex : new AggregateException(error, ex);
				}
			}
			
			TagCompound? saveData = null;
			List<TagCompound>? globalSaveData = null;
			bool forcedError = false;
			if (item is null) {
				forcedError = true;
				goto FailImmediately;
			}

			// Read the modded data for the item
			bool hasSaveData = reader.ReadBoolean();
			if (hasSaveData) {
				// Always read the data if the bit indicated as such, even when it won't actually be used
				saveData = ReadItemModData(reader, item, tagKeyLookup, ref error, ref nbtFail);
			}

			bool hasGlobalData = reader.ReadBoolean();
			if (hasGlobalData) {
				// Always read the data if the bit indicated as such, even when it won't actually be used
				globalSaveData = ReadItemGlobalModData(reader, item, contentLookup, tagKeyLookup, ref error, ref nbtFail);
			}

			FailImmediately:

			if (forcedError || error is not null) {
				if (isolated) {
					if (error is AggregateException aggregate)
						error = aggregate.Flatten();

					if (!forcedError) {
						if (item is not null)
							MagicStorageMod.Instance.Logger.Error($"Error loading item \"{item.IdentifierAndStack()}\" from compressed stream", error);
						else
							MagicStorageMod.Instance.Logger.Error($"Error loading unknown item from compressed stream", error);
					} else {
						if (error is not null)
							MagicStorageMod.Instance.Logger.Error("Error loading unknown item from compressed stream", error);
						else
							MagicStorageMod.Instance.Logger.Error("Error loading unknown item from compressed stream for unknown reason");
					}
				}

				// Replace with an error item
				TagCompound tag = readData.ToTagData();
				tag["data"] = saveData;
				tag["globalData"] = globalSaveData;
				item = Utility.PrepareFailureItem(nbtFail ? BaseErrorDummyItem.NBTFailItemType : BaseErrorDummyItem.NetReadFailItemType, tag, readData);
			}

			Debug.Assert(item is not null);

			return item;
		}

		private static Item ReadItemMetadata(
			ValueReader reader,
			bool readStack,
			bool readFavorite,
			ItemContentNameLookup contentLookup,
			StackCompressor maxStackReader,
			ref DeserializedNetItem readData
		) {
			Item item;

			// Read the ID of the item
			bool isModdedItem = reader.ReadBoolean();
			if (isModdedItem) {
				(string modName, string name) = contentLookup.ReadNames(reader);
				int type = contentLookup.GetContentType<ModItem>(modName, name);
				
				item = new Item(type);

				if (item.ModItem is UnloadedItem unloadedItem) {
					unloadedItem.ModName = modName;
					unloadedItem.ItemName = name;
				}
			} else {
				// Reminder: netID can be negative!
				int netID = reader.ReadInt32(NetCompression.GetBitSize(ItemID.Count) + 1);
				item = new Item(netID);
			}

			readData.type = item.type;

			// Read the prefix of the item
			bool hasModdedPrefix = reader.ReadBoolean();
			int prefix = 0;
			if (hasModdedPrefix) {
				(string modName, string name) = contentLookup.ReadNames(reader);
				if (!ModContent.TryFind(modName, name, out ModPrefix modPrefix)) {
					prefix = ModContent.PrefixType<UnloadedPrefix>();

					UnloadedGlobalItem unloadedGlobalItem = item.GetGlobalItem<UnloadedGlobalItem>();
					unloadedGlobalItem.ModPrefixMod = modName;
					unloadedGlobalItem.ModPrefixName = name;
				} else
					prefix = modPrefix.Type;

				readData.modPrefixMod = modName;
				readData.modPrefixName = name;
			} else {
				bool hasPrefix = reader.ReadBoolean();

				if (hasPrefix)
					prefix = reader.ReadByte(NetCompression.GetBitSize(PrefixID.Count));

				if (prefix != 0 && prefix < PrefixID.Count)
					readData.prefix = prefix;
			}

			if (prefix != 0)
				item.Prefix(prefix);

			// Read the maximum and current stack of the item
			if (readStack) {
				bool hasCustomStack = reader.ReadBoolean();
				bool isPartialOrOverflow = reader.ReadBoolean();

				item.maxStack = hasCustomStack ? maxStackReader.ReadFrom(reader) : maxStackReader.CommonMaxStack;
				readData.stack = item.stack = isPartialOrOverflow ? maxStackReader.ReadFrom(reader, item.maxStack) : item.maxStack;
			}

			if (readFavorite)
				readData.favorite = item.favorited = reader.ReadBoolean();
			
			return item;
		}

		private static TagCompound? ReadItemModData(
			ValueReader reader,
			Item item,
			GenericKeyLookup tagKeyLookup,
			ref Exception? error,
			ref bool nbtFail
		) {
			TagCompound? tagData = null;
			
			IDisposable? scope = null;

			try {
				tagData = ReadTag(reader, tagKeyLookup);

				if (item.ModItem is not UnloadedItem) {
					// Only flag that an error occurred; the remaining data still needs to be read
					try {
						item.ModItem?.LoadData(tagData);
					} catch (Exception ex) {
						if (error is null && ex.Message.Contains("NBT Deserialization") || ex.Message.Contains("NBT Serialization"))
							nbtFail = true;

						Exception errorWithReason = new InvalidOperationException($"Error loading mod data for item {item.ModItem.Name} from mod {item.ModItem.Mod.Name}.", ex);
						error = error is null ? errorWithReason : new AggregateException(error, errorWithReason);
					}
				}
			} catch (Exception ex) {
				error = error is null ? ex : new AggregateException(error, ex);
				nbtFail = false;
			} finally {
				try {
					scope?.Dispose();
				} catch (Exception ex) {
					error = error is null ? ex : new AggregateException(error, ex);
					nbtFail = false;
				}
			}

			if (item.ModItem is UnloadedItem unloadedItem)
				unloadedItem.data = tagData;

			return tagData;
		}

		private static List<TagCompound>? ReadItemGlobalModData(
			ValueReader reader,
			Item item,
			ItemContentNameLookup contentLookup,
			GenericKeyLookup tagKeyLookup,
			ref Exception? error,
			ref bool nbtFail
		) {
			List<TagCompound>? globalData = [];
			List<Exception> globalErrors = [];

			IDisposable? scope = null;

			int globalCount = (int)NetCompression.lengthTiers.ReadFrom(reader);
			for (int i = 0; i < globalCount; i++) {
				TagCompound? globalTagData = null;

				try {
					scope = reader.ReadScope(NetCompression.lengthTiers, optimizeForBytes: false);

					(string modName, string name) = contentLookup.ReadNames(reader);
					globalTagData = ReadTag(reader, tagKeyLookup);

					if (ModContent.TryFind(modName, name, out GlobalItem globalItem) && item.TryGetGlobalItem(globalItem, out globalItem)) {
						// Only flag that an error occurred; the remaining data still needs to be read
						try {
							globalItem.LoadData(item, globalTagData);
						} catch (Exception ex) {
							if (error is null && globalErrors.Count == 0 && ex.Message.Contains("NBT Deserialization") || ex.Message.Contains("NBT Serialization"))
								nbtFail = true;

							globalErrors.Add(new InvalidOperationException($"Error loading global mod data for global item {globalItem.Name} from mod {globalItem.Mod.Name}.", ex));
						}
					} else
						item.GetGlobalItem<UnloadedGlobalItem>().data.Add(globalTagData);
				} catch (Exception ex) {
					globalErrors.Add(ex);
					nbtFail = false;
				} finally {
					try {
						scope?.Dispose();
					} catch (Exception ex) {
						globalErrors.Add(ex);
						nbtFail = false;
					}
				}

				if (globalTagData is not null)
					globalData.Add(globalTagData);
			}

			if (globalErrors.Count > 0) {
				if (globalErrors.Count > 1) {
					var aggregate = new AggregateException(globalErrors);
					error = error is null ? aggregate : new AggregateException(error, aggregate);
				} else
					error = error is null ? globalErrors[0] : new AggregateException(error, globalErrors[0]);
			}

			return globalData is { Count: > 0 } ? globalData : null;
		}

		public static List<Item> LoadItems(BinaryReader reader, bool readStacks = true, bool readFavorites = true, int? listCountBitSizeOverride = null) {
			ValueReader valueReader = new(reader);
			return LoadItems(valueReader, readStacks, readFavorites, listCountBitSizeOverride);
		}

		public static List<Item> LoadItems(ValueReader reader, bool readStacks = true, bool readFavorites = true, int? listCountBitSizeOverride = null) {
			StackCompressor maxStackReader = new();
			ItemContentNameLookup contentLookup = new();
			GenericKeyLookup tagKeyLookup = new();

			maxStackReader.SetCommonMaxStack((int)reader.ReadUInt32(BitBuffer128.MAX_INT - 1));
			contentLookup.LoadFrom(reader);
			tagKeyLookup.LoadFrom(reader);

			uint read = listCountBitSizeOverride is { } predefinedCount
				? reader.ReadUInt32(predefinedCount)
				: NetCompression.lengthTiers.ReadFrom(reader);
			int itemCount = (int)read;

			List<Item> items = new();

			for (int i = 0; i < itemCount; i++) {
				IDisposable? scope = null;
				Item? item = null;
				Exception? error = null;

				try {
					scope = reader.ReadScope(NetCompression.lengthTiers, optimizeForBytes: false);
					item = LoadItemInternal(reader, readStacks, readFavorites, false, contentLookup, tagKeyLookup, maxStackReader, out error);
				} catch (Exception ex) {
					error = ex;
				} finally {
					try {
						scope?.Dispose();
					} catch (Exception ex) {
						error = error is null ? ex : new AggregateException(error, ex);
					}
				}

				if (error is not null) {
					if (error is AggregateException aggregate)
						error = aggregate.Flatten();

					if (item is not null)
						MagicStorageMod.Instance.Logger.Error($"Error loading item \"{item.IdentifierAndStack()}\" from compressed stream", error);
					else
						MagicStorageMod.Instance.Logger.Error($"Error loading unknown item from compressed stream", error);
				}
			}

			return items;
		}

		private record class GlobalSaveData(string ModName, string Name, TagCompound Data);

		private static void PrepareLookupsAndData(
			ItemContentNameLookup contentLookup,
			GenericKeyLookup tagKeyLookup,
			Item item,
			out TagCompound? data,
			out List<GlobalSaveData>? globalData
		) {
			// Populate the lookup with any names that should be compressed
			
			if (item.ModItem is ModItem modItem) {
				if (modItem is UnloadedItem unloadedItem) {
					contentLookup.AddMod(unloadedItem.ModName);
					contentLookup.AddContent(unloadedItem.ItemName);

					data = unloadedItem.data;
				} else if (modItem is BaseErrorDummyItem errorItem) {
					contentLookup.AddMod(errorItem.OriginalMod);
					contentLookup.AddContent(errorItem.OriginalName);

					data = errorItem.data;
				} else {
					contentLookup.Add(modItem);

					data = [];
					modItem.SaveData(data);
				}

				if (data is { Count: > 0 }) {
					// March through the tag and add each entry's name
					PrepareTagLookup(tagKeyLookup, data);
				} else
					data = null;
			} else
				data = null;

			int prefix = item.prefix;
			if (item.ModItem is BaseErrorDummyItem errorItemForPrefix)
				prefix = errorItemForPrefix.OriginalPrefix;

			if (PrefixLoader.GetPrefix(item.prefix) is ModPrefix modPrefix) {
				if (modPrefix is UnloadedPrefix) {
					var globalItem = item.GetGlobalItem<UnloadedGlobalItem>();
					contentLookup.AddMod(globalItem.ModPrefixMod);
					contentLookup.AddContent(globalItem.ModPrefixName);
				} else
					contentLookup.Add(modPrefix);
			}

			if (item.EntityGlobals.Length > 0) {
				globalData = [];

				foreach (var globalItem in ItemLoader.HookSaveData.Enumerate(item)) {
					TagCompound saveData;

					// UnloadedGlobalItem is not saved directly
					if (globalItem is UnloadedGlobalItem unloadedGlobalItem) {
						foreach (var tag in unloadedGlobalItem.data) {
							if (tag.TryGet("mod", out string modName) && tag.TryGet("name", out string name) && tag.TryGet("data", out saveData) && saveData.Count > 0) {
								contentLookup.AddMod(modName);
								contentLookup.AddContent(name);
								PrepareTagLookup(tagKeyLookup, saveData);

								globalData.Add(new GlobalSaveData(modName, name, saveData));
							}
						}

						continue;
					}

					saveData = [];
					globalItem.SaveData(item, saveData);

					if (saveData.Count > 0) {
						contentLookup.Add(globalItem);
						PrepareTagLookup(tagKeyLookup, saveData);

						globalData.Add(new GlobalSaveData(globalItem.Mod.Name, globalItem.Name, saveData));
					}
				}

				if (globalData.Count == 0)
					globalData = null;
			} else
				globalData = null;
		}

		private static void PrepareTagLookup(GenericKeyLookup lookup, TagCompound root) {
			Queue<TagCompound> queue = [];
			queue.Enqueue(root);

			Span<Range> allocatedRanges = stackalloc Range[1024];

			while (queue.TryDequeue(out TagCompound? current)) {
				foreach ((string key, object value) in current) {
					lookup.AddKey(key);

					if (key == "<type>" && value is string tagSerializableType) {
						foreach (string slice in EnumerateSerializedTypeSlices(tagSerializableType)) {
							if (!string.IsNullOrEmpty(slice))
								lookup.AddKey(slice);
						}
					}

					if (value is TagCompound tag)
						queue.Enqueue(tag);
					else if (value is IList<TagCompound> list) {
						foreach (TagCompound listTag in list)
							queue.Enqueue(listTag);
					}
				}
			}
		}

		private struct TypeModifiers {
			public bool array;
			public bool genericDefinition;
			public bool genericArgument;
		}

		private static IEnumerable<string> EnumerateSerializedTypeSlices(string keyString) {
			Stack<TypeModifiers> typeStack = [];
			TypeModifiers modifier = default;

			int start = 0;
			for (int i = 0; i < keyString.Length; i++) {
				char current = keyString[i];

				if (current is '.') {
					// Namespace or class separation identifier

					// Namespace.Class...
					//          ^

					yield return keyString[start..(i++)];

					yield return ".";

					start = i;
					current = i < keyString.Length ? keyString[i] : default;
				}

				if (current is '`') {
					// Generic argument count identifier
					modifier.genericDefinition = true;

					// Scan past the count
					while (++i < keyString.Length && keyString[i] is >= '0' and <= '9') ;

					yield return keyString[start..i];

					start = i;
					current = i < keyString.Length ? keyString[i] : default;
				}
				
				if (current is '[') {
					if (modifier.genericDefinition) {
						if (i < keyString.Length - 1 && keyString[i + 1] is '[') {
							// Start of the argument list

							// ...Generic`1[[Namespace.Class...
							//             ^

							yield return keyString[start..(i++)];

							yield return "[[";

							typeStack.Push(modifier);
							modifier = new() { genericArgument = true };

							start = ++i;
							current = i < keyString.Length ? keyString[i] : default;

							goto AfterOpenBracket;
						} else if (i > 0 && keyString[i - 1] is ',') {
							// Successive generic argument

							// ..., PublicKeyToken=null],[Namespace.Class...
							//                           ^

							yield return keyString[start..(++i)];

							typeStack.Push(modifier);
							modifier = new() { genericArgument = true };

							start = i;
							current = i < keyString.Length ? keyString[i] : default;

							goto AfterOpenBracket;
						}
					}

					// Array type

					// ...Class[]
					//         ^

					modifier.array = true;

					start = i;
					current = i < keyString.Length ? keyString[i] : default;
				}

				AfterOpenBracket:

				if (modifier.genericArgument && current is ',') {
					// End of generic argument type, start of type assembly

					// ...[Namespace.Class, Source.Assembly, Version=...
					//                    ^

					yield return keyString[start..i];

					yield return ", ";

					i += 2;
					start = i;

					// Scan past the assembly identifier
					while (++i < keyString.Length && keyString[i] is not ']') ;

					yield return keyString[start..i];

					modifier = typeStack.Pop();

					start = i;
					current = i < keyString.Length ? keyString[i] : default;
				} else if (modifier.array && current is '*' or ',') {
					// Array rank indentifiers

					// ...Class[*]
					//          ^
					// ...Class[,]
					//          ^
					// ...Class[,,]
					//          ^^

					while (++i < keyString.Length && keyString[i] is not ']') ;

					current = i < keyString.Length ? keyString[i] : default;
				}

				if (current is ']') {
					if (modifier.array) {
						// End of array type

						// ...Class[]
						//          ^
						// ...Class[*]
						//           ^
						// ...Class[,,]
						//            ^

						modifier.array = false;

						yield return keyString[start..(++i)];

						start = i;
					} else if (modifier.genericDefinition && i < keyString.Length - 1 && keyString[i + 1] is ']') {
						// End of argument list

						// ..., PublicKeyToken=null]]
						//                         ^

						yield return "]]";

						i += 2;
						start = i;
					}
				}
			}

			if (start < keyString.Length)
				yield return keyString[start..];
		}
	}
}
