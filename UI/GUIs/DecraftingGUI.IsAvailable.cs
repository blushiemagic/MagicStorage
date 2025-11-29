using MagicStorage.Common.Systems;

namespace MagicStorage {
	partial class DecraftingGUI {
		public static bool IsAvailable(int itemType) => CraftingGUI.GetItemCountsWithBlockedItemsRemoved().TryGetValue(itemType, out int count) && count > 0 && IsAvailable_CheckShimmering(itemType);

		private static bool IsAvailable_CheckShimmering(int itemType) {
			if (MagicUI.HasActiveThread(out ShimmeringRefreshThread thread)) {
				// Item transmutation takes priority over decrafting
				if (thread.itemTransmuteAvailableSnapshot[itemType])
					return true;

				// The item may have decrafting recipes, but they may not be available, so this needs to be accounted for
				int decraftingRecipeIndex = thread.itemTypeToDecraftRecipeIndexSnapshot[itemType];
				return decraftingRecipeIndex >= 0 && thread.decraftingRecipeAvailableSnapshot[decraftingRecipeIndex];
			}

			// Need to manually check the item
			return MagicCache.ShimmerInfos[itemType].GetAttempt(out _).IsSuccessful();
		}
	}
}
