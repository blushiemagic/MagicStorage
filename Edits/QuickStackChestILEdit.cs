using MagicStorage.Common.Systems;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SerousCommonLib.API;
using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using ILChest = Terraria.IL_Chest;

namespace MagicStorage.Edits {
	internal class QuickStackChestILEdit : Edit {
		public override void LoadEdits() {
			// NOTE: logic was changed from a detour to an edit to make this easily work on 1.4.3 and 1.4.4
			ILChest.ServerPlaceItem += Patch_Chest_ServerPlaceItem;
		}

		public override void UnloadEdits() {
			ILChest.ServerPlaceItem -= Patch_Chest_ServerPlaceItem;
		}

		private static void Patch_Chest_ServerPlaceItem(ILContext il) {
			ILHelper.CommonPatchingWrapper(il, MagicStorageMod.Instance, throwOnFail: false, Chest_ServerPlaceItem);
		}

		private static readonly MethodInfo Chest_PutItemInNearbyChest = typeof(Chest).GetMethod(nameof(Chest.PutItemInNearbyChest), BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo NetMessage_SendData = typeof(NetMessage).GetMethod(nameof(NetMessage.SendData), BindingFlags.Public | BindingFlags.Static);

		private delegate bool TryStorageItemTransferDelegate(int plr, int slot, out int type);

		private static bool Chest_ServerPlaceItem(ILCursor c, ref string badReturnReason) {
			var successLocal = c.Context.MakeLocalVariable<bool>();
			var typeLocal = c.Context.MakeLocalVariable<int>();

			bool foundAny = false;

			while (c.TryGotoNext(MoveType.After, i => i.MatchCall(Chest_PutItemInNearbyChest))) {
				foundAny = true;

				c.Emit(OpCodes.Ldarg_0);
				c.Emit(OpCodes.Ldarg_1);
				c.Emit(OpCodes.Ldloca, typeLocal);
				c.EmitDelegate<TryStorageItemTransferDelegate>(static (int plr, int slot, out int type) => {
					Player player = Main.player[plr];

					Item item;
					if (slot >= PlayerItemSlotID.Bank4_0 && slot < PlayerItemSlotID.Bank4_0 + 40)
						item = player.bank4.item[slot - PlayerItemSlotID.Bank4_0];
					else
						item = player.inventory[slot];

					// NOTE: in 1.4.4, sounds aren't played from quick stacking due to the new particle system being used instead
					bool playSound = false;
					type = item.type;
					return Netcode.TryQuickStackItemIntoNearbyStorageSystems(player, item, ref playSound);
				});
				c.Emit(OpCodes.Stloc, successLocal);

				if (!c.TryGotoNext(MoveType.After, i => i.MatchCall(NetMessage_SendData))) {
					badReturnReason = "Could not find NetMessage.SendData() call after vanilla Chest.PutItemInNearbyChest() call";
					return false;
				}

				c.EmitIfBlock(out _,
					condition: cursor => cursor.Emit(OpCodes.Ldloc, successLocal),
					action: cursor => {
						cursor.Emit(OpCodes.Ldarg_0);
						cursor.Emit(OpCodes.Ldloc, typeLocal);
						cursor.EmitDelegate(NetHelper.SendQuickStackToStorage);
					});
			}

			if (!foundAny) {
				badReturnReason = "Could not find any calls to Chest.PutItemInNearbyChest()";
				return false;
			}

			return true;
		}
	}
}
