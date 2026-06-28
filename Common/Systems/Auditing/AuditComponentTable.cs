using MagicStorage.Components;
using System.IO;

namespace MagicStorage.Common.Systems.Auditing {
	internal class AuditComponentTable : BaseAuditTable<TEStorageComponent, int, AuditComponentTable.Entry> {
		public class Entry : IAuditable<Entry>, IAuditableEntry<Entry, TEStorageComponent, int> {
			public int Type { get; private set; }

			public string Name { get; private set; }

			public Entry() { }

			static void IAuditable<Entry>.DeserializeOne(BinaryReader reader, ref Entry instance) {
				instance.Type = reader.ReadUInt16();
				instance.Name = reader.ReadString();
			}

			public void Serialize(BinaryWriter writer) {
				writer.Write((ushort)Type);
				writer.Write(Name);
			}

			static Entry IAuditableEntry<Entry, TEStorageComponent, int>.CreateFrom(TEStorageComponent source) {
				return new Entry() {
					Type = source.Type,
					Name = source.Name
				};
			}

			static Entry IAuditableEntry<Entry, TEStorageComponent, int>.CreateFrom<TAlternate>(TAlternate source) {
				return new Entry() {
					Type = TAlternate.GetValue(source),
					Name = TAlternate.GetName(source)
				};
			}

			static int IAuditableEntry<Entry, TEStorageComponent, int>.GetKey(Entry self) => self.Type;

			static int IAuditableEntry<Entry, TEStorageComponent, int>.GetKey(TEStorageComponent source) => source.Type;

			static string IAuditableEntry<Entry, TEStorageComponent, int>.GetName(Entry self) => self.Name;
		}

		// NOTE: ref parameters require exact type matches, so this redirect is needed to simplify caller logic
		public static void DeserializeOne(BinaryReader reader, ref AuditComponentTable instance) {
			BaseAuditTable<TEStorageComponent, int, Entry> redirect = instance;
			DeserializeOne(reader, ref redirect);
		}
	}
}
