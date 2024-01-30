using MagicStorage.Common.Systems;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Terraria;

namespace MagicStorage.Modules {
	internal class UseInventoryNoFavoritesModule : EnvironmentModule {
		private int[] types = new int[58 + 40];
		private int[] stacks = new int[58 + 40];
		private bool[] favorited = new bool[58 + 40];

		public override IEnumerable<Item> GetAdditionalItems(EnvironmentSandbox sandbox) => sandbox.player.inventory.Take(58).Concat(sandbox.player.bank4.item).Where(i => !i.favorited);

		public override void PreUpdateUI() {
			Player player = Main.LocalPlayer;

			bool needRefresh = false;
			HashSet<int> typesToUpdate = new();

			CheckInventory(player.inventory, 58, 0, typesToUpdate, ref needRefresh);
			if (player.useVoidBag())
				CheckInventory(player.bank4.item, 40, 58, typesToUpdate, ref needRefresh);

			if (needRefresh) {
				MagicUI.SetRefresh();
				MagicUI.SetNextCollectionsToRefresh(typesToUpdate);
			}
		}

		private void CheckInventory(Item[] inventory, int inventoryMaxIndex, int arrayOffset, HashSet<int> typesToUpdate, ref bool needRefresh) {
			for (int i = 0; i < inventoryMaxIndex; i++) {
				Item item = inventory[i];
				int n = i + arrayOffset;

				if (Interlocked.Exchange(ref types[n], item.type) != item.type) {
					typesToUpdate.Add(item.type);
					needRefresh = true;
				}

				if (Interlocked.Exchange(ref stacks[n], item.stack) != item.stack) {
					typesToUpdate.Add(item.type);
					needRefresh = true;
				}

				if (favorited[n] != item.favorited) {
					favorited[n] = item.favorited;
					typesToUpdate.Add(item.type);
					needRefresh = true;
				}
			}
		}
	}
}
