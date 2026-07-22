using Microsoft.Xna.Framework;
using Terraria;

namespace MagicStorage {
	partial class Utility {
		/// <summary>
		/// Gets whether all three mechanical bosses have been defeated.
		/// </summary>
		public static bool DownedAllMechs => NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3;

		/// <summary>
		/// Gets the vanilla panel color without transparency.
		/// </summary>
		public static Color PanelColorWithoutTransparency => new(63, 82, 151);  // Same color as vanilla, but without the transparency
	}
}
