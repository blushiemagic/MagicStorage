using MagicStorage.Common;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Systems.Shimmering;
using MagicStorage.Components;
using MagicStorage.Edits;
using MagicStorage.Sorting;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace MagicStorage {
	public static partial class Utility {
		public static bool DownedAllMechs => NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3;

		public static int GetCardinality(this BitArray bitArray) {
			int[] ints = new int[(bitArray.Count >> 5) + 1];

			bitArray.CopyTo(ints, 0);

			int count = 0;

			// fix for not truncated bits in last integer that may have been set to true with SetAll()
			ints[^1] &= ~(-1 << (bitArray.Count % 32));

			for (int i = 0; i < ints.Length; i++) {
				int c = ints[i];

				// magic (http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel)
				unchecked {
					c -= (c >> 1) & 0x55555555;
					c = (c & 0x33333333) + ((c >> 2) & 0x33333333);
					c = ((c + (c >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
				}

				count += c;
			}

			return count;
		}

		public static bool AreStrictlyEqual(Item item1, Item item2, bool checkStack = false, bool checkPrefix = true) => AreStrictlyEqual(item1, item2, checkStack, checkPrefix, null);

		public static bool AreStrictlyEqual(Item item1, Item item2, bool checkStack, bool checkPrefix, ConditionalWeakTable<Item, byte[]> savedItemTagIO) {
			int stack1 = item1.stack;
			int stack2 = item2.stack;
			int prefix1 = item1.prefix;
			int prefix2 = item2.prefix;
			bool favorite1 = item1.favorited;
			bool favorite2 = item2.favorited;

			item1.favorited = false;
			item2.favorited = false;

			bool equal;

			if (!checkPrefix) {
				item1.prefix = 0;
				item2.prefix = 0;
			}

			if (!checkStack) {
				item1.stack = 1;
				item2.stack = 1;
			}

			if (!ItemData.Matches(item1, item2)) {
				equal = false;
				goto ReturnFromMethod;
			}

			try {
				equal = TagIOSave(item1, savedItemTagIO).SequenceEqual(TagIOSave(item2, savedItemTagIO));
			} catch {
				// Swallow the exception and disallow stacking
				equal = false;
			}

			ReturnFromMethod:

			item1.stack = stack1;
			item2.stack = stack2;
			item1.prefix = prefix1;
			item2.prefix = prefix2;
			item1.favorited = favorite1;
			item2.favorited = favorite2;

			return equal;
		}

		private static byte[] TagIOSave(Item item, ConditionalWeakTable<Item, byte[]> savedItemTagIO)
		{
			if (savedItemTagIO?.TryGetValue(item, out byte[] retVal) is true)
			{
				return retVal;
			}
			using MemoryStream memoryStream = new(200);
			TagIO.ToStream(ItemIO.Save(item), memoryStream, false);
			retVal = memoryStream.ToArray();
			savedItemTagIO?.Add(item, retVal);
			return retVal;
		}

		public static void Write(this BinaryWriter writer, Point16 position) {
			writer.Write(position.X);
			writer.Write(position.Y);
		}

		public static Point16 ReadPoint16(this BinaryReader reader)
			=> new(reader.ReadInt16(), reader.ReadInt16());

		public static void GetResearchStats(int itemType, out bool canBeResearched, out int sacrificesNeeded, out int currentSacrificeTotal) {
			// NOTE: 1.4.4 adds this handy method which does all of the work for me.  cool!
			canBeResearched = Main.LocalPlayerCreativeTracker.ItemSacrifices.TryGetSacrificeNumbers(itemType, out currentSacrificeTotal, out sacrificesNeeded);
		}

		public static bool IsFullyResearched(int itemType, bool mustBeResearchable) {
			GetResearchStats(itemType, out bool canBeResearched, out int sacrificesNeeded, out int currentSacrificeTotal);

			return (!mustBeResearchable || (canBeResearched && sacrificesNeeded > 0)) && currentSacrificeTotal >= sacrificesNeeded;
		}

		public static SafeOrdering<T> AsSafe<T>(this IComparer<T> comparer, Func<T, string> reportObjectFunc) => new(comparer, reportObjectFunc);

		private class ItemTypeComparer : IEqualityComparer<Item> {
			public static ItemTypeComparer Instance { get; } = new();

			public bool Equals(Item x, Item y) => x.type == y.type;

			public int GetHashCode([DisallowNull] Item obj) => obj.type;
		}

		internal static bool RecipesMatchForHistory(Recipe recipe1, Recipe recipe2) {
			return recipe1.createItem.type == recipe2.createItem.type
				&& recipe1.requiredItem.SequenceEqual(recipe2.requiredItem, ItemTypeComparer.Instance)
				&& recipe1.requiredTile.SequenceEqual(recipe2.requiredTile, EqualityComparer<int>.Default);
		}

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

		public static TEStorageHeart GetHeartFromAccess(Point16 access) {
			if (access.X < 0 || access.Y < 0)
				return null;

			Tile tile = Main.tile[access.X, access.Y];

			if (TileLoader.GetTile(tile.TileType) is not StorageAccess storage)
				return null;

			return storage.GetHeart(access.X, access.Y);
		}

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

		public static bool IsPylonValidForRemoteAccessLinking(Player player, TeleportPylonInfo info, bool checkNPCDanger) {
			string key = null;

			NearbyEffectsBlockingDuringPylonScanningDetour.DoBlockHooks = true;

			int necessaryNPCCount = HowManyNPCsDoesPylonNeed(info);
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
				CheckValidDestination(info, ref flag);
				if (!flag)
					key = "Net.CannotTeleportToPylonBecauseNotMeetingBiomeRequirements";
			}

			if (info.ModPylon is ModPylon destinationPylon)
				destinationPylon.ValidTeleportCheck_DestinationPostCheck(info, ref flag, ref key);

			player.ForceUpdateBiomes();

			NearbyEffectsBlockingDuringPylonScanningDetour.DoBlockHooks = false;

			return flag;
		}

		public static void CallOnStackHooks(Item destination, Item source, int numTransfered) {
			ItemLoader.OnStack(destination, source, numTransfered);
		}

		/// <summary>
		/// Forces an enumeration to be evaluated, then returns the result
		/// </summary>
		public static IEnumerable<T> Evaluate<T>(this IEnumerable<T> enumerable)
			=> enumerable.ToArray();

		internal static int ConstrainedSum(this IEnumerable<int> source) {
			ClampedArithmetic sum = 0;

			foreach (int i in source)
				sum += i;
			
			return sum;
		}

		public static bool HasRecursiveRecipe(this Recipe recipe) => RecursiveRecipe.recipeToRecursiveRecipe.TryGetValue(recipe, out _);

		public static RecursiveRecipe GetRecursiveRecipe(this Recipe recipe) => RecursiveRecipe.recipeToRecursiveRecipe.TryGetValue(recipe, out RecursiveRecipe recursive) ? recursive : null;

		public static bool TryGetRecursiveRecipe(this Recipe recipe, out RecursiveRecipe recursive) => RecursiveRecipe.recipeToRecursiveRecipe.TryGetValue(recipe, out recursive);

		public static void ConvertToGPSCoordinates(Vector2 worldCoordinate, out int compassCoordinate, out int depthCoordinate) {
			// Copy/paste of logic from the info accessories
			compassCoordinate = (int)(worldCoordinate.X * 2f / 16f - Main.maxTilesX);
			depthCoordinate = (int)(worldCoordinate.Y * 2f / 16f - Main.worldSurface * 2.0);
		}

		public static void ConvertToGPSCoordinates(Point16 tileCoordinate, out int compassCoordinate, out int depthCoordinate) {
			// Copy/paste of logic from the info accessories
			compassCoordinate = (int)(tileCoordinate.X * 2f - Main.maxTilesX);
			depthCoordinate = (int)(tileCoordinate.Y * 2f - Main.worldSurface * 2.0);
		}

		public static void ConvertToGPSCoordinates(Vector2 worldCoordinate, out string compassText, out string depthText) {
			// Copy/paste of logic from the info accessories
			ConvertToGPSCoordinates(worldCoordinate, out int compass, out int depth);

			// Get the compass text
			compassText = compass switch {
				>0 => Language.GetTextValue("GameUI.CompassEast", compass),
				 0 => Language.GetTextValue("GameUI.CompassCenter"),
				<0 => Language.GetTextValue("GameUI.CompassWest", compass)
			};
			
			// Get the depth text
			float sizeFactor = Main.maxTilesX / 4200f;
			sizeFactor *= sizeFactor;

			int cavernsOffset = 1200;

			float surface = (float)((worldCoordinate.Y / 16f - (65f + 10f * sizeFactor)) / (Main.worldSurface / 5.0));

			string layerText;
			if (worldCoordinate.Y > (Main.maxTilesY - 204) * 16)
				layerText = Language.GetTextValue("GameUI.LayerUnderworld");
			else if (worldCoordinate.Y > Main.rockLayer * 16.0 + cavernsOffset / 2f + 16.0)
				layerText = Language.GetTextValue("GameUI.LayerCaverns");
			else if (depth > 0)
				layerText = Language.GetTextValue("GameUI.LayerUnderground");
			else if (surface < 1f)
				layerText = Language.GetTextValue("GameUI.LayerSpace");
			else
				layerText = Language.GetTextValue("GameUI.LayerSurface");

			depth = Math.Abs(depth);

			string coordText = depth switch {
				0 => Language.GetTextValue("GameUI.DepthLevel"),
				_ => Language.GetTextValue("GameUI.Depth", depth)
			};

			depthText = $"{coordText} {layerText}";
		}

		public static void AddOrSumCount(this Dictionary<int, int> itemCounts, int type, int count) {
			if (!itemCounts.ContainsKey(type))
				itemCounts[type] = count;
			else
				itemCounts[type] += count;
		}

		public static string ToBase64NoCompression(Item item) {
			MemoryStream ms = new MemoryStream();
			TagIO.ToStream(ItemIO.Save(item), ms, false);
			return Convert.ToBase64String(ms.ToArray());
		}

		public static string ToBase64NoCompression(TagCompound tag) {
			MemoryStream ms = new MemoryStream();
			TagIO.ToStream(tag, ms, false);
			return Convert.ToBase64String(ms.ToArray());
		}

		public static Item FromBase64NoCompression(string base64) {
			MemoryStream ms = new MemoryStream(Convert.FromBase64String(base64));
			return SafelyLoadItem(TagIO.FromStream(ms, false));
		}

		public static void SetVanillaAdjTiles(Item item, out bool hasSnow, out bool hasGraveyard) {
			hasSnow = false;
			hasGraveyard = false;
			
			Player player = Main.LocalPlayer;
			bool[] adjTiles = player.adjTile;
			if (item.createTile >= TileID.Dirt) {
				adjTiles[item.createTile] = true;
				switch (item.createTile) {
					case TileID.GlassKiln:
					case TileID.Hellforge:
						adjTiles[TileID.Furnaces] = true;
						break;
					case TileID.AdamantiteForge:
						adjTiles[TileID.Furnaces] = true;
						adjTiles[TileID.Hellforge] = true;
						break;
					case TileID.MythrilAnvil:
						adjTiles[TileID.Anvils] = true;
						break;
					case TileID.BewitchingTable:
					case TileID.Tables2:
						adjTiles[TileID.Tables] = true;
						break;
					case TileID.AlchemyTable:
						adjTiles[TileID.Bottles] = true;
						adjTiles[TileID.Tables] = true;
						break;
					case TileID.Tombstones:
						hasGraveyard = true;
						break;
				}

				switch (item.createTile) {
					case TileID.WorkBenches:
					case TileID.Tables:
					case TileID.Tables2:
						adjTiles[TileID.Chairs] = true;
						break;
				}

				TileLoader.AdjTiles(Main.LocalPlayer, item.createTile);

				if (TileID.Sets.CountsAsWaterSource[item.createTile])
					player.adjWater = true;
				if (TileID.Sets.CountsAsLavaSource[item.createTile])
					player.adjLava = true;
				if (TileID.Sets.CountsAsHoneySource[item.createTile])
					player.adjHoney = true;
				if (player.adjTile[TileID.Tombstones])
					hasGraveyard = true;
			}

			int globeItem = ModContent.ItemType<Items.BiomeGlobe>();

			if (item.type == ItemID.WaterBucket || item.type == ItemID.BottomlessBucket || item.type == globeItem)
				player.adjWater = true;
			if (item.type == ItemID.LavaBucket || item.type == ItemID.BottomlessLavaBucket || item.type == globeItem)
				player.adjLava = true;
			if (item.type == ItemID.HoneyBucket || item.type == ItemID.BottomlessHoneyBucket || item.type == globeItem)
				player.adjHoney = true;
			if (item.type == ItemID.BottomlessShimmerBucket)
				player.adjShimmer = true;
			if (item.type == ModContent.ItemType<Items.SnowBiomeEmulator>() || item.type == globeItem)
				hasSnow = true;
			if (item.type == globeItem) {
				adjTiles[TileID.Campfire] = true;
				adjTiles[TileID.DemonAltar] = true;
				hasGraveyard = true;
			}
		}

		private static Action<ModConfig> ConfigManagerSave;

		public static void SaveModConfig(ModConfig config) {
			(ConfigManagerSave ??= CreateConfigManagerSave())(config);

			static Action<ModConfig> CreateConfigManagerSave() {
				MethodInfo configManagerSaveMethod = typeof(ConfigManager).GetMethod("Save", BindingFlags.Static | BindingFlags.NonPublic, new[] { typeof(ModConfig) }) 
					?? throw new InvalidOperationException("Cannot get 'Terraria.ModLoader.Config.ConfigManager.Save' method.");
				ParameterExpression modConfigParameter = Expression.Parameter(typeof(ModConfig));
				return Expression.Lambda<Action<ModConfig>>(Expression.Call(configManagerSaveMethod, modConfigParameter), modConfigParameter).Compile();
			}
		}

		public static Item GetItemSample(int item) => ContentSamples.ItemsByType[item];

		public static bool IsSuccessful(this ShimmerInfo.ShimmerAttemptResult result) => result != ShimmerInfo.ShimmerAttemptResult.None;

		public static bool IsSuccessfulButNotDecraftable(this ShimmerInfo.ShimmerAttemptResult result) => result != ShimmerInfo.ShimmerAttemptResult.None && result != ShimmerInfo.ShimmerAttemptResult.DecraftedItem;

		public static IEnumerable<IShimmerResultReport> GetShimmerReports(this IShimmerResult result, int item) => result.GetShimmerReports(ContentSamples.ItemsByType[item], item);

		internal static IEnumerable<T> Filter<T>(this IEnumerable<T> source, StorageGUI.ThreadContext thread, Func<T, Item> objToItem) {
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(thread);
			ArgumentNullException.ThrowIfNull(objToItem);

			return new ThreadFilterEnumerator<T>(thread, source, objToItem);
		}

		internal static IEnumerable<Item> Filter(this IEnumerable<Item> source, StorageGUI.ThreadContext thread) {
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(thread);

			return new ThreadFilterItemEnumerator(thread, source);
		}

		internal static ParallelQuery<T> Filter<T>(this ParallelQuery<T> query, StorageGUI.ThreadContext thread, Func<T, Item> objToItem) {
			ArgumentNullException.ThrowIfNull(query);
			ArgumentNullException.ThrowIfNull(thread);
			ArgumentNullException.ThrowIfNull(objToItem);

			return new ThreadFilterParallelEnumerator<T>(thread, query, objToItem).GetQuery();
		}

		internal static ParallelQuery<Item> Filter(this ParallelQuery<Item> query, StorageGUI.ThreadContext thread) {
			ArgumentNullException.ThrowIfNull(query);
			ArgumentNullException.ThrowIfNull(thread);

			return new ThreadFilterParallelItemEnumerator(thread, query).GetQuery();
		}

		public static Item SafelyLoadItem(TagCompound tag) {
			try {
				return ItemIO.Load(tag);
			} catch (KeyNotFoundException) {
				// Item was malformed
				return new Item();
			} catch (Exception ex) {
				MagicStorageMod.Instance.Logger.Error("Error loading item from tag", ex);
				return new Item();
			}
		}
	}
}
