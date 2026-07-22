using Terraria;

namespace MagicStorage.CrossMod {
	/// <summary>
	/// Sells duplicate items that have no prefix while preserving prefixed copies.
	/// </summary>
	public sealed class SellAllItemsWithNoPrefixFinder : SellingDuplicateFinder {
		/// <inheritdoc/>
		public override Item GetBetterItem(Item item1, Item item2) => item1.prefix != 0 || item2.prefix == 0 ? item1 : item2;

		/// <inheritdoc/>
		public override bool IsValidForDuplicateSelling(Item orig, Item check) => check.prefix == 0;
	}

	/// <summary>
	/// Sells duplicate items while preserving the copy with the highest value.
	/// </summary>
	public sealed class SellAllExceptMostExpensiveFinder : SellingDuplicateFinder {
		/// <inheritdoc/>
		public override Item GetBetterItem(Item item1, Item item2) => item1.value >= item2.value ? item1 : item2;

		/// <inheritdoc/>
		public override bool IsValidForDuplicateSelling(Item orig, Item check) => true;
	}

	/// <summary>
	/// Sells duplicate items while preserving the copy with the lowest value.
	/// </summary>
	public sealed class SellAllExceptLeastExpensiveFinder : SellingDuplicateFinder {
		/// <inheritdoc/>
		public override Item GetBetterItem(Item item1, Item item2) => item1.value <= item2.value ? item1 : item2;

		/// <inheritdoc/>
		public override bool IsValidForDuplicateSelling(Item orig, Item check) => true;
	}
}
