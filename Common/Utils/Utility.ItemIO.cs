using MagicStorage.Common.IO;
using MagicStorage.Items.ErrorDisplay;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;

namespace MagicStorage {
	partial class Utility {
		public static TagCompound SaveItem(Item item) {
			if (item.ModItem is BaseErrorDummyItem errorItem) {
				if (errorItem.data is not { Count: > 0 })
					return new TagCompound();

				return errorItem.data;
			}

			// Use standard saving
			return ItemIO.Save(item);
		}

		public static Item SafelyLoadItem(TagCompound tag) {
			Item loadedItem = null;
			int failureType = BaseErrorDummyItem.NetReadFailItemType;

			try {
				loadedItem = ItemIO.Load(tag);
			} catch (KeyNotFoundException) {
				// Item was malformed
			} catch (Exception ex) {
				MagicStorageMod.Instance.Logger.Error("Error loading item from tag", ex);

				if (ex.Message.Contains("NBT Deserialization"))
					failureType = BaseErrorDummyItem.NBTFailItemType;
			} finally {
				loadedItem ??= PrepareFailureItem(failureType, tag, DeserializedNetItem.FromTagData(tag));
			}

			return loadedItem;
		}

		public static Item PrepareFailureItem(int type, TagCompound data, DeserializedNetItem readData) {
			// Can't use UnloadedItem.Setup() since that expects the ModItem format, whereas this could be a vanilla item
			int prefix = readData.prefix;
			if (prefix == ModContent.PrefixType<UnloadedPrefix>() && (readData.modPrefixMod is null || readData.modPrefixName is null))
				prefix = 0;

			Item item = new Item(type, readData.stack, prefix);

			if (item.ModItem is not BaseErrorDummyItem errorItem)
				throw new ArgumentException("Item type must be a " + nameof(BaseErrorDummyItem), nameof(type));

			readData.GetContentNames(out string modName, out string name);

			errorItem.OriginalMod = modName ?? "<unknown>";
			errorItem.OriginalName = name ?? "<unknown>";
			errorItem.data = data;

			if (prefix == ModContent.PrefixType<UnloadedPrefix>()) {
				UnloadedGlobalItem globalItem = item.GetGlobalItem<UnloadedGlobalItem>();
				globalItem.ModPrefixMod = readData.modPrefixMod;
				globalItem.ModPrefixName = readData.modPrefixName;
			}

			return item;
		}

		public static int FindPrefix(TagCompound data, out string modName, out string name) {
			if (data.TryGet("prefix", out byte vanillaPrefix)) {
				modName = null;
				name = null;
				return vanillaPrefix;
			}

			if (!data.TryGet("modPrefixMod", out modName) || !data.TryGet("modPrefixName", out name)) {
				modName = null;
				name = null;
				return 0;
			}

			if (!ModContent.TryFind(modName, name, out ModPrefix modPrefix))
				return ModContent.PrefixType<UnloadedPrefix>();

			return modPrefix.Type;
		}

		// Copies of ItemIO.ReceiveModData() that allows the exception to propagate instead of being caught and logged
		internal static void UnsafelyReceiveModData(Item item, BinaryReader reader) {
			if (item.IsAir)
				return;

			// Local capturing
			Item i = item;
			reader.SafeRead(r => i.ModItem?.NetReceive(r));
		}

		internal static void UnsafelyReceiveGlobalModData(Item item, DeserializedNetItem readData, BinaryReader reader, out GlobalItem lastReadGlobal) {
			if (item.IsAir) {
				lastReadGlobal = null;
				return;
			}

			UnloadedGlobalItem unloadedGlobalItem = null;

			try {
				foreach (var globalItem in ItemLoader.HookNetReceive.Enumerate(item)) {
					lastReadGlobal = globalItem;

					if (globalItem is UnloadedGlobalItem unloaded)
						unloadedGlobalItem = unloaded;

					// Local capturing
					Item i = item;
					GlobalItem g = globalItem;
					reader.SafeRead(r => g.NetReceive(i, r));
				}
			} finally {
				if (unloadedGlobalItem is not null) {
					readData.modPrefixMod = unloadedGlobalItem.ModPrefixMod;
					readData.modPrefixName = unloadedGlobalItem.ModPrefixName;
				}
			}

			lastReadGlobal = null;
		}

		public static byte[] ToByteArrayNoCompression(Item item) {
			MemoryStream ms = new MemoryStream();
			TagIO.ToStream(SaveItem(item), ms, false);
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
