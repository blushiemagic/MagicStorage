using MagicStorage.Common.Systems;
using MagicStorage.Common.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace MagicStorage.UI {
	public class NewUISlotZone : UIElement {
		public const int Padding = 4;

		private static readonly Asset<Texture2D> InventoryBack = TextureAssets.InventoryBack;

		private readonly float inventoryScale;
		public int NumColumns { get; private set; } = -1;
		public int NumRows { get; private set; } = -1;

		public MagicStorageItemSlot[,] Slots { get; set; } = new[,] { { new MagicStorageItemSlot(0) } };
		
		public int ZoneWidth { get; private set; }
		public int ZoneHeight { get; private set; }

		public delegate MagicStorageItemSlot GetNewItemSlot(int slot, float zoneScale);
		public GetNewItemSlot InitializeSlot;

		public delegate void HoverSlotChanged(NewUISlotZone zone, int oldSlot, int newSlot);
		public event HoverSlotChanged OnHoverSlotChanged;

		public int HoverSlot { get; private set; } = -1;

		private int _cachedHoverSlot = -1;
		private Item _cachedHoverSource;
		private HoverItemSignature _cachedHoverSignature;
		private Item _cachedHoverItem;

		public NewUISlotZone(float scale) {
			inventoryScale = scale;
		}

		private int _lock;

		public void SetDimensions(int columns, int rows) {
			using var _ = new Locker(ref _lock);

			if (NumColumns == columns && NumRows == rows)
				return;

			NumColumns = columns;
			NumRows = rows;
			
			Texture2D texture = InventoryBack.Value;
			float slotWidth = texture.Width * inventoryScale;
			float slotHeight = texture.Height * inventoryScale;

			ZoneWidth = (int)((slotWidth + Padding) * columns);
			ZoneHeight = (int)((slotHeight + Padding) * rows);

			if (Slots is not null) {
				foreach (var slot in Slots)
					slot.Remove();
			}

			Slots = new MagicStorageItemSlot[rows, columns];

			for (int r = 0; r < rows; r++) {
				for (int c = 0; c < columns; c++) {
					int slotIndex = r * columns + c;

					var slot = Slots[r, c] = InitializeSlot?.Invoke(slotIndex, inventoryScale) ?? new MagicStorageItemSlot(slotIndex, scale: inventoryScale);

					float x = (slotWidth + Padding) * c;
					float y = (slotHeight + Padding) * r;

					slot.Left.Set(x, 0);
					slot.Top.Set(y, 0);

					Append(slot);
				}
			}
		}

		public void SetItems(IEnumerable<Item> source) {
			using var __ = new Locker(ref _lock);

			int oneDimIndex = 0;

			foreach (Item item in source) {
				if (oneDimIndex >= NumColumns * NumRows)
					return;

				int column = oneDimIndex % NumColumns;
				int row = oneDimIndex / NumColumns;

				Slots[row, column].SetBoundItem(item);

				oneDimIndex++;
			}

			while (oneDimIndex < NumColumns * NumRows) {
				int column = oneDimIndex % NumColumns;
				int row = oneDimIndex / NumColumns;

				Slots[row, column].SetBoundItem(new Item() { stack = 0 });

				oneDimIndex++;
			}
		}

		public void SetItemsAndContexts(int count, UISlotZone.GetItem getItem) {
			using var _ = new Locker(ref _lock);

			if (NumColumns < 0 || NumRows < 0)
				return;

			int i;
			for (i = 0; i < count; i++) {
				if (i >= NumColumns * NumRows)
					return;

				int context = 0;
				Item item = getItem(i, ref context);

				int column = i % NumColumns;
				int row = i / NumColumns;

				var slot = Slots[row, column];
				slot.SetBoundItem(item);
				slot.Context = context;
			}

			while (i < NumColumns * NumRows) {
				int column = i % NumColumns;
				int row = i / NumColumns;

				var slot = Slots[row, column];
				slot.SetBoundItem(new Item() { stack = 0 });
				slot.Context = ItemSlot.Context.InventoryItem;

				i++;
			}
		}

		public void ClearContexts() {
			using var _ = new Locker(ref _lock);

			for (int r = 0; r < NumRows; r++) {
				for (int c = 0; c < NumColumns; c++)
					Slots[r, c].Context = ItemSlot.Context.InventoryItem;
			}
		}

		public void ClearItems() {
			using var _ = new Locker(ref _lock);

			foreach (var slot in Slots)
				slot.SetBoundItem(new Item() { stack = 0 });
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);

			if (NumColumns <= 0 || NumRows <= 0)
				return;

			if (HoverSlot >= 0) {
				var @lock = new Locker(ref _lock);
				bool freedLock = false;

				try {
					if (HoverSlot < NumColumns * NumRows) {
						var slot = Slots[HoverSlot / NumColumns, HoverSlot % NumColumns];
						Item hoverItem = slot.StoredItem;

						if (!hoverItem.IsAir) {
							Main.HoverItem = GetHoverItemClone(HoverSlot, hoverItem);
							MagicUI.mouseText = string.Empty;
						}

						freedLock = true;
						@lock.Dispose();
					} else {
						freedLock = true;
						@lock.Dispose();

						//Failsafe
						SetHoverSlot(-1);
					}
				} finally {
					if (!freedLock)
						@lock.Dispose();
				}
			}
		}

		public void SetHoverSlot(int slot) {
			using var _ = new Locker(ref _lock);

			if (HoverSlot != slot) {
				int oldSlot = HoverSlot;
				HoverSlot = slot;

				if (slot >= 0 && slot < NumColumns * NumRows)
					OnHoverSlotChanged?.Invoke(this, oldSlot, slot);
			}
		}

		private Item GetHoverItemClone(int slot, Item source) {
			HoverItemSignature signature = new(source);

			if (_cachedHoverSlot != slot || !ReferenceEquals(_cachedHoverSource, source) || _cachedHoverSignature != signature) {
				_cachedHoverSlot = slot;
				_cachedHoverSource = source;
				_cachedHoverSignature = signature;
				_cachedHoverItem = source.Clone();
			}

			return _cachedHoverItem;
		}

		private readonly record struct HoverItemSignature(int Type, int Stack, int Prefix, bool Favorited) {
			public HoverItemSignature(Item item) : this(item.type, item.stack, item.prefix, item.favorited) { }
		}
	}
}
