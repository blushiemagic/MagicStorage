using Terraria;

namespace MagicStorage.CrossMod {
	public sealed class SellAllItemsWithNoPrefixFinder : SellingDuplicateFinder {
		public override Item GetBetterItem(Item item1, Item item2) => item1.prefix != 0 || item2.prefix == 0 ? item1 : item2;

		public override bool IsValidForDuplicateSelling(Item orig, Item check) => check.prefix == 0;
	}

	public sealed class SellAllExceptMostExpensiveFinder : SellingDuplicateFinder {
		public override Item GetBetterItem(Item item1, Item item2) => item1.value >= item2.value ? item1 : item2;

		public override bool IsValidForDuplicateSelling(Item orig, Item check) => true;
	}

	public sealed class SellAllExceptLeastExpensiveFinder : SellingDuplicateFinder {
		public override Item GetBetterItem(Item item1, Item item2) => item1.value <= item2.value ? item1 : item2;

		public override bool IsValidForDuplicateSelling(Item orig, Item check) => true;
	}
}
