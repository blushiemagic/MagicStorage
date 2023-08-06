using MagicStorage.Components;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.GameContent.Drawing;
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

		internal static bool TryQuickStackItemIntoNearbyStorageSystems(Player self, Item item, ref bool playSound)
			=> TryQuickStackItemIntoNearbyStorageSystems(self.Center, self.GetNearbyCenters(), item, ref playSound);

		internal static bool TryQuickStackItemIntoNearbyStorageSystems(Vector2 depositOrigin, IEnumerable<TEStorageCenter> nearbyCenters, Item item, ref bool playSound) {
			if (item.IsAir || !nearbyCenters.Any())
				return false;

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// "Consume" the item and ask the server to do the depositing
				NetHelper.RequestQuickStackToNearbyStorage(depositOrigin, item, nearbyCenters);
				item.SetDefaults(0, false);
				playSound = false;
				return false;
			}

			int startStack = item.stack;

			//Quick stack to nearby chests failed or was only partially completed.  Try to do the same for nearby storage systems
			foreach (TEStorageCenter center in nearbyCenters) {
				if (center.GetHeart() is not TEStorageHeart heart || !heart.HasItem(item, ignorePrefix: true))
					continue;

				int oldType = item.type;
				int oldStack = item.stack;

				heart.DepositItem(item);

				if (oldType != item.type || oldStack != item.stack) {
					playSound = true;
					Chest.VisualizeChestTransfer(depositOrigin, center.Position.ToWorldCoordinates(16, 16), ContentSamples.ItemsByType[oldType], oldStack - item.stack);
				}

				if (item.stack <= 0)
					item.TurnToAir();

				if (item.IsAir)
					return true;
			}

			return item.stack < startStack;
		}

		public override void PreSaveAndQuit() {
			RequestingOperatorKey = false;
		}
	}
}
