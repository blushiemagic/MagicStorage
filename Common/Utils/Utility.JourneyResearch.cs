using Terraria;

namespace MagicStorage {
	partial class Utility {
		public static void GetResearchStats(int itemType, out bool canBeResearched, out int sacrificesNeeded, out int currentSacrificeTotal) {
			// NOTE: 1.4.4 adds this handy method which does all of the work for me.  cool!
			canBeResearched = Main.LocalPlayerCreativeTracker.ItemSacrifices.TryGetSacrificeNumbers(itemType, out currentSacrificeTotal, out sacrificesNeeded);
		}

		public static bool IsFullyResearched(int itemType, bool mustBeResearchable) {
			GetResearchStats(itemType, out bool canBeResearched, out int sacrificesNeeded, out int currentSacrificeTotal);

			return (!mustBeResearchable || (canBeResearched && sacrificesNeeded > 0)) && currentSacrificeTotal >= sacrificesNeeded;
		}
	}
}
