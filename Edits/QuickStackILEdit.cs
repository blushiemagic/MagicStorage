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
			// TODO: edit no longer loads, might need to edit different method(s)
		//	ILPlayer.QuickStackAllChests += Player_QuickStackAllChests;
		}

		public override void UnloadEdits() {
			ILPlayer.QuickStackAllChests -= Player_QuickStackAllChests;
		}

		private static void Player_QuickStackAllChests(ILContext il) {
			ILHelper.CommonPatchingWrapper(il, MagicStorageMod.Instance, throwOnFail: false, PatchMethod);
		}

		private static bool PatchMethod(ILCursor c, ref string badReturnReason) {
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
			c.EmitDelegate((Player self, ref bool flag) => {
				//Guaranteed to run only in singleplayer/servers
				IEnumerable<TEStorageHeart> hearts = self.GetNearbyNetworkHearts();

				for (int i = 10; i < 50; i++) {
					Item item = self.inventory[i];

					if (item.type > ItemID.None && item.stack > 0 && !item.favorited && !item.IsACoin) {
						bool success = Netcode.TryQuickStackItemIntoNearbyStorageSystems(hearts, item, ref flag);

						if (success && Main.netMode != NetmodeID.Server && StoragePlayer.LocalPlayer.ViewingStorage().X >= 0)
							StorageGUI.SetRefresh();
					}
				}
			});

			return true;
		}
	}
}
