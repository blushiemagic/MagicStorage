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
		/// <summary>
		/// Saves an item to tag data, preserving stored error-item data when present.
		/// </summary>
		/// <param name="item">The item to save.</param>
		/// <returns>The saved item tag data.</returns>
		public static TagCompound SaveItem(Item item) {
			if (item.ModItem is BaseErrorDummyItem errorItem) {
				if (errorItem.data is not { Count: > 0 })
					return new TagCompound();

				return errorItem.data;
			}

			// Use standard saving
			return ItemIO.Save(item);
		}

		/// <summary>
		/// Loads an item from tag data, returning an error display item when loading fails.
		/// </summary>
		/// <param name="tag">The saved item tag data.</param>
		/// <returns>The loaded item, or an error display item containing the original data.</returns>
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

		/// <summary>
		/// Creates an error display item for item data that could not be loaded normally.
		/// </summary>
		/// <param name="type">The error item type to create.</param>
		/// <param name="data">The original serialized item data.</param>
		/// <param name="readData">The partially deserialized item metadata.</param>
		/// <returns>The prepared error display item.</returns>
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

		/// <summary>
		/// Resolves the prefix stored in serialized item data.
		/// </summary>
		/// <param name="data">The serialized item data.</param>
		/// <param name="modName">The resolved mod prefix mod name, when the prefix is modded.</param>
		/// <param name="name">The resolved mod prefix name, when the prefix is modded.</param>
		/// <returns>The resolved prefix type, or the unloaded prefix type when the modded prefix is missing.</returns>
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

		/// <summary>
		/// Serializes an item to uncompressed tag bytes.
		/// </summary>
		/// <param name="item">The item to serialize.</param>
		/// <returns>The uncompressed serialized bytes.</returns>
		public static byte[] ToByteArrayNoCompression(Item item) {
			MemoryStream ms = new MemoryStream();
			TagIO.ToStream(SaveItem(item), ms, false);
			return ms.ToArray();
		}

		/// <summary>
		/// Serializes an item to an uncompressed byte span.
		/// </summary>
		/// <param name="item">The item to serialize.</param>
		/// <returns>The uncompressed serialized bytes.</returns>
		public static ReadOnlySpan<byte> ToByteSpanNoCompression(Item item) => ToByteArrayNoCompression(item);

		/// <summary>
		/// Serializes an item to an uncompressed base64 string.
		/// </summary>
		/// <param name="item">The item to serialize.</param>
		/// <returns>The uncompressed serialized item as base64.</returns>
		public static string ToBase64NoCompression(Item item) => Convert.ToBase64String(ToByteArrayNoCompression(item));

		/// <summary>
		/// Serializes tag data to uncompressed bytes.
		/// </summary>
		/// <param name="tag">The tag data to serialize.</param>
		/// <returns>The uncompressed serialized bytes.</returns>
		public static byte[] ToByteArrayNoCompression(TagCompound tag) {
			MemoryStream ms = new MemoryStream();
			TagIO.ToStream(tag, ms, false);
			return ms.ToArray();
		}

		/// <summary>
		/// Serializes tag data to an uncompressed byte span.
		/// </summary>
		/// <param name="tag">The tag data to serialize.</param>
		/// <returns>The uncompressed serialized bytes.</returns>
		public static ReadOnlySpan<byte> ToByteSpanNoCompression(TagCompound tag) => ToByteArrayNoCompression(tag);

		/// <summary>
		/// Serializes tag data to an uncompressed base64 string.
		/// </summary>
		/// <param name="tag">The tag data to serialize.</param>
		/// <returns>The uncompressed serialized tag data as base64.</returns>
		public static string ToBase64NoCompression(TagCompound tag) => Convert.ToBase64String(ToByteArrayNoCompression(tag));

		/// <summary>
		/// Loads an item from uncompressed tag bytes.
		/// </summary>
		/// <param name="bytes">The uncompressed serialized item bytes.</param>
		/// <returns>The loaded item, or an error display item when loading fails.</returns>
		public static Item FromByteArrayNoCompression(byte[] bytes) {
			MemoryStream ms = new MemoryStream(bytes);
			return SafelyLoadItem(TagIO.FromStream(ms, false));
		}

		/// <summary>
		/// Loads an item from an uncompressed byte span.
		/// </summary>
		/// <param name="bytes">The uncompressed serialized item bytes.</param>
		/// <returns>The loaded item, or an error display item when loading fails.</returns>
		public static Item FromByteSpanNoCompression(ReadOnlySpan<byte> bytes) => FromByteArrayNoCompression(bytes.ToArray());

		/// <summary>
		/// Loads an item from an uncompressed base64 string.
		/// </summary>
		/// <param name="base64">The uncompressed serialized item data as base64.</param>
		/// <returns>The loaded item, or an error display item when loading fails.</returns>
		public static Item FromBase64NoCompression(string base64) => FromByteArrayNoCompression(Convert.FromBase64String(base64));
	}
}
