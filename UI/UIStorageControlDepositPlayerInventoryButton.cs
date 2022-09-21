using MagicStorage.Components;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI {
	public class UIStorageControlDepositPlayerInventoryButton : UITextPanel<LocalizedText> {
		public Func<Player, Item[]> GetInventory;
		public Action<Item[]> NetReceiveInventoryResult;

		internal static Action<Item[]> PendingResultAction;

		public UIStorageControlDepositPlayerInventoryButton(LocalizedText text, float textScale = 1, bool large = false) : base(text, textScale, large) { }

		public override void Click(UIMouseEvent evt) {
			base.Click(evt);

			if (StoragePlayer.LocalPlayer.GetStorageHeart() is not TEStorageHeart heart)
				return;

			Item[] inv = GetInventory?.Invoke(Main.LocalPlayer);

			if (inv is null)
				return;  // Nothing to do

			if (Main.netMode == NetmodeID.SinglePlayer)
				TryDepositItems(inv, heart, true, out _);
			else
				NetHelper.ClientRequestDepositFromBank(inv, heart.Position, NetReceiveInventoryResult);
		}

		internal static void TryDepositItems(Item[] inv, TEStorageHeart heart, bool playSound, out bool changed) {
			changed = false;

			// Try to deposit each item manually so that any leftovers stay in the same slots
			for (int i = 0; i < inv.Length; i++) {
				Item item = inv[i];

				if (item.IsAir || item.favorited)
					continue;

				int stack = item.stack;

				heart.DepositItem(item);

				if (stack != item.stack)
					changed = true;
			}

			if (playSound && changed)
				SoundEngine.PlaySound(SoundID.Grab);
		}
	}
}
