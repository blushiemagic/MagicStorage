using SerousCommonLib.API;
using Terraria;

namespace MagicStorage.Edits {
	public class PlayerPickTileListenerDetour : Edit {
		internal static int PickTilePlayer = -1;

		public override void LoadEdits() {
			On_Player.PickTile += Player_PickTile;
		}

		public override void UnloadEdits() {
			On_Player.PickTile -= Player_PickTile;
		}

		private static void Player_PickTile(On_Player.orig_PickTile orig, Player self, int x, int y, int pickPower) {
			try {
				PickTilePlayer = self.whoAmI;
				orig(self, x, y, pickPower);
			} finally {
				PickTilePlayer = -1;
			}
		}
	}
}
