using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.Shimmering;
using MagicStorage.Common.Threading.Refreshing;

namespace MagicStorage {
	partial class DecraftingGUI {
		public static bool IsAvailable(int itemType) => IsAvailable(NullThread, itemType);

		public static bool IsAvailable<T>(T thread, int itemType)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IShimmerSnapshotsProvider
		{
			if (!CraftingGUI.GetItemCountsWithBlockedItemsRemoved(thread).TryGetValue(itemType, out int count) || count <= 0)
				return false;

			if (thread is null) {
				// Need to manually check the item
				return MagicCache.ShimmerInfos[itemType].GetAttempt(out _) != ShimmerInfo.ShimmerAttemptResult.None;
			}

			var snapshots = thread.ShimmerSnapshots;

			// Item transmutation takes priority over decrafting
			if (snapshots.itemTransmuteAvailableSnapshot[itemType])
				return true;

			// The item may have decrafting recipes, but they may not be available, so this needs to be accounted for
			int decraftingRecipeIndex = snapshots.itemTypeToDecraftRecipeIndexSnapshot[itemType];
			return decraftingRecipeIndex >= 0 && snapshots.decraftingRecipeAvailableSnapshot[decraftingRecipeIndex];
		}
	}
}
