using MagicStorage.Common.Systems.Shimmering;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using Terraria.UI;

namespace MagicStorage.UI.Shimmer {
	public class ShimmerReportIcon : UIElement {
		private IShimmerResultReport _report;

		public IShimmerResultReport Report => _report;

		public ShimmerReportIcon() { }

		public ShimmerReportIcon(float width, float height) {
			Width.Set(width, 0f);
			Height.Set(height, 0f);
		}

		public void SetReport(IShimmerResultReport report) {
			ArgumentNullException.ThrowIfNull(report);
			_report = report;
			if (_report is not null)
				_report.Parent = this;
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);
			_report?.Update();
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			bool permitDrawing = _report?.Render(spriteBatch) ?? false;
			if (permitDrawing)
				spriteBatch.Draw(_report.Texture.Value, GetDimensions().ToRectangle(), _report.GetAnimationFrame(), Color.White);
		}
	}
}
