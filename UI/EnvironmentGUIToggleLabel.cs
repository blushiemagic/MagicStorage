using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace MagicStorage.UI {
	public class EnvironmentGUIToggleLabel : UIToggleImage {
		public readonly string Module;

		public readonly UIText Text;

		public EnvironmentGUIToggleLabel(string name, string module, bool defaultState = false) : base(ModContent.Request<Texture2D>("Terraria/Images/UI/Settings_Toggle"), 13, 13, new Point(16, 0), new Point(0, 0)) {
			Module = module;

			Append(Text = new UIText(name) {
				Left = { Pixels = 20 }
			});

			SetState(defaultState);
		}
	}
}
