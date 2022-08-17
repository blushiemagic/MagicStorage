using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.UI {
	internal class RecipeHistoryButton : UIElement, IColorable {
		private static readonly Asset<Texture2D> sourceAsset = ModContent.Request<Texture2D>("MagicStorage/Assets/RecipeHistory", AssetRequestMode.ImmediateLoad);

		private Rectangle _frame;

		public Color Color { get; set; }

		public float Scale;

		public RecipeHistoryButton(float scale) {
			Scale = scale;
			_frame = sourceAsset.Frame(1, 2, 0, 0);
			Width.Set(_frame.Width * scale, 0f);
			Height.Set(_frame.Height * scale, 0f);
			Color = Color.White;
		}

		private void SetFrame(bool hovering) {
			_frame = sourceAsset.Frame(1, 2, 0, hovering ? 1 : 0);
			Width.Set(_frame.Width * Scale, 0f);
			Height.Set(_frame.Height * Scale, 0f);

			Recalculate();
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			spriteBatch.Draw(sourceAsset.Value, GetDimensions().Center(), _frame, Color, 0f, _frame.Size() / 2f, Scale, SpriteEffects.None, 0);
		}

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);

			MagicUI.mouseText = Language.GetTextValue("Mods.MagicStorage.ViewHistory");
			SetFrame(true);
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			MagicUI.mouseText = "";
			SetFrame(false);
		}
	}
}
