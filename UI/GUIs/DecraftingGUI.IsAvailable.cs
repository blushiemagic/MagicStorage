using MagicStorage.Common.Systems;

namespace MagicStorage {
	partial class DecraftingGUI {
		public static bool IsAvailable(int itemType) {
			if (currentlyThreading) {
				if (MagicUI.activeThread.state is not ThreadState state)
					return false;
				return state.itemTransmuteAvailableSnapshot[itemType] || state.decraftingRecipeAvailableSnapshot[state.itemTypeToDecraftRecipeIndexSnapshot[itemType]];
			}

			// Need to manually check the item
			return MagicCache.ShimmerInfos[itemType].GetAttempt(out _).IsSuccessful();
		}
	}
}
