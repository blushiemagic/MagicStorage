using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace MagicStorage.UI.Security {
	public class PasswordHiddenIndicator : UIImage {
		public bool Hidden { get; private set; }

		public event Action<PasswordHiddenIndicator> OnVisibilityChanged;

		public PasswordHiddenIndicator() : base(GetAsset(hidden: true)) {
			Hidden = true;
		}

		public PasswordHiddenIndicator(bool hidden) : base(GetAsset(hidden)) {
			Hidden = hidden;
		}

		private static Asset<Texture2D> GetAsset(bool hidden) {
			var asset = hidden ? TextureAssets.InventoryTickOff : TextureAssets.InventoryTickOn;
			// Ensure that the asset has loaded
			asset.Wait?.Invoke();
			return asset;
		}

		public void SetHidden(bool hidden) {
			SetImage(GetAsset(hidden));
			Hidden = hidden;

			// Notify subscribers about the visibility change
			OnVisibilityChanged?.Invoke(this);
		}

		public override void LeftClick(UIMouseEvent evt) {
			SetHidden(!Hidden);

			base.LeftClick(evt);
		}
	}
}
