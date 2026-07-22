using MagicStorage.Components;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
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
				Main.NewText(MagicStorageMod.Instance.GetLocalization("ServerOperator.CommandInfo.ClientKeyResponseFailed").Value, Color.Red);
			else
				Main.NewText(MagicStorageMod.Instance.GetLocalization("ServerOperator.CommandInfo.ClientKeyResponseSuccess").Value, Color.Green);
		}

		internal static bool TryQuickStackItemIntoNearbyStorageSystems(Player self, Item item, ref bool playSound) {
			using var _ = SecuritySystem.CreateAccessContext(self.whoAmI);
			return TryQuickStackItemIntoNearbyStorageSystems(self.Center, self.GetNearbyCenters(), item, ref playSound);
		}

		internal static bool TryQuickStackItemIntoNearbyStorageSystems(Vector2 depositOrigin, IEnumerable<TEStorageCenter> nearbyCenters, Item item, ref bool playSound) {
			if (Main.netMode == NetmodeID.MultiplayerClient || item.IsAir || !nearbyCenters.Any())
				return false;

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
					CraftingGUI.NotifyStorageInventoryChanged(oldType);
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
