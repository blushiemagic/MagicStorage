using MagicStorage.Common.Systems;
using MagicStorage.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SerousCommonLib.API;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
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
			ILHelper.CommonPatchingWrapper(il, MagicStorageMod.Instance, throwOnFail: true, PatchMethod);
		}

		private static bool PatchMethod(ILCursor c, ref string badReturnReason) {

			// Finds all return instructions and picks the last one :|
			if (!c.TryFindNext(out ILCursor[] foundRets, i => i.MatchRet())) {
				badReturnReason = "Failed to find any IL returns";
				return false;
			}

			ILCursor d = foundRets[foundRets.Length-1];
			d.GotoPrev(MoveType.After);

			d.Emit(OpCodes.Ldarg_0);
			d.EmitDelegate((Player self) => {
				//Guaranteed to run only in singleplayer/servers
				IEnumerable<TEStorageHeart> hearts = self.GetNearbyNetworkHearts();

				for (int i = 10; i < 50; i++) {
					Item item = self.inventory[i];

					if (item.type > ItemID.None && item.stack > 0 && !item.favorited && !item.IsACoin) {
						bool success = Netcode.TryQuickStackItemIntoNearbyStorageSystems(hearts, item);

						if (success && Main.netMode != NetmodeID.Server && StoragePlayer.LocalPlayer.ViewingStorage().X >= 0)
							StorageGUI.SetRefresh();
					}
				}
			});

			return true;
		}
	}
}
