using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace MagicStorage {
	public static partial class Utility {
		// The rest of the implementation is found at /Common/Utils

		public static void SaveModConfig(ModConfig config) => ConfigManager.Save(config);

		public static Item GetItemSample(int item) => ContentSamples.ItemsByType[item];

		public static Item SafelyLoadItem(TagCompound tag) {
			try {
				return ItemIO.Load(tag);
			} catch (KeyNotFoundException) {
				// Item was malformed
				return new Item();
			} catch (Exception ex) {
				MagicStorageMod.Instance.Logger.Error("Error loading item from tag", ex);
				return new Item();
			}
		}

		/// <summary>
		/// Generates a hash for a byte array.  This method is <b>NOT</b> optimized for <see cref="object.GetHashCode"/> usage!
		/// </summary>
		public static int ComputeDataHash(byte[] data) {
			if (data is not { Length: >0 })
				return 0;

			// Taken from: https://stackoverflow.com/a/468084
			unchecked {
				const int p = 16777619;
				int hash = (int)2166136261;

				for (int i = 0; i < data.Length; i++)
					hash = (hash ^ data[i]) * p;

				hash += hash << 13;
				hash ^= hash >> 7;
				hash += hash << 3;
				hash ^= hash >> 17;
				hash += hash << 5;
				return hash;
			}
		}
	}
}
