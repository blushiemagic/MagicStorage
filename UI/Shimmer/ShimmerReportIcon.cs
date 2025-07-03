using MagicStorage.Common.Systems.Shimmering;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using Terraria.UI;
using Terraria;

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
			if (permitDrawing) {
				var dims = GetDimensions();

				Rectangle frame = _report.GetAnimationFrame();

				float scale = 1f;
				float dimensionLimit = Math.Min(dims.Width, dims.Height) - 4;
				if (frame.Width > dimensionLimit || frame.Height > dimensionLimit)
					scale = dimensionLimit / Math.Max(frame.Width, frame.Height);

				spriteBatch.Draw(_report.Texture.Value, dims.Center(), frame, Color.White, 0, frame.Size() / 2f, scale, SpriteEffects.None, 0);
			}
		}
	}
}
