using Terraria;
using Terraria.ID;

namespace MagicStorage {
	partial class DecraftingGUI {
		internal static Item GetHeader(int slot, ref int context) => selectedItem == -1 ? new Item() : ContentSamples.ItemsByType[selectedItem];
	}
}
