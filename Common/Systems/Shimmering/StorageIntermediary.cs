using System.Collections.Generic;
using Terraria;

namespace MagicStorage.Common.Systems.Shimmering {
	public class StorageIntermediary {
		public readonly List<Item> toDeposit = new();
		public readonly List<Item> toWithdraw = new();

		public void Deposit(Item item) {
			toDeposit.Add(item);
		}

		public void Withdraw(int item, int stack = 1) {
			toWithdraw.Add(new Item(item, stack));
		}
	}
}
