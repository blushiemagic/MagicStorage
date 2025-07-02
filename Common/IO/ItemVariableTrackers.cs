using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.IO {
	public sealed class ItemTypeTracker : DataSizeTracker<Item> {
		public override int BitCount => NetCompression.GetBitSize(ItemLoader.ItemCount);

		public override void Receive(ref Item value, ValueReader reader) => value.SetDefaults((int)reader.ReadUInt32(BitCount));

		public void Receive(ref int type, ValueReader reader) => type = (int)reader.ReadUInt32(BitCount);

		public override void Send(Item value, ValueWriter writer) => writer.Write((uint)value.type, BitCount);

		public void Send(int type, ValueWriter writer) => writer.Write((uint)type, BitCount);
	}

	public sealed class ItemPrefixTracker : DataSizeTracker<Item> {
		public override int BitCount => NetCompression.GetBitSize(PrefixLoader.PrefixCount);

		public override void Receive(ref Item value, ValueReader reader) => value.Prefix((int)reader.ReadUInt32(BitCount));

		public void Receive(ref int prefix, ValueReader reader) => prefix = (int)reader.ReadUInt32(BitCount);

		public override void Send(Item value, ValueWriter writer) => writer.Write((uint)value.prefix, BitCount);

		public void Send(int prefix, ValueWriter writer) => writer.Write((uint)prefix, BitCount);
	}
}
