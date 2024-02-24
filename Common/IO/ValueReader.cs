using System;
using System.IO;

namespace MagicStorage.Common.IO {
	public class ValueReader {
		internal static bool LogReads = false;

		private BitBuffer128 _bits;
		private int _head;
		private readonly BinaryReader _stream;

		public ValueReader(BinaryReader stream) {
			_bits = new BitBuffer128();
			_head = 0;
			_stream = stream;
		}

		private void CheckBits(int numBits) {
			// Read bytes from the stream until we have enough bits
			while (_head < numBits) {
			//	if (LogReads)
			//		MagicStorageMod.Instance.Logger.Info($"STREAMED BITS [head = {_head}, numBits = {numBits}]");

				byte b = _stream.ReadByte();
				_bits.Set(b, ref _head);
			}
		}

		public bool ReadBoolean() {
			CheckBits(1);
			bool ret = _bits.GetBoolean(ref _head);

			if (LogReads)
				MagicStorageMod.Instance.Logger.Info($"READ [bool]: {ret}");

			return ret;
		}

		public byte ReadByte(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_BYTE)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_BYTE}");

			CheckBits(numBits);
			byte ret = _bits.GetByte(ref _head, (byte)numBits);

			if (LogReads)
				MagicStorageMod.Instance.Logger.Info($"READ [byte]: {ret:X02} ({numBits} bits)");

			return ret;
		}

		public sbyte ReadSByte(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_BYTE)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_BYTE}");
			
			CheckBits(Math.Min(numBits + 1, BitBuffer128.MAX_BYTE));

			bool negative;
			using (FlagSwitch.Create(ref LogReads, false))
				negative = ReadBoolean();

			if (numBits == BitBuffer128.MAX_BYTE)
				numBits--;

			byte mold = (byte)(negative ? byte.MaxValue << numBits : 0);
			sbyte ret = (sbyte)(mold | _bits.GetByte(ref _head, (byte)numBits));

			if (LogReads)
				MagicStorageMod.Instance.Logger.Info($"READ [sbyte]: {ret:X02} ({numBits} bits)");

			return ret;
		}

		public ushort ReadUInt16(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_SHORT)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_SHORT}");
			
			CheckBits(numBits);
			ushort ret = _bits.GetUInt16(ref _head, (byte)numBits);

			if (LogReads)
				MagicStorageMod.Instance.Logger.Info($"READ [ushort]: {ret:X04} ({numBits} bits)");

			return ret;
		}

		public short ReadInt16(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_SHORT)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_SHORT}");
			
			CheckBits(Math.Min(numBits + 1, BitBuffer128.MAX_SHORT));

			bool negative;
			using (FlagSwitch.Create(ref LogReads, false))
				negative = ReadBoolean();

			if (numBits == BitBuffer128.MAX_SHORT)
				numBits--;

			ushort mold = (ushort)(negative ? ushort.MaxValue << numBits : 0);
			short ret = (short)(mold | _bits.GetUInt16(ref _head, (byte)numBits));

			if (LogReads)
				MagicStorageMod.Instance.Logger.Info($"READ [short]: {ret:X04} ({numBits} bits)");

			return ret;
		}

		public uint ReadUInt32(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_INT)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_INT}");
			
			CheckBits(numBits);
			uint ret = _bits.GetUInt32(ref _head, (byte)numBits);

			if (LogReads)
				MagicStorageMod.Instance.Logger.Info($"READ [uint]: {ret:X08} ({numBits} bits)");

			return ret;
		}

		public int ReadInt32(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_INT)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_INT}");
			
			CheckBits(Math.Min(numBits + 1, BitBuffer128.MAX_INT));

			bool negative;
			using (FlagSwitch.Create(ref LogReads, false))
				negative = ReadBoolean();

			if (numBits == BitBuffer128.MAX_INT)
				numBits--;

			uint mold = negative ? uint.MaxValue << numBits : 0;
			int ret = (int)(mold | _bits.GetUInt32(ref _head, (byte)numBits));

			if (LogReads)
				MagicStorageMod.Instance.Logger.Info($"READ [int]: {ret:X08} ({numBits} bits)");

			return ret;
		}

		public ulong ReadUInt64(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_LONG)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_LONG}");
			
			CheckBits(numBits);
			ulong ret = _bits.GetUInt64(ref _head, (byte)numBits);

			if (LogReads)
				MagicStorageMod.Instance.Logger.Info($"READ [ulong]: {ret:X016} ({numBits} bits)");

			return ret;
		}

		public long ReadInt64(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_LONG)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_LONG}");
			
			CheckBits(Math.Min(numBits + 1, BitBuffer128.MAX_LONG));

			bool negative;
			using (FlagSwitch.Create(ref LogReads, false))
				negative = ReadBoolean();

			if (numBits == BitBuffer128.MAX_LONG)
				numBits--;

			ulong mold = negative ? ulong.MaxValue << numBits : 0;
			long ret = (long)(mold | _bits.GetUInt64(ref _head, (byte)numBits));

			if (LogReads)
				MagicStorageMod.Instance.Logger.Info($"READ [long]: {ret:X016} ({numBits} bits)");

			return ret;
		}

		public byte[] ReadBytes() {
			if (LogReads)
				MagicStorageMod.Instance.Logger.Info("READ START [byte[]]");

			int length = Read7BitEncodedInt();
			byte[] bytes = GC.AllocateUninitializedArray<byte>(length);

			for (int i = 0; i < length; i++)
				bytes[i] = ReadByte(BitBuffer128.MAX_BYTE);

			if (LogReads)
				MagicStorageMod.Instance.Logger.Info($"READ FINISH [byte[]]: {length} bytes");

			return bytes;
		}

		public int Read7BitEncodedInt() {
			int read = 0;
			int shift = 0;

			if (LogReads)
				MagicStorageMod.Instance.Logger.Info("READ START [7BitEncodedInt]");

			while (shift < BitBuffer128.MAX_INT) {
				byte b = ReadByte(BitBuffer128.MAX_BYTE - 1);
				read |= (b & 0x7F) << shift;
				shift += BitBuffer128.MAX_BYTE - 1;
				
				bool more = ReadBoolean();
				if (!more) {
					if (LogReads)
						MagicStorageMod.Instance.Logger.Info($"READ FINISH [7BitEncodedInt]: {read:X08}");

					return read;
				}
			}

			throw new FormatException("Invalid 7-bit encoded integer");
		}
	}
}
