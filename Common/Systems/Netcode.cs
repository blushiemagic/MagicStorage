using MagicStorage.Common.Systems.Debugging;
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
		internal static bool KeyIsGenerated => opKey != null;

		internal static bool RequestingOperatorKey { get; set; }

		private static readonly char[] randomCharacters = Enumerable.Range('0', 10).Concat(Enumerable.Range('A', 26)).Concat(Enumerable.Range('a', 26)).Select(i => (char)i).ToArray();

		private const int KeyLength = 12;

		public static string GetOrGenerateOperatorKey() {
			AttemptKeyGeneration();
			return opKey;
		}

		public static void AttemptKeyGeneration() {
			if (opKey is null && Main.netMode == NetmodeID.Server) {
				opKey = GenerateKey();

				string keyMsg = MagicStorageMod.Instance.GetLocalization("ServerOperator.CommandInfo.ServerKeyText").Format(opKey);

				Utility.WriteLineColoredSafely(keyMsg, ConsoleColor.Yellow, ConsoleColor.Black);
				// Send the text to the server log as well
				MagicStorageMod.Instance.Logger.Info("\n" + keyMsg);
			}
		}

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
			if (Main.netMode == NetmodeID.MultiplayerClient || item.IsAir)
				return false;

			using var debugging = DebugMessage.CreateIf(DebugControls.Names.QuickStacking);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, new NetmodeContextMessage(
						ChatMessage: new("Attempting to quick stack {0} to nearby storage centers...", item.ToChatTag()),
						ConsoleOrLogMessage: new("Attempting to quick stack item \"{0}\" to nearby storage centers...", item.IdentifierAndStack())
					))
					.Indent();
			}

			List<TEStorageCenter> enumeratedCenters = [.. nearbyCenters];

			if (enumeratedCenters.Count == 0) {
				if (debugging.IsDebugging)
					debugging.Report(false, "Failed.  No storage centers were nearby.");

				return false;
			}

			int startStack = item.stack;

			//Quick stack to nearby chests failed or was only partially completed.  Try to do the same for nearby storage systems
			foreach (TEStorageCenter center in enumeratedCenters) {
				if (debugging.IsDebugging) {
					debugging
						.Report(false, "Attempting to quick stack to storage center {0} at {1}...", center.FullName, center.Position.DebugString())
						.Indent();
				}

				if (center.GetHeart() is not TEStorageHeart heart) {
					if (debugging.IsDebugging)
						debugging.Report(false, "Failed.  Storage center is not connected to a Storage Heart.");

					goto NextItem;
				}

				if (!heart.HasItem(item, ignorePrefix: true)) {
					if (debugging.IsDebugging)
						debugging.Report(false, "Failed.  Storage Heart linked to storage center does not contain a similar item.");

					goto NextItem;
				}

				int oldType = item.type;
				int oldStack = item.stack;

				heart.DepositItem(item);

				if (oldType != item.type || oldStack != item.stack) {
					if (debugging.IsDebugging)
						debugging.Report(false, "Success.  Deposited {0}/{1} of the stack.", oldStack - item.stack, oldStack);

					playSound = true;
					Chest.VisualizeChestTransfer(depositOrigin, center.Position.ToWorldCoordinates(16, 16), ContentSamples.ItemsByType[oldType], oldStack - item.stack);
				} else {
					if (debugging.IsDebugging)
						debugging.Report(false, "Failed.  Storage could not fit any of the item stack.");
				}

				if (item.stack <= 0)
					item.TurnToAir();

				if (item.IsAir)
					return true;

				NextItem:

				if (debugging.IsDebugging)
					debugging.Unindent();
			}

			return item.stack < startStack;
		}

		public override void PreSaveAndQuit() {
			RequestingOperatorKey = false;
		}
	}
}
