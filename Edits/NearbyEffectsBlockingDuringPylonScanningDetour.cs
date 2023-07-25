using SerousCommonLib.API;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Terraria.ModLoader;

namespace MagicStorage.Edits {
	internal class NearbyEffectsBlockingDuringPylonScanningDetour : Edit {
		public static bool DoBlockHooks;

		private static readonly MethodInfo TileLoader_NearbyEffects = typeof(TileLoader).GetMethod(nameof(TileLoader.NearbyEffects), BindingFlags.Public | BindingFlags.Static);

		private delegate void orig_TileLoader_NearbyEffects(int i, int j, int type, bool closer);
		private delegate void hook_TileLoader_NearbyEffects(orig_TileLoader_NearbyEffects orig, int i, int j, int type, bool closer);
		private static Hook On_TileLoader_NearbyEffects;

		public override void LoadEdits() {
			On_TileLoader_NearbyEffects = new Hook(TileLoader_NearbyEffects, new hook_TileLoader_NearbyEffects(Hook_TileLoader_NearbyEffects));
		}

		public override void UnloadEdits() {
			On_TileLoader_NearbyEffects = null;
		}

		private void Hook_TileLoader_NearbyEffects(orig_TileLoader_NearbyEffects orig, int i, int j, int type, bool closer) {
			if (!DoBlockHooks)
				orig(i, j, type, closer);
		}
	}
}
