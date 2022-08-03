using MagicStorage.Common.Systems;
using MagicStorage.Components;
using MagicStorage.UI;
using MagicStorage.UI.States;
using Microsoft.Xna.Framework.Input;

namespace MagicStorage {
	public static class EnvironmentGUI {
		public const int Padding = 4;

		internal static MouseState curMouse;
		internal static MouseState oldMouse;

		internal static TEEnvironmentAccess currentAccess;

		public static bool MouseClicked => curMouse.LeftButton == ButtonState.Pressed && oldMouse.LeftButton == ButtonState.Released;

		internal static void Unload() {
			currentAccess = null;
		}

		public static void LoadModules(TEEnvironmentAccess access) {
			MagicUI.environmentUI.GetPage<EnvironmentUIState.ModulesPage>("Modules").LoadModules(access);

			currentAccess = access;
		}
	}
}
