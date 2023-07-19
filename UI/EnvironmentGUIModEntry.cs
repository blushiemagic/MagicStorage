using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.UI {
	public sealed class EnvironmentGUIModEntry : UIElement {
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
					//--1.4.4 TESTING--//
            		//Fix this to have proper localization
				//string name = module.DisplayName.GetTranslation(Language.ActiveCulture);
				string name = "DERP 1.4.4";

				EnvironmentGUIToggleLabel label = new(this, name, module.Name, defaultState: true);
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

		public void SetLabel(EnvironmentModule module, bool value) {
			if (labelsByName.TryGetValue(module.Name, out var label))
				label.SetState(value);
		}

		public void ToggleLabel(EnvironmentModule module) {
			if (labelsByName.TryGetValue(module.Name, out var label))
				label.Toggle();
		}

		public bool GetLabel(EnvironmentModule module) => labelsByName.TryGetValue(module.Name, out var label) && label.IsOn;

		public bool IsAvailable(string name, [NotNullWhen(true)] out EnvironmentModule module) {
			module = null;
			if (!labelsByName.TryGetValue(name, out var label))
				return false;

			bool available = name == label.Module;

			module = Mod.TryFind<EnvironmentModule>(name, out var env) ? env : null;

			available &= module?.IsAvailable() ?? false;

			return available;
		}
	}
}
