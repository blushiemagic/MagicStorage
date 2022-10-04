using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Modules {
	public class InfiniteItemsInventory {
		private HashSet<int> inventory;

		private HashSet<int> Inventory => inventory ??= (inventory = InitializeInventory());

		private bool inventoryWasUpdated = true;

		private List<Item> items;

		public void Add(int item) {
			if (inventory is null)
				inventoryWasUpdated = true;

			if (Inventory.Add(item))
				inventoryWasUpdated = true;
		}

		public void Remove(int item) {
			if (inventory is null)
				inventoryWasUpdated = true;

			if (Inventory.Remove(item))
				inventoryWasUpdated = true;
		}

		public IEnumerable<Item> GetItems() {
			if (inventoryWasUpdated) {
				inventoryWasUpdated = false;
				items = Inventory.OrderBy(i => i).Select(MakeItem).ToList();
			} else {
				for (int i = 0; i < items.Count; i++)
					items[i].stack = items[i].maxStack;
			}

			return items;
		}

		private static HashSet<int> InitializeInventory() {
			HashSet<int> inv = new();

			for (int i = 0; i < ItemLoader.ItemCount; i++) {
				if (ItemID.Sets.Deprecated[i])
					continue;

				if (Utility.IsFullyResearched(i, true))
					inv.Add(i);
			}

			return inv;
		}

		private static Item MakeItem(int type) {
			Item item = new(type);
			item.stack = item.maxStack;
			return item;
		}
	}

	internal class JourneyInfiniteItems : EnvironmentModule {
		public static InfiniteItemsInventory inventory = new();

		public override bool IsAvailable() => Main.LocalPlayer.difficulty == PlayerDifficultyID.Creative;

		public override IEnumerable<Item> GetAdditionalItems(EnvironmentSandbox sandbox) => inventory.GetItems();
	}
}
