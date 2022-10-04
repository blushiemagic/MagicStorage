using MagicStorage.Common.Systems;
using MagicStorage.Components;
using MagicStorage.UI.States;

namespace MagicStorage {
	public static class EnvironmentGUI {
		public const int Padding = 4;

		internal static TEEnvironmentAccess currentAccess;

		internal static void Unload() {
			currentAccess = null;
		}

		public static void LoadModules(TEEnvironmentAccess access) {
			MagicUI.environmentUI.GetPage<EnvironmentUIState.ModulesPage>("Modules").LoadModules(access);

			currentAccess = access;
		}
	}
}
