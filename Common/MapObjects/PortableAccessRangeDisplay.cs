using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Map;
using Terraria.ModLoader;

namespace MagicStorage.Common.MapObjects {
	internal class PortableAccessRangeDisplay : ModMapLayer {
		private struct MapOverlayDrawContextCapture {
			public Vector2 mapPosition;
			public Vector2 mapOffset;
			public Rectangle? clippingRect;
			public float mapScale;
			public float drawScale;

			public static MapOverlayDrawContextCapture Capture(MapOverlayDrawContext context) {
				MapOverlayDrawContextCapture capture = new();

				capture.mapPosition = context._mapPosition;
				capture.mapOffset = context._mapOffset;
				capture.clippingRect = context._clippingRect;
				capture.mapScale = context._mapScale;
				capture.drawScale = context._drawScale;
				
				return capture;
			}
		}

		public override void Draw(ref MapOverlayDrawContext context, ref string text) {
			if (!Main.mapFullscreen || !PortableAccessAreas.CanDrawAreas(Main.LocalPlayer, out Point16 accessLocation, out float playerToPylonRange))
				return;

			MapOverlayDrawContextCapture capture = MapOverlayDrawContextCapture.Capture(context);

			PortableAccessAreas.GetDrawingInformation(Main.LocalPlayer, accessLocation, playerToPylonRange, false, out var contexts);
			foreach (var drawContext in contexts)
				DrawMapObject(drawContext, capture);

			PortableAccessAreas.GetDrawingInformation(Main.LocalPlayer, accessLocation, playerToPylonRange, true, out contexts);
			foreach (var drawContext in contexts)
				DrawMapObject(drawContext, capture);
		}

		private static void DrawMapObject(PortableAccessAreas.DrawingContext drawContext, MapOverlayDrawContextCapture capture) {
			if (!drawContext.valid)
				return;

			drawContext.ExtractToMap(out Texture2D texture, out var position, out var color, out var frame, out var scale, out var alignment);

			position = (position - capture.mapPosition) * capture.mapScale + capture.mapOffset;
			if (capture.clippingRect.HasValue && !capture.clippingRect.Value.Contains(position.ToPoint()))
				return;

			Rectangle sourceRectangle = frame.GetSourceRectangle(texture);
			Vector2 vector = sourceRectangle.Size() * alignment.OffsetMultiplier;

			float num = scale * Main.mapFullscreenScale;

			Main.spriteBatch.Draw(texture, position, sourceRectangle, color, 0f, vector, num, SpriteEffects.None, 0f);
		}
	}
}
