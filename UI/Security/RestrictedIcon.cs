using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace MagicStorage.UI.Security {
	public class RestrictedIcon : UIElement {
		private static Asset<Texture2D> _lockedSheet;
		private static Asset<Texture2D> _unlockedSheet;

		public bool Locked { get; set; }

		public RestrictedIcon(bool locked) {
			Locked = locked;

			_lockedSheet ??= TextureAssets.HbLock[0];
			_unlockedSheet ??= TextureAssets.HbLock[1];

			var size = GetSize();
			Width.Set(size.X, 0f);
			Height.Set(size.Y, 0f);
			MinWidth.Set(size.X, 0f);
			MinHeight.Set(size.Y, 0f);
		}

		public Vector2 GetSize() {
			var asset = Locked ? _lockedSheet : _unlockedSheet;
			// Ensure the asset is loaded before returning the size
			asset.Wait?.Invoke();
			Texture2D sheet = asset.Value;
			return sheet.Frame(2, 1, 0, 0).Size();
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			base.DrawSelf(spriteBatch);

			Texture2D sheet = (Locked ? _lockedSheet : _unlockedSheet).Value;
			CalculatedStyle dimensions = GetDimensions();

			Rectangle icon = sheet.Frame(2, 1, 0, 0);
			spriteBatch.Draw(sheet, dimensions.Position(), icon, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

			Rectangle outline = sheet.Frame(2, 1, 1, 0);
			spriteBatch.Draw(sheet, dimensions.Position(), outline, Color.Yellow, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
		}
	}
}
