using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.Shimmering {
	public static class ShimmerMetrics {
		private static readonly Dictionary<int, Func<int, int>> _getShimmerTransformItem = new() {
			[ItemID.RodofDiscord] = GetMoonLordGatekeptItem,
			[ItemID.Clentaminator] = GetMoonLordGatekeptItem,
			[ItemID.BottomlessBucket] = GetMoonLordGatekeptItem,
			[ItemID.BottomlessShimmerBucket] = GetMoonLordGatekeptItem,
			[ItemID.LunarBrick] = MoonPhaseTransform
		};

		private static int GetMoonLordGatekeptItem(int type) {
			if (!NPC.downedMoonlord)
				return ItemID.None;

			return type switch {
				ItemID.RodofDiscord => ItemID.RodOfHarmony,
				ItemID.Clentaminator => ItemID.Clentaminator2,
				ItemID.BottomlessBucket => ItemID.BottomlessShimmerBucket,
				ItemID.BottomlessShimmerBucket => ItemID.BottomlessBucket,
				_ => ItemID.None
			};
		}

		private static int MoonPhaseTransform(int type) {
			if (type != ItemID.LunarBrick)
				return ItemID.None;

			return Main.GetMoonPhase() switch {
				MoonPhase.QuarterAtRight => ItemID.StarRoyaleBrick,
				MoonPhase.HalfAtRight => ItemID.CryocoreBrick,
				MoonPhase.ThreeQuartersAtRight => ItemID.CosmicEmberBrick,
				MoonPhase.Full => ItemID.HeavenforgeBrick,
				MoonPhase.ThreeQuartersAtLeft => ItemID.LunarRustBrick,
				MoonPhase.HalfAtLeft => ItemID.AstraBrick,
				MoonPhase.QuarterAtLeft => ItemID.DarkCelestialBrick,
				_ => ItemID.MercuryBrick,
			};
		}

		public static Condition GetTransformCondition(int item) {
			// Check the Moon Lord gatekept items
			if (item is ItemID.RodofDiscord or ItemID.Clentaminator or ItemID.BottomlessBucket or ItemID.BottomlessShimmerBucket)
				return Condition.DownedMoonLord;

			// Check the Lunar Brick
			if (item is ItemID.LunarBrick) {
				return Main.GetMoonPhase() switch {
					MoonPhase.QuarterAtRight => Condition.MoonPhaseWaxingCrescent,
					MoonPhase.HalfAtRight => Condition.MoonPhaseFirstQuarter,
					MoonPhase.ThreeQuartersAtRight => Condition.MoonPhaseWaxingGibbous,
					MoonPhase.Full => Condition.MoonPhaseFull,
					MoonPhase.ThreeQuartersAtLeft => Condition.MoonPhaseWaningGibbous,
					MoonPhase.HalfAtLeft => Condition.MoonPhaseThirdQuarter,
					MoonPhase.QuarterAtLeft => Condition.MoonPhaseWaningCrescent,
					_ => Condition.MoonPhaseNew,
				};
			}

			// TODO: tModLoader shimmer API, Mod.Call() for custom conditions

			return null;
		}

		internal static int TransformItem(int item) {
			if (_getShimmerTransformItem.TryGetValue(item, out var getItem))
				return getItem(item);

			if (ContentSamples.ItemsByType[item].createTile == TileID.MusicBoxes)
				return ItemID.MusicBox;

			return ItemID.Sets.ShimmerTransformToItem[item];
		}

		public static IShimmerResult AttemptItemTransmutation(Item item, StorageIntermediary storage, bool net) {
			var info = MagicCache.ShimmerInfos[item.type];
			var result = info.GetResult();

			result?.OnShimmer(item, info.iconicItem, storage, net);

			if (item.stack <= 0)
				item.TurnToAir();

			return result;
		}

		public static bool IsDecraftAvailable(Recipe recipe) {
			if (recipe.Disabled)
				return false;

			int decraftingIndex = recipe.RecipeIndex;

			if (!NPC.downedBoss3 && ShimmerTransforms.RecipeSets.PostSkeletron[decraftingIndex])
				return false;

			if (!NPC.downedGolemBoss && ShimmerTransforms.RecipeSets.PostGolem[decraftingIndex])
				return false;

			return RecipeLoader.DecraftAvailable(recipe);
		}

		public static Recipe GetDecraftingRecipeFor(int item) {
			if (MagicCache.ShimmerInfos[item].GetAttempt(out int recipeIndex) != ShimmerInfo.ShimmerAttemptResult.DecraftedItem)
				return null;

			return Main.recipe[recipeIndex];
		}

		internal readonly struct DecraftResult {
			public readonly int type;
			public readonly int stack;

			public DecraftResult(int type, int stack) {
				this.type = type;
				this.stack = stack;
			}
		}

		internal static IEnumerable<DecraftResult> AttemptDecraft(Recipe recipe, int iconicItemType, ref int stack, bool applyIngredientReductionRules = true) {
			// Recipe is guaranteed to be available at this point
			// The following logic is derived from Item::GetShimmered()
			int amount = GetDecraftAmount(iconicItemType, stack);
			if (amount <= 0)
				return Array.Empty<DecraftResult>();

			IEnumerable<Item> items = recipe.customShimmerResults is { } list ? list : recipe.requiredItem;

			List<DecraftResult> results = new();

			foreach (Item item in items) {
				int ingredientStack = amount * item.stack;

				if (applyIngredientReductionRules) {
					// tModLoader has a bug where it doesn't properly check ingredient consumption when decrafting
					// Adding that bug here wouldn't be sensible
					RecipeLoader.ConsumeItem(recipe, item.type, ref ingredientStack);
				}

				if (ingredientStack <= 0)
					continue;

				results.Add(new DecraftResult(item.type, ingredientStack));
			}

			stack -= amount * recipe.createItem.stack;

			return results;
		}

		internal static void SendShimmerResults(BinaryWriter writer, List<IShimmerResult> results) {
			writer.Write((short)results.Count);
			foreach (var result in results) {
				byte type = result switch {
					TransformItem _ => 0,
					CoinLuck _ => 1,
					NPCSpawn _ => 2,
					Decraft _ => 3,
					_ => throw new ArgumentException($"Invalid shimmer result type \"{result?.GetType().ToString() ?? "null"}\"")
				};

				writer.Write(type);
				result.Send(writer);
			}
		}

		internal static List<IShimmerResult> ReceiveShimmerResults(BinaryReader reader) {
			int count = reader.ReadInt16();
			List<IShimmerResult> results = new(count);

			for (int i = 0; i < count; i++) {
				byte type = reader.ReadByte();
				IShimmerResult result = type switch {
					0 => default(TransformItem),
					1 => default(CoinLuck),
					2 => default(NPCSpawn),
					3 => default(Decraft),
					_ => throw new ArgumentException($"Invalid shimmer result net type \"{type}\"")
				};

				results.Add(result.Receive(reader));
			}

			return results;
		}

		public static int GetDecraftAmount(int type, int stack) {
			DecraftAmountStackOverride = stack;

			int result = ContentSamples.ItemsByType[type].GetDecraftAmount();

			DecraftAmountStackOverride = null;

			return result;
		}

		/*
		[ThreadStatic]
		internal static bool IgnoreMakeNPC;

		public static bool CanShimmerIgnoreMakeNPC(this Item item) {
			IgnoreMakeNPC = true;

			bool result = item.CanShimmer();

			IgnoreMakeNPC = false;

			return result;
		}
		*/

		// Hidden functions from Item
		private static Func<Item, int> _getShimmerEquivalentType;
		public static int GetShimmerEquivalentType(this Item item) {
			static Func<Item, int> MakeFunction() {
				// Generate a System.Linq.Expression delegate that takes an Item as a parameter, calls Terraria.Item.GetShimmerEquivalentType(), and returns the result
				// This is done to avoid using reflection, which is slow
				var itemParameter = Expression.Parameter(typeof(Item), "item");
				var call = Expression.Call(itemParameter, typeof(Item).GetMethod("GetShimmerEquivalentType", BindingFlags.NonPublic | BindingFlags.Instance));
				var lambda = Expression.Lambda<Func<Item, int>>(call, itemParameter);
				return lambda.Compile();
			}

			return (_getShimmerEquivalentType ??= MakeFunction())(item);
		}

		public static int GetShimmerEquivalentType(int type) {
			return ContentSamples.ItemsByType[type].GetShimmerEquivalentType();
		}

		[ThreadStatic]
		internal static int? DecraftAmountStackOverride;

		private static Func<Item, int> _getDecraftAmount;
		public static int GetDecraftAmount(this Item item) {
			static Func<Item, int> MakeFunction() {
				// Generate a System.Ling.Expression delegate that takes an Item as a parameter, calls Terraria.Item.GetDecraftAmount(), and returns the result
				// This is done to avoid using reflection, which is slow
				var itemParameter = Expression.Parameter(typeof(Item), "item");
				var call = Expression.Call(itemParameter, typeof(Item).GetMethod("GetDecraftAmount", BindingFlags.NonPublic | BindingFlags.Instance));
				var lambda = Expression.Lambda<Func<Item, int>>(call, itemParameter);
				return lambda.Compile();
			}

			return (_getDecraftAmount ??= MakeFunction())(item);
		}

		private static Func<Player, float> _calculateCoinLuck;
		public static float CalculateCoinLuck(this Player player, float forcedCoinLuckValue) {
			static Func<Player, float> MakeFunction() {
				// Generate a System.Linq.Expression delegate that takes a Player as a parameter, calls Terraria.Player.CalculateCoinLuck(), and returns the result
				// This is done to avoid using reflection, which is slow
				var playerParameter = Expression.Parameter(typeof(Player), "player");
				var call = Expression.Call(playerParameter, typeof(Player).GetMethod("CalculateCoinLuck", BindingFlags.NonPublic | BindingFlags.Instance));
				var lambda = Expression.Lambda<Func<Player, float>>(call, playerParameter);
				return lambda.Compile();
			}

			using (ObjectSwitch.Create(ref player.coinLuck, forcedCoinLuckValue))
				return (_calculateCoinLuck ??= MakeFunction())(player);
		}

		private static Action<NPC> _getShimmered;
		public static void GetShimmered(this NPC npc) {
			static Action<NPC> MakeFunction() {
				// Generate a System.Linq.Expression delegate that takes an NPC as a parameter, calls Terraria.NPC.GetShimmered(), and returns the result
				// This is done to avoid using reflection, which is slow
				var npcParameter = Expression.Parameter(typeof(NPC), "npc");
				var call = Expression.Call(npcParameter, typeof(NPC).GetMethod("GetShimmered", BindingFlags.NonPublic | BindingFlags.Instance));
				var lambda = Expression.Lambda<Action<NPC>>(call, npcParameter);
				return lambda.Compile();
			}

			(_getShimmered ??= MakeFunction())(npc);
		}
	}
}
