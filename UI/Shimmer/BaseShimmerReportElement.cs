using MagicStorage.Common.Systems.Shimmering;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace MagicStorage.UI.Shimmer {
	public abstract class BaseShimmerReportElement : UIElement {
		protected readonly UIPanel _panel;
		protected ShimmerReportIcon _icon;

		protected IShimmerResultReport _report;

		public IShimmerResultReport Report => _report;

		public BaseShimmerReportElement() {
			_panel = new UIPanel();
			_panel.Width.Set(0, 1f);
			_panel.Height.Set(0, 1f);
			_panel.SetPadding(0);
			Append(_panel);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			base.DrawSelf(spriteBatch);

			if (_panel.IsMouseHovering && _report is ItemReport itemReport)
				Main.HoverItem = ContentSamples.ItemsByType[itemReport.itemType].Clone();
		}

		public void SetReport(IShimmerResultReport report) {
			_icon.SetReport(report);
			_report = report;
			OnReportSet();
		}

		protected virtual void OnReportSet() { }
	}
}
