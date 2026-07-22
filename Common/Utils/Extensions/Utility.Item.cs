using Terraria;
using Terraria.ID;

namespace MagicStorage {
	partial class Utility {
		/// <summary>
		/// Gets an item's content identifier with stack count when greater than one.
		/// </summary>
		/// <param name="item">The item to describe.</param>
		/// <returns>The item identifier with optional stack count.</returns>
		public static string IdentifierAndStack(this Item item) => $"{ItemID.Search.GetName(item.type)}{(item.stack > 1 ? $" ({item.stack})" : "")}";

		/// <summary>
		/// Gets an item's content identifier with the specified stack count.
		/// </summary>
		/// <param name="item">The item whose type should be described.</param>
		/// <param name="stack">The stack count to display.</param>
		/// <returns>The item identifier with optional stack count.</returns>
		public static string IdentifierWithStack(this Item item, int stack) => $"{ItemID.Search.GetName(item.type)}{(stack > 1 ? $" ({stack})" : "")}";

		/// <summary>
		/// Gets an item type identifier with the specified stack count.
		/// </summary>
		/// <param name="type">The item type to describe.</param>
		/// <param name="stack">The stack count to display.</param>
		/// <returns>The item identifier with optional stack count.</returns>
		public static string ItemIdentifierWithStack(int type, int stack) => $"{ItemID.Search.GetName(type)}{(stack > 1 ? $" ({stack})" : "")}";
	}
}
