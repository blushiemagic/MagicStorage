using MagicStorage.Common.Systems.RecurrentRecipes;
using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.ModLoader;
using Terraria;
using MagicStorage.Components;
using Terraria.DataStructures;
using Terraria.ID;
using MagicStorage.CrossMod;
using MagicStorage.Common;

namespace MagicStorage {
	partial class CraftingGUI {
		private class CraftingContext {
			public List<Item> sourceItems, availableItems, toWithdraw, results;

			public Dictionary<int, int> itemCounts;

			public List<bool> fromModule;

			public EnvironmentSandbox sandbox;

			public List<Item> consumedItemsFromModules;

			public IEnumerable<EnvironmentModule> modules;

			public int toCraft;

			public bool simulation;

			public Recipe recipe;

			public IEnumerable<Item> ConsumedItems => toWithdraw.Concat(consumedItemsFromModules);
		}

		/// <summary>
		/// Attempts to craft a certain amount of items from the currently assigned Crafting Access.
		/// </summary>
		/// <param name="toCraft">How many items should be crafted</param>
		public static void Craft(int toCraft) {
			TEStorageHeart heart = GetHeart();
			if (heart is null)
				return;  // Bail

			NetHelper.Report(true, $"Attempting to craft {toCraft} {Lang.GetItemNameValue(selectedRecipe.createItem.type)}");

			// Additional safeguard against absurdly high craft targets
			int origCraftRequest = toCraft;
			toCraft = Math.Min(toCraft, AmountCraftableForCurrentRecipe());

			if (toCraft != origCraftRequest)
				NetHelper.Report(false, $"Craft amount reduced to {toCraft}");

			if (toCraft <= 0) {
				NetHelper.Report(false, "Amount to craft was less than 1, aborting");
				return;
			}

			CraftingContext context;
			if (MagicStorageConfig.IsRecursionEnabled && selectedRecipe.HasRecursiveRecipe()) {
				// Recursive crafting uses special logic which can't just be injected into the previous logic
				context = Craft_WithRecursion(toCraft);

				if (context is null)
					return;  // Bail
			} else {
				context = InitCraftingContext(selectedRecipe, toCraft);

				int target = toCraft;

				ExecuteInCraftingGuiEnvironment(() => Craft_DoStandardCraft(context));

				NetHelper.Report(true, $"Crafted {target - context.toCraft} items");

				if (target == context.toCraft) {
					//Could not craft anything, bail
					return;
				}
			}

			NetHelper.Report(true, "Compacting results list...");

			context.toWithdraw = CompactItemList(context.toWithdraw);
			
			context.results = CompactItemList(context.results);

			if (Main.netMode == NetmodeID.SinglePlayer) {
				NetHelper.Report(true, "Spawning excess results on player...");

				foreach (Item item in HandleCraftWithdrawAndDeposit(heart, context.toWithdraw, context.results))
					Main.LocalPlayer.QuickSpawnItem(new EntitySource_TileEntity(heart), item, item.stack);

				StorageGUI.SetRefresh();
			} else if (Main.netMode == NetmodeID.MultiplayerClient) {
				NetHelper.Report(true, "Sending craft results to server...");

				NetHelper.SendCraftRequest(heart.Position, context.toWithdraw, context.results);
			}
		}

		private static void Craft_DoStandardCraft(CraftingContext context) {
			//Do lazy crafting first (batch loads of ingredients into one "craft"), then do normal crafting
			if (!AttemptLazyBatchCraft(context)) {
				NetHelper.Report(false, "Batch craft operation failed.  Attempting repeated crafting of a single result.");

				AttemptCraft(AttemptSingleCraft, context);
			}
		}

		private static CraftingContext Craft_WithRecursion(int toCraft) {
			// Unlike normal crafting, the crafting tree has to be respected
			// This means that simple IsAvailable and AmountCraftable checks would just slow it down
			// Hence, the logic here will just assume that it's craftable and just ignore branches in the recursion tree that aren't available or are already satisfied
			if (!selectedRecipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe))
				throw new InvalidOperationException("Recipe object did not have a RecursiveRecipe object assigned to it");

			if (toCraft <= 0)
				return null;  // Bail

			CraftingContext context = InitCraftingContext(recursiveRecipe.original, toCraft);

			NetHelper.Report(true, "Attempting recurrent crafting...");

			// Local capturing
			var ctx = context;
			ExecuteInCraftingGuiEnvironment(() => Craft_DoRecursionCraft(ctx));

			// Sanity check
			selectedRecipe = recursiveRecipe.original;

			return context;
		}

