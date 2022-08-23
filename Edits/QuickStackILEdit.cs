using MagicStorage.Common.Systems;
using MagicStorage.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Edits {
	internal class QuickStackILEdit : ILoadable {
		public Mod Mod { get; private set; } = null!;

		public void Load(Mod mod) {
			Mod = mod;

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

					if (item.type > ItemID.None && item.stack > 0 && !item.favorited && !item.IsACoin)
						Netcode.TryPlaceItemInNearbyStorageSystems(hearts, item, true, ref flag);
				}
			});

			ILHelper.UpdateInstructionOffsets(c);

			ILHelper.CompleteLog(MagicStorageMod.Instance, c, beforeEdit: false);

			return;
			bad_il:
			throw new Exception("Unable to fully patch " + il.Method.Name + "()\n" +
				"Reason: Could not find instruction sequence for patch #" + patchNum);
		}

		public void Unload() {
			IL.Terraria.Player.QuickStackAllChests -= Player_QuickStackAllChests;

			Mod = null!;
		}
	}
}
