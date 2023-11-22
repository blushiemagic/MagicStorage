using SerousCommonLib.UI;
using Terraria.UI;

namespace MagicStorage.UI {
	public class MagicStorageItemSlot : EnhancedItemSlotV2 {
		/// <inheritdoc/>
		public MagicStorageItemSlot(int slot, int context = ItemSlot.Context.InventoryItem, float scale = 1f) : base(slot, context, scale) { }

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);

			if (Parent is NewUISlotZone zone && zone.HoverSlot != id)
				zone.HoverSlot = id;

			StoredItem.newAndShiny = false;
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			if (Parent is NewUISlotZone zone && zone.HoverSlot == id)
				zone.HoverSlot = -1;
		}
	}
}
