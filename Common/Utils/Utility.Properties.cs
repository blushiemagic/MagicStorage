using Microsoft.Xna.Framework;
using Terraria;

namespace MagicStorage {
	partial class Utility {
		public static bool DownedAllMechs => NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3;

		public static Color PanelColorWithoutTransparency => new(63, 82, 151);  // Same color as vanilla, but without the transparency
	}
}
