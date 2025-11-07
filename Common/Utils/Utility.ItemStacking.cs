using MagicStorage.CrossMod;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage {
	partial class Utility {
		public static bool AreStrictlyEqual(Item item1, Item item2, bool checkStack = false, bool checkPrefix = true) => AreStrictlyEqual(item1, item2, checkStack, checkPrefix, null);

		public static bool AreStrictlyEqual(Item item1, Item item2, bool checkStack, bool checkPrefix, ConditionalWeakTable<Item, byte[]> savedItemTagIO) {
			int stack1 = item1.stack;
			int stack2 = item2.stack;
			int prefix1 = item1.prefix;
			int prefix2 = item2.prefix;
			bool favorite1 = item1.favorited;
			bool favorite2 = item2.favorited;

			item1.favorited = false;
			item2.favorited = false;

			bool equal;

			if (!checkPrefix) {
				item1.prefix = 0;
				item2.prefix = 0;
			}

			if (!checkStack) {
				item1.stack = 1;
				item2.stack = 1;
			}

			if (!ItemData.Matches(item1, item2)) {
				equal = false;
				goto ReturnFromMethod;
			}

			try {
				equal = TagIOSave(item1, savedItemTagIO).SequenceEqual(TagIOSave(item2, savedItemTagIO));
			} catch {
				// Swallow the exception and disallow stacking
				equal = false;
			}

			ReturnFromMethod:

			item1.stack = stack1;
			item2.stack = stack2;
			item1.prefix = prefix1;
			item2.prefix = prefix2;
			item1.favorited = favorite1;
			item2.favorited = favorite2;

			return equal;
		}

		private static byte[] TagIOSave(Item item, ConditionalWeakTable<Item, byte[]> savedItemTagIO)
		{
			if (savedItemTagIO?.TryGetValue(item, out byte[] retVal) is true)
			{
				return retVal;
			}
			using MemoryStream memoryStream = new(200);
			TagCompound saveData = StorageAggregatorLoader.GetItemData(item, out var tag) ? tag : SaveItem(item);
			TagIO.ToStream(saveData, memoryStream, false);
			retVal = memoryStream.ToArray();
			savedItemTagIO?.Add(item, retVal);
			return retVal;
		}

		public static void CallOnStackHooks(Item destination, Item source, int numTransfered) {
			ItemLoader.OnStack(destination, source, numTransfered);
		}
	}
}
