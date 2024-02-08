using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI.Selling {
	internal class SellStackPopup : UIElement {
		private readonly UIPanel _panel;
		private readonly MagicStorageItemSlot _slot;
		private readonly UIText _quantityLabel;
		private readonly UIText _quantityAmount;
		private readonly UITextPanel<char> _quantityIncrease;
		private readonly UITextPanel<char> _quantityDecrease;
		private readonly UITextPanel<LocalizedText> _confirm;
		private readonly UITextPanel<LocalizedText> _cancel;
		private int _quantity = -1;
		private readonly bool _updatingQuantity;

		public Item PreviewItem => _slot?.StoredItem.Clone();

		public int Quantity => _quantity;

		public bool UpdatingQuantity => _updatingQuantity;

		private static float? _maxAmountWidth;
		private static float MaxAmountWidth => _maxAmountWidth ??= FontAssets.MouseText.Value.MeasureString("9999").X;

		public event Action<SellStackPopup> OnConfirmAmount;
		public event Action<SellStackPopup> OnCancel;

		public SellStackPopup(Item item, bool updating, int quantity = 1) {
			_panel = new UIPanel();
			_panel.Width.Set(0, 1f);
			_panel.Height.Set(0, 1f);
			_panel.SetPadding(0);
			// Same color as vanilla, but without the transparency
			_panel.BackgroundColor = new Color(63, 82, 151);
			Append(_panel);

			_slot = new MagicStorageItemSlot(0, MagicSlotContext.SellActionPreview) {
				IgnoreClicks = true
			};
			_slot.Left.Set(4, 0f);
			_slot.Top.Set(4, 0f);
			_slot.SetBoundItem(item.Clone());
			_panel.Append(_slot);

			_quantityLabel = new UIText(Language.GetText("Mods.MagicStorage.StorageGUI.Popup.Label"));
			_quantityLabel.Left.Set(_slot.Left.Pixels + _slot.Width.Pixels + 4, 0f);
			_quantityLabel.Top = _slot.Top;
			_panel.Append(_quantityLabel);

			_quantityIncrease = new UITextPanel<char>('+');
			_quantityIncrease.Left.Set(_quantityLabel.Left.Pixels + _quantityLabel.MinWidth.Pixels + 4, 0f);
			_quantityIncrease.Top = _slot.Top;
			_quantityIncrease.Height.Set(20, 0f);
			_quantityIncrease.OnLeftClick += (evt, e) => SetQuantity(_quantity + 1);
			_panel.Append(_quantityIncrease);

			_quantityDecrease = new UITextPanel<char>('-');
			_quantityDecrease.Left.Set(_quantityIncrease.Left.Pixels + _quantityIncrease.MinWidth.Pixels + MaxAmountWidth + 8, 0f);
			_quantityDecrease.Top = _slot.Top;
			_quantityDecrease.Height.Set(20, 0f);
			_quantityDecrease.OnLeftClick += (evt, e) => SetQuantity(_quantity - 1);
			_panel.Append(_quantityDecrease);

			_quantityAmount = new UIText(string.Empty);
			_quantityAmount.Height.Set(20, 0f);
			_quantityAmount.Top.Set(_slot.Top.Pixels + (_quantityIncrease.MinHeight.Pixels - _quantityAmount.Height.Pixels) / 2f, 0f);
			SetQuantity(quantity);
			_panel.Append(_quantityAmount);

			_cancel = new UITextPanel<LocalizedText>(Language.GetText("UI.Cancel"));
			_cancel.Left.Set(-_cancel.MinWidth.Pixels - 4, 1f);
			_cancel.Top.Set(-_cancel.MinHeight.Pixels - 4, 1f);
			_cancel.OnLeftClick += (evt, e) => OnCancel?.Invoke(this);
			_panel.Append(_cancel);

			_confirm = new UITextPanel<LocalizedText>(Language.GetText(updating ? "UI.Save" :  "UI.Create"));
			_confirm.Left.Set(-_confirm.MinWidth.Pixels - _cancel.MinWidth.Pixels - 8, 1f);
			_confirm.Top.Set(-_confirm.MinHeight.Pixels - 4, 1f);
			_confirm.OnLeftClick += (evt, e) => OnConfirmAmount?.Invoke(this);
			_panel.Append(_confirm);

			Width.Set(_slot.Width.Pixels + _quantityLabel.MinWidth.Pixels + _quantityIncrease.MinWidth.Pixels + _quantityDecrease.MinWidth.Pixels + MaxAmountWidth + 20, 0f);
			Height.Set(_quantityIncrease.MinHeight.Pixels + _confirm.MinHeight.Pixels + 12, 0f);
		}

		public void SetItem(Item item) {
			_slot.SetBoundItem(item.Clone());
			_slot.Recalculate();
		}

		public void SetQuantity(int quantity) {
			int oldQuantity = _quantity;
			_quantity = Utils.Clamp(quantity, 0, _slot.StoredItem.stack);

			if (oldQuantity != _quantity) {
				string text = _quantity.ToString();
				float width = FontAssets.MouseText.Value.MeasureString(text).X;

				_quantityAmount.SetText(text);
				_quantityAmount.Left.Set(_quantityDecrease.Left.Pixels - MaxAmountWidth - 4 + width, 0f);
				_quantityAmount.Recalculate();
			}
		}
	}
}
