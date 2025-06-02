using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.Auditing {
	internal readonly record struct ReducedItem(int Type, int Stack) : IAlternateAuditSource<ReducedItem, Item, int> {
		public ReducedItem(Item item) : this(item.type, item.stack) { }

		public ReducedItem WithStack(int stack) => new(Type, stack);

		static string IAlternateAuditSource<ReducedItem, Item, int>.GetName(ReducedItem self) => ItemID.Search.GetName(self.Type);

		static int IAlternateAuditSource<ReducedItem, Item, int>.GetValue(ReducedItem self) => self.Type;

		ReducedItem IAlternateAuditSource<ReducedItem, Item, int>.CreateFrom(Item source) => new(source);
	}

	internal static class ReducedItemExtensions {
		public static void Write(this BinaryWriter writer, ReducedItem item) {
			writer.Write7BitEncodedInt(item.Type);
			writer.Write7BitEncodedInt(item.Stack);
		}

		public static ReducedItem ReadReducedItem(this BinaryReader reader) {
			int type = reader.Read7BitEncodedInt();
			int stack = reader.Read7BitEncodedInt();
			return new ReducedItem(type, stack);
		}
	}
}
