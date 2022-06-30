using MagicStorage.Components;
using System.Collections.Generic;

namespace MagicStorage {
	public static class EnvironmentModuleLoader {
		private static List<EnvironmentModule> modules = new();
		public static int Count { get; private set; }

		internal static int Add(EnvironmentModule module) {
			modules.Add(module);
			Count++;
			return Count - 1;
		}

		public static EnvironmentModule Get(int index) => index < 0 || index >= modules.Count ? null : modules[index];

		internal static void Load() {
			modules = new();
			Count = 0;
		}

		internal static void Unload() {
			modules = null;
			Count = 0;
		}
	}
}
