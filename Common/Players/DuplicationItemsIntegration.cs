using MagicStorage.Modules;
using Terraria.ModLoader;

namespace MagicStorage.Common.Players {
	internal class DuplicationItemsIntegration : ModPlayer {
		public override void OnEnterWorld() {
			// Ensure that the next usage of the module forces the inventory to be reinitialized
			JourneyInfiniteItems.inventory.Clear();
		}
	}
}
