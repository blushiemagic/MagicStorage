using System;
using System.Collections;
using System.Linq;

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
	}
}
