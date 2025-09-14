using MagicStorage.Common.Systems.Shimmering;
using System.Collections.Generic;
using Terraria.ID;

namespace MagicStorage {
	partial class Utility {
		public static bool IsSuccessful(this ShimmerInfo.ShimmerAttemptResult result) => result != ShimmerInfo.ShimmerAttemptResult.None;

		public static bool IsSuccessfulButNotDecraftable(this ShimmerInfo.ShimmerAttemptResult result) => result != ShimmerInfo.ShimmerAttemptResult.None && result != ShimmerInfo.ShimmerAttemptResult.DecraftedItem;

		public static IEnumerable<IShimmerResultReport> GetShimmerReports(this IShimmerResult result, int item) => result.GetShimmerReports(ContentSamples.ItemsByType[item], item);
	}
}
