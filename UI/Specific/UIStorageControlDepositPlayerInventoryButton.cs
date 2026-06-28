using MagicStorage.Common.Systems;
using MagicStorage.Components;
using System;
using System.Collections;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI {
	public class UIStorageControlDepositPlayerInventoryButton : UITextPanel<LocalizedText> {
		public int TellerBankID { get; }

		public UIStorageControlDepositPlayerInventoryButton(LocalizedText text, int tellerBankID, float textScale = 1, bool large = false) : base(text, textScale, large) {
			TellerBankID = tellerBankID;
		}

		public override void LeftClick(UIMouseEvent evt) {
			base.LeftClick(evt);

			if (StoragePlayer.LocalPlayer.GetStorageHeart() is not TEStorageHeart heart)
				return;

			if (!SecuritySystem.CanPlayerAccessImmediately(Main.LocalPlayer, heart.assignedNetwork)) {
				SecuritySystem.PrintStorageInaccessible();
				return;
			}

			if (Main.netMode == NetmodeID.SinglePlayer) {
				Item[] inventory = PlayerInventoryTeller.LoadBank(Main.LocalPlayer, TellerBankID);
				BitArray hasItem = PlayerInventoryTeller.PrepareHandleArray(inventory);
				bool depositedAny = false;

				// Since the inventory is referenced directly, in-place mutations of it will also affect the player inventory
				using (SecuritySystem.CreateAccessContext())
					PlayerInventoryTeller.HandleInventory(inventory, hasItem, heart, ref depositedAny);

				if (depositedAny)
					SoundEngine.PlaySound(SoundID.Grab);
			} else
				PlayerInventoryTeller.SendDepositToStorageRequest(TellerBankID, heart);
		}
	}
}
