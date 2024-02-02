using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI.Input {
	public class NewUISearchBar : TextInputBar {
		public Func<string> GetHoverText { get; set; }

		public NewUISearchBar(LocalizedText hintText) : base(hintText) { }

		protected override bool PreDrawText(SpriteBatch spriteBatch, ref Color color) {
			if (MagicUI.lastKnownSearchBarErrorReason is not null && !MagicUI.CurrentlyRefreshing)
				color = Color.Red;

			return true;
		}

		public override void OnActivityLost() {
			MagicUI.mouseText = "";
		}

		public override void OnInputChanged() {
			if (MagicStorageConfig.SearchBarRefreshOnKey)
				MagicUI.SetRefresh(forceFullRefresh: true);
		}

		public override void OnInputCleared() {
			MagicUI.SetRefresh(forceFullRefresh: true);
		}

		public override void OnInputFocusLost() {
			if (!MagicStorageConfig.SearchBarRefreshOnKey)
				MagicUI.SetRefresh(forceFullRefresh: true);
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);
			MagicUI.mouseText = "";
		}

		protected override void RestrictedUpdate(GameTime gameTime) {
			if (State.IsActive) {
				// Update the hover text if any is present
				if (IsMouseHovering && GetHoverText?.Invoke() is string hoverText) {
					if (MagicUI.lastKnownSearchBarErrorReason is not null && !MagicUI.CurrentlyRefreshing)
						hoverText += $"\n[c/ff0000:{MagicUI.lastKnownSearchBarErrorReason}]";

					if (!string.IsNullOrWhiteSpace(hoverText))
						MagicUI.mouseText = hoverText;
					else
						MagicUI.mouseText = "";
				}
			} else if (State.WasActive)
				MagicUI.mouseText = "";
		}
	}
}
