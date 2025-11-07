using Terraria;

namespace MagicStorage.Common.IO {
	internal class StackCompressor {
		private static readonly LengthCompressor<uint> _tiers;

		static StackCompressor() {
			// NOTE: Bits for the prefix end up being read from the stream from LSB to MSB
			var tier0 = EncodingTier.CreateZero<uint>  (prefix: 0b___0, 1, size: 512);
			var tier1 = tier0.CreateSuccessive         (prefix: 0b__01, 2, size: 8192);
			var tier2 = tier1.CreateSuccessive         (prefix: 0b_011, 3, size: 131072);
			var tier3 = tier2.CreateSuccessiveUnbounded(prefix: 0b0111, 4);

			_tiers = new LengthCompressor<uint>(tier0, tier1, tier2, tier3);
		}

		public int CommonMaxStack { get; private set; }

		public StackCompressor() {
			CommonMaxStack = Item.CommonMaxStack;
		}

		public StackCompressor(int commonMaxStack) {
			CommonMaxStack = commonMaxStack;
		}

		public void SetCommonMaxStack(int maxStack) => CommonMaxStack = maxStack;

		public void WriteTo(ValueWriter writer, int value, int maximum = 0) {
			if (maximum <= 0)
				maximum = CommonMaxStack;

			uint raw = (uint)value;
			uint delta = ZigzagEncode(value - maximum);

			bool deltaEncode = _tiers.GetBitCost(delta) < _tiers.GetBitCost(raw);
			writer.Write(deltaEncode);

			uint toWrite = deltaEncode ? delta : raw;
			_tiers.WriteTo(writer, toWrite);
		}

		public int ReadFrom(ValueReader reader, int maximum = 0) {
			if (maximum <= 0)
				maximum = CommonMaxStack;

			bool deltaDecode = reader.ReadBoolean();
			uint read = _tiers.ReadFrom(reader);

			return deltaDecode ? maximum + ZigzagDecode(read) : (int)read;
		}

		// Zigzag encoding maps small negative integers to small positive integers
		// E.g. 0 -> 0, -1 -> 1, 1 -> 2, -2 -> 3, 2 -> 4, etc.
		private static uint ZigzagEncode(int value) => (uint)((value << 1) ^ (value >> 31));

		private static int ZigzagDecode(uint value) => (int)((value >> 1) ^ (-(value & 1)));
	}
}
