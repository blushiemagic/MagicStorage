using MagicStorage.Common.Players;
using System;
using System.IO;
using Terraria;

namespace MagicStorage.Common.Systems.Auditing {
	internal class AuditPlayerTable : BaseAuditTable<Player, Guid, AuditPlayerTable.Entry> {
		public class Entry : IAuditable<Entry>, IAuditableEntry<Entry, Player, Guid> {
			public Guid Guid { get; private set; }
			public string Name { get; private set; }

			public Entry() { }

			static void IAuditable<Entry>.DeserializeOne<T>(BinaryReader reader, ref T instance) {
				instance.Guid = new Guid(reader.ReadBytes(16));
				instance.Name = StringScrambling.Unscramble(reader.ReadBytes(reader.Read7BitEncodedInt()));
			}

			void IAuditable<Entry>.Serialize(BinaryWriter writer) {
				writer.Write(Guid.ToByteArray());

				byte[] scrambled = StringScrambling.Scramble(Name);
				writer.Write7BitEncodedInt(scrambled.Length);
				writer.Write(scrambled);
			}

			static Entry IAuditableEntry<Entry, Player, Guid>.CreateFrom(Player source) {
				return new Entry() {
					Guid = source.GetModPlayer<SecurityPlayer>().UniqueID,
					Name = source.name
				};
			}

			static Entry IAuditableEntry<Entry, Player, Guid>.CreateFrom<TAlternate>(TAlternate source) {
				return new Entry() {
					Guid = TAlternate.GetValue(source),
					Name = TAlternate.GetName(source)
				};
			}

			static Guid IAuditableEntry<Entry, Player, Guid>.GetKey(Entry self) => self.Guid;

			static Guid IAuditableEntry<Entry, Player, Guid>.GetKey(Player source) => source.GetModPlayer<SecurityPlayer>().UniqueID;

			static string IAuditableEntry<Entry, Player, Guid>.GetName(Entry self) => self.Name;
		}
	}
}
