using System.Collections.Generic;

namespace MagicStorage {
	partial class Utility {
		public static void AddOrSumCount(this Dictionary<int, int> itemCounts, int type, int count) {
			if (!itemCounts.ContainsKey(type))
				itemCounts[type] = count;
			else
				itemCounts[type] += count;
		}
	}
}
