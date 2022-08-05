using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace MagicStorage.UI {
	internal abstract class BaseOptionElement : UIElement {
		private static Asset<Texture2D> BackTexture => MagicStorage.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackground", AssetRequestMode.ImmediateLoad);
		private static Asset<Texture2D> BackTextureActive => MagicStorage.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackgroundActive", AssetRequestMode.ImmediateLoad);

		private bool hovering;

		private UIImage background, icon;

		protected abstract Asset<Texture2D> GetIcon();

		protected abstract bool IsSelected();

		public override void OnInitialize() {
			background = new(BackTexture);
			Append(background);

			icon = new(GetIcon());
			icon.Left.Set(1, 0f);
			icon.Top.Set(1, 0f);
			Append(icon);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			background.SetImage(IsSelected() ? BackTextureActive : BackTexture);
			background.Color = hovering ? Color.Silver : Color.White;

			base.DrawSelf(spriteBatch);
		}

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);

			hovering = true;
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			hovering = false;
		}
	}
}
