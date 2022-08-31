using MagicStorage.Components;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems {
	internal class Netcode : ModSystem {
		private static string opKey;
		public static string ServerOperatorKey {
			get {
				if (opKey is null && Main.netMode == NetmodeID.Server)
					opKey = GenerateKey();
				return opKey;
			}
		}

		internal static bool KeyIsGenerated => opKey != null;

		internal static bool RequestingOperatorKey { get; set; }

		private static readonly char[] randomCharacters = Enumerable.Range('0', 10).Concat(Enumerable.Range('A', 26)).Concat(Enumerable.Range('a', 26)).Select(i => (char)i).ToArray();

		private const int KeyLength = 12;

		private static string GenerateKey() {
			StringBuilder sb = new(KeyLength);

			for (int i = 0; i < KeyLength; i++)
				sb.Append(Main.rand.Next(randomCharacters));

			return sb.ToString();
		}

		public static bool IsKeyValidForConfirmationMessage(string key) {
			if (key.Length != KeyLength)
				return false;

			if (key.Any(c => Array.IndexOf(randomCharacters, c) < 0))
				return false;

			return true;
		}

		internal static void ClientPrintKeyReponse(bool valid) {
			if (!valid)
				Main.NewText("Inputted key did not match the key stored on the server.", Color.Red);
			else
				Main.NewText("Server Operator status was successfully modified.", Color.Green);
		}

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
					bool success = TryQuickStackItemIntoNearbyStorageSystems(player, item, ref playSound);

					NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, playerNumber, b7, item.prefix);

					if (success)
						NetHelper.SendQuickStackToStorage(playerNumber);
				}

				return true;
			}

			return base.HijackGetData(ref messageType, ref reader, playerNumber);
		}

		internal static bool TryQuickStackItemIntoNearbyStorageSystems(Player self, Item item, ref bool playSound)
			=> TryQuickStackItemIntoNearbyStorageSystems(self.GetNearbyNetworkHearts(), item, ref playSound);

		internal static bool TryQuickStackItemIntoNearbyStorageSystems(IEnumerable<TEStorageHeart> hearts, Item item, ref bool playSound) {
			if (item.IsAir)
				return false;

			//Quick stack to nearby chests failed or was only partially completed.  Try to do the same for nearby storage systems
			foreach (TEStorageHeart heart in hearts) {
				if (!heart.HasItem(item, ignorePrefix: true))
					continue;

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

		public override void PreSaveAndQuit() {
			RequestingOperatorKey = false;
		}
	}
}