		private static void Craft_DoRecursionCraft(CraftingContext ctx) {
			var simulation = GetCraftingSimulationForCurrentRecipe();

			if (simulation.AmountCrafted <= 0) {
				NetHelper.Report(false, "Crafting simulation resulted in zero crafts, aborting");
				return;
			}

			// At this point, the amount to craft has already been clamped by the max amount possible
			// Hence, just consume the items
			List<Item> consumedItems = new();

			ctx.simulation = true;

			foreach (var m in simulation.RequiredMaterials) {
				if (m.Stack <= 0)
					continue;  // Safeguard: material was already "used up" by higher up recipes

				var material = m;

				List<Item> origWithdraw = new(ctx.toWithdraw);
				List<Item> origResults = new(ctx.results);
				List<Item> origFromModule = new(ctx.consumedItemsFromModules);

				bool skipItemConsumption = false;

				foreach (int type in material.GetValidItems()) {
					// Bug fix: only consume up to the amount of materials needed
					if (!ctx.itemCounts.TryGetValue(type, out int quantity) || quantity <= 0) {
						// Item was not present
						continue;
					}

					int possibleStack = Math.Min(material.Stack, quantity);

					Item item = new Item(type, possibleStack);

					if (!CanConsumeItem(ctx, item, origWithdraw, origResults, origFromModule, out bool wasAvailable, out int stackConsumed, checkRecipeGroup: false)) {
						if (wasAvailable) {
							NetHelper.Report(false, $"Skipping consumption of item \"{Lang.GetItemNameValue(item.type)}\"");
							skipItemConsumption = true;
							break;
						}
					} else {
						// Consume the item
						material.UpdateStack(-stackConsumed);
						item.stack = stackConsumed;
						consumedItems.Add(item);

						ctx.itemCounts[type] -= stackConsumed;

						if (material.Stack <= 0)
							break;
					}
				}

				if (!skipItemConsumption && material.Stack > 0) {
					NetHelper.Report(false, $"Material requirement \"{Lang.GetItemNameValue(material.GetValidItems().First())}\" could not be met, aborting");
					return;
				}
			}

			ctx.simulation = false;

			NetHelper.Report(true, $"Recursion crafting used the following materials:\n  {
				(consumedItems.Count > 0
					? string.Join("\n  ", consumedItems.Select(static i => $"{i.stack} {Lang.GetItemNameValue(i.type)}"))
					: "none")
				}");

			// Actually consume the items
			foreach (Item item in consumedItems) {
				int stack = item.stack;
				AttemptToConsumeItem(ctx, item.type, ref stack, checkRecipeGroup: false);
			}

			// Run the "on craft" logic for the final result, but with the SimulatingCrafts flag disabled this time
			// (It should be false by this point, but it's forced back to false as a sanity check)
			_simulatingCrafts = false;

			// Inform other mods that the items were crafted
			using (FlagSwitch.ToggleTrue(ref CatchDroppedItems)) {
				DroppedItems.Clear();
				foreach (Item item in simulation.ExcessResults.Where(static i => i.Stack > 0).Select(static i => new Item(i.type, i.Stack, i.prefix))) {
					ctx.results.Add(item);
					RecipeLoader.OnCraft(item, ctx.recipe, consumedItems, new Item());
				}
			}

			NetHelper.Report(true, $"Success! Crafted {simulation.AmountCrafted} items and {simulation.ExcessResults.Count - 1} extra item types");
		}

		private static void AttemptCraft(Func<CraftingContext, bool> func, CraftingContext context) {
			// NOTE: [ThreadStatic] only runs the field initializer on one thread
			DroppedItems ??= new();

			List<Item> consumedItems = new();

			while (context.toCraft > 0) {
				if (!func(context))
					break;  // Could not craft any more items

				Item resultItem = selectedRecipe.createItem.Clone();
				context.toCraft -= resultItem.stack;

				resultItem.Prefix(-1);
				context.results.Add(resultItem);

				consumedItems = context.ConsumedItems.ToList();

				foreach (EnvironmentModule module in context.modules)
					module.OnConsumeItemsForRecipe(context.sandbox, selectedRecipe, consumedItems);

				// Inform other mods that the items were crafted
				using (FlagSwitch.ToggleTrue(ref CatchDroppedItems)) {
					foreach (Item item in ExtraCraftItemsSystem.GetSimulatedItemDrops(selectedRecipe)) {
						context.results.Add(item);
						RecipeLoader.OnCraft(resultItem, selectedRecipe, consumedItems, new Item());
					}

					DroppedItems.Clear();
					RecipeLoader.OnCraft(resultItem, selectedRecipe, consumedItems, new Item());
				}
			}
		}

