using MagicStorage.Common.Systems;
using MagicStorage.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SerousCommonLib.API;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using ILPlayer = IL.Terraria.Player;

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

		private static readonly FieldInfo Player_inventoryChestStack = typeof(Player).GetField(nameof(Player.inventoryChestStack), BindingFlags.Public | BindingFlags.Instance);

		private static bool PatchMethod(ILCursor c, ref string badReturnReason) {
			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdfld(Player_inventoryChestStack))) { 
				badReturnReason = "Could not find reference to Player.inventoryChestStack";
				return false;
			}
			if (!c.TryGotoNext(MoveType.Before, i => i.MatchRet())) {
				badReturnReason = "Could not find return statement for multiplayer client clause";
				return false;
			}

			// Load a dummy value
			var dummyLocal = c.Context.MakeLocalVariable<bool>();
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloca, dummyLocal);
			c.EmitDelegate(TryStorageQuickStack);

			int playSoundLocal = -1;
			if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdloc(out playSoundLocal),
				i => i.MatchBrfalse(out _),
				i => i.MatchLdcI4(7),
				i => i.MatchLdcI4(-1),
				i => i.MatchLdcI4(-1))) {
				badReturnReason = "Could not find instruction sequence for playSound local variable";
				return false;
			}

			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloca, playSoundLocal);
			c.EmitDelegate(TryStorageQuickStack);

			return true;
		}

		private static void TryStorageQuickStack(Player player, ref bool flag) {
			// Guaranteed to run only in singleplayer or on a multiplayer client due to QuickStackAllChests only being invoked from the inventory UI button
			IEnumerable<TEStorageCenter> centers = player.GetNearbyCenters();

			for (int i = 10; i < 50; i++) {
				Item item = player.inventory[i];

				if (item.type > ItemID.None && item.stack > 0 && !item.favorited && !item.IsACoin) {
					if (Main.netMode == NetmodeID.MultiplayerClient) {
						// Important: inventory slot needs to be synced first when sending the request
						NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, PlayerItemSlotID.Inventory0 + i, item.prefix);
						NetHelper.RequestQuickStackToNearbyStorage(player.Center, PlayerItemSlotID.Inventory0 + i, centers);
						player.inventoryChestStack[i] = true;
					} else {
						TryItemTransfer(player, item, centers, ref flag);
						player.inventory[i] = item;
					}
				}
			}
		}

		private static void TryItemTransfer(Player player, Item item, IEnumerable<TEStorageCenter> centers, ref bool playSound) {
			int type = item.type;
			bool success = Netcode.TryQuickStackItemIntoNearbyStorageSystems(centers, item, ref playSound);

			if (success && player.GetModPlayer<StoragePlayer>().ViewingStorage().X >= 0)
				MagicUI.SetNextCollectionsToRefresh(type);
		}
	}
}
