using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SerousCommonLib.UI;
using SerousCommonLib.UI.Layouts;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace MagicStorage.UI.Security {
	public class NetworkConfigurationIcon : UIPanel {
		private static Asset<Texture2D> _icon;

		public NetworkConfigurationIcon() {
			_icon ??= ModContent.Request<Texture2D>("MagicStorage/Assets/ConfigNetwork");

			SetPadding(4);

			/*
			this.GetLayoutManager().Attributes = new LayoutAttributes()
				.WithSize(
					width: new LayoutUnit(pixels: 24f),
					height: new LayoutUnit(pixels: 24f)
				);
			*/
			Width.Set(32, 0f);
			Height.Set(32, 0f);
			MinWidth.Set(32, 0f);
			MinHeight.Set(32, 0f);
			
			_icon.Wait?.Invoke();  // UIImage ctor reads the size of the texture, so we need to wait for it to be loaded

			UIImage icon = new UIImage(_icon);
			/*
			icon.GetLayoutManager().Attributes = new LayoutAttributes()
				.InheritSizeFrom(icon)
				.WithGravity(LayoutGravityType.CenterBoth);
			*/
			icon.HAlign = 0.5f;
			icon.VAlign = 0.5f;

			Append(icon);
		}
	}
}
