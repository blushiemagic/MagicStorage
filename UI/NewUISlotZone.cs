using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace MagicStorage.UI {
	internal class NewUISlotZone : UIElement {
		private const int Padding = 4;

		private static readonly Asset<Texture2D> InventoryBack = TextureAssets.InventoryBack;

		private readonly float inventoryScale;
		public int NumColumns { get; private set; } = 10;
		public int NumRows { get; private set; } = 4;

		public MagicStorageItemSlot[,] Slots { get; set; } = new[,] { { new MagicStorageItemSlot(0) } };
		
		public int ZoneHeight { get; private set; }

		public delegate MagicStorageItemSlot GetNewItemSlot(int slot, float zoneScale);
		public event GetNewItemSlot InitializeSlot;

		public int HoverSlot { get; internal set; }

		public NewUISlotZone(float scale) {
			inventoryScale = scale;
		}

		public void SetDimensions(int columns, int rows) {
			NumColumns = columns;
			NumRows = rows;
			ZoneHeight = (int)(InventoryBack.Value.Height * inventoryScale) * rows + Padding;

			if (Slots is not null) {
				foreach (var slot in Slots)
					slot.Remove();
			}

			Slots = new MagicStorageItemSlot[rows, columns];

			Texture2D texture = InventoryBack.Value;
			float slotWidth = texture.Width * inventoryScale;
			float slotHeight = texture.Height * inventoryScale;

			for (int r = 0; r < rows; r++) {
				for (int c = 0; c < columns; c++) {
					var slot = Slots[r, c] = InitializeSlot?.Invoke(r * rows + c, inventoryScale) ?? new MagicStorageItemSlot(r * rows + c, scale: inventoryScale);

					float x = (slotWidth + Padding) * c;
					float y = (slotHeight + Padding) * r;

					slot.Left.Set(x, 0);
					slot.Top.Set(y, 0);

					Append(slot);
				}
			}
		}

		public void SetItems(IEnumerable<Item> source) {
			int oneDimIndex = 0;

			foreach (Item item in source) {
				int column = oneDimIndex % NumColumns;
				int row = oneDimIndex / NumColumns;

				Slots[row, column].SetItem(item);

				oneDimIndex++;

				if (oneDimIndex >= NumColumns * NumRows)
					return;
			}

			while (oneDimIndex < NumColumns * NumRows) {
				int column = oneDimIndex % NumColumns;
				int row = oneDimIndex / NumColumns;

				Slots[row, column].SetItem(new Item() { stack = 0 });

				oneDimIndex++;
			}
		}

		public void SetItemsAndContexts(int count, UISlotZone.GetItem getItem) {
			int i;
			for (i = 0; i < count; i++) {
				int context = 0;
				Item item = getItem(i, ref context);

				int column = i % NumColumns;
				int row = i / NumColumns;

				var slot = Slots[row, column];
				slot.SetItem(item);
				slot.Context = context;
			}

			while (i < NumColumns * NumRows) {
				int column = i % NumColumns;
				int row = i / NumColumns;

				var slot = Slots[row, column];
				slot.SetItem(new Item() { stack = 0 });
				slot.Context = ItemSlot.Context.InventoryItem;

				i++;
			}
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);

			if (HoverSlot >= 0) {
				var slot = Slots[HoverSlot / NumColumns, HoverSlot % NumColumns];
				Item hoverItem = slot.StoredItem;

				if (!hoverItem.IsAir) {
					Main.HoverItem = hoverItem.Clone();
					MagicUI.mouseText = string.Empty;
				}
			}
		}
	}
}
