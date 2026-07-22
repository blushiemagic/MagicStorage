using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SerousCommonLib.UI;
using System;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI.Input {
	public class NewUISearchBar : TextInputBar {
		public Func<string> GetHoverText { get; set; }

		internal bool BlockRefreshThreads { get; set; }

		public NewUISearchBar(LocalizedText hintText) : base(hintText) { }

		protected override bool PreDrawText(SpriteBatch spriteBatch, ref Color textColor, ref Color hintColor) {
			if (State.HasText && MagicUI.lastKnownSearchBarErrorReason is not null && !MagicUI.CurrentlyRefreshing)
				textColor = Color.Red;

			return true;
		}

		public override void OnActivityLost() {
			MagicUI.mouseText = "";
			base.OnActivityLost();
		}

		public override void OnInputChanged() {
			if (MagicStorageConfig.SearchBarRefreshOnKey && !BlockRefreshThreads)
				MagicUI.StartMainZoneRefreshThread(caller: "NewUISearchBar.OnInputChanged()", forceMainZoneRebuild: true);

			base.OnInputChanged();
		}

		public override void OnInputCleared() {
			if (!BlockRefreshThreads)
				MagicUI.StartMainZoneRefreshThread(caller: "NewUISearchBar.OnInputCleared()", forceMainZoneRebuild: true);

			base.OnInputCleared();
		}

		public override void OnInputFocusLost() {
			if (!BlockRefreshThreads)
				MagicUI.StartMainZoneRefreshThread(caller: "NewUISearchBar.OnInputFocusLost()", forceMainZoneRebuild: true);

			base.OnInputFocusLost();
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);
			MagicUI.mouseText = "";
		}

		protected override void RestrictedUpdate(GameTime gameTime) {
			if (State.IsActive) {
				// Update the hover text if any is present
				if (IsMouseHovering && GetHoverText?.Invoke() is string hoverText) {
					if (MagicUI.lastKnownSearchBarErrorReason is string errorText && !MagicUI.CurrentlyRefreshing)
						Utility.AddErrorTextMultiline(ref hoverText, errorText);

					if (!string.IsNullOrWhiteSpace(hoverText))
						MagicUI.mouseText = hoverText;
					else
						MagicUI.mouseText = "";
				}
			}
		}
	}
}
