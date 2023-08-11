using MagicStorage.Common.Systems;
using MagicStorage.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SerousCommonLib.API;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using ILPlayer = Terraria.IL_Player;
using ILChest = Terraria.IL_Chest;
using System;
using Terraria.ModLoader;

namespace MagicStorage.Edits {
	internal class QuickStackILEdit : Edit {
		public override void LoadEdits() {
			ILPlayer.QuickStackAllChests += Player_QuickStackAllChests;
			ILChest.ServerPlaceItem += Chest_ServerPlaceItem;
		}

		public override void UnloadEdits() {
			ILPlayer.QuickStackAllChests -= Player_QuickStackAllChests;
			ILChest.ServerPlaceItem -= Chest_ServerPlaceItem;
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
				// NOTE: searchCount = 0 should refer to the netcode portion of the logic... avoid it and hook into the Chest quick stacking netcode instead further down in the file
				foundAny = true;

				// Emit the logic
				if (searchCount > 0) {
					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate(TryStorageQuickStack);
				}

				// Go to the second "ret" after the one for this if block, then emit the logic again
				int retFound = 0;
				while (c.TryGotoNext(MoveType.After, i => i.MatchRet()) && retFound < 1)
					retFound++;

				if (retFound != 1) {
					badReturnReason = $"Mismatch for ret instructions detected.  Expected 1 match, found {retFound} instead";
					return false;
				}

				// Move behind the second ret
				if (searchCount > 0) {
					c.Index--;

					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate(TryStorageQuickStack);
				}

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
			// Guaranteed to run only in singleplayer due to the multiplayer logic being skipped
			IEnumerable<TEStorageCenter> centers = player.GetNearbyCenters();

			// No centers nearby?  Bail immediately since nothing would happen anyway
			if (!centers.Any())
				return;

			for (int i = 10; i < 50; i++) {
				Item item = player.inventory[i];

				if (!item.IsAir && !item.favorited && !item.IsACoin) {
					TryItemTransfer(player, item, centers);
					player.inventory[i] = item;
				}
			}

			if (player.useVoidBag()) {
				for (int i = 0; i < 40; i++) {
					Item item = player.bank4.item[i];

					if (!item.IsAir && !item.favorited && !item.IsACoin) {
						TryItemTransfer(player, item, centers);
						player.bank4.item[i] = item;
					}
				}
			}
		}

		private static readonly MethodInfo Chest_PutItemInNearbyChest = typeof(Chest).GetMethod(nameof(Chest.PutItemInNearbyChest), BindingFlags.Public | BindingFlags.Static);

		private static void Chest_ServerPlaceItem(ILContext il) {
			ILHelper.CommonPatchingWrapper(il, MagicStorageMod.Instance, throwOnFail: false, Patch_Chest_ServerPlaceItem);
		}

		private static bool Patch_Chest_ServerPlaceItem(ILCursor c, ref string badReturnReason) {
			bool foundAny = false;
			while (c.TryGotoNext(MoveType.After, i => i.MatchCall(Chest_PutItemInNearbyChest))) {
				// Inject an item transfer call
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate(CheckStorageQuickStacking);
			}

			if (!foundAny) {
				badReturnReason = "Could not find any references to Chest.PutItemInNearbyChest()";
				return false;
			}

			return true;
		}

		private static Item CheckStorageQuickStacking(Item item, byte plr) {
			Player player = Main.player[plr];
			TryItemTransfer(player, item, player.GetNearbyCenters());
			return item;
		}

		private static bool TryItemTransfer(Player player, Item item, IEnumerable<TEStorageCenter> centers) {
			// NOTE: in 1.4.4, sounds aren't played from quick stacking due to the new particle system being used instead
			bool playSound = false;
			int type = item.type;
			bool success = Netcode.TryQuickStackItemIntoNearbyStorageSystems(player.Center, centers, item, ref playSound);

			if (success) {
				if (Main.netMode == NetmodeID.SinglePlayer) {
					// Refresh the UI since the item was quick stacked
					MagicUI.SetNextCollectionsToRefresh(type);
				} else if (Main.netMode == NetmodeID.Server) {
					// Inform the client of the quick stack result so that their UI can be refreshed
					ModPacket packet = MagicStorageMod.Instance.GetPacket();
					packet.Write((byte)MessageType.ServerQuickStackToStorageResult);
					packet.Write(playSound);
					packet.Write(type);
					packet.Send(toClient: player.whoAmI);
				}
			}

			return success;
		}
	}
}
