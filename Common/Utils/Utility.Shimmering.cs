using MagicStorage.Common.Systems.Shimmering;
using System.Collections.Generic;
using Terraria.ID;

namespace MagicStorage {
	partial class Utility {
		/// <summary>
		/// Checks whether a shimmer attempt produced any successful result.
		/// </summary>
		/// <param name="result">The shimmer attempt result to inspect.</param>
		/// <returns><see langword="true" /> when the result is not <see cref="ShimmerInfo.ShimmerAttemptResult.None" />; otherwise, <see langword="false" />.</returns>
		public static bool IsSuccessful(this ShimmerInfo.ShimmerAttemptResult result) => result != ShimmerInfo.ShimmerAttemptResult.None;

		/// <summary>
		/// Checks whether a shimmer attempt succeeded without producing a decraft result.
		/// </summary>
		/// <param name="result">The shimmer attempt result to inspect.</param>
		/// <returns><see langword="true" /> when the result succeeded and was not a decraft; otherwise, <see langword="false" />.</returns>
		public static bool IsSuccessfulButNotDecraftable(this ShimmerInfo.ShimmerAttemptResult result) => result != ShimmerInfo.ShimmerAttemptResult.None && result != ShimmerInfo.ShimmerAttemptResult.DecraftedItem;

		/// <summary>
		/// Gets shimmer reports for the specified item type.
		/// </summary>
		/// <param name="result">The shimmer result provider.</param>
		/// <param name="item">The item type to report on.</param>
		/// <returns>The shimmer reports for the item type.</returns>
		public static IEnumerable<IShimmerResultReport> GetShimmerReports(this IShimmerResult result, int item) => result.GetShimmerReports(ContentSamples.ItemsByType[item], item);
	}
}
