using MagicStorage.Common;
using System;
using System.Linq.Expressions;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class Utility {
		// Copy/pasted from tML source for handling pylon validity
		// We don't need the "nearby player" checks for limited-range portable accesses, hence why they aren't included
		private static SceneMetrics _sceneMetrics;

		private static Func<TeleportPylonInfo, int> _HowManyNPCsDoesPylonNeed;
		public static int HowManyNPCsDoesPylonNeed(TeleportPylonInfo info) {
			return (_HowManyNPCsDoesPylonNeed ??= CreateMethodCall())(info);

			static Func<TeleportPylonInfo, int> CreateMethodCall() {
				MethodInfo method = typeof(TeleportPylonsSystem).GetMethod("HowManyNPCsDoesPylonNeed", BindingFlags.Instance | BindingFlags.NonPublic)
					?? throw new Exception("Cannot get 'Terraria.GameContent.TeleportPylonsSystem.HowManyNPCsDoesPylonNeed' method");
				ParameterExpression parameter = Expression.Parameter(typeof(TeleportPylonInfo), "info");
				return Expression.Lambda<Func<TeleportPylonInfo, int>>(Expression.Call(parameter, method), parameter).Compile();
			}
		}

		private static Func<TeleportPylonInfo, int, bool> _DoesPylonHaveEnoughNPCsAroundIt;
		private static bool DoesPylonHaveEnoughNPCsAroundIt(TeleportPylonInfo info, int necessaryNPCCount) {
			return (_DoesPylonHaveEnoughNPCsAroundIt ??= CreateMethodCall())(info, necessaryNPCCount);

			static Func<TeleportPylonInfo, int, bool> CreateMethodCall() {
				MethodInfo method = typeof(TeleportPylonsSystem).GetMethod("DoesPylonHaveEnoughNPCsAroundIt", BindingFlags.Instance | BindingFlags.NonPublic)
					?? throw new Exception("Cannot get 'Terraria.GameContent.TeleportPylonsSystem.DoesPylonHaveEnoughNPCsAroundIt' method");
				ParameterExpression parameter1 = Expression.Parameter(typeof(TeleportPylonInfo), "info");
				ParameterExpression parameter2 = Expression.Parameter(typeof(int), "necessaryNPCCount");
				return Expression.Lambda<Func<TeleportPylonInfo, int, bool>>(Expression.Call(parameter1, method, parameter2), parameter1, parameter2).Compile();
			}
		}

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

		private static void CheckValidDestination(TeleportPylonInfo info, ref bool flag) {
			_sceneMetrics ??= new();

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

		private static Func<TeleportPylonInfo, bool> _DoesPylonAcceptTeleportation;
		private static bool DoesPylonAcceptTeleportation(TeleportPylonInfo info) {
			// Force the drone tracker to be ignored
			using (ObjectSwitch.SwapNull(ref Main.DroneCameraTracker))
				return (_DoesPylonAcceptTeleportation ??= CreateMethodCall())(info);

			static Func<TeleportPylonInfo, bool> CreateMethodCall() {
				MethodInfo method = typeof(TeleportPylonsSystem).GetMethod("DoesPylonAcceptTeleportation", BindingFlags.Instance | BindingFlags.NonPublic)
					?? throw new Exception("Cannot get 'Terraria.GameContent.TeleportPylonsSystem.DoesPylonAcceptTeleportation' method");
				ParameterExpression parameter = Expression.Parameter(typeof(TeleportPylonInfo), "info");
				return Expression.Lambda<Func<TeleportPylonInfo, bool>>(Expression.Call(parameter, method), parameter).Compile();
			}
		}
	}
}
