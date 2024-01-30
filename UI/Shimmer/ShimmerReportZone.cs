using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.Shimmering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace MagicStorage.UI.Shimmer {
	public class ShimmerReportZone : UIElement {
		public class IconContainer : BaseShimmerReportElement {
			public readonly int id;

			public float Scale { get; set; }

			public IconContainer(int slot, float scale = 1f) {
				id = slot;
				Scale = scale;

				Width.Set(TextureAssets.InventoryBack9.Value.Width * scale, 0f);
				Height.Set(TextureAssets.InventoryBack9.Value.Height * scale, 0f);

				// Icon needs to be created here; OnInitialize runs too late
				_icon = new ShimmerReportIcon() {
					HAlign = 0.5f,
					VAlign = 0.5f
				};
				_icon.Width.Set(0, 1f);
				_icon.Height.Set(0, 1f);
				Append(_icon);
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);

				if (IsMouseHovering && _report?.Label?.Value is { } report)
					MagicUI.mouseText = report;
			}

			private readonly Item[] _dummyItemCollection = new Item[11];

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				// Draw a standard item slot
				using (ObjectSwitch.Create(ref Main.inventoryScale, Scale)) {
					_dummyItemCollection[10] = new Item();
					ItemSlot.Draw(spriteBatch, _dummyItemCollection, ItemSlot.Context.InventoryItem, 10, GetDimensions().Position());
				}
			}

			public override void MouseOver(UIMouseEvent evt) {
				base.MouseOver(evt);

				var parent = (ShimmerReportZone)Parent;
				if (parent.HoverSlot != id)
					parent.HoverSlot = id;
			}

			public override void MouseOut(UIMouseEvent evt) {
				base.MouseOut(evt);

				var parent = (ShimmerReportZone)Parent;
				if (parent.HoverSlot == id)
					parent.HoverSlot = -1;
			}
		}

		public const int Padding = NewUISlotZone.Padding;

		private static readonly Asset<Texture2D> InventoryBack = TextureAssets.InventoryBack;

		private readonly float inventoryScale;
		public int NumColumns { get; private set; } = -1;
		public int NumRows { get; private set; } = -1;

		public IconContainer[,] Slots { get; private set; } = new IconContainer[,] { { new IconContainer(0) } };

		public int ZoneWidth { get; private set; }
		public int ZoneHeight { get; private set; }

		public int HoverSlot { get; internal set; } = -1;

		public ShimmerReportZone(float scale) {
			inventoryScale = scale;
		}

		public void SetDimensions(int columns, int rows) {
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

			Slots = new IconContainer[rows, columns];

			for (int r = 0; r < rows; r++) {
				for (int c = 0; c < columns; c++) {
					int slotIndex = r * columns + c;

					var slot = Slots[r, c] = new IconContainer(slotIndex, scale: inventoryScale);

					float x = (slotWidth + Padding) * c;
					float y = (slotHeight + Padding) * r;

					slot.Left.Set(x, 0);
					slot.Top.Set(y, 0);

					Append(slot);
				}
			}
		}

		public void SetReports(IEnumerable<IShimmerResultReport> source) {
			int oneDimIndex = 0;

			foreach (IShimmerResultReport report in source) {
				if (oneDimIndex >= NumColumns * NumRows)
					return;

				int column = oneDimIndex % NumColumns;
				int row = oneDimIndex / NumColumns;

				Slots[row, column].SetReport(report);

				oneDimIndex++;
			}

			while (oneDimIndex < NumColumns * NumRows) {
				int column = oneDimIndex % NumColumns;
				int row = oneDimIndex / NumColumns;

				Slots[row, column].SetReport(null);

				oneDimIndex++;
			}
		}

		public void SetReportsFrom(int itemType) {
			if (NumColumns <= 0 || NumRows <= 0)
				return;

			// Update the row count to match the number of reports
			// Creating new reports from a type is efficient enough that we can do it every frame
			var reports = MagicCache.ShimmerInfos[itemType].GetShimmerReports().ToList();
			SetDimensions(NumColumns, Math.Max((reports.Count - 1) / 7 + 1, 1));

			// Set the reports
			SetReports(reports);
		}

		public void ClearReports() {
			foreach (var slot in Slots)
				slot.SetReport(null);
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);

			if (NumColumns <= 0 || NumRows <= 0)
				return;

			if (HoverSlot >= 0) {
				if (HoverSlot < NumColumns * NumRows) {
					var slot = Slots[HoverSlot / NumColumns, HoverSlot % NumColumns];

					if (slot.Report is null)
						MagicUI.mouseText = string.Empty;
				} else
					HoverSlot = -1;
			}
		}
	}
}
