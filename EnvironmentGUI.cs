using MagicStorage.Components;
using MagicStorage.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace MagicStorage {
	public static class EnvironmentGUI {
		private const int Padding = 4;

		internal static MouseState curMouse;
		internal static MouseState oldMouse;

		private static UIPanel basePanel;
		private static float panelTop;
		private static float panelLeft;
		private static float panelWidth;
		private static float panelHeight;

		private static UIList list;

		internal static Dictionary<string, EnvironmentGUIModEntry> entriesByMod = new();

		internal static TEEnvironmentAccess currentAccess;

		public static bool MouseClicked => curMouse.LeftButton == ButtonState.Pressed && oldMouse.LeftButton == ButtonState.Released;

		public static bool RightMouseClicked => curMouse.RightButton == ButtonState.Pressed && oldMouse.RightButton == ButtonState.Released;

		public static void Initialize() {
			entriesByMod ??= new();

			panelTop = Main.instance.invBottom + 60;
			panelLeft = 20f;

			basePanel?.Remove();
			basePanel?.RemoveAllChildren();
			basePanel = new UIPanel();
			float innerPanelWidth = 600f + Padding;
			panelWidth = basePanel.PaddingLeft + innerPanelWidth + basePanel.PaddingRight;
			panelHeight = Main.screenHeight - panelTop;
			basePanel.Left.Set(panelLeft, 0f);
			basePanel.Top.Set(panelTop, 0f);
			basePanel.Width.Set(panelWidth, 0f);
			basePanel.Height.Set(panelHeight, 0f);
			basePanel.Recalculate();

			list?.Remove();
			list?.Clear();

			list = new();
            list.SetPadding(0);
            list.Width.Set(-20, 1f);
            list.Height.Set(0, 0.9f);
            list.Left.Set(20, 0);
            list.Top.Set(0, 0.05f);
            basePanel.Append(list);

            UIScrollbar scroll = new();
            scroll.Width.Set(20, 0);
            scroll.Height.Set(0, 0.825f);
            scroll.Left.Set(0, 0.95f);
            scroll.Top.Set(0, 0.1f);

            list.SetScrollbar(scroll);
            list.Append(scroll);
            list.ListPadding = 10;

			foreach (Mod mod in ModLoader.Mods) {
				EnvironmentGUIModEntry entry = new(mod);
				
				if (!entry.Exists)
					continue;

				entry.Width.Set(0, 0.93f);
				entriesByMod[mod.Name] = entry;
				list.Add(entry);
			}

			/*
			MagicStorage.Instance.Logger.Debug("Environment GUI Initialized\n" +
				"Known modules:\n" +
				"  " + string.Join("\n  ", entriesByMod.Values.SelectMany(entry => entry.Mod.GetContent<EnvironmentModule>().Select(m => m.FullName))));
			*/

			basePanel.Activate();
		}

		internal static void Unload() {
			currentAccess = null;

			entriesByMod = null;
			basePanel = null;
			list = null;
		}

		public static void Update(GameTime gameTime) {
			try {
				oldMouse = StorageGUI.oldMouse;
				curMouse = StorageGUI.curMouse;
				
				if (Main.playerInventory && StoragePlayer.LocalPlayer.ViewingStorage().X >= 0 && StoragePlayer.IsStorageEnvironment()) {
					basePanel?.Update(gameTime);
				}
			} catch (Exception e) {
				Main.NewTextMultiline(e.ToString(), c: Color.White);
			}
		}

		public static void LoadModules(TEEnvironmentAccess access) {
			//Assign the "state" for each module
			foreach (var entry in entriesByMod.Values) {
				foreach (var module in access.Modules.Where(m => m.Mod == entry.Mod))
					entry.SetLabel(module, access.Enabled(module));
			}

			currentAccess = access;
		}

		public static void Draw() {
			try {
				Player player = Main.LocalPlayer;

				if (Main.mouseX > panelLeft && Main.mouseX < panelLeft + panelWidth && Main.mouseY > panelTop && Main.mouseY < panelTop + panelHeight) {
					player.mouseInterface = true;
					player.cursorItemIconEnabled = false;
					InterfaceHelper.HideItemIconCache();
				}

				basePanel.Draw(Main.spriteBatch);
			} catch (Exception e) {
				Main.NewTextMultiline(e.ToString(), c: Color.White);
			}
		}
	}
}
