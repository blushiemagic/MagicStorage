using System.Diagnostics;
using System.Reflection;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class Utility {
		internal static bool TryScanStackTraceForMods(StackTrace trace, out Mod recentModCaller) {
			for (int i = 0; i < trace.FrameCount; i++) {
				// Find the assembly the frame was from
				if (trace.GetFrame(i)?.GetMethod() is not { DeclaringType.Assembly: Assembly assembly })
					continue;

				// If the assembly was from a mod, return it
				foreach (Mod mod in ModLoader.Mods) {
					if (object.ReferenceEquals(mod.Code, assembly)) {
						recentModCaller = mod;
						return true;
					}
				}
			}

			recentModCaller = null;
			return false;
		}
	}
}
