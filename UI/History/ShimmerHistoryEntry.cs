using MagicStorage.UI.Shimmer;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.UI.History {
	public class ShimmerHistoryEntry : HistoryEntry<int> {
		public ShimmerReportZone resultZone;

		public ShimmerHistoryEntry(int index, IHistoryCollection<int> history) : base(index, history) { }

		public override void OnInitialize() {
			base.OnInitialize();

			resultZone = new(CraftingGUI.InventoryScale * 0.8f);
			resultZone.Left.Set(resultSlot.Width.Pixels + 4, 0f);

			resultZone.SetDimensions(1, 1);

			UpdateZoneDimensions();

			Append(resultZone);
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);

			// Always update the reports
			var oldColumns = resultZone.NumColumns;
			var oldRows = resultZone.NumRows;

			resultZone.SetReportsFrom(Value);

			// Only recalculate the size if the number of columns or rows changed
			if (oldColumns != resultZone.NumColumns || oldRows != resultZone.NumRows) {
				UpdateZoneDimensions();
				Recalculate();
			}
		}

		private void UpdateZoneDimensions() {
			resultZone.Width.Set(resultZone.ZoneWidth, 0f);
			resultZone.Height.Set(resultZone.ZoneHeight, 0f);

			Width.Set(resultSlot.Width.Pixels + 4 + resultZone.ZoneWidth + 4, 0f);
			Height.Set(Math.Max(resultSlot.Height.Pixels, resultZone.ZoneHeight) + 4, 0f);
		}

		protected override Item GetItemForResult() {
			int item = Value;

			if (item <= ItemID.None || item >= ItemLoader.ItemCount || ContentSamples.ItemsByType[item] is not { IsAir: false } sample)
				return new Item();

			return sample;
		}

		protected override void OnValueSet(int value) {
			resultZone.SetReportsFrom(value);
			UpdateZoneDimensions();
			Recalculate();
		}

		protected override void GetResultContext(int value, ref int context) {
			if (value == DecraftingGUI.selectedItem)
				context = ItemSlot.Context.TrashItem;
			else if (!DecraftingGUI.IsAvailable(value))
				context = ItemSlot.Context.ChestItem;
		}
	}
}
