using MagicStorage.Common.Systems;
using MagicStorage.Items.ErrorDisplay;
using Microsoft.Xna.Framework.Graphics;
using SerousCommonLib.UI;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.UI {
	public class MagicStorageItemSlot : EnhancedItemSlotV2 {
		private Item _errorOverrideItem;
		public override Item StoredItem => _errorOverrideItem ?? base.StoredItem;

		/// <inheritdoc/>
		public MagicStorageItemSlot(int slot, int context = MagicSlotContext.Normal, float scale = 1f) : base(slot, context, scale) { }

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);

			if (Parent is NewUISlotZone zone && zone.HoverSlot != id)
				zone.SetHoverSlot(id);

			StoredItem.newAndShiny = false;
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			if (Parent is NewUISlotZone zone && zone.HoverSlot == id)
				zone.SetHoverSlot(-1);

			MagicUI.mouseText = "";
		}

		public override void LeftClick(UIMouseEvent evt) {
			if (BaseErrorDummyItem.LoadTestItems || base.StoredItem?.ModItem is not BaseErrorDummyItem)
				base.LeftClick(evt);
		}

		public override void RightClick(UIMouseEvent evt) {
			if (BaseErrorDummyItem.LoadTestItems || base.StoredItem?.ModItem is not BaseErrorDummyItem)
				base.RightClick(evt);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			// In the event that something goes wrong in the base method, replace the item with a special dummy item from Magic Storage
			_errorOverrideItem = null;

			try {
				if (StoredItem?.ModItem is BaseErrorDummyItem)
					HandleAsErrorItem(spriteBatch, silent: false);
				else
					base.DrawSelf(spriteBatch);
			} catch {
				// Something went wrong, display an error item instead
				HandleAsErrorItem(spriteBatch, silent: true);
			}
		}

		private void HandleAsErrorItem(SpriteBatch spriteBatch, bool silent) {
			bool hovering = IsMouseHovering;
			IsMouseHovering = false;  // Prevent both the tooltip from drawing and click actions from being registered

			if (StoredItem?.ModItem is not BaseErrorDummyItem)
				_errorOverrideItem = new Item(BaseErrorDummyItem.RenderFailItemType);

			if (hovering) {
				Item displayedItem = StoredItem;
				if (displayedItem.type == BaseErrorDummyItem.RenderFailItemType)
					MagicUI.mouseText = Language.GetTextValue("Mods.MagicStorage.HoverText.Errors.RenderFail");
				else if (displayedItem.type == BaseErrorDummyItem.NetReadFailItemType)
					MagicUI.mouseText = Language.GetTextValue("Mods.MagicStorage.HoverText.Errors.NetFail");
			}

			try {
				base.DrawSelf(spriteBatch);
			} catch {
				if (!silent)
					throw;
			} finally {
				_errorOverrideItem = null;
				IsMouseHovering = hovering;
			}
		}
	}
}
