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

		public static AuditItem CreateAndLink(Item item, AuditFile source) => new(source.Items.Add(item), item.stack);

		public static AuditItem CreateAndLink(ReducedItem item, AuditFile source) {
			var sample = ContentSamples.ItemsByType[item.Type];
			return new(source.Items.Add(sample), item.Stack);
		}

		public static AuditItem DeserializeOne(BinaryReader reader, AuditFile source) {
			int index = reader.Read7BitEncodedInt();
			int stack = reader.Read7BitEncodedInt();
			return new(index, stack);
		}

		public void Serialize(BinaryWriter writer, AuditFile source) {
			writer.Write7BitEncodedInt(index);
			writer.Write7BitEncodedInt(stack);
		}
	}
}
