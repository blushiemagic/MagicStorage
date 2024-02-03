using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace MagicStorage.UI {
	public abstract class BaseOptionElement : UIElement {
		private static Asset<Texture2D> BackTexture => MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackground", AssetRequestMode.ImmediateLoad);
		private static Asset<Texture2D> BackTextureActive => MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackgroundActive", AssetRequestMode.ImmediateLoad);

		private static Asset<Texture2D> GeneralBackTextureActive => MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackgroundGeneralActive", AssetRequestMode.ImmediateLoad);

		private UIImage background, icon;

		public BaseOptionElement() {
			Width.Set(32, 0);
			Height.Set(32, 0);
		}

		protected abstract Asset<Texture2D> GetIcon();

		protected abstract bool IsSelected();

		protected abstract bool IsGeneralOption();

		protected abstract string GetHoverText();

		public override void OnInitialize() {
			background = new(BackTexture) {
				ScaleToFit = true
			};
			Append(background);

			icon = new(GetIcon()) {
				ScaleToFit = true,
				HAlign = 0.5f,
				VAlign = 0.5f
			};
			Append(icon);
		}

		public void SetSize(float size) {
			Width.Set(size, 0f);
			Height.Set(size, 0f);

			Recalculate();
		}

		protected override void DrawChildren(SpriteBatch spriteBatch) {
			background.SetImage(IsSelected() ? (IsGeneralOption() ? GeneralBackTextureActive : BackTextureActive) : BackTexture);
			background.Color = IsMouseHovering ? Color.Silver : Color.White;
			background.Recalculate();

			icon.SetImage(GetIcon());
			icon.Recalculate();

			base.DrawChildren(spriteBatch);
		}

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);

			MagicUI.mouseText = GetHoverText();
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			MagicUI.mouseText = "";
		}
	}
}
