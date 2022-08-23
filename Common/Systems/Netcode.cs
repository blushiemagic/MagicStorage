using MagicStorage.Components;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems {
	internal class Netcode : ModSystem {
		public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber) {
			if (messageType == MessageID.QuickStackChests) {
				//Do the logic for chests, then do the logic for storage systems when applicable
				byte b7 = reader.ReadByte();

				if (Main.netMode == NetmodeID.Server && playerNumber < Main.maxPlayers && b7 < 58) {
					Item item = Main.player[playerNumber].inventory[b7];
					
					int oldType = item.type, oldStack = item.stack;

					Chest.ServerPlaceItem(playerNumber, b7);

					bool skipSound = false;

					if (oldType != item.type || oldStack != item.stack)
						skipSound = true;

					bool playSound = false;
					TryPlaceItemInNearbyStorageSystems(Main.player[playerNumber], item, skipSound, ref playSound);
				}

				return true;
			}

			return base.HijackGetData(ref messageType, ref reader, playerNumber);
		}

		internal static void TryPlaceItemInNearbyStorageSystems(Player self, Item item, bool skipSound, ref bool playSound)
			=> TryPlaceItemInNearbyStorageSystems(self.GetNearbyNetworkHearts(), item, skipSound, ref playSound);

		internal static void TryPlaceItemInNearbyStorageSystems(IEnumerable<TEStorageHeart> hearts, Item item, bool skipSound, ref bool playSound) {
			if (item.IsAir)
				return;

			//Quick stack to nearby chests failed or was only partially completed.  Try to do the same for nearby storage systems
			foreach (TEStorageHeart heart in hearts) {
				int oldType = item.type;
				int oldStack = item.stack;

				heart.DepositItem(item);

				if (oldType != item.type || oldStack != item.stack)
					playSound = true;

				if (item.IsAir)
					break;
			}

			if (!skipSound && playSound)
				SoundEngine.PlaySound(SoundID.Grab);
		}
	}
}
