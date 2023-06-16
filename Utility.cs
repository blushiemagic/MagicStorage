using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Components;
using MagicStorage.Edits;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace MagicStorage {
	public static partial class Utility {
		public static bool DownedAllMechs => NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3;

		public static string GetSimplifiedGenericTypeName(this Type type) {
			//Handle all invalid cases here:
			if (type.FullName is null)
				return type.Name;

			if (!type.IsGenericType)
				return type.FullName;

			string parent = type.GetGenericTypeDefinition().FullName!;

			//Include all but the "`X" part
			parent = parent[..parent.IndexOf('`')];

			//Construct the child types
			return $"{parent}<{string.Join(", ", type.GetGenericArguments().Select(GetSimplifiedGenericTypeName))}>";
		}

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
			canBeResearched = false;
			currentSacrificeTotal = 0;

			if (!CreativeItemSacrificesCatalog.Instance.TryGetSacrificeCountCapToUnlockInfiniteItems(itemType, out sacrificesNeeded))
				return;

			canBeResearched = Main.LocalPlayerCreativeTracker.ItemSacrifices.SacrificesCountByItemIdCache.TryGetValue(itemType, out currentSacrificeTotal);
		}

		public static bool IsFullyResearched(int itemType, bool mustBeResearchable) {
			GetResearchStats(itemType, out bool canBeResearched, out int sacrificesNeeded, out int currentSacrificeTotal);

			return (!mustBeResearchable || (canBeResearched && sacrificesNeeded > 0)) && currentSacrificeTotal >= sacrificesNeeded;
		}

		public static SafeOrdering<T> AsSafe<T>(this IComparer<T> comparer, Func<T, string> reportObjectFunc) => new(comparer, reportObjectFunc);

		public static Vector2 TopLeft(CalculatedStyle dims) => new(dims.X, dims.Y);

		public static Vector2 Top(CalculatedStyle dims) => new(dims.X + dims.Width / 2f, dims.Y);

		public static Vector2 TopRight(CalculatedStyle dims) => new(dims.X + dims.Width, dims.Y);

		public static Vector2 Left(CalculatedStyle dims) => new(dims.X, dims.Y + dims.Height / 2f);

		public static Vector2 Right(CalculatedStyle dims) => new(dims.X + dims.Width, dims.Y + dims.Height / 2f);

		public static Vector2 BottomLeft(CalculatedStyle dims) => new(dims.X, dims.Y + dims.Height);

		public static Vector2 Bottom(CalculatedStyle dims) => new(dims.X + dims.Width / 2f, dims.Y + dims.Height);

		public static Vector2 BottomRight(CalculatedStyle dims) => new(dims.X + dims.Width, dims.Y + dims.Height);

		private class ItemTypeComparer : IEqualityComparer<Item> {
			public static ItemTypeComparer Instance { get; } = new();

			public bool Equals(Item x, Item y) => x.type == y.type;

			public int GetHashCode([DisallowNull] Item obj) => obj.type;
		}

		internal static bool RecipesMatchForHistory(Recipe recipe1, Recipe recipe2) {
			if (RecursiveCraftIntegration.Enabled) {
				if (RecursiveCraftIntegration.IsCompoundRecipe(recipe1))
					recipe1 = RecursiveCraftIntegration.GetOverriddenRecipe(recipe1);

				if (RecursiveCraftIntegration.IsCompoundRecipe(recipe2))
					recipe2 = RecursiveCraftIntegration.GetOverriddenRecipe(recipe2);
			}

			return recipe1.createItem.type == recipe2.createItem.type
				&& recipe1.requiredItem.SequenceEqual(recipe2.requiredItem, ItemTypeComparer.Instance)
				&& recipe1.requiredTile.SequenceEqual(recipe2.requiredTile, EqualityComparer<int>.Default);
		}

		private static List<(StorageAccess access, Point16 position)> GetNearbyAccesses(Player self) {
			Point16 centerTile = self.Center.ToTileCoordinates16();

			List<(StorageAccess access, Point16 position)> storageAccesses = new();

			for (int x = centerTile.X - 17; x <= centerTile.X + 17; x++) {
				if (x < 0 || x >= Main.maxTilesX)
					continue;

				for (int y = centerTile.Y - 17; y <= centerTile.Y + 17; y++) {
					if (y < 0 || y >= Main.maxTilesY)
						continue;

					if (TileLoader.GetTile(Main.tile[x, y].TileType) is StorageAccess access)
						storageAccesses.Add((access, new Point16(x, y)));
				}
			}

			static Point16 AccessTopLeft(Point16 position) {
				//Assumes that all accesses are only 2x2, which they should be anyway
				Tile tile = Main.tile[position.X, position.Y];

				return position - new Point16(tile.TileFrameX / 18, tile.TileFrameY / 18);
			}

			//Avoid lazy evaluation making the List local possibly invalid
			return storageAccesses.DistinctBy(t => AccessTopLeft(t.position)).ToList();
		}

		public static IEnumerable<TEStorageHeart> GetNearbyNetworkHearts(this Player self)
			=> GetNearbyAccesses(self)
				.Select(t => (heart: t.access.GetHeart(t.position.X, t.position.Y), heartPosition: t.position))
				.Where(t => t.heart is not null)
				.DistinctBy(t => t.heartPosition)
				.Select(t => t.heart);

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

		public static void FindAndModify(List<TooltipLine> tooltips, string searchPhrase, string replacePhrase) {
			int searchIndex = tooltips.FindIndex(t => t.Text.Contains(searchPhrase));
			if (searchIndex >= 0)
				tooltips[searchIndex].Text = tooltips[searchIndex].Text.Replace(searchPhrase, replacePhrase);
		}

		public static void FindAndRemoveLine(List<TooltipLine> tooltips, string fullLine) {
			int searchIndex = tooltips.FindIndex(t => t.Text == fullLine);
			if (searchIndex >= 0)
				tooltips.RemoveAt(searchIndex);
		}

		public static void FindAndInsertLines(Mod mod, List<TooltipLine> tooltips, string searchLine, Func<int, string> lineNames, string replaceLines) {
			int searchIndex = tooltips.FindIndex(t => t.Text == searchLine);
			if (searchIndex >= 0) {
				tooltips.RemoveAt(searchIndex);

				int inserted = 0;
				foreach (var line in replaceLines.Split('\n')) {
					tooltips.Insert(searchIndex++, new TooltipLine(mod, lineNames(inserted), line));
					inserted++;
				}
			}
		}

		public static void CallOnStackHooks(Item destination, Item source, int numTransfered) {
			BuildOnStackHooksDelegate();
			onStackHooksDelegate?.Invoke(destination, source, numTransfered);
		}

		/// <summary>
		/// Forces an enumeration to be evaluated, then returns the result
		/// </summary>
		public static IEnumerable<T> Evaluate<T>(this IEnumerable<T> enumerable)
			=> enumerable.ToArray();

		internal static int ConstrainedSum(this IEnumerable<int> source) {
			int sum = 0;

			foreach (int i in source) {
				// Check overflow
				if (sum + i < 0)
					return int.MaxValue;

				sum += i;
			}
			
			return sum;
		}

		public static bool IsRecursiveRecipe(this Recipe recipe) => RecursiveRecipe.recipeToRecursiveRecipe.TryGetValue(recipe, out _);

		internal static readonly MethodInfo LocalizationLoader_AutoloadTranslations = typeof(LocalizationLoader).GetMethod("AutoloadTranslations", BindingFlags.NonPublic | BindingFlags.Static)!;
		internal static readonly MethodInfo LocalizationLoader_SetLocalizedText = typeof(LocalizationLoader).GetMethod("SetLocalizedText", BindingFlags.NonPublic | BindingFlags.Static)!;
		internal static readonly FieldInfo LanguageManager__localizedTexts = typeof(LanguageManager).GetField("_localizedTexts", BindingFlags.NonPublic | BindingFlags.Instance)!;

		/// <summary>
		/// Force's the localization for the given mod, <paramref name="mod"/>, to be loaded for use with <seealso cref="Language"/>
		/// </summary>
		/// <param name="mod">The mod instance</param>
		public static void ForceLoadModHJsonLocalization(Mod mod) {
			Dictionary<string, ModTranslation> modTranslationDictionary = new();

			LocalizationLoader_AutoloadTranslations.Invoke(null, new object[] { mod, modTranslationDictionary });

			Dictionary<string, LocalizedText> dict = LanguageManager__localizedTexts.GetValue(LanguageManager.Instance) as Dictionary<string, LocalizedText>;

			var culture = Language.ActiveCulture;
			foreach (ModTranslation translation in modTranslationDictionary.Values) {
				//LocalizedText text = new LocalizedText(translation.Key, translation.GetTranslation(culture));
				LocalizedText text = Activator.CreateInstance(typeof(LocalizedText), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, null, new object[] { translation.Key, translation.GetTranslation(culture) }, CultureInfo.InvariantCulture) as LocalizedText;

				LocalizationLoader_SetLocalizedText.Invoke(null, new object[] { dict, text });
			}
		}
	}
}
