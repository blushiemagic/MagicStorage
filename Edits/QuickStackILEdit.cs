using MagicStorage.Common.Systems;
using MagicStorage.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SerousCommonLib.API;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using ILPlayer = Terraria.IL_Player;

namespace MagicStorage.Edits {
	internal class QuickStackILEdit : Edit {
		public override void LoadEdits() {
			ILPlayer.QuickStackAllChests += Player_QuickStackAllChests;
		}

		public override void UnloadEdits() {
			ILPlayer.QuickStackAllChests -= Player_QuickStackAllChests;
		}

		private static void Player_QuickStackAllChests(ILContext il) {
			ILHelper.CommonPatchingWrapper(il, MagicStorageMod.Instance, throwOnFail: false, PatchMethod);
		}

		private static readonly MethodInfo Player_useVoidBag = typeof(Player).GetMethod(nameof(Player.useVoidBag), BindingFlags.Public | BindingFlags.Instance);

		private static bool PatchMethod(ILCursor c, ref string badReturnReason) {
			// First, find the locations where "Player.useVoidBag()" returning false causes an early return
			// These locations need to run the storage quick stacking logic
			// Also, two "ret" instructions later is where the proper return after checking the void bag is handled
			// The storage quick stacking logic should be run there as well

			bool foundAny = false;
			int searchCount = 0;
			while (c.TryGotoNext(MoveType.After, i => i.MatchLdarg(0),
				i => i.MatchCall(Player_useVoidBag),
				i => i.MatchBrtrue(out _))) {
				foundAny = true;

				// Emit the logic
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate(TryStorageQuickStack);

				// Go to the second "ret" after the one for this if block, then emit the logic again
				int retFound = 0;
				while (c.TryGotoNext(MoveType.After, i => i.MatchRet()) && retFound < 1)
					retFound++;

				if (retFound != 1) {
					badReturnReason = $"Mismatch for ret instructions detected.  Expected 1 match, found {retFound} instead";
					return false;
				}

				// Move behind the second ret
				c.Index--;

				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate(TryStorageQuickStack);

				searchCount++;
			}

			if (!foundAny) {
				badReturnReason = "Could not find any calls to Player.useVoidBag()";
				return false;
			}

			if (searchCount != 2) {
				badReturnReason = "Could not find all target locations for edits";
				return false;
			}

			return true;
		}

		private static void TryStorageQuickStack(Player player) {
			// Guaranteed to run only in singleplayer or on a multiplayer client due to QuickStackAllChests only being invoked from the inventory UI button
			IEnumerable<TEStorageHeart> hearts = player.GetNearbyNetworkHearts();

			for (int i = 10; i < 50; i++) {
				if (TryItemTransfer(player, player.inventory[i], hearts) && Main.netMode == NetmodeID.MultiplayerClient) {
					NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, PlayerItemSlotID.Inventory0 + i, player.inventory[i].prefix);
					NetMessage.SendData(MessageID.QuickStackChests, -1, -1, null, PlayerItemSlotID.Inventory0 + i);
					player.inventoryChestStack[i] = true;
				}
			}

			if (player.useVoidBag()) {
				for (int i = 0; i < 40; i++) {
					if (TryItemTransfer(player, player.bank4.item[i], hearts) && Main.netMode == NetmodeID.MultiplayerClient) {
						NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, PlayerItemSlotID.Bank4_0 + i, player.bank4.item[i].prefix);
						NetMessage.SendData(MessageID.QuickStackChests, -1, -1, null, PlayerItemSlotID.Bank4_0 + i);
						player.disableVoidBag = i;
					}
				}
			}
		}

		private static bool TryItemTransfer(Player player, Item item, IEnumerable<TEStorageHeart> hearts) {
			if (!item.IsAir && !item.favorited && !item.IsACoin) {
				// NOTE: in 1.4.4, sounds aren't played from quick stacking due to the new particle system being used instead
				bool playSound = false;
				int type = item.type;
				bool success = Netcode.TryQuickStackItemIntoNearbyStorageSystems(hearts, item, ref playSound);

				if (success && Main.netMode != NetmodeID.Server && player.GetModPlayer<StoragePlayer>().ViewingStorage().X >= 0) {
					StorageGUI.SetRefresh();
					StorageGUI.SetNextItemTypeToRefresh(type);
					CraftingGUI.SetNextDefaultRecipeCollectionToRefresh(type);
				}

				return success;
			}

			return false;
		}
	}
}
