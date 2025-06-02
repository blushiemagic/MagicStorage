using System.IO;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Common.Systems.Auditing {
	internal class AuditItem {
		public readonly int index;
		public readonly int stack;

		private AuditItem(int index, int stack) {
			this.index = index;
			this.stack = stack;
		}

		public int GetItemType(AuditFile source) => source.Items.GetKeyFromIndex(index);

		public static AuditItem CreateAndLink(AuditFile source, Item item) => new(source.Items.Add(item), item.stack);

		public static AuditItem CreateAndLink(AuditFile source, ReducedItem item) => new(source.Items.Add(item), item.Stack);

		public static AuditItem DeserializeOne(BinaryReader reader) {
			int index = reader.Read7BitEncodedInt();
			int stack = reader.Read7BitEncodedInt();
			return new(index, stack);
		}

		public void Serialize(BinaryWriter writer) {
			writer.Write7BitEncodedInt(index);
			writer.Write7BitEncodedInt(stack);
		}
	}
}
