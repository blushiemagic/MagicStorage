using MagicStorage.Common.Systems.Debugging;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.Shimmering {
	public static class ShimmerMetrics {
		private class TransformationContext {
			public int SourceItem { get; set; }
			public Func<int> GetResultItem { get; set; }
			public List<Condition> Requirements { get; }
			public Mod ContextSource { get; set; }

			public TransformationContext(int sourceItem, Func<int> getResultItem, Condition requirement, Mod contextSource) {
				SourceItem = sourceItem;
				GetResultItem = getResultItem;
				Requirements = requirement is null ? [] : [ requirement ];
				ContextSource = contextSource;
			}

			public TransformationContext(int sourceItem, Func<int> getResultItem, IEnumerable<Condition> requirements, Mod contextSource) {
				SourceItem = sourceItem;
				GetResultItem = getResultItem;
				Requirements = requirements is null ? [] : [.. requirements];
				ContextSource = contextSource;
			}

			public bool CanTransform() {
				foreach (var requirement in Requirements) {
					if (!requirement.IsMet())
						return false;
				}

				return true;
			}
		}

		private static readonly List<TransformationContext> _transformations = [
			new(ItemID.RodofDiscord, static () => ItemID.RodOfHarmony, [ Condition.DownedMoonLord ], null),
			new(ItemID.Clentaminator, static () => ItemID.Clentaminator2, [ Condition.DownedMoonLord ], null),
			new(ItemID.BottomlessBucket, static () => ItemID.BottomlessShimmerBucket, [ Condition.DownedMoonLord ], null),
			new(ItemID.BottomlessShimmerBucket, static () => ItemID.BottomlessBucket, [ Condition.DownedMoonLord ], null),
			new(ItemID.LunarBrick, SelectLunarBrickTransformation, (Condition)null, null),
		];

		private static int SelectLunarBrickTransformation() {
			return Main.GetMoonPhase() switch {
				MoonPhase.QuarterAtRight => ItemID.StarRoyaleBrick,
				MoonPhase.HalfAtRight => ItemID.CryocoreBrick,
				MoonPhase.ThreeQuartersAtRight => ItemID.CosmicEmberBrick,
				MoonPhase.Full => ItemID.HeavenforgeBrick,
				MoonPhase.ThreeQuartersAtLeft => ItemID.LunarRustBrick,
				MoonPhase.HalfAtLeft => ItemID.AstraBrick,
				MoonPhase.QuarterAtLeft => ItemID.DarkCelestialBrick,
				_ => ItemID.None
			};
		}

		public static void AddTransformation(Mod caller, int targetItem, int resultItem, Condition requirement) {
			// Local capturing
			int item = resultItem;
			AddTransformation(caller, targetItem, () => item, requirement);
		}

		public static void AddTransformation(Mod caller, int targetItem, int resultItem, IEnumerable<Condition> requirements) {
			// Local capturing
			int item = resultItem;
			AddTransformation(caller, targetItem, () => item, requirements);
		}

		public static void AddTransformation(Mod caller, int targetItem, Func<int> getResultItem, Condition requirement) {
			TransformationContext newContext = new(targetItem, getResultItem, requirement, caller);

			if (FindContextAndIndex(targetItem, out int index) is { } oldContext) {
				LogTransformationChange(caller, oldContext, action: "overwritten");
				_transformations[index] = newContext;
			} else
				_transformations.Add(newContext);
		}

		public static void AddTransformation(Mod caller, int targetItem, Func<int> getResultItem, IEnumerable<Condition> requirements) {
			TransformationContext newContext = new(targetItem, getResultItem, requirements, caller);

			if (FindContextAndIndex(targetItem, out int index) is { } oldContext) {
				LogTransformationChange(caller, oldContext, action: "overwritten");
				_transformations[index] = newContext;
			} else
				_transformations.Add(newContext);
		}

		public static void AddTransformationRequirement(Mod caller, int targetItem, Condition requirement) {
			if (FindContext(targetItem) is not TransformationContext context)
				throw new ArgumentException($"No transformation exists for the specified target item: {Utility.ItemIdentifier(targetItem)}", nameof(targetItem));

			if (requirement is not null && !context.Requirements.Contains(requirement))
				context.Requirements.Add(requirement);
		}

		public static void AddTransformationRequirements(Mod caller, int targetItem, IEnumerable<Condition> requirements) {
			if (FindContext(targetItem) is not TransformationContext context)
				throw new ArgumentException($"No transformation exists for the specified target item: {Utility.ItemIdentifier(targetItem)}", nameof(targetItem));

			foreach (var requirement in requirements) {
				if (requirement is not null && !context.Requirements.Contains(requirement))
					context.Requirements.Add(requirement);
			}
		}

		public static void SetTransformationResult(Mod caller, int targetItem, int resultItem) {
			// Local capturing
			int item = resultItem;
			SetTransformationResult(caller, targetItem, () => item);
		}

		public static void SetTransformationResult(Mod caller, int targetItem, Func<int> getResultItem) {
			if (FindContext(targetItem) is not TransformationContext context)
				throw new ArgumentException($"No transformation exists for the specified target item: {Utility.ItemIdentifier(targetItem)}", nameof(targetItem));

			LogTransformationChange(caller, context, action: "modified");
			context.ContextSource = caller;
			context.GetResultItem = getResultItem;
		}

		public static void SetTransformationRequirement(Mod caller, int targetItem, Condition requirement) {
			if (FindContext(targetItem) is not TransformationContext context)
				throw new ArgumentException($"No transformation exists for the specified target item: {Utility.ItemIdentifier(targetItem)}", nameof(targetItem));

			LogTransformationChange(caller, context, action: "modified");
			context.ContextSource = caller;
			context.Requirements.Clear();
			if (requirement is not null)
				context.Requirements.Add(requirement);
		}

		public static void SetTransformationRequirements(Mod caller, int targetItem, IEnumerable<Condition> requirements) {
			if (FindContext(targetItem) is not TransformationContext context)
				throw new ArgumentException($"No transformation exists for the specified target item: {Utility.ItemIdentifier(targetItem)}", nameof(targetItem));

			LogTransformationChange(caller, context, action: "modified");
			context.ContextSource = caller;
			context.Requirements.Clear();
			if (requirements is not null)
				context.Requirements.AddRange(requirements);
		}

		public static void RemoveTransformation(Mod caller, int targetItem) {
			if (FindContextAndIndex(targetItem, out int index) is null) {
				MagicStorageMod.Instance.Logger.Warn($"Mod \"{caller.Name}\" attempted to remove a shimmer transformation for item \"{Utility.ItemIdentifier(targetItem)}\" that does not exist.");
				return;
			}

			LogTransformationChange(caller, _transformations[index], action: "removed");
			_transformations.RemoveAt(index);
		}

		private static TransformationContext FindContext(int targetItem) {
			foreach (var transformation in _transformations) {
				if (transformation.SourceItem == targetItem)
					return transformation;
			}

			return null;
		}

		private static TransformationContext FindContextAndIndex(int targetItem, out int index) {
			for (int i = 0; i < _transformations.Count; i++) {
				var transformation = _transformations[i];

				if (transformation.SourceItem == targetItem) {
					index = i;
					return transformation;
				}
			}

			index = -1;
			return null;
		}

		private static TransformationContext FindAvailableContext(int targetItem) {
			foreach (var transformation in _transformations) {
				if (transformation.SourceItem == targetItem) {
					if (transformation.CanTransform()) 
						return transformation;
					break;
				}
			}

			return null;
		}

		public static bool HasRegisteredTransformation(int targetItem) => FindContext(targetItem) is not null;

		public static bool HasSatisfiedTransformation(int targetItem) => FindAvailableContext(targetItem) is not null;

		private static void LogTransformationChange(Mod caller, TransformationContext context, string action) {
			string originalOwner = context.ContextSource?.Name ?? "Terraria";
			MagicStorageMod.Instance.Logger.Info($"Shimmer transformation for item \"{Utility.ItemIdentifier(context.SourceItem)}\" from mod \"{originalOwner}\" was {action} by mod \"{caller.Name}\"");
		}

		public static IEnumerable<Condition> GetTransformConditions(int item) {
			// FIX: v0.7.1 - Lunar Brick only has differing transmutations depending on the moon cycle
			/*
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
			*/

			item = GetShimmerEquivalentType(item);

			if (FindContext(item) is TransformationContext context) {
				foreach (var requirement in context.Requirements)
					yield return requirement;
			}

			// NOTE: The calling site for GetTransformCondition() also enumerates through Recipe.DecraftConditions
			int recipeIndex = ShimmerTransforms.GetDecraftingRecipeIndex(item);

			if (recipeIndex >= 0) {
				if (ShimmerTransforms.RecipeSets.PostSkeletron[recipeIndex])
					yield return Condition.DownedSkeletron;

				if (ShimmerTransforms.RecipeSets.PostGolem[recipeIndex])
					yield return Condition.DownedGolem;
			}
		}

		internal static int TransformItem(int item) {
			bool hasTransmutations = false;

			foreach (var transmutation in _transformations) {
				if (transmutation.SourceItem == item) {
					hasTransmutations = true;

					if (transmutation.CanTransform())
						return Math.Max(ItemID.None, transmutation.GetResultItem());
				}
			}

			if (hasTransmutations) {
				// Defined transmutations failed to transmute the item
				return ItemID.None;
			}

			if (ContentSamples.ItemsByType[item].createTile == TileID.MusicBoxes)
				return ItemID.MusicBox;

			return Math.Max(ItemID.None, ItemID.Sets.ShimmerTransformToItem[item]);
		}

		public static IShimmerResult AttemptItemTransmutation(Item item, StorageIntermediary storage, bool net) {
			using var debugging = DebugMessage.CreateIf(DebugControls.Names.ShimmeringRequests);

			if (debugging.IsDebugging) {
				debugging
					.Report(false, new NetmodeContextMessage(
						ChatMessage: new("Attempting to shimmer {0} ...", item.ToChatTag()),
						ConsoleOrLogMessage: new("Attempting to shimmer item \"{0}\"...", item.PrefixedIdentifierAndStack())
					))
					.Indent();
			}

			var info = MagicCache.ShimmerInfos[item.type];
			var result = info.GetResult();

			if (debugging.IsDebugging) {
				debugging
					.Report(false, new NetmodeContextMessage(
						ChatMessage: new("Iconic item: {0}", Utility.GetItemChatTag(info.iconicItem, 1, 0)),
						ConsoleOrLogMessage: new("Iconic item: {0}", Utility.ItemIdentifier(info.iconicItem))
					))
					.Report(false, "Shimmer action category: {0}", result?.GetType().Name ?? "<null>");
			}

			using (var debuggingHook = debugging.CreateIf(DebugControls.Names.ShimmeringRequestsVerbose)) {
				if (debuggingHook.IsDebugging) {
					debuggingHook
						.Report(false, "Details:")
						.Indent();
				}

				result?.OnShimmer(item, info.iconicItem, storage, net);
			}

			if (item.stack <= 0)
				item.TurnToAir();

			if (debugging.IsDebugging)
				debugging.Report(false, "Item after shimmer attempt: {0}", item.PrefixedIdentifierAndStack());

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
			using var debugging = DebugMessage.CreateIf(DebugControls.Names.ShimmeringRequestsVerbose);

			// Recipe is guaranteed to be available at this point
			// The following logic is derived from Item::GetShimmered()
			int amount = GetDecraftAmount(iconicItemType, stack);
			if (amount <= 0) {
				if (debugging.IsDebugging)
					debugging.Report(false, "Failed.  Source item stack was too small.");

				return Array.Empty<DecraftResult>();
			}

			IEnumerable<Item> items = recipe.customShimmerResults is { } list ? list : recipe.requiredItem;

			List<DecraftResult> results = new();

			foreach (Item item in items) {
				int ingredientStack = amount * item.stack;

				if (applyIngredientReductionRules)
					RecipeLoader.ConsumeIngredient(recipe, item.type, ref ingredientStack, isDecrafting: true);

				if (ingredientStack <= 0) {
					if (debugging.IsDebugging)
						debugging.Report(false, new NetmodeContextMessage(
							ChatMessage: new("Result {0} was skipped", item.ToChatTag()),
							ConsoleOrLogMessage: new("Result \"{0}\" was skipped", item.PrefixedIdentifierAndStack())
						));

					continue;
				}

				if (debugging.IsDebugging) {
					debugging.Report(false, new NetmodeContextMessage(
						ChatMessage: new("Spawned {0}", Utility.GetItemChatTag(item.type, ingredientStack, item.prefix)),
						ConsoleOrLogMessage: new("Spawned \"{0}\"", item.PrefixedIdentifierWithStack(ingredientStack))
					));
				}

				results.Add(new DecraftResult(item.type, ingredientStack));
			}

			stack -= amount * recipe.createItem.stack;

			return results;
		}

		internal static void SendShimmerResults(BinaryWriter writer, List<IShimmerResult> results) {
			NetHelper.Report(true, $"[ShimmerMetrics] Sending {results.Count} shimmer results...");

			writer.Write((short)results.Count);
			foreach (var result in results) {
				byte type = result switch {
					TransformItem _ => 0,
					CoinLuck _ => 1,
					NPCSpawn _ => 2,
					Decraft _ => 3,
					_ => throw new ArgumentException($"Invalid shimmer result type \"{result?.GetType().ToString() ?? "null"}\"")
				};

				NetHelper.Report(false, $"  {result.GetType().Name}");

				writer.Write(type);
				result.Send(writer);
			}
		}

		internal static List<IShimmerResult> ReceiveShimmerResults(BinaryReader reader) {
			int count = reader.ReadInt16();

			NetHelper.Report(true, $"[ShimmerMetrics] Receiving {count} shimmer results...");

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

				NetHelper.Report(false, $"  {result.GetType().Name}");

				results.Add(result.Receive(reader));
			}

			return results;
		}

		[ThreadStatic]
		internal static int? DecraftAmountStackOverride;

		public static int GetDecraftAmount(int type, int stack) {
			DecraftAmountStackOverride = stack;

			int result = ContentSamples.ItemsByType[type].FindDecraftAmount();

			DecraftAmountStackOverride = null;

			return result;
		}

		public static int GetShimmerEquivalentType(int type) {
			return ContentSamples.ItemsByType[type].GetShimmerEquivalentType();
		}

		public static float CalculateCoinLuck(this Player player, float forcedCoinLuckValue) {
			using (ObjectSwitch.Create(ref player.coinLuck, forcedCoinLuckValue))
				return player.CalculateCoinLuck();
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
	}
}
