using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SerousCommonLib.API.Input;
using SerousCommonLib.UI;
using Terraria.Localization;

namespace MagicStorage.UI.Input {
	public abstract class SecurityNetworkTextInputBar : TextInputBar {
		public SecurityNetworkTextInputBar(string hintTextKey) : base(Language.GetText(hintTextKey)) { }
	}

	public sealed class SecurityNetworkNameTextInputBar : SecurityNetworkTextInputBar {
		public SecurityNetworkNameTextInputBar() : base("Mods.MagicStorage.Security.UI.NameHint") { }
	}

	public sealed class SecurityNetworkPasswordTextInputBar : SecurityNetworkTextInputBar {
		public Color? BackgroundColorOverride { get; set; }

		public Color? TextColorOverride { get; set; }

		public SecurityNetworkPasswordTextInputBar() : base("Mods.MagicStorage.Security.UI.PasswordHint") {
			// Passwords are not allowed to have spaces
			Controller = new SimpleTextInputController(' ');
		}

		protected override bool PreDrawBackBar(SpriteBatch spriteBatch, ref Color color) {
			if (BackgroundColorOverride is { } colorOverride)
				color = colorOverride;

			return base.PreDrawBackBar(spriteBatch, ref color);
		}

		protected override bool PreDrawText(SpriteBatch spriteBatch, ref Color textColor, ref Color hintColor) {
			if (TextColorOverride is { } colorOverride) {
				textColor = colorOverride;
				hintColor = colorOverride * 0.75f;
			}

			return base.PreDrawText(spriteBatch, ref textColor, ref hintColor);
		}
	}

	public sealed class SecurityNetworkSearchNameTextInputBar : SecurityNetworkTextInputBar {
		public SecurityNetworkSearchNameTextInputBar() : base("Mods.MagicStorage.Security.UI.SearchHint") { }

		public override void OnActivityGained() {
			State.Set(string.Empty);
			base.OnActivityGained();
		}
	}

	public sealed class SecurityNetworkAccessPasswordTextInputBar : SecurityNetworkTextInputBar {
		public SecurityNetworkAccessPasswordTextInputBar() : base("Mods.MagicStorage.Security.UI.AccessPasswordHint") {
			State.ForcedFocus = true;
			State.LoseFocusOnEnter = false;

			// Passwords are not allowed to have spaces
			Controller = new SimpleTextInputController(' ');
		}

		public override void OnActivityGained() {
			State.Set(string.Empty);
			base.OnActivityGained();
		}
	}
}
