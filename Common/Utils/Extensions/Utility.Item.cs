using System.Text;
using Terraria;
using Terraria.ID;

namespace MagicStorage {
	partial class Utility {
		public static string IdentifierAndStack(this Item @this) => BuildItemIdentifier(@this.type, @this.stack);

		public static string IdentifierWithStack(this Item @this, int stack) => BuildItemIdentifier(@this.type, stack);

		public static string ItemIdentifierWithStack(int type, int stack) => BuildItemIdentifier(type, stack);

		private static string BuildItemIdentifier(int type, int stack) {
			if (type <= ItemID.None || stack <= 0)
				return "None";

			StringBuilder sb = new(ItemID.Search.GetName(type));

			if (stack > 1)
				sb.Append(" (").Append(stack).Append(')');

			return sb.ToString();
		}

		public static string PrefixedIdentifierAndStack(this Item @this) => BuildItemIdentifier(@this.type, @this.stack, @this.prefix);

		public static string ItemIdentifierWithPrefix(this Item @this, int prefix) => BuildItemIdentifier(@this.type, @this.stack, prefix);

		public static string PrefixedIdentifierWithStack(this Item @this, int stack) => BuildItemIdentifier(@this.type, stack, @this.prefix);

		public static string PrefixedItemIdentifierWithStack(int type, int stack, int prefix) => BuildItemIdentifier(type, stack, prefix);

		private static string BuildItemIdentifier(int type, int stack, int prefix) {
			if (type <= ItemID.None || stack <= 0)
				return "None";

			StringBuilder sb = new();

			if (prefix > 0)
				sb.Append('{').Append(PrefixID.Search.GetName(prefix)).Append("} ");

			sb.Append(ItemID.Search.GetName(type));

			if (stack > 1)
				sb.Append(" (").Append(stack).Append(')');

			return sb.ToString();
		}

		public static string ToChatTag(this Item @this) => GetItemChatTag(@this.type, @this.stack, @this.prefix);

		public static string GetItemChatTag(int type, int stack, int prefix) {
			if (type <= ItemID.None || stack <= 0)
				return "None";

			// NOTE: This method does not handle the "d" (mod data) tag option since it's supposed to be just an easy tag builder

			if (stack > 1) {
				if (prefix > 0)
					return $"[i/s{stack},p{prefix}:{type}]";
				else
					return $"[i/s{stack}:{type}]";
			} else {
				if (prefix > 0)
					return $"[i/p{prefix}:{type}]";
				else
					return $"[i:{type}]";
			}
		}
	}
}
