using MagicStorage.Components;
using System.IO;

namespace MagicStorage.Common.Systems.Auditing {
	internal class AuditComponentTable : BaseAuditTable<TEStorageComponent, int, AuditComponentTable.Entry> {
		public class Entry : IAuditable<Entry>, IAuditableEntry<Entry, TEStorageComponent, int> {
			public int Type { get; private set; }

			public string Name { get; private set; }

			public Entry() { }

			static void IAuditable<Entry>.DeserializeOne<T>(BinaryReader reader, ref T instance) {
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
	}
}
