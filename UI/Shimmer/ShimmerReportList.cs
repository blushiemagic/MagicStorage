using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.Shimmering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SerousCommonLib.UI;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace MagicStorage.UI.Shimmer {
	public class ShimmerReportList : UIElement {
		private class Entry : BaseShimmerReportElement {
			private readonly UIPanel _panel;
			private readonly UIText _label;

			public Entry() {
				Width.Set(-24, 1f);
				Height.Set(ENTRY_HEIGHT, 0f);

				// The members need to be created here; OnInitialize runs too late
				_panel = new UIPanel();
				_panel.Width.Set(0, 1f);
				_panel.Height.Set(0, 1f);
				_panel.SetPadding(0);
				Append(_panel);

				_icon = new ShimmerReportIcon() {
					VAlign = 0.5f
				};
				_icon.Left.Set(4, 0f);
				_icon.Width.Set(ENTRY_ICON_HEIGHT, 0f);
				_icon.Height.Set(ENTRY_ICON_HEIGHT, 0f);
				_panel.Append(_icon);

				_label = new UIText(string.Empty, 0.8f) {
					VAlign = 0.5f,
					DynamicallyScaleDownToWidth = true
				};
				_label.Left.Set(40, 0f);
				_label.MaxWidth.Set(-40, 1f);
				_panel.Append(_label);
			}

			protected override void OnReportSet() {
				SetLabelFromReport();

				// Make NPC entries larger
				if (_report is NPCSpawnReport) {
					float stretch = ENTRY_ICON_HEIGHT * 0.5f;

					Height.Pixels += stretch;
					_icon.Height.Pixels += stretch;
					Recalculate();
				}
			}

			private void SetLabelFromReport() {
				string oldText = _label.Text;

				if (_report is null)
					_label.SetText(string.Empty);
				else
					_label.SetText(_report.Label);

				if (_label.Text != oldText)
					_label.Recalculate();
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);
				SetLabelFromReport();
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				base.DrawSelf(spriteBatch);

				if (_icon.IsMouseHovering && _report is ItemReport itemReport)
					Main.HoverItem = ContentSamples.ItemsByType[itemReport.itemType].Clone();
			}
		}

		public const float ENTRY_HEIGHT = 28f;
		public const float ENTRY_ICON_HEIGHT = 24f;

		public readonly NewUIList _innerList = new();

		public ShimmerReportList() {
			_innerList = new();
			_innerList.Width.Set(0, 1f);
			_innerList.Height.Set(0, 1f);
			Append(_innerList);
		}

		public void Add(IShimmerResultReport report) {
			Entry entry = new();
			entry.SetReport(report);
			_innerList.Add(entry);
		}

		public void Add(IEnumerable<IShimmerResultReport> reports) {
			foreach (IShimmerResultReport report in reports)
				Add(report);
		}

		public void AddFrom(int item) => Add(MagicCache.ShimmerInfos[item].GetShimmerReports());

		public void Clear() {
			_innerList.Clear();
		}

		public IShimmerResultReport GetReport(int index) => ((Entry)_innerList._items[index]).Report;
	}
}
