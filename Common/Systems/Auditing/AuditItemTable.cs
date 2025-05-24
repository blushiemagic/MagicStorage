using System.IO;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Common.Systems.Auditing {
	internal class AuditItemTable : BaseAuditTable<Item, int, AuditItemTable.Entry> {
		public class Entry : IAuditable<Entry>, IAuditableEntry<Entry, Item, int> {
			public int ItemType { get; private set; }
			public string ItemName { get; private set; }

			public Entry() { }

			static void IAuditable<Entry>.DeserializeOne<T>(BinaryReader reader, ref T instance) {
				int type = reader.ReadInt32();
				instance.ItemType = type;
				instance.ItemName = type >= ItemID.Count ? StringScrambling.Unscramble(reader.ReadBytes(reader.Read7BitEncodedInt())) : null;
			}

			void IAuditable<Entry>.Serialize(BinaryWriter writer) {
				writer.Write(ItemType);
				if (ItemType >= ItemID.Count) {
					byte[] scrambled = StringScrambling.Scramble(ItemName);
					writer.Write7BitEncodedInt(scrambled.Length);
					writer.Write(scrambled);
				}
			}

			static Entry IAuditableEntry<Entry, Item, int>.CreateFrom(Item source) {
				return new Entry() {
					ItemType = source.type,
					ItemName = source.ModItem?.Name
				};
			}

			static int IAuditableEntry<Entry, Item, int>.GetKey(Entry self) => self.ItemType;

			static int IAuditableEntry<Entry, Item, int>.GetKey(Item source) => source.type;

			static string IAuditableEntry<Entry, Item, int>.GetName(Entry self) => self.ItemType < ItemID.Count ? ItemID.Search.GetName(self.ItemType) : self.ItemName;
		}
	}
}
