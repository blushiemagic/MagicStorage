using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class Utility {
		// Copy/pasted from tML source for handling pylon validity
		// We don't need the "nearby player" checks for limited-range portable accesses, hence why they aren't included
		private static SceneMetrics _sceneMetrics = new();

		public static int HowManyNPCsDoesPylonNeed(TeleportPylonInfo info) {
			if (info.TypeOfPylon != TeleportPylonType.Victory)
				return 2;

			return 0;
		}

		private static bool DoesPylonHaveEnoughNPCsAroundIt(TeleportPylonInfo info, int necessaryNPCCount) {
			if (PylonLoader.ValidTeleportCheck_PreNPCCount(info, ref necessaryNPCCount) is bool value)
				return value;
			if (info.ModPylon is ModPylon pylon)
				return pylon.ValidTeleportCheck_NPCCount(info, necessaryNPCCount);
			if (necessaryNPCCount <= 0)
				return true;

			Point16 positionInTiles = info.PositionInTiles;
			return TeleportPylonsSystem.DoesPositionHaveEnoughNPCs(necessaryNPCCount, positionInTiles);
		}

		private static void CheckNPCDanger(TeleportPylonInfo info, ref bool flag) {
			flag &= !NPC.AnyDanger(quickBossNPCCheck: false,
				#if TML_144
				ignorePillarsAndMoonlordCountdown: true
				#else
				ignorePillars: true
				#endif
				);
			flag = PylonLoader.ValidTeleportCheck_PreAnyDanger(info) is bool value
				? value
				: info.ModPylon?.ValidTeleportCheck_AnyDanger(info) ?? flag;
		}

		private static void CheckLihzahrdPylon(TeleportPylonInfo info, ref bool flag) {
			if (!NPC.downedPlantBoss && info.PositionInTiles.Y > Main.worldSurface && Main.tile[info.PositionInTiles.X, info.PositionInTiles.Y].WallType == WallID.LihzahrdBrickUnsafe)
				flag = false;
		}

		private static void CheckValidDestination(TeleportPylonInfo info, ref bool flag) {
			// For whatever reason, this code has a chance to throw an error, which causes a hard game crash
			try {
				SceneMetrics sceneMetrics = _sceneMetrics;
				SceneMetricsScanSettings settings = new SceneMetricsScanSettings {
					VisualScanArea = null,
					BiomeScanCenterPositionInWorld = info.PositionInTiles.ToWorldCoordinates(),
					ScanOreFinderData = false
				};

				sceneMetrics.ScanAndExportToMain(settings);
				flag = DoesPylonAcceptTeleportation(info);
			} catch {
				// Swallow any exceptions and assume that the pylon was invalid
				flag = false;
			}
		}

		private static bool DoesPylonAcceptTeleportation(TeleportPylonInfo info) {
			if (PylonLoader.ValidTeleportCheck_PreBiomeRequirements(info, _sceneMetrics) is bool value)
				return value;
			if (info.ModPylon is ModPylon pylon) 
				return pylon.ValidTeleportCheck_BiomeRequirements(info, _sceneMetrics);
			switch (info.TypeOfPylon) {
				case TeleportPylonType.SurfacePurity: {
						bool num = info.PositionInTiles.Y <= Main.worldSurface;
						bool flag2 = info.PositionInTiles.X >= Main.maxTilesX - 380 || info.PositionInTiles.X <= 380;
						if (!num || flag2)
							return false;

						if (_sceneMetrics.EnoughTilesForJungle || _sceneMetrics.EnoughTilesForSnow || _sceneMetrics.EnoughTilesForDesert || _sceneMetrics.EnoughTilesForGlowingMushroom || _sceneMetrics.EnoughTilesForHallow || _sceneMetrics.EnoughTilesForCrimson || _sceneMetrics.EnoughTilesForCorruption)
							return false;

						return true;
					}
				case TeleportPylonType.Jungle:
					return _sceneMetrics.EnoughTilesForJungle;
				case TeleportPylonType.Snow:
					return _sceneMetrics.EnoughTilesForSnow;
				case TeleportPylonType.Desert:
					return _sceneMetrics.EnoughTilesForDesert;
				case TeleportPylonType.Beach: {
						bool flag = info.PositionInTiles.Y <= Main.worldSurface && info.PositionInTiles.Y > Main.worldSurface * 0.3499999940395355;
						return (info.PositionInTiles.X >= Main.maxTilesX - 380 || info.PositionInTiles.X <= 380) && flag;
					}
				case TeleportPylonType.GlowingMushroom:
					return _sceneMetrics.EnoughTilesForGlowingMushroom;
				case TeleportPylonType.Hallow:
					return _sceneMetrics.EnoughTilesForHallow;
				case TeleportPylonType.Underground:
					return info.PositionInTiles.Y >= Main.worldSurface;
				case TeleportPylonType.Victory:
					return true;
				default:
					return true;
			}
		}
	}
}
