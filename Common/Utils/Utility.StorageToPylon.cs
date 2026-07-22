using MagicStorage.Components;
using MagicStorage.Edits;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class Utility {
		/// <summary>
		/// Finds storage centers reachable from storage accesses near the player.
		/// </summary>
		/// <param name="self">The player to search around.</param>
		/// <returns>The nearby storage centers connected to access tiles.</returns>
		public static IEnumerable<TEStorageCenter> GetNearbyCenters(this Player self) {
			Point16 centerTile = self.Center.ToTileCoordinates16();

			HashSet<Point16> foundAccesses = new();

			const int range = 39;
			int startX = centerTile.X - range, startY = centerTile.Y - range;
			int endX = centerTile.X + range, endY = centerTile.Y + range;

			startX = Utils.Clamp(startX, 0, Main.maxTilesX - 1);
			startY = Utils.Clamp(startY, 0, Main.maxTilesY - 1);
			endX = Utils.Clamp(endX, 0, Main.maxTilesX - 1);
			endY = Utils.Clamp(endY, 0, Main.maxTilesY - 1);

			for (int x = startX; x <= endX; x++) {
				for (int y = startY; y <= endY; y++) {
					Tile tile = Main.tile[x, y];
					if (TileLoader.GetTile(tile.TileType) is StorageAccess access)
						foundAccesses.Add(new Point16(x - tile.TileFrameX / 18, y - tile.TileFrameY / 18));
				}
			}

			return foundAccesses
				.Select(static p => TileEntity.ByPosition.TryGetValue(p, out TileEntity entity) && entity is TEStorageCenter ? p : TEStorageComponent.FindStorageCenter(p))
				.Where(static p => p != Point16.NegativeOne)
				.Distinct()
				.Select(static p => TileEntity.ByPosition.TryGetValue(p, out TileEntity entity) ? entity : null)
				.OfType<TEStorageCenter>();
		}

		/// <summary>
		/// Gets the storage heart connected to a storage access tile.
		/// </summary>
		/// <param name="access">The access tile position.</param>
		/// <returns>The connected storage heart, or <see langword="null" /> when none exists.</returns>
		public static TEStorageHeart GetHeartFromAccess(Point16 access) {
			if (access.X < 0 || access.Y < 0)
				return null;

			Tile tile = Main.tile[access.X, access.Y];

			if (TileLoader.GetTile(tile.TileType) is not StorageAccess storage)
				return null;

			return storage.GetHeart(access.X, access.Y);
		}

		/// <summary>
		/// Enumerates valid pylons near a player for remote access linking.
		/// </summary>
		/// <param name="player">The player to search around.</param>
		/// <param name="range">The search range in pixels, or a negative value for unlimited range.</param>
		/// <returns>The valid nearby pylons.</returns>
		public static IEnumerable<TeleportPylonInfo> NearbyPylons(Player player, float range) {
			if (!Main.PylonSystem.HasAnyPylon() || range == 0)
				yield break;

			Point16 playerCenter = player.Center.ToTileCoordinates16();
			short pX = playerCenter.X, pY = playerCenter.Y;
			float r = range / 16;

			foreach (TeleportPylonInfo pylon in Main.PylonSystem.Pylons) {
				if (!IsPylonValidForRemoteAccessLinking(player, pylon, checkNPCDanger: false))
					continue;

				if (range < 0) {
					yield return pylon;
					continue;
				}

				int x = pylon.PositionInTiles.X, y = pylon.PositionInTiles.Y;
				float xMin = x - r, xMax = x + r + 1, yMin = y - r, yMax = y + r + 1;

				//Range < 0 is treated as infinite range
				if (xMin <= pX && pX <= xMax && yMin <= pY && pY <= yMax)
					yield return pylon;
			}
		}

		/// <summary>
		/// Checks whether a storage system has a valid pylon near one of its centers.
		/// </summary>
		/// <param name="player">The player used for pylon validity checks.</param>
		/// <param name="heart">The storage heart identifying the storage system.</param>
		/// <param name="tileRange">The tile search range, or a negative value for unlimited range.</param>
		/// <returns><see langword="true" /> when a valid pylon is within range; otherwise, <see langword="false" />.</returns>
		public static bool StorageSystemHasNearbyPylon(Player player, TEStorageHeart heart, int tileRange) {
			if (!Main.PylonSystem.HasAnyPylon() || heart is null || tileRange == 0)
				return false;

			List<TeleportPylonInfo> validPylons = Main.PylonSystem.Pylons.Where(pylon => IsPylonValidForRemoteAccessLinking(player, pylon, checkNPCDanger: false)).ToList();

			if (validPylons.Count == 0)
				return false;

			//Tile range < 0 is treated as infinite range
			if (tileRange < 0)
				return true;

			foreach (var center in TileEntity.ByPosition.Values.OfType<TEStorageCenter>()) {
				if (GetHeartFromAccess(center.Position)?.Position != heart.Position)
					continue;

				int x = center.Position.X, y = center.Position.Y;
				int xMin = x - tileRange, xMax = x + tileRange + 1, yMin = y - tileRange, yMax = y + tileRange + 1;

				foreach (TeleportPylonInfo pylon in validPylons) {
					int pX = pylon.PositionInTiles.X, pY = pylon.PositionInTiles.Y;

					if (xMin <= pX && pX <= xMax && yMin <= pY && pY <= yMax)
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks whether a player is near any center in a storage system.
		/// </summary>
		/// <param name="player">The player to check.</param>
		/// <param name="heart">The storage heart identifying the storage system.</param>
		/// <param name="range">The range in pixels, or a negative value for unlimited range.</param>
		/// <returns><see langword="true" /> when the player is within range; otherwise, <see langword="false" />.</returns>
		public static bool PlayerIsNearStorageSystem(Player player, TEStorageHeart heart, float range) {
			if (heart is null || range == 0)
				return false;

			//Range < 0 is treated as infinite range
			if (range < 0)
				return true;

			Point16 playerCenter = player.Center.ToTileCoordinates16();
			short pX = playerCenter.X, pY = playerCenter.Y;
			float r = range / 16;

			foreach (var center in TileEntity.ByPosition.Values.OfType<TEStorageCenter>()) {
				if (GetHeartFromAccess(center.Position)?.Position != heart.Position)
					continue;

				int x = center.Position.X, y = center.Position.Y;
				float xMin = x - r, xMax = x + r + 1, yMin = y - r, yMax = y + r + 1;

				if (xMin <= pX && pX <= xMax && yMin <= pY && pY <= yMax)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Checks whether a player is near a storage access tile.
		/// </summary>
		/// <param name="player">The player to check.</param>
		/// <param name="access">The storage access tile position.</param>
		/// <param name="range">The range in pixels, or a negative value for unlimited range.</param>
		/// <returns><see langword="true" /> when the player is within range; otherwise, <see langword="false" />.</returns>
		public static bool PlayerIsNearAccess(Player player, Point16 access, float range) {
			if (range == 0)
				return false;

			//Infinite range
			if (range < 0)
				return true;

			float r = range / 16;

			float pX = player.Center.X / 16, pY = player.Center.Y / 16;

			int x = access.X, y = access.Y;
			float xMin = x - r, xMax = x + r + 1, yMin = y - r, yMax = y + r + 1;

			return xMin <= pX && pX <= xMax && yMin <= pY && pY <= yMax;
		}

		/// <summary>
		/// Checks whether a player is near a valid pylon.
		/// </summary>
		/// <param name="player">The player to check.</param>
		/// <param name="pylon">The pylon to check.</param>
		/// <param name="range">The range in pixels, or a negative value for unlimited range.</param>
		/// <returns><see langword="true" /> when the player is within range of a valid pylon; otherwise, <see langword="false" />.</returns>
		public static bool PlayerIsNearPylon(Player player, TeleportPylonInfo pylon, float range) {
			if (range == 0)
				return false;
			
			//Range < 0 is treated as infinite range
			if (range < 1)
				return true;

			Point16 playerCenter = player.Center.ToTileCoordinates16();
			short pX = playerCenter.X, pY = playerCenter.Y;
			float r = range / 16;

			if (!IsPylonValidForRemoteAccessLinking(player, pylon, checkNPCDanger: false))
				return false;

			int x = pylon.PositionInTiles.X, y = pylon.PositionInTiles.Y;
			float xMin = x - r, xMax = x + r + 1, yMin = y - r, yMax = y + r + 1;

			return xMin <= pX && pX <= xMax && yMin <= pY && pY <= yMax;
		}

		//Copy for Common/Systems/PortableAccessAreas.cs
		internal static bool PlayerIsNearPylonIgnoreValidity(Player player, TeleportPylonInfo pylon, float range) {
			if (range == 0)
				return false;
			
			//Range < 0 is treated as infinite range
			if (range < 1)
				return true;

			Point16 playerCenter = player.Center.ToTileCoordinates16();
			short pX = playerCenter.X, pY = playerCenter.Y;
			float r = range / 16;

			int x = pylon.PositionInTiles.X, y = pylon.PositionInTiles.Y;
			float xMin = x - r, xMax = x + r + 1, yMin = y - r, yMax = y + r + 1;

			return xMin <= pX && pX <= xMax && yMin <= pY && pY <= yMax;
		}

		/// <summary>
		/// Checks whether a pylon is valid for remote access linking.
		/// </summary>
		/// <param name="player">The player used for biome and destination checks.</param>
		/// <param name="info">The pylon to validate.</param>
		/// <param name="checkNPCDanger">Whether nearby NPC danger should block linking.</param>
		/// <returns><see langword="true" /> when the pylon is valid; otherwise, <see langword="false" />.</returns>
		public static bool IsPylonValidForRemoteAccessLinking(Player player, TeleportPylonInfo info, bool checkNPCDanger) {
			string key = null;

			NearbyEffectsBlockingDuringPylonScanningDetour.DoBlockHooks = true;

			int necessaryNPCCount = HowManyNPCsDoesPylonNeed(info, player);
			bool flag = DoesPylonHaveEnoughNPCsAroundIt(info, necessaryNPCCount);
			if (!flag)
				key = "Net.CannotTeleportToPylonBecauseNotEnoughNPCs";

			if (flag && checkNPCDanger) {
				CheckNPCDanger(info, ref flag);
				if (!flag)
					key = "Net.CannotTeleportToPylonBecauseThereIsDanger";
			}

			if (flag) {
				CheckLihzahrdPylon(info, ref flag);
				if (!flag)
					key = "Net.CannotTeleportToPylonBecauseAccessingLihzahrdTempleEarly";
			}

			if (flag) {
				CheckValidDestination(info, player, ref flag);
				if (!flag)
					key = "Net.CannotTeleportToPylonBecauseNotMeetingBiomeRequirements";
			}

			if (info.ModPylon is ModPylon destinationPylon)
				destinationPylon.ValidTeleportCheck_DestinationPostCheck(info, ref flag, ref key);

			player.ForceUpdateBiomes();

			NearbyEffectsBlockingDuringPylonScanningDetour.DoBlockHooks = false;

			return flag;
		}
	}
}
