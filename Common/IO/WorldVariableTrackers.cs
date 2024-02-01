using Terraria;

namespace MagicStorage.Common.IO {
	public sealed class WorldWidthTracker : DataSizeTracker<int> {
		public override int BitCount => NetCompression.GetBitSize(Main.maxTilesX);

		public override void Receive(ref int value, ValueReader reader) => value = reader.ReadInt32(BitCount);

		public override void Send(int value, ValueWriter writer) => writer.Write(value, BitCount);
	}

	public sealed class WorldHeightTracker : DataSizeTracker<int> {
		public override int BitCount => NetCompression.GetBitSize(Main.maxTilesY);

		public override void Receive(ref int value, ValueReader reader) => value = reader.ReadInt32(BitCount);

		public override void Send(int value, ValueWriter writer) => writer.Write(value, BitCount);
	}
}
