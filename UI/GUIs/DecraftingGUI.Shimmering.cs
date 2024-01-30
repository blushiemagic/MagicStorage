using MagicStorage.Common.Systems;
using Terraria.ID;
using Terraria;
using System;
using MagicStorage.Common.Systems.Shimmering;
using MagicStorage.Components;
using Terraria.DataStructures;
using System.Collections.Generic;

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
			toShimmer = Math.Min(toShimmer, CraftingGUI.itemCounts[selectedItem]);

			if (toShimmer != origShimmerRequest)
				NetHelper.Report(false, $"Shimmer amount reduced to {toShimmer}");

			if (toShimmer <= 0) {
				NetHelper.Report(false, "Amount to shimmer was less than 1, aborting");
				return;
			}

			ShimmerContext context = new() {
				toShimmer = toShimmer,
				storage = new StorageIntermediary(heart),
				results = new List<IShimmerResult>()
			};

			int target = toShimmer;

			CraftingGUI.ExecuteInCraftingGuiEnvironment(() => Shimmer_DoShimmering(context));

			NetHelper.Report(true, $"Shimmered {target - context.toShimmer} items");

			if (target == context.toShimmer) {
				//Could not shimmer anything, bail
				return;
			}

			NetHelper.Report(true, "Compacting item results list...");

			var toWithdraw = CraftingGUI.CompactItemList(context.storage.toWithdraw);
			
			var toDeposit = CraftingGUI.CompactItemList(context.storage.toDeposit);

			if (Main.netMode == NetmodeID.SinglePlayer) {
				NetHelper.Report(true, "Spawning excess item results on player...");

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
