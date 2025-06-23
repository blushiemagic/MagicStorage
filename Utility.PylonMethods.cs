using MagicStorage.Common;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class Utility {
		// NOTE: Previously, this code used reflection to invoke the methods.  A publicizer has been introduced since then, so calling indirectly is no longer necessary.
		private static SceneMetrics _sceneMetrics;

		private static int HowManyNPCsDoesPylonNeed(TeleportPylonInfo info, Player player) => Main.PylonSystem.HowManyNPCsDoesPylonNeed(info, player);

		private static bool DoesPylonHaveEnoughNPCsAroundIt(TeleportPylonInfo info, int necessaryNPCCount) => Main.PylonSystem.DoesPylonHaveEnoughNPCsAroundIt(info, necessaryNPCCount);

		private static void CheckNPCDanger(TeleportPylonInfo info, ref bool flag) {
			flag &= !NPC.AnyDanger(quickBossNPCCheck: false, ignorePillarsAndMoonlordCountdown: true);
			flag = PylonLoader.ValidTeleportCheck_PreAnyDanger(info) is bool value
				? value
				: info.ModPylon?.ValidTeleportCheck_AnyDanger(info) ?? flag;
		}

		private static void CheckLihzahrdPylon(TeleportPylonInfo info, ref bool flag) {
			if (!NPC.downedPlantBoss && info.PositionInTiles.Y > Main.worldSurface && Main.tile[info.PositionInTiles.X, info.PositionInTiles.Y].WallType == WallID.LihzahrdBrickUnsafe)
				flag = false;
		}

		private static void CheckValidDestination(TeleportPylonInfo info, Player player, ref bool flag) {
			_sceneMetrics ??= new();

			try {
				SceneMetrics sceneMetrics = _sceneMetrics;
				SceneMetricsScanSettings settings = new SceneMetricsScanSettings {
					VisualScanArea = null,
					BiomeScanCenterPositionInWorld = info.PositionInTiles.ToWorldCoordinates(),
					ScanOreFinderData = false
				};

				sceneMetrics.ScanAndExportToMain(settings);
				flag = DoesPylonAcceptTeleportation(info, player);
			} catch {
				// Swallow any exceptions and assume that the pylon was invalid
				flag = false;
			}
		}

		private static bool DoesPylonAcceptTeleportation(TeleportPylonInfo info, Player player) {
			// Force the drone tracker to be ignored
			using (ObjectSwitch.SwapNull(ref Main.DroneCameraTracker))
				return Main.PylonSystem.DoesPylonAcceptTeleportation(info, player);
		}
	}
}
