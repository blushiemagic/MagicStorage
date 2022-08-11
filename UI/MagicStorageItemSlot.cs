using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;

namespace MagicStorage.UI {
	public class MagicStorageItemSlot : UIElement {
		public int Context { get; set; }

		public float Scale { get; private set; }

		public virtual Item StoredItem => getItem?.Invoke() ?? storedItem;

		protected Item storedItem;

		private Item storedItemBeforeHandle;

		public bool ItemChanged {
			get {
				var item = storedItem;
				return item != null && storedItemBeforeHandle != null && item.IsNotSameTypePrefixAndStack(storedItemBeforeHandle);
			}
		}
		public bool ItemTypeChanged => (storedItem?.type ?? -1) != (storedItemBeforeHandle?.type ?? -2);

		public Func<Item, bool> ValidItemFunc;

		public Action<Item> OnItemChanged;

		public bool IgnoreClicks { get; set; }

		public bool IgnoreNextHandleAction { get; set; }

		public readonly int slot;

		public Func<Item> getItem;

		private Item[] dummy = new Item[11];

		public MagicStorageItemSlot(int slot, int context = ItemSlot.Context.InventoryItem, float scale = 1f) {
			this.slot = slot;
			Context = context;
			Scale = scale;

			storedItem = new Item();
			storedItem.SetDefaults();

			Width.Set(TextureAssets.InventoryBack9.Value.Width * scale, 0f);
			Height.Set(TextureAssets.InventoryBack9.Value.Height * scale, 0f);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			float oldScale = Main.inventoryScale;
			Main.inventoryScale = Scale;
			Rectangle rectangle = GetDimensions().ToRectangle();

			//Lazy hardcoding lol
			bool parentWasClicked = Parent is UIDragablePanel panel && panel.UIDelay > 0;
			if (!IgnoreNextHandleAction && ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface) {
				Main.LocalPlayer.mouseInterface = true;

				if (Parent is UIDragablePanel panel2)
					panel2.Dragging = false;

				if (!parentWasClicked && (ValidItemFunc == null || ValidItemFunc(Main.mouseItem))) {
					bool oldLeft = Main.mouseLeft;
					bool oldLeftRelease = Main.mouseLeftRelease;
					bool oldRight = Main.mouseRight;
					bool oldRightRelease = Main.mouseRightRelease;

					if (IgnoreClicks)
						Main.mouseLeft = Main.mouseLeftRelease = Main.mouseRight = Main.mouseRightRelease = false;

					// Handle handles all the click and hover actions based on the context.
					storedItemBeforeHandle = StoredItem.Clone();
					ItemSlot.Handle(ref storedItem, Context);

					if (ItemChanged || ItemTypeChanged)
						OnItemChanged?.Invoke(storedItem);

					Main.mouseLeft = oldLeft;
					Main.mouseLeftRelease = oldLeftRelease;
					Main.mouseRight = oldRight;
					Main.mouseRightRelease = oldRightRelease;
				}
			}

			IgnoreNextHandleAction = false;

			// Draw draws the slot itself and Item. Depending on context, the color will change, as will drawing other things like stack counts.
			dummy[10] = StoredItem;
			ItemSlot.Draw(spriteBatch, dummy, Context, 10, rectangle.TopLeft());

			Main.inventoryScale = oldScale;
		}

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);

			if (Parent is NewUISlotZone zone && zone.HoverSlot != slot)
				zone.HoverSlot = slot;

			StoredItem.newAndShiny = false;
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			if (Parent is NewUISlotZone zone && zone.HoverSlot == slot)
				zone.HoverSlot = -1;
		}

		public void SetItem(Item item, bool clone = false) {
			storedItem = clone ? item.Clone() : item;
		}

		public void SetItem(int itemType, int stack = 1) {
			storedItem.SetDefaults(itemType);
			storedItem.stack = stack;
		}
	}
}
