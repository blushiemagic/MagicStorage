namespace MagicStorage.Common.Systems.RecurrentRecipes {
	/// <summary>
	/// Identifies the inventory and environment snapshot used to build a selected recipe crafting simulation.
	/// </summary>
	/// <param name="AmountToCraft">The requested amount of the final result item.</param>
	/// <param name="InventoryHash">An order-independent hash of available item counts.</param>
	/// <param name="InfiniteItemsHash">An order-independent hash of item types treated as infinite.</param>
	/// <param name="BlockedItemsHash">An order-independent hash of blocked item type/prefix pairs.</param>
	/// <param name="TilesHash">A hash of available crafting tile indexes.</param>
	/// <param name="ConditionsHash">A hash of recipe-condition availability.</param>
	/// <param name="CreativeUnitPresent">Whether all recipe ingredients are effectively infinite.</param>
	/// <param name="RecursionInfinite">Whether recursive crafting ignores the configured depth limit.</param>
	/// <param name="RecursionDepth">The configured recursive crafting depth limit.</param>
	public readonly record struct CraftingSimulationContext(
		int AmountToCraft,
		int InventoryHash,
		int InfiniteItemsHash,
		int BlockedItemsHash,
		int TilesHash,
		int ConditionsHash,
		bool CreativeUnitPresent,
		bool RecursionInfinite,
		int RecursionDepth
	);
}
