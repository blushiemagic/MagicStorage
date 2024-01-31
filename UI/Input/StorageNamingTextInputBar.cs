using MagicStorage.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;

namespace MagicStorage.UI.Input {
	public class StorageNamingTextInputBar : TextInputBar {
		public StorageNamingTextInputBar(LocalizedText hintText) : base(hintText) { }

		protected override bool PreDrawText(SpriteBatch spriteBatch, ref Color color) {
			if (State.HasText && State.HasChanges)
				color = Color.Goldenrod;

			return true;
		}

		public override void OnActivityGained() {
			if (GetStorageHeart(out var heart))
				State.Set(heart.storageName);
		}

		public override void OnActivityLost() => SetNameToCurrentText();

		public override void OnInputCleared() {
			if (GetStorageHeart(out var heart)) {
				bool changed = heart.storageName != string.Empty;
				heart.storageName = string.Empty;

				if (changed) {
					NetHelper.SendStorageHeartName(heart);

					Main.NewText(Language.GetTextValue("Mods.MagicStorage.StorageGUI.NameWasCleared"));
				}
			}
		}

		public override void OnInputFocusLost() => SetNameToCurrentText();

		private void SetNameToCurrentText() {
			if (State.HasChanges && GetStorageHeart(out var heart)) {
				heart.storageName = State.InputText;
				NetHelper.SendStorageHeartName(heart);

				Main.NewText(Language.GetTextValue("Mods.MagicStorage.StorageGUI.NameWasSetTo", heart.storageName));
			}
		}	

		private static bool GetStorageHeart(out TEStorageHeart heart) {
			Point16 storage = StoragePlayer.LocalPlayer.ViewingStorage();

			if (storage == Point16.NegativeOne) {
				heart = null;
				return false;
			}

			if (!TileEntity.ByPosition.TryGetValue(storage, out TileEntity te) || te is not TEStorageComponent component) {
				heart = null;
				return false;
			}

			return (heart = component.GetHeart()) is not null;
		}
	}
}
