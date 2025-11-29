using MagicStorage.Common.Systems;
using Terraria.ID;
using Terraria;
using System;
using MagicStorage.Common.Systems.Shimmering;
using MagicStorage.Components;
using Terraria.DataStructures;
using System.Collections.Generic;
using MagicStorage.Common;

namespace MagicStorage {
	partial class DecraftingGUI {
		private class ShimmerContext {
			public int toShimmer;
			public StorageIntermediary storage;
			public List<IShimmerResult> results;
		}

		/// <summary>
		/// Attempts to shimmer a certain amount of items from the currently assigned Aether Interface
		/// </summary>
		/// <param name="toShimmer">How many items should be shimmered</param>
		public static void Shimmer(int toShimmer) {
			TEStorageHeart heart = GetHeart();
			if (heart is null)
				return;  // Bail

			NetHelper.Report(true, $"Attempting to shimmer {toShimmer} {Lang.GetItemNameValue(selectedItem)}");

			// Additional safeguard against absurdly high craft targets
			int origShimmerRequest = toShimmer;
			toShimmer = Math.Min(toShimmer, CraftingGUI.GetCurrentInventory().GetIngredientQuantity(selectedItem));

			if (toShimmer != origShimmerRequest)
				NetHelper.Report(false, $"Shimmer amount reduced to {toShimmer}");

			if (toShimmer <= 0) {
				NetHelper.Report(false, "Amount to shimmer was less than 1, aborting");
				return;
			}

			ShimmerContext context = new() {
				toShimmer = toShimmer,
				storage = new StorageIntermediary(heart) { IgnoreContentChanges = false },
				results = new List<IShimmerResult>()
			};

			int target = toShimmer;

			CraftingGUI.ExecuteInCraftingGuiEnvironment(context, Shimmer_DoShimmering);

			NetHelper.Report(true, $"Shimmered {target - context.toShimmer} items");

			if (target == context.toShimmer) {
				//Could not shimmer anything, bail
				return;
			}

			// At this point, the items to withdraw consist of the shimmered item(s)
			// Attempt to actually consume the items by hijacking the CraftingGUI crafting logic ("recipe" and "toCraft" are ignored here)
			List<Item> consumedItems = [];

			var fakeCraftContext = CraftingGUI.InitCraftingContext(null, 0);

			fakeCraftContext.simulation = true;

			bool success = true;
			foreach (Item shimmered in context.storage.toWithdraw) {
				int stack = shimmered.stack;
				if (!CraftingGUI.AttemptToConsumeItem(fakeCraftContext, shimmered.type, ref stack, checkRecipeGroup: false)) {
					success = false;
					break;
				}
			}

			if (!success) {
				NetHelper.Report(false, "Item requirement for shimmering could not be fulfilled, aborting");
				return;
			}

			fakeCraftContext.simulation = false;

			// Actually consume the items now
			// It would be preferable to not call AttemptToConsumeItem again, but I can't be bothered to make a better implementation right now
			foreach (Item shimmered in context.storage.toWithdraw) {
				int stack = shimmered.stack;
				CraftingGUI.AttemptToConsumeItem(fakeCraftContext, shimmered.type, ref stack, checkRecipeGroup: false);
			}

			NetHelper.Report(true, "Compacting item results list...");

			// Overwrite the withdraw list with the actual items to withdraw from storage
			// Items withdrawn from Configuration Interface modules are stored in a separate list
			//   because the server likely doesn't know player-specific information like their
			//   inventory contents
			context.storage.toWithdraw.Clear();
			context.storage.toWithdraw.AddRange(fakeCraftContext.toWithdraw);

			var toWithdraw = CraftingGUI.CompactItemList(context.storage.toWithdraw);

			// The same doesn't need to be done for shimmering
			// The only thing relevant for result items is prefix rolling, which shimmering
			//   doesn't do in the first place
			
			var toDeposit = CraftingGUI.CompactItemList(context.storage.toDeposit);

			if (Main.netMode == NetmodeID.SinglePlayer) {
				NetHelper.Report(true, "Handling storage inventory changes and spawning excess results on player...");

				using(SecuritySystem.CreateAccessContext())
					foreach (Item item in CraftingGUI.HandleCraftWithdrawAndDeposit(heart, toWithdraw, toDeposit))
						Main.LocalPlayer.QuickSpawnItem(new EntitySource_TileEntity(heart), item, item.stack);

				MagicUI.SetRefresh();
			} else if (Main.netMode == NetmodeID.MultiplayerClient) {
				NetHelper.Report(true, "Sending shimmer request to server...");

				NetHelper.RequestItemShimmering(selectedItem, toShimmer, context.storage, context.results);
			}
		}

		private static void Shimmer_DoShimmering(ShimmerContext context) {
			Item shimmeringItem = new Item(selectedItem, context.toShimmer);
			bool net = Main.netMode == NetmodeID.MultiplayerClient;

			while (!shimmeringItem.IsAir) {
				var result = ShimmerMetrics.AttemptItemTransmutation(shimmeringItem, context.storage, net);

				context.toShimmer = shimmeringItem.stack;

				if (result is null) {
					// No more results, bail
					break;
				}

				context.results.Add(result);
			}
		}
	}
}
