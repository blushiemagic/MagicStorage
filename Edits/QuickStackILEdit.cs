using MagicStorage.Common.Systems;
using MagicStorage.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Edits {
	internal class QuickStackILEdit : Edit {
		public override void LoadEdits() {
			IL.Terraria.Player.QuickStackAllChests += Player_QuickStackAllChests;
		}

		private void Player_QuickStackAllChests(ILContext il) {
			ILCursor c = new(il);

			int patchNum = 1;

			ILHelper.CompleteLog(MagicStorageMod.Instance, c, beforeEdit: true);

			int playSoundLocal = -1;
			if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdloc(out playSoundLocal),
				i => i.MatchBrfalse(out _),
				i => i.MatchLdcI4(7),
				i => i.MatchLdcI4(-1),
				i => i.MatchLdcI4(-1)))
				goto bad_il;

			patchNum++;

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
							StorageGUI.needRefresh = true;
					}
				}
			});

			ILHelper.UpdateInstructionOffsets(c);

			ILHelper.CompleteLog(MagicStorageMod.Instance, c, beforeEdit: false);

			return;
			bad_il:
			string msg = "Unable to fully patch " + il.Method.Name + "()\n" +
				"Reason: Could not find instruction sequence for patch #" + patchNum;

			if (!BuildInfo.IsDev)
				throw new Exception(msg);
			else
				Mod.Logger.Error(msg);
		}

		public override void UnloadEdits() {
			IL.Terraria.Player.QuickStackAllChests -= Player_QuickStackAllChests;
		}
	}
}
