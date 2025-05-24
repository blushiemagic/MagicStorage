using System.IO;
using Terraria;

namespace MagicStorage.Common.Systems.Auditing {
	internal readonly record struct ReducedItem(int Type, int Stack) {
		public ReducedItem(Item item) : this(item.type, item.stack) { }

		public ReducedItem WithStack(int stack) => new(Type, stack);
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
