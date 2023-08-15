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
using ILChest = IL.Terraria.Chest;
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

		private static readonly FieldInfo Player_inventoryChestStack = typeof(Player).GetField(nameof(Player.inventoryChestStack), BindingFlags.Public | BindingFlags.Instance);

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
			c.EmitDelegate(TryStorageQuickStack);

			return true;
		}

		private static void TryStorageQuickStack(Player player, ref bool flag) {
			// Guaranteed to run only in singleplayer due to the multiplayer logic being ignored
			IEnumerable<TEStorageCenter> centers = player.GetNearbyCenters();

			for (int i = 10; i < 50; i++) {
				Item item = player.inventory[i];

				if (item.type > ItemID.None && item.stack > 0 && !item.favorited && !item.IsACoin) {
					TryItemTransfer(player, item, centers, ref flag);
					player.inventory[i] = item;
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
			bool playSound = false;
			TryItemTransfer(player, item, player.GetNearbyCenters(), ref playSound);
			return item;
		}

		private static bool TryItemTransfer(Player player, Item item, IEnumerable<TEStorageCenter> centers, ref bool playSound) {
			int type = item.type;
			bool success = Netcode.TryQuickStackItemIntoNearbyStorageSystems(centers, item, ref playSound);

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
