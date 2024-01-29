using MagicStorage.Common.Systems;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Modules {
	internal class UseInventoryModule : EnvironmentModule {
		private int[] types = new int[58];
		private int[] stacks = new int[58];

		public override IEnumerable<Item> GetAdditionalItems(EnvironmentSandbox sandbox) => sandbox.player.inventory.Take(58);

		public override void PreUpdateUI() {
			Item[] inv = Main.LocalPlayer.inventory;

			bool needRefresh = false;
			HashSet<int> typesToUpdate = new();

			for (int i = 0; i < 58; i++) {
				Item item = inv[i];

				if (types[i] != item.type) {
					typesToUpdate.Add(types[i]);
					types[i] = item.type;
					needRefresh = true;
					typesToUpdate.Add(item.type);
				}

				if (stacks[i] != item.stack) {
					stacks[i] = item.stack;
					needRefresh = true;
					typesToUpdate.Add(item.stack);
				}
			}

			if (needRefresh) {
				if (MagicUI.IsCraftingUIOpen()) {
					MagicUI.SetRefresh();
					CraftingGUI.SetNextDefaultRecipeCollectionToRefresh(typesToUpdate);
				} else if (MagicUI.IsDecraftingUIOpen()) {
					MagicUI.SetRefresh();
					DecraftingGUI.SetNextDefaultItemCollectionToRefresh(typesToUpdate);
				}
			}
		}
	}
}
