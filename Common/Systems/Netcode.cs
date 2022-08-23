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
					Player player = Main.player[playerNumber];

					ref Item item = ref player.inventory[b7];

					//Basically manually doing Chest.ServerPlaceItem, but inserting TryPlaceItemInNearbyStorageSystems before the SendData call
					item = Chest.PutItemInNearbyChest(item, player.Center);

					bool playSound = false;
					bool success = TryPlaceItemInNearbyStorageSystems(player, item, ref playSound);

					NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, playerNumber, b7, item.prefix);

					if (success)
						NetHelper.SendQuickStackToStorage(playerNumber);
				}

				return true;
			}

			return base.HijackGetData(ref messageType, ref reader, playerNumber);
		}

		internal static bool TryPlaceItemInNearbyStorageSystems(Player self, Item item, ref bool playSound)
			=> TryPlaceItemInNearbyStorageSystems(self.GetNearbyNetworkHearts(), item, ref playSound);

		internal static bool TryPlaceItemInNearbyStorageSystems(IEnumerable<TEStorageHeart> hearts, Item item, ref bool playSound) {
			if (item.IsAir)
				return false;

			//Quick stack to nearby chests failed or was only partially completed.  Try to do the same for nearby storage systems
			foreach (TEStorageHeart heart in hearts) {
				int oldType = item.type;
				int oldStack = item.stack;

				heart.DepositItem(item);

				if (oldType != item.type || oldStack != item.stack)
					playSound = true;

				if (item.IsAir)
					return true;
			}

			return false;
		}
	}
}
