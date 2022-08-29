using MagicStorage.Components;
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

namespace MagicStorage.Common.Systems {
	internal class PortableAccessAreas : ModSystem {
		public override void PostDrawTiles() {
			if (!MagicStorageMod.UsingPrivateBeta || Main.gameMenu)
				return;

			#if NETPLAY
			if (!MagicStorageBetaConfig.ShowDebugPylonRangeAreas)
				return;
			#endif

			if (Main.LocalPlayer.HeldItem.ModItem is not PortableAccess portableAccess || portableAccess.location.X < 0 || !portableAccess.GetEffectiveRange(out float playerToPylonRange, out _) || playerToPylonRange < 0)
				return;

			SpriteBatch spriteBatch = Main.spriteBatch;

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

			DrawAreas(Main.LocalPlayer, spriteBatch, portableAccess.location, playerToPylonRange, drawNear: false);
			DrawAreas(Main.LocalPlayer, spriteBatch, portableAccess.location, playerToPylonRange, drawNear: true);

			spriteBatch.End();
		}

		private void DrawAreas(Player player, SpriteBatch spriteBatch, Point16 access, float playerToPylonRange, bool drawNear) {
			Texture2D pixels = Mod.Assets.Request<Texture2D>("Assets/DebugPixels", AssetRequestMode.ImmediateLoad).Value;

			Rectangle withinRange = new(3, 1, 1, 1), outsideRange = new(1, 1, 1, 1);
			Vector2 areaScale = new(playerToPylonRange * 2f);

			Rectangle source = drawNear ? withinRange : outsideRange;

			List<TeleportPylonInfo> pylons = Main.PylonSystem.Pylons
				.Where(p => Utility.IsPylonValidForRemoteAccessLinking(p, false))
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

			var factors = GetFactorForAllAreasBasedOnNeighbors(areas);

			int curFactor = 0;
			foreach (Vector2 center in centers) {
				Vector2 drawCenter = center - Main.screenPosition;

				spriteBatch.Draw(pixels, drawCenter, source, Color.White * factors[curFactor], 0, Vector2.One / 2f, areaScale, SpriteEffects.None, 0);
			}
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