		private static bool AttemptLazyBatchCraft(CraftingContext context) {
			NetHelper.Report(false, "Attempting batch craft operation...");

			List<Item> origResults = new(context.results);
			List<Item> origWithdraw = new(context.toWithdraw);
			List<Item> origFromModule = new(context.consumedItemsFromModules);

			//Try to batch as many "crafts" into one craft as possible
			int crafts = (int)Math.Ceiling(context.toCraft / (float)selectedRecipe.createItem.stack);

			//Skip item consumption code for recipes that have no ingredients
			if (selectedRecipe.requiredItem.Count == 0) {
				NetHelper.Report(false, "Recipe had no ingredients, skipping consumption...");
				goto SkipItemConsumption;
			}

			context.simulation = true;

			List<Item> batch = new(selectedRecipe.requiredItem.Count);

			//Reduce the number of batch crafts until this recipe can be completely batched for the number of crafts
			while (crafts > 0) {
				bool didAttemptToConsumeItem = false;

				foreach (Item reqItem in selectedRecipe.requiredItem) {
					Item clone = reqItem.Clone();
					clone.stack *= crafts;

					if (!CanConsumeItem(context, clone, origWithdraw, origResults, origFromModule, out bool wasAvailable, out int stackConsumed)) {
						if (wasAvailable) {
							NetHelper.Report(false, $"Skipping consumption of item \"{Lang.GetItemNameValue(reqItem.type)}\". (Batching {crafts} crafts)");

							// Indicate to later logic that an attempt was made
							didAttemptToConsumeItem = true;
						} else {
							// Did not have enough items
							crafts--;
							batch.Clear();
							didAttemptToConsumeItem = false;
							break;
						}
					} else {
						//Consume the item
						clone.stack = stackConsumed;
						batch.Add(clone);
					}
				}

				if (batch.Count > 0 || didAttemptToConsumeItem) {
					//Successfully batched items for the craft
					break;
				}
			}

			// Remove any empty items since they wouldn't do anything anyway
			batch.RemoveAll(i => i.stack <= 0);

			context.simulation = false;

			if (crafts <= 0) {
				//Craft batching failed
				return false;
			}

			//Consume the batched items
			foreach (Item item in batch) {
				int stack = item.stack;

				AttemptToConsumeItem(context, item.type, ref stack);
			}

			NetHelper.Report(true, $"Batch crafting used the following materials:\n  {string.Join("\n  ", batch.Select(static i => $"{i.stack} {Lang.GetItemNameValue(i.type)}"))}");

			SkipItemConsumption:

			// NOTE: [ThreadStatic] only runs the field initializer on one thread
			DroppedItems ??= new();

			//Create the resulting items
			List<Item> consumedItems = context.ConsumedItems.ToList();

			for (int i = 0; i < crafts; i++) {
				Item resultItem = selectedRecipe.createItem.Clone();
				context.toCraft -= resultItem.stack;

				resultItem.Prefix(-1);
				context.results.Add(resultItem);

				foreach (EnvironmentModule module in context.modules)
					module.OnConsumeItemsForRecipe(context.sandbox, selectedRecipe, consumedItems);

				// Inform other mods that the items were crafted
				using (FlagSwitch.ToggleTrue(ref CatchDroppedItems)) {
					foreach (Item item in ExtraCraftItemsSystem.GetSimulatedItemDrops(selectedRecipe)) {
						context.results.Add(item);
						RecipeLoader.OnCraft(resultItem, selectedRecipe, consumedItems, new Item());
					}

					DroppedItems.Clear();
					RecipeLoader.OnCraft(resultItem, selectedRecipe, consumedItems, new Item());
				}
			}

			NetHelper.Report(false, $"Batch craft operation succeeded ({crafts} crafts batched)");

			return true;
		}

		private static bool AttemptSingleCraft(CraftingContext context) {
			List<Item> origResults = new(context.results);
			List<Item> origWithdraw = new(context.toWithdraw);
			List<Item> origFromModule = new(context.consumedItemsFromModules);

			NetHelper.Report(false, "Attempting one craft operation...");

			context.simulation = true;

			List<int> stacksConsumed = new();

			foreach (Item reqItem in selectedRecipe.requiredItem) {
				if (!CanConsumeItem(context, reqItem, origWithdraw, origResults, origFromModule, out bool wasAvailable, out int stackConsumed)) {
					if (wasAvailable)
						NetHelper.Report(false, $"Skipping consumption of item \"{Lang.GetItemNameValue(reqItem.type)}\".");
					else {
						NetHelper.Report(false, $"Required item \"{Lang.GetItemNameValue(reqItem.type)}\" was not available.");
						return false;  // Did not have enough items
					}
				} else
					NetHelper.Report(false, $"Required item \"{Lang.GetItemNameValue(reqItem.type)}\" was available.");

				stacksConsumed.Add(stackConsumed);
			}

			context.simulation = false;

			//Consume the source items as well since the craft was successful
			int consumeStackIndex = 0;
			foreach (Item reqItem in selectedRecipe.requiredItem) {
				int stack = stacksConsumed[consumeStackIndex];
				AttemptToConsumeItem(context, reqItem.type, ref stack);
				consumeStackIndex++;
			}

			NetHelper.Report(false, "Craft operation succeeded");

			return true;
		}
	}
}
