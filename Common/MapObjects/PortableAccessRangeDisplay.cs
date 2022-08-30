using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.Map;
using Terraria.ModLoader;

namespace MagicStorage.Common.MapObjects {
	internal class PortableAccessRangeDisplay : ModMapLayer {
		private struct MapOverlayDrawContextCapture {
			public static readonly FieldInfo _mapPosition = typeof(MapOverlayDrawContext).GetField("_mapPosition", BindingFlags.NonPublic | BindingFlags.Instance);
			public static readonly FieldInfo _mapOffset = typeof(MapOverlayDrawContext).GetField("_mapOffset", BindingFlags.NonPublic | BindingFlags.Instance);
			public static readonly FieldInfo _clippingRect = typeof(MapOverlayDrawContext).GetField("_clippingRect", BindingFlags.NonPublic | BindingFlags.Instance);
			public static readonly FieldInfo _mapScale = typeof(MapOverlayDrawContext).GetField("_mapScale", BindingFlags.NonPublic | BindingFlags.Instance);
			public static readonly FieldInfo _drawScale = typeof(MapOverlayDrawContext).GetField("_drawScale", BindingFlags.NonPublic | BindingFlags.Instance);

			public Vector2 mapPosition;
			public Vector2 mapOffset;
			public Rectangle? clippingRect;
			public float mapScale;
			public float drawScale;

			public static MapOverlayDrawContextCapture Capture(MapOverlayDrawContext context) {
				MapOverlayDrawContextCapture capture = new();

				capture.mapPosition = (Vector2)_mapPosition.GetValue(context);
				capture.mapOffset = (Vector2)_mapOffset.GetValue(context);
				capture.clippingRect = (Rectangle?)_clippingRect.GetValue(context);
				capture.mapScale = (float)_mapScale.GetValue(context);
				capture.drawScale = (float)_drawScale.GetValue(context);
				
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

			drawContext.Extract(out Texture2D texture, out var position, out var color, out var frame, out var scale, out var alignment);

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
