using System.Collections.Generic;
using System;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using System.Linq;
using MagicStorage.Components;
using System.Runtime.CompilerServices;

namespace MagicStorage {
	partial class CraftingGUI {
		internal static readonly List<ItemData> blockStorageItems = new();

		[ThreadStatic]
		internal static bool _simulatingCrafts;
		/// <summary>
		/// Whether crafting simulations are currently being performed.  Use of this property is encouraged if a recipe does more than spawn items on the player.
		/// </summary>
		public static bool SimulatingCrafts {
			[MethodImpl(MethodImplOptions.NoInlining)]
			get => _simulatingCrafts;
		}

		private static CraftingContext InitCraftingContext(int toCraft) {
			var sourceItems = storageItems.Where(item => !blockStorageItems.Contains(new ItemData(item))).ToList();
			var availableItems = sourceItems.Select(item => item.Clone()).ToList();
			var fromModule = storageItemsFromModules.Where((_, n) => !blockStorageItems.Contains(new ItemData(storageItems[n]))).ToList();
			List<Item> toWithdraw = new(), results = new();

			TEStorageHeart heart = GetHeart();

			EnvironmentSandbox sandbox = new(Main.LocalPlayer, heart);

			return new CraftingContext() {
				sourceItems = sourceItems,
				availableItems = availableItems,
				toWithdraw = toWithdraw,
				results = results,
				itemCounts = GetItemCountsWithBlockedItemsRemoved(),
				sandbox = sandbox,
				consumedItemsFromModules = new(),
				fromModule = fromModule,
				modules = heart?.GetModules() ?? Array.Empty<EnvironmentModule>(),
				toCraft = toCraft
			};
		}

		private static bool CanConsumeItem(CraftingContext context, Item reqItem, List<Item> origWithdraw, List<Item> origResults, List<Item> origFromModule, out bool wasAvailable, out int stackConsumed, bool checkRecipeGroup = true) {
			wasAvailable = true;

			stackConsumed = reqItem.stack;

			RecipeLoader.ConsumeItem(selectedRecipe, reqItem.type, ref stackConsumed);

			foreach (EnvironmentModule module in context.modules)
				module.ConsumeItemForRecipe(context.sandbox, selectedRecipe, reqItem.type, ref stackConsumed);

			if (stackConsumed <= 0)
				return false;

			int stack = stackConsumed;
			bool consumeSucceeded = AttemptToConsumeItem(context, reqItem.type, ref stack, checkRecipeGroup);

			if (stack > 0 || !consumeSucceeded) {
				context.results.Clear();
				context.results.AddRange(origResults);

				context.toWithdraw.Clear();
				context.toWithdraw.AddRange(origWithdraw);

				context.consumedItemsFromModules.Clear();
				context.consumedItemsFromModules.AddRange(origFromModule);

				wasAvailable = false;
				return false;
			}

			return true;
		}

		private static bool AttemptToConsumeItem(CraftingContext context, int reqType, ref int stack, bool checkRecipeGroup = true) {
			return CheckContextItemCollection(context, context.results, reqType, ref stack, null, checkRecipeGroup)
				|| CheckContextItemCollection(context, GetAvailableItems(context), reqType, ref stack, OnAvailableItemConsumed, checkRecipeGroup)
				|| CheckContextItemCollection(context, GetModuleItems(context), reqType, ref stack, OnModuleItemConsumed, checkRecipeGroup);
		}

		private static IEnumerable<Item> GetAvailableItems(CraftingContext context) {
			for (int i = 0; i < context.sourceItems.Count; i++) {
				if (!context.fromModule[i])
					yield return context.availableItems[i];
			}
		}

		private static void OnAvailableItemConsumed(CraftingContext context, int index, Item tryItem, int stackToConsume) {
			if (!context.simulation) {
				Item consumed = tryItem.Clone();
				consumed.stack = stackToConsume;

				context.toWithdraw.Add(consumed);
			}
		}

		private static IEnumerable<Item> GetModuleItems(CraftingContext context) {
			for (int i = 0; i < context.sourceItems.Count; i++) {
				if (context.fromModule[i])
					yield return context.sourceItems[i];
			}
		}

		private static void OnModuleItemConsumed(CraftingContext context, int index, Item tryItem, int stackToConsume) {
			if (!context.simulation) {
				Item consumed = tryItem.Clone();
				consumed.stack = stackToConsume;

				context.consumedItemsFromModules.Add(consumed);
			}
		}

		private static bool CheckContextItemCollection(CraftingContext context, IEnumerable<Item> items, int reqType, ref int stack, Action<CraftingContext, int, Item, int> onItemConsumed, bool checkRecipeGroup = true) {
			int index = 0;
			foreach (Item tryItem in !context.simulation ? items : items.Select(static i => new Item(i.type, i.stack))) {
				// Recursion crafting can cause the item stack to be zero
				if (tryItem.stack <= 0)
					continue;

				if (reqType == tryItem.type || (checkRecipeGroup && RecipeGroupMatch(selectedRecipe, tryItem.type, reqType))) {
					int stackToConsume;

					if (tryItem.stack > stack) {
						stackToConsume = stack;
						stack = 0;
					} else {
						stackToConsume = tryItem.stack;
						stack -= tryItem.stack;
					}

					if (!context.simulation)
						OnConsumeItemForRecipe_Obsolete(context, tryItem, stackToConsume);

					onItemConsumed?.Invoke(context, index, tryItem, stackToConsume);

					tryItem.stack -= stackToConsume;

					if (tryItem.stack <= 0)
						tryItem.type = ItemID.None;

					if (stack <= 0)
						break;
				}

				index++;
			}

			return stack <= 0;
		}

		[Obsolete]
		private static void OnConsumeItemForRecipe_Obsolete(CraftingContext context, Item tryItem, int stackToConsume) {
			foreach (var module in context.modules)
				module.OnConsumeItemForRecipe(context.sandbox, tryItem, stackToConsume);
		}

		private static List<Item> CompactItemList(List<Item> items) {
			List<Item> compacted = new();

			for (int i = 0; i < items.Count; i++) {
				Item item = items[i];

				if (item.IsAir)
					continue;

				bool fullyCompacted = false;
				for (int j = 0; j < compacted.Count; j++) {
					Item existing = compacted[j];

					if (ItemCombining.CanCombineItems(item, existing)) {
						if (existing.stack + item.stack <= existing.maxStack) {
							Utility.CallOnStackHooks(existing, item, item.stack);

							existing.stack += item.stack;
							item.stack = 0;
							fullyCompacted = true;
						} else {
							int diff = existing.maxStack - existing.stack;

							Utility.CallOnStackHooks(existing, item, diff);

							existing.stack = existing.maxStack;
							item.stack -= diff;
						}

						break;
					}
				}

				if (item.IsAir)
					continue;

				if (!fullyCompacted)
					compacted.Add(item);
			}

			return compacted;
		}
	}
}
