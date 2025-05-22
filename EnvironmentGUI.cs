using MagicStorage.Common.Systems;
using MagicStorage.Components;
using MagicStorage.UI.States;

namespace MagicStorage {
	public static class EnvironmentGUI {
		public const int Padding = 4;

		internal static TEEnvironmentAccess currentAccess;
		internal static bool accessPopulationPending;

		internal static void Unload() {
			currentAccess = null;
		}

		public static void LoadModules(TEEnvironmentAccess access) {
			if (!StoragePlayer.IsCurrentLocalNetworkAccessible()) {
				NetHelper.Report(true, "EnvironmentGUI: LoadModules invoked with inaccessible network");

				MagicUI.environmentUI.GetDefaultPage<EnvironmentUIState.ModulesPage>().LoadModules(access);
				accessPopulationPending = false;
			} else
				accessPopulationPending = true;

			currentAccess = access;
		}
	}
}
