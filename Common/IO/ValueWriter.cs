using System;
using System.IO;

namespace MagicStorage.Common.IO {
	public class ValueWriter {
		internal static bool LogWrites = false;

		private BitBuffer128 _bits;
		private int _head;
		private readonly BinaryWriter _stream;

		public ValueWriter(BinaryWriter stream) {
			_bits = new BitBuffer128();
			_head = 0;
			_stream = stream;
		}

		private void CheckBits() {
			if (_head >= 64) {
			//	if (LogWrites)
			//		MagicStorageMod.Instance.Logger.Info($"FLUSHED BITS [head = {_head}]");

				_bits.FlushBytes(_stream, ref _head, writeLastBits: false);
			}
		}

		public void Flush() {
		//	if (LogWrites)
		//		MagicStorageMod.Instance.Logger.Info($"FLUSHED BITS [head = {_head}]");

			_bits.FlushBytes(_stream, ref _head, writeLastBits: true);
		}

		public void Write(bool value) {
			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE [bool]: {value}");

			_bits.Set(value, ref _head);

			CheckBits();
		}

		public void Write(byte value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_BYTE)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_BYTE}");

			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE [byte]: {value:X02} ({numBits} bits)");

			_bits.Set(value, ref _head, (byte)numBits);

			CheckBits();
		}

		public void Write(sbyte value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_BYTE)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_BYTE}");

			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE [sbyte]: {value:X02} ({numBits} bits)");

			_bits.Set(value < 0, ref _head);

			CheckBits();

			if (numBits == BitBuffer128.MAX_BYTE)
				numBits--;

			_bits.Set((byte)(value & sbyte.MaxValue), ref _head, (byte)numBits);

			CheckBits();
		}

		public void Write(ushort value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_SHORT)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_SHORT}");

			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE [ushort]: {value:X04} ({numBits} bits)");

			_bits.Set(value, ref _head, (byte)numBits);

			CheckBits();
		}

		public void Write(short value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_SHORT)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_SHORT}");

			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE [short]: {value:X04} ({numBits} bits)");

			_bits.Set(value < 0, ref _head);

			CheckBits();

			if (numBits == BitBuffer128.MAX_SHORT)
				numBits--;

			_bits.Set((ushort)(value & short.MaxValue), ref _head, (byte)numBits);

			CheckBits();
		}

		public void Write(uint value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_INT)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_INT}");

			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE [uint]: {value:X08} ({numBits} bits)");

			_bits.Set(value, ref _head, (byte)numBits);

			CheckBits();
		}

		public void Write(int value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_INT)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_INT}");

			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE [int]: {value:X08} ({numBits} bits)");

			_bits.Set(value < 0, ref _head);

			CheckBits();

			if (numBits == BitBuffer128.MAX_INT)
				numBits--;

			_bits.Set((uint)(value & int.MaxValue), ref _head, (byte)numBits);

			CheckBits();
		}

		public void Write(ulong value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_LONG)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_LONG}");

			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE [ulong]: {value:X016} ({numBits} bits)");

			_bits.Set(value, ref _head, (byte)numBits);

			CheckBits();
		}

		public void Write(long value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			if (numBits > BitBuffer128.MAX_LONG)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_LONG}");

			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE [long]: {value:X016} ({numBits} bits)");

			_bits.Set(value < 0, ref _head);

			CheckBits();

			if (numBits == BitBuffer128.MAX_LONG)
				numBits--;

			_bits.Set((ulong)(value & long.MaxValue), ref _head, (byte)numBits);

			CheckBits();
		}

		public void WriteBytes(byte[] bytes) {
			if (bytes is null)
				throw new ArgumentNullException(nameof(bytes), "Value cannot be null");

			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE START [byte[]]: {bytes.Length} bytes");

			Write7BitEncodedInt(bytes.Length);

			for (int i = 0; i < bytes.Length; i++)
				Write(bytes[i], BitBuffer128.MAX_BYTE);

			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE FINISH [byte[]]");
		}

		public void Write7BitEncodedInt(int value) {
			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE START [7BitEncodedInt]: {value:X08}");
			
			uint num = (uint)value;

			while (num >= 128u) {
				Write((byte)(num & 0x7F), BitBuffer128.MAX_BYTE - 1);
				Write(true);
				num >>= 7;
			}

			Write((byte)num, BitBuffer128.MAX_BYTE - 1);
			Write(false);

			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE FINISH [7BitEncodedInt]");
		}
	}
}
