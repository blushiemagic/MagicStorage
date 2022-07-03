using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.CrossMod.Testing {
	[Autoload(false)]  //Shouldn't appear in the public builds
	internal class ModuleAlwaysHaveWorkbenchActive : EnvironmentModule {
		public override void ModifyCraftingZones(EnvironmentSandbox sandbox, ref CraftingInformation information) {
			information.adjTiles[TileID.WorkBenches] = true;
		}
	}
}
