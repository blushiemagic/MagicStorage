using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.UI {
	internal class EnvironmentGUIModEntry : UIElement {
		public readonly Mod Mod;

		public readonly bool Exists;

		internal Dictionary<string, EnvironmentGUIToggleLabel> labelsByName = new();

		public EnvironmentGUIModEntry(string mod) {
			Exists = ModLoader.TryGetMod(mod, out Mod);

			if (Exists)
				Exists &= Mod.GetContent<EnvironmentModule>().Any();
		}

		public EnvironmentGUIModEntry(Mod mod) {
			Mod = mod;

			Exists = Mod.GetContent<EnvironmentModule>().Any();
		}

		public override void OnInitialize() {
			if (!Exists) {
				Remove();
				return;
			}

			List<EnvironmentModule> modules = Mod.GetContent<EnvironmentModule>().ToList();

			UIText modLabel = new(Mod.Name, 1.1f);
			Append(modLabel);

			UIHorizontalSeparator separator = new();
			separator.Top.Set(30, 0f);
			separator.Width.Set(0, 1f);
			Append(separator);

			float top = 40;

			foreach (EnvironmentModule module in modules) {
				EnvironmentGUIToggleLabel label = new(module.DisplayName.GetTranslation(Language.ActiveCulture), module.Name, defaultState: true);
				label.Top.Set(top, 0f);
				label.Height.Set(20, 0f);
				label.Width.Set(0, 1f);
				Append(label);
				
				top += label.Height.Pixels + 6;

				labelsByName[module.Name] = label;
			}

			/*
			MagicStorage.Instance.Logger.Debug($"Environment GUI Mod Entry for mod \"{Mod.Name}\" initialized\n" +
				"Labels:\n" +
				"  " + string.Join("\n  ", labelsByName.Values.Select(entry => Mod.TryFind<EnvironmentModule>(entry.Module, out var env) ? env.FullName : "unknown")));
			*/

			Height.Set(top, 0f);
			Recalculate();
		}

		public override void Update(GameTime gameTime) {
			foreach ((string name, EnvironmentGUIToggleLabel label) in labelsByName) {
				bool available = name == label.Module;

				EnvironmentModule module = Mod.TryFind<EnvironmentModule>(name, out var env) ? env : null;

				available &= module?.IsAvailable() ?? false;

				Rectangle dim = InterfaceHelper.GetFullRectangle(label);

				//Man, I'd love to be able to just use the UIElement events, but that would require rewriting all of the GUI classes, so I won't bother
				// -- absoluteAquarian
				var curMouse = EnvironmentGUI.curMouse;

				if (!available)
					label.Text.TextColor = Color.Gray;
				else if (curMouse.X > dim.X && curMouse.X < dim.X + dim.Width && curMouse.Y > dim.Y - 3f && curMouse.Y < dim.Y + dim.Height) {
					label.Text.TextColor = Color.Yellow;

					if (EnvironmentGUI.MouseClicked) {
						bool valid = module is not null && module.IsAvailable();

					//	Main.NewText($"Clicked label \"{label.Text.Text}\" -- Valid? {valid}");

						if (valid && EnvironmentGUI.currentAccess is not null) {
							label.Toggle();
							EnvironmentGUI.currentAccess.SetEnabled(module, label.IsOn);

							NetHelper.ClientSendTEUpdate(EnvironmentGUI.currentAccess.Position);

						//	Main.NewText($"\"{label.Text.Text}\" label toggled to {label.IsOn}");

							SoundEngine.PlaySound(SoundID.MenuTick);
						}
					}
				} else
					label.Text.TextColor = Color.White;

				if (!available)
					Main.instance.MouseText(module.DisabledTooltip.GetTranslation(Language.ActiveCulture));
			}
		}

		public void SetLabel(EnvironmentModule module, bool value) {
			if (labelsByName.TryGetValue(module.Name, out var label))
				label.SetState(value);
		}

		public void ToggleLabel(EnvironmentModule module) {
			if (labelsByName.TryGetValue(module.Name, out var label))
				label.Toggle();
		}

		public bool GetLabel(EnvironmentModule module) => labelsByName.TryGetValue(module.Name, out var label) && label.IsOn;
	}
}
