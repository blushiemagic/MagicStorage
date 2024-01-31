using MagicStorage.Common.Systems;
using MagicStorage.Components;
using Microsoft.Xna.Framework;
using SerousCommonLib.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.UI.States {
	public class EnvironmentUIState : BaseStorageUI {
		public override string DefaultPage => "Modules";

		public override int GetFilteringOption() => throw new NotImplementedException();

		public override string GetSearchText() => throw new NotImplementedException();

		public override int GetSortingOption() => throw new NotImplementedException();

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
			PanelWidth = panel.PaddingLeft + innerPanelWidth + panel.PaddingRight;
		}

		public override float GetMinimumResizeHeight() => 150;

		public override void Recalculate() {
			base.Recalculate();

			if (!Main.gameMenu && PanelHeight < GetMinimumResizeHeight()) {
				// Attempt to force the UI layout to one that takes up less vertical space
				if (MagicUI.AttemptForcedLayoutChange(this))
					return;

				MagicUI.CloseUIDueToHeightLimit();
				pendingUIChange = true;
			}
		}

		public class ModulesPage : BaseStorageUIPage {
			private NewUIList list;

			internal Dictionary<string, EnvironmentGUIModEntry> entriesByMod = new();

			private UIText noModulesLoaded;
			private NewUIScrollbar scroll;

			public ModulesPage(BaseStorageUI parent) : base(parent, "Modules") { }

			public override void OnInitialize() {
				base.OnInitialize();

				list = new();
				list.SetPadding(0);
				list.Width.Set(-20, 1f);
				list.Height.Set(-20, 1f);
				list.Left.Set(10, 0f);
				list.Top.Set(10, 0f);

				scroll = new();
				scroll.Height.Set(-30, 1f);
				scroll.Left.Set(-20, 1f);
				scroll.Top.Set(10, 0f);

				list.SetScrollbar(scroll);
				list.Append(scroll);
				list.ListPadding = 10;
				Append(list);

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
					foreach (var module in EnvironmentModuleLoader.modules.Where(m => m.Mod == entry.Mod)) {
						if (entry.labelsByName.TryGetValue(module.Name, out var label))
							label.SetState(access.Enabled(module));
					}
				}
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);

				EnvironmentUIState parent = parentUI as EnvironmentUIState;

				Player player = Main.LocalPlayer;

				if (Main.mouseX > parent.PanelLeft && Main.mouseX < parent.PanelRight && Main.mouseY > parent.PanelTop && Main.mouseY < parent.PanelBottom) {
					player.mouseInterface = true;
					player.cursorItemIconEnabled = false;
					InterfaceHelper.HideItemIconCache();
				}
			}
		}
	}
}
