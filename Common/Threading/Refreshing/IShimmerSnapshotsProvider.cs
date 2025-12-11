using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.Shimmering;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Threading.Refreshing {
	public interface IShimmerSnapshotsProvider {
		ShimmerSnapshots ShimmerSnapshots { get; }
	}

	public class ShimmerSnapshots {
		public bool[] decraftingRecipeAvailableSnapshot { get; private set; }
		public int[] itemTypeToDecraftRecipeIndexSnapshot { get; private set; }
		public bool[] itemTransmuteAvailableSnapshot { get; private set; }

		public void CollectObjects() {
			decraftingRecipeAvailableSnapshot = [.. Main.recipe.Take(Recipe.numRecipes).Select(ShimmerMetrics.IsDecraftAvailable)];
			var decraftSnapshots = itemTypeToDecraftRecipeIndexSnapshot = ItemID.Sets.Factory.CreateIntSet(-1);
			var transmutationSnapshots = itemTransmuteAvailableSnapshot = ItemID.Sets.Factory.CreateBoolSet(false);

			for (int i = 0; i < ItemLoader.ItemCount; i++) {
				var attempt = MagicCache.ShimmerInfos[i].GetAttempt(out int decraftingRecipeIndex);

				if (attempt is ShimmerInfo.ShimmerAttemptResult.DecraftedItem)
					decraftSnapshots[i] = decraftingRecipeIndex;
				else if (attempt is not ShimmerInfo.ShimmerAttemptResult.None)
					transmutationSnapshots[i] = true;
			}
		}
	}
}
