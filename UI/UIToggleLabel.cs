using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.UI {
	public class UIToggleLabel : UIToggleImage {
		public readonly UIText Text;

		public UIToggleLabel(string name, bool defaultState = false) : base(ModContent.Request<Texture2D>("Terraria/Images/UI/Settings_Toggle"), 13, 13, new Point(16, 0), new Point(0, 0)) {
			Append(Text = new UIText(name) {
				Left = { Pixels = 20 }
			});

			SetState(defaultState);
		}

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);

			Text.TextColor = Color.Yellow;
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			Text.TextColor = Color.White;
		}
	}
}
