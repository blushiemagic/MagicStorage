using MagicStorage.Components;
using MagicStorage.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
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

			if (Main.LocalPlayer.HeldItem.ModItem is not PortableAccess portableAccess || !portableAccess.GetEffectiveRange(out float playerToPylonRange, out _) || playerToPylonRange < 0)
				return;

			SpriteBatch spriteBatch = Main.spriteBatch;

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

			DrawAreas(Main.LocalPlayer, spriteBatch, playerToPylonRange, drawNear: false);
			DrawAreas(Main.LocalPlayer, spriteBatch, playerToPylonRange, drawNear: true);

			spriteBatch.End();
		}

		private void DrawAreas(Player player, SpriteBatch spriteBatch, float playerToPylonRange, bool drawNear) {
			Texture2D pixels = Mod.Assets.Request<Texture2D>("Assets/DebugPixels", AssetRequestMode.ImmediateLoad).Value;

			Rectangle withinRange = new(3, 1, 1, 1), outsideRange = new(1, 1, 1, 1);
			Vector2 areaScale = new(playerToPylonRange * 2f);

			Rectangle source = drawNear ? withinRange : outsideRange;

			foreach (TEStorageHeart heart in TileEntity.ByPosition.Values.OfType<TEStorageHeart>().Where(heart => drawNear == Utility.PlayerIsNearStorageSystem(player, heart, playerToPylonRange))) {
				Vector2 center = heart.Position.ToWorldCoordinates(16, 16) - Main.screenPosition;

				spriteBatch.Draw(pixels, center, source, Color.White * 0.4f, 0, Vector2.One / 2f, areaScale, SpriteEffects.None, 0);
			}

			foreach (TeleportPylonInfo pylon in Utility.NearbyPylons(player, playerToPylonRange)) {
				Vector2 center = pylon.PositionInTiles.ToWorldCoordinates() - Main.screenPosition;

				spriteBatch.Draw(pixels, center, source, Color.White * 0.4f, 0, Vector2.One / 2f, areaScale, SpriteEffects.None, 0);
			}
		}
	}
}
