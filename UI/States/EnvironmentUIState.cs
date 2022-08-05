﻿using MagicStorage.Components;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.UI.States {
	public sealed class EnvironmentUIState : BaseStorageUI {
		public override string DefaultPage => "Modules";

		protected override IEnumerable<string> GetMenuOptions() {
			yield return "Modules";
		}

		protected override BaseStorageUIPage InitPage(string page)
			=> page switch {
				"Modules" => new ModulesPage(this),
				_ => throw new ArgumentException("Unknown page: " + page, nameof(page))
			};

		protected override void PostInitializePages() {
			float innerPanelWidth = 600f + EnvironmentGUI.Padding;
			PanelWidth = PaddingLeft + innerPanelWidth + panel.PaddingRight;

			panel.Left.Set(PanelLeft, 0f);
			panel.Top.Set(PanelTop, 0f);
			panel.Width.Set(PanelWidth, 0f);
			panel.Height.Set(PanelHeight, 0f);
		}

		public class ModulesPage : BaseStorageUIPage {
			private static UIList list;

			internal static Dictionary<string, EnvironmentGUIModEntry> entriesByMod = new();

			private static UIText noModulesLoaded;
			private static UIScrollbar scroll;

			public ModulesPage(BaseStorageUI parent) : base(parent, "Modules") { }

			public override void OnInitialize() {
				list = new();
				list.SetPadding(0);
				list.Width.Set(-20, 1f);
				list.Height.Set(0, 0.9f);
				list.Left.Set(20, 0);
				list.Top.Set(0, 0.05f);
				Append(list);

				scroll = new();
				scroll.Width.Set(20, 0);
				scroll.Height.Set(0, 0.825f);
				scroll.Left.Set(0, 0.95f);
				scroll.Top.Set(0, 0.1f);
				scroll.SetView(viewSize: 1f, maxViewSize: 2f);

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

				if (entriesByMod.Count == 0) {
					noModulesLoaded = new(Language.GetTextValue("Mods.MagicStorage.EnvironmentGUI.NoModules"), textScale: 1.3f);
					list.Add(noModulesLoaded);
				}
			}

			public void LoadModules(TEEnvironmentAccess access) {
				//Assign the "state" for each module
				foreach (var entry in entriesByMod.Values) {
					foreach (var module in access.Modules.Where(m => m.Mod == entry.Mod))
						entry.SetLabel(module, access.Enabled(module));
				}
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);

				EnvironmentUIState parent = parentUI as EnvironmentUIState;

				Player player = Main.LocalPlayer;

				if (Main.mouseX > parent.PanelLeft && Main.mouseX < parent.PanelWidth && Main.mouseY > parent.PanelTop && Main.mouseY < parent.PanelBottom) {
					player.mouseInterface = true;
					player.cursorItemIconEnabled = false;
					InterfaceHelper.HideItemIconCache();
				}
			}
		}
	}
}
