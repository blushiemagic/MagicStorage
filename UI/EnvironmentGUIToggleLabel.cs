using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.UI {
	public class EnvironmentGUIToggleLabel : UIToggleImage {
		public readonly string Module;

		public readonly UIText Text;

		public readonly EnvironmentGUIModEntry Source;

		public EnvironmentGUIToggleLabel(EnvironmentGUIModEntry source, string name, string module, bool defaultState = false) : base(ModContent.Request<Texture2D>("Terraria/Images/UI/Settings_Toggle"), 13, 13, new Point(16, 0), new Point(0, 0)) {
			Source = source;
			Module = module;

			Append(Text = new UIText(name) {
				Left = { Pixels = 20 }
			});

			SetState(defaultState);
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);

			if (!Source.IsAvailable(Module, out _)) {
				//Force the state to false
				SetState(false);
				return;
			}
		}

		public override void LeftClick(UIMouseEvent evt) {
			//UIToggleImage.Click() calls Toggle(), so we don't have to
			base.LeftClick(evt);

			if (!Source.IsAvailable(Module, out var module))
				return;

			NetHelper.Report(true, $"Clicked label \"{Text.Text}\" -- Valid? {module is not null}");

			if (module is not null && EnvironmentGUI.currentAccess is not null) {
				EnvironmentGUI.currentAccess.SetEnabled(module, IsOn);

				NetHelper.ClientSendTEUpdate(EnvironmentGUI.currentAccess.Position);

				string state = IsOn ? "[c/00ff00:enabled]" : "[c/ff0000:disabled]";
				Main.NewText($"Module \"{Text.Text}\" was set to {state}.");

				SoundEngine.PlaySound(SoundID.MenuTick);
			}
		}

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);

			if (Source.IsAvailable(Module, out var module))
				Text.TextColor = Color.Yellow;
			else {
				Text.TextColor = Color.Gray;
				
				//--1.4.4 TESTING--//
				//Fix this to have proper localization
				//string text = module?.DisabledTooltip.GetTranslation(Language.ActiveCulture);
				var text = "DERP 1.4.4";

				MagicUI.mouseText = text ?? Language.GetTextValue("Mods.MagicStorage.EnvironmentGUI.EntryDisabledDefault");
			}
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			if (Source.IsAvailable(Module, out _))
				Text.TextColor = Color.White;
			else {
				Text.TextColor = Color.Gray;
				MagicUI.mouseText = "";
			}
		}
	}
}
