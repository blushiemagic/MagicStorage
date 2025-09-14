using Terraria;
using Terraria.ID;

namespace MagicStorage {
	partial class Utility {
		public static string IdentifierAndStack(this Item item) => $"{ItemID.Search.GetName(item.type)}{(item.stack > 1 ? $" ({item.stack})" : "")}";

		public static string IdentifierWithStack(this Item item, int stack) => $"{ItemID.Search.GetName(item.type)}{(stack > 1 ? $" ({stack})" : "")}";

		public static string ItemIdentifierWithStack(int type, int stack) => $"{ItemID.Search.GetName(type)}{(stack > 1 ? $" ({stack})" : "")}";
	}
}
