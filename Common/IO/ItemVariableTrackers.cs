using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.IO {
	public sealed class ItemTypeTracker : DataSizeTracker<Item> {
		// Reminder: netID can be negative!
		public override int BitCount => NetCompression.GetBitSize(ItemLoader.ItemCount) + 1;

		public override void Receive(ref Item value, ValueReader reader) => value.netDefaults(reader.ReadInt32(BitCount));

		public void Receive(ref int netID, ValueReader reader) => netID = reader.ReadInt32(BitCount);

		public override void Send(Item value, ValueWriter writer) => writer.Write(value.netID, BitCount);

		public void Send(int netID, ValueWriter writer) => writer.Write(netID, BitCount);
	}

	public sealed class ItemPrefixTracker : DataSizeTracker<Item> {
		public override int BitCount => NetCompression.GetBitSize(PrefixLoader.PrefixCount);

		public override void Receive(ref Item value, ValueReader reader) {
			int prefix = 0;
			Receive(ref prefix, reader);
			value.Prefix(prefix);
		}

		public void Receive(ref int prefix, ValueReader reader) => prefix = (int)reader.ReadUInt32(BitCount - 1);

		public override void Send(Item value, ValueWriter writer) => Send(value.prefix, writer);

		public void Send(int prefix, ValueWriter writer) => writer.Write((uint)prefix, BitCount - 1);
	}
}
