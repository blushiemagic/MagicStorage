using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.Auditing {
	internal class AuditItemTable : BaseAuditTable<Item, int, AuditItemTable.Entry> {
		private static int _unknownItemKey = -1;

		public class Entry : IAuditable<Entry>, IAuditableEntry<Entry, Item, int> {
			public int ItemType { get; private set; }
			public string ItemName { get; private set; }

			public Entry() { }

			static void IAuditable<Entry>.DeserializeOne(BinaryReader reader, ref Entry instance) {
				/*
				int type = reader.ReadInt32();
				instance.ItemType = type;
				instance.ItemName = type >= ItemID.Count ? StringScrambling.Unscramble(reader.ReadBytes(reader.Read7BitEncodedInt())) : null;
				*/
				int type = reader.ReadInt32();
				if (type >= ItemID.None && type < ItemID.Count) {
					// Entry was a vanilla item
					instance.ItemType = type;
					instance.ItemName = ItemID.Search.GetName(type);
				} else {
					string identifier = StringScrambling.Unscramble(reader.ReadBytes(reader.Read7BitEncodedInt()));

					if (ModContent.TryFind(identifier, out ModItem modItem)) {
						// The entry currently exists
						instance.ItemType = modItem.Type;
						instance.ItemName = identifier;
					} else {
						// Entry used ModItem.Name or just doesn't exist; check for either case
						foreach (var content in ModContent.GetContent<ModItem>()) {
							if (content.Name == identifier) {
								// Found an item with the same name
								instance.ItemType = content.Type;
								instance.ItemName = content.FullName;
								return;
							}
						}

						// No item with the identifier was found
						instance.ItemType = _unknownItemKey;
						instance.ItemName = identifier;

						--_unknownItemKey;  // Ensure that each unknown item has a unique key, even if the identifier is the same
					}
				}
			}

			void IAuditable<Entry>.Serialize(BinaryWriter writer) {
				writer.Write(ItemType >= ItemID.None && ItemType < ItemID.Count ? ItemType : -1);  // Modded item types can change between mod reloads; negative numbers are used to indicate a modded item
				if (ItemType >= ItemID.Count) {
					byte[] scrambled = StringScrambling.Scramble(ItemName);
					writer.Write7BitEncodedInt(scrambled.Length);
					writer.Write(scrambled);
				}
			}

			static Entry IAuditableEntry<Entry, Item, int>.CreateFrom(Item source) {
				return new Entry() {
					ItemType = source.type,
					ItemName = ItemID.Search.GetName(source.type)
				};
			}

			static Entry IAuditableEntry<Entry, Item, int>.CreateFrom<TAlternate>(TAlternate source) {
				return new Entry() {
					ItemType = TAlternate.GetValue(source),
					ItemName = TAlternate.GetName(source)
				};
			}

			static int IAuditableEntry<Entry, Item, int>.GetKey(Entry self) => self.ItemType;

			static int IAuditableEntry<Entry, Item, int>.GetKey(Item source) => source.type;

			static string IAuditableEntry<Entry, Item, int>.GetName(Entry self) => self.ItemName;
		}

		// NOTE: ref parameters require exact type matches, so this redirect is needed to simplify caller logic
		public static void DeserializeOne(BinaryReader reader, ref AuditItemTable instance) {
			BaseAuditTable<Item, int, Entry> redirect = instance;
			DeserializeOne(reader, ref redirect);
		}
	}
}
