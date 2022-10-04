using MagicStorage.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.Common.Systems {
	internal class PortableAccessAreas : ModSystem {
		public struct DrawingContext {
			public readonly System.Drawing.RectangleF area;
			public readonly float colorFactor;
			public readonly Vector2 worldCenter;
			public readonly float playerToPylonRange;
			public readonly bool drawNear;

			public readonly bool valid;

			public DrawingContext(System.Drawing.RectangleF area, float colorFactor, Vector2 worldCenter, float playerToPylonRange, bool drawNear) {
				if (area.X < 0) {
					area.Width -= -area.X;
					area.X = 0;
				}
				if (area.Y < 0) {
					area.Height -= -area.Y;
					area.Y = 0;
				}
				if (area.Right >= Main.maxTilesX * 16)
					area.Width = Main.maxTilesX * 16 - area.X;
				if (area.Bottom >= Main.maxTilesY * 16)
					area.Height = Main.maxTilesY * 16 - area.Y;

				if (area.Width <= 0 || area.Height <= 0) {
					valid = false;
					this.area = default;
					this.colorFactor = 0;
					this.worldCenter = default;
					this.playerToPylonRange = 0;
					this.drawNear = false;
					return;
				}

				this.area = area;
				this.colorFactor = colorFactor;
				this.worldCenter = worldCenter;
				this.playerToPylonRange = playerToPylonRange;
				this.drawNear = drawNear;
				valid = true;
			}

			public void Extract(out Asset<Texture2D> asset, out Vector2 position, out Rectangle source, out Color color, out Vector2 origin, out float scale) {
				asset = DebugArea;
				position = worldCenter - Main.screenPosition;
				
				Rectangle withinRange = new(3, 1, 1, 1), outsideRange = new(1, 1, 1, 1);
				source = drawNear ? withinRange : outsideRange;

				color = Color.White * colorFactor;

				origin = Vector2.One / 2f;

				scale = playerToPylonRange * 2f;
			}

			public void Extract(out Texture2D texture, out Vector2 position, out Color color, out SpriteFrame frame, out float scale, out Alignment alignment) {
				texture = DebugArea.Value;
				position = worldCenter.ToTileCoordinates16().ToVector2();
				color = Color.White * colorFactor;
				
				SpriteFrame withinRange = new(5, 3, 3, 1), outsideRange = new(5, 3, 1, 1);
				frame = drawNear ? withinRange : outsideRange;

				frame.PaddingX = frame.PaddingY = 0;

				scale = playerToPylonRange * 2f / 16;

				alignment = Alignment.Center;
			}
		}

		public static Asset<Texture2D> DebugArea { get; private set; }

		public override void Load() {
			DebugArea = Mod.Assets.Request<Texture2D>("Assets/DebugPixels");
		}

		public override void PostDrawTiles() {
			if (!MagicStorageMod.UsingPrivateBeta || Main.gameMenu)
				return;

			#if NETPLAY
			if (!MagicStorageBetaConfig.ShowDebugPylonRangeAreas)
				return;
			#endif

			if (!CanDrawAreas(Main.LocalPlayer, out Point16 accessLocation, out float playerToPylonRange))
				return;

			SpriteBatch spriteBatch = Main.spriteBatch;

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

			DrawAreas(Main.LocalPlayer, spriteBatch, accessLocation, playerToPylonRange, drawNear: false);
			DrawAreas(Main.LocalPlayer, spriteBatch, accessLocation, playerToPylonRange, drawNear: true);

			spriteBatch.End();
		}

		internal static bool CanDrawAreas(Player player, out Point16 accessLocation, out float playerToPylonRange) {
			if (player.HeldItem.ModItem is not PortableAccess portableAccess || portableAccess.location.X < 0 || !portableAccess.GetEffectiveRange(out playerToPylonRange, out _) || playerToPylonRange < 0) {
				accessLocation = Point16.NegativeOne;
				playerToPylonRange = 0;
			} else
				accessLocation = portableAccess.location;

			return accessLocation.X >= 0;
		}

		private void DrawAreas(Player player, SpriteBatch spriteBatch, Point16 access, float playerToPylonRange, bool drawNear) {
			GetDrawingInformation(player, access, playerToPylonRange, drawNear, out var contexts);

			foreach (DrawingContext context in contexts) {
				context.Extract(out Asset<Texture2D> asset, out var position, out var source, out var color, out var origin, out var scale);

				spriteBatch.Draw(asset.Value, position, source, color, 0, origin, scale, SpriteEffects.None, 0);
			}
		}

		internal static void GetDrawingInformation(Player player, Point16 access, float playerToPylonRange, bool drawNear, out List<DrawingContext> contexts) {
			List<TeleportPylonInfo> pylons = Main.PylonSystem.Pylons
				.Where(p => Utility.IsPylonValidForRemoteAccessLinking(player, p, false))
				.Where(p => drawNear == Utility.PlayerIsNearPylonIgnoreValidity(player, p, playerToPylonRange))
				.ToList();

			System.Drawing.RectangleF GetNearbyRange(Vector2 center) {
				float x = center.X, y = center.Y;
				float xMin = x - playerToPylonRange, xMax = x + playerToPylonRange + 1, yMin = y - playerToPylonRange, yMax = y + playerToPylonRange + 1;

				return new(xMin, yMin, xMax - xMin, yMax - yMin);
			}

			var centers = pylons.Select(p => p.PositionInTiles.ToWorldCoordinates());

			if (drawNear == Utility.PlayerIsNearAccess(player, access, playerToPylonRange))
				centers = centers.Prepend(access.ToWorldCoordinates(16, 16));

			var areas = centers.Select(GetNearbyRange).ToList();

			var colorFactorPerArea = GetFactorForAllAreasBasedOnNeighbors(areas);

			contexts = areas.Zip(colorFactorPerArea, centers).Select(t => new DrawingContext(t.First, t.Second, t.Third, playerToPylonRange, drawNear)).ToList();
		}

		private static List<float> GetFactorForAllAreasBasedOnNeighbors(List<System.Drawing.RectangleF> areas) {
			//Get how many collisions there are per area
			List<int> collisionsByIndex = new();

			//And the neighbors being collided with
			List<List<int>> collidingNeighbors = new();

			for (int i = 0; i < areas.Count; i++) {
				var area = areas[i];
				collisionsByIndex.Add(0);
				collidingNeighbors.Add(new());

				for (int j = 0; j < areas.Count; j++) {
					if (j == i)
						continue;
					
					var check = areas[j];

					if (check.IntersectsWith(area)) {
						collisionsByIndex[i]++;
						collidingNeighbors[i].Add(j);
					}
				}
			}

			//Factor will be based on the maximum collisions count for an area and its colliding neighbors
			return collidingNeighbors.Select((neighbors, i) => 0.4f / neighbors.Select(n => collisionsByIndex[n]).Prepend(collisionsByIndex[i]).Append(1).Max()).ToList();
		}
	}
}
