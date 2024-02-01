using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.IO {
	public sealed class ItemTypeTracker : DataSizeTracker<Item> {
		public override int BitCount => NetCompression.GetBitSize(ItemLoader.ItemCount);

		public override void Receive(ref Item value, ValueReader reader) => value.SetDefaults(reader.ReadInt32(BitCount));

		public override void Send(Item value, ValueWriter writer) => writer.Write(value.type, BitCount);
	}

	public sealed class ItemPrefixTracker : DataSizeTracker<Item> {
		public override int BitCount => NetCompression.GetBitSize(PrefixLoader.PrefixCount);

		public override void Receive(ref Item value, ValueReader reader) => value.Prefix(reader.ReadInt32(BitCount));

		public override void Send(Item value, ValueWriter writer) => writer.Write(value.prefix, BitCount);
	}
}
