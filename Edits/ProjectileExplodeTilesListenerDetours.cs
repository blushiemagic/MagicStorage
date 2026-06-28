using MagicStorage.Common;
using Microsoft.Xna.Framework;
using SerousCommonLib.API;
using Terraria;

namespace MagicStorage.Edits {
	public class ProjectileExplodeTilesListenerDetours : Edit {
		internal static int ExplodeTilesPlayer = -1;

		public override void LoadEdits() {
			On_Projectile.ExplodeTiles += Projectile_ExplodeTiles;
			On_Projectile.CanExplodeTile += Projectile_CanExplodeTile;
		}

		public override void UnloadEdits() {
			On_Projectile.ExplodeTiles -= Projectile_ExplodeTiles;
			On_Projectile.CanExplodeTile -= Projectile_CanExplodeTile;
		}

		private static bool CanTrackOwner(Projectile self) => self.friendly && !self.hostile && !self.npcProj && !self.trap;

		private static void Projectile_ExplodeTiles(On_Projectile.orig_ExplodeTiles orig, Projectile self, Vector2 compareSpot, int radius, int minI, int maxI, int minJ, int maxJ, bool wallSplode) {
			// Only allow "friendly" (usually player-owned) explosives to count
			int newOwner = CanTrackOwner(self) ? self.owner : -1;

			using (ObjectSwitch.Create(ref ExplodeTilesPlayer, newOwner))
				orig(self, compareSpot, radius, minI, maxI, minJ, maxJ, wallSplode);
		}

		// A separate detour is needed since CanExplodeTile() is public
		private static bool Projectile_CanExplodeTile(On_Projectile.orig_CanExplodeTile orig, Projectile self, int x, int y) {
			// Only allow "friendly" (usually player-owned) explosives to count
			int newOwner = CanTrackOwner(self) ? self.owner : -1;

			using (ObjectSwitch.Create(ref ExplodeTilesPlayer, newOwner))
				return orig(self, x, y);
		}
	}
}
