using MonoMod.RuntimeDetour.HookGen;
using System.Reflection;
using Terraria.ModLoader;

namespace MagicStorage.Edits {
	internal class NearbyEffectsBlockingDuringPylonScanningEdit : Edit {
		public static bool DoBlockHooks;

		private static readonly MethodInfo TileLoader_NearbyEffects = typeof(TileLoader).GetMethod(nameof(TileLoader.NearbyEffects), BindingFlags.Public | BindingFlags.Static);

		private delegate void orig_TileLoader_NearbyEffects(int i, int j, int type, bool closer);
		private delegate void hook_TileLoader_NearbyEffects(orig_TileLoader_NearbyEffects orig, int i, int j, int type, bool closer);
		private static event hook_TileLoader_NearbyEffects On_TileLoader_NearbyEffects {
			add => HookEndpointManager.Add<hook_TileLoader_NearbyEffects>(TileLoader_NearbyEffects, value);
			remove => HookEndpointManager.Remove<hook_TileLoader_NearbyEffects>(TileLoader_NearbyEffects, value);
		}

		public override void LoadEdits() {
			On_TileLoader_NearbyEffects += Hook_TileLoader_NearbyEffects;
		}

		public override void UnloadEdits() {
			On_TileLoader_NearbyEffects -= Hook_TileLoader_NearbyEffects;
		}

		private void Hook_TileLoader_NearbyEffects(orig_TileLoader_NearbyEffects orig, int i, int j, int type, bool closer) {
			if (!DoBlockHooks)
				orig(i, j, type, closer);
		}
	}
}
