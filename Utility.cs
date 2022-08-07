using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ModLoader.IO;

namespace MagicStorage {
	public static class Utility {
		public static string GetSimplifiedGenericTypeName(this Type type) {
			//Handle all invalid cases here:
			if (type.FullName is null)
				return type.Name;

			if (!type.IsGenericType)
				return type.FullName;

			string parent = type.GetGenericTypeDefinition().FullName!;

			//Include all but the "`X" part
			parent = parent[..parent.IndexOf('`')];

			//Construct the child types
			return $"{parent}<{string.Join(", ", type.GetGenericArguments().Select(GetSimplifiedGenericTypeName))}>";
		}

		public static int GetCardinality(this BitArray bitArray) {
			int[] ints = new int[(bitArray.Count >> 5) + 1];

			bitArray.CopyTo(ints, 0);

			int count = 0;

			// fix for not truncated bits in last integer that may have been set to true with SetAll()
			ints[^1] &= ~(-1 << (bitArray.Count % 32));

			for (int i = 0; i < ints.Length; i++) {
				int c = ints[i];

				// magic (http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel)
				unchecked {
					c -= (c >> 1) & 0x55555555;
					c = (c & 0x33333333) + ((c >> 2) & 0x33333333);
					c = ((c + (c >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
				}

				count += c;
			}

			return count;
		}

		public static bool AreStrictlyEqual(Item item1, Item item2, bool checkStack = false, bool checkPrefix = true) {
			int stack1 = item1.stack;
			int stack2 = item2.stack;
			int prefix1 = item1.prefix;
			int prefix2 = item2.prefix;

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

			equal = TagIOSave(item1).SequenceEqual(TagIOSave(item2));

			ReturnFromMethod:

			item1.stack = stack1;
			item2.stack = stack2;
			item1.prefix = prefix1;
			item2.prefix = prefix2;

			return equal;
		}

		private static byte[] TagIOSave(Item item) {
			using MemoryStream memoryStream = new();
			TagIO.ToStream(ItemIO.Save(item), memoryStream);
			return memoryStream.ToArray();
		}

		public static void Write(this BinaryWriter writer, Point16 position) {
			writer.Write(position.X);
			writer.Write(position.Y);
		}

		public static Point16 ReadPoint16(this BinaryReader reader)
			=> new(reader.ReadInt16(), reader.ReadInt16());

		public static void GetResearchStats(int itemType, out bool canBeResearched, out int sacrificesNeeded, out int currentSacrificeTotal) {
			canBeResearched = false;
			currentSacrificeTotal = 0;

			if (!CreativeItemSacrificesCatalog.Instance.TryGetSacrificeCountCapToUnlockInfiniteItems(itemType, out sacrificesNeeded))
				return;

			if (!Main.LocalPlayerCreativeTracker.ItemSacrifices.SacrificesCountByItemIdCache.TryGetValue(itemType, out currentSacrificeTotal))
				return;

			canBeResearched = true;
		}

		public static bool IsFullyResearched(int itemType, bool mustBeResearchable) {
			GetResearchStats(itemType, out bool canBeResearched, out int sacrificesNeeded, out int currentSacrificeTotal);

			return (!mustBeResearchable || (canBeResearched && sacrificesNeeded > 0)) && currentSacrificeTotal >= sacrificesNeeded;
		}

		public static SafeOrdering<T> AsSafe<T>(this IComparer<T> comparer) => new(comparer);
	}
}
