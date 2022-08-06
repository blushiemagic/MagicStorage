using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace MagicStorage.UI {
	internal abstract class BaseOptionElement : UIElement {
		private static Asset<Texture2D> BackTexture => MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackground", AssetRequestMode.ImmediateLoad);
		private static Asset<Texture2D> BackTextureActive => MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackgroundActive", AssetRequestMode.ImmediateLoad);

		private bool hovering;

		private UIImage background, icon;

		public BaseOptionElement() {
			Width.Set(32, 0);
			Height.Set(32, 0);
		}

		protected abstract Asset<Texture2D> GetIcon();

		protected abstract bool IsSelected();

		protected abstract string GetHoverText();

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
			MagicUI.mouseText = GetHoverText();
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			hovering = false;
			MagicUI.mouseText = "";
		}
	}
}
