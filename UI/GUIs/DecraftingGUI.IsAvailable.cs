using MagicStorage.Common.Systems;

namespace MagicStorage {
	partial class DecraftingGUI {
		public static bool IsAvailable(int itemType) => CraftingGUI.itemCounts.TryGetValue(itemType, out int count) && count > 0 && IsAvailable_CheckShimmering(itemType);

		private static bool IsAvailable_CheckShimmering(int itemType) {
			if (currentlyThreading) {
				if (MagicUI.activeThread.state is not ThreadState state)
					return false;

				// Item transmutation takes priority over decrafting
				if (state.itemTransmuteAvailableSnapshot[itemType])
					return true;

				// The item may have decrafting recipes, but they may not be available, so this needs to be accounted for
				int decraftingRecipeIndex = state.itemTypeToDecraftRecipeIndexSnapshot[itemType];
				return decraftingRecipeIndex >= 0 && state.decraftingRecipeAvailableSnapshot[decraftingRecipeIndex];
			}

			// Need to manually check the item
			return MagicCache.ShimmerInfos[itemType].GetAttempt(out _).IsSuccessful();
		}
	}
}
