using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

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

		public void WriteUnsigned<T>(T value, int numBits) where T : IUnsignedNumber<T>, IBinaryInteger<T> {
			if (typeof(T) == typeof(byte)) {
				if (numBits > BitBuffer128.MAX_BYTE)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_BYTE}");
			} else if (typeof(T) == typeof(ushort)) {
				if (numBits > BitBuffer128.MAX_SHORT)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_SHORT}");
			} else if (typeof(T) == typeof(uint)) {
				if (numBits > BitBuffer128.MAX_INT)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_INT}");
			} else if (typeof(T) == typeof(ulong)) {
				if (numBits > BitBuffer128.MAX_LONG)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_LONG}");
			} else
				throw new NotSupportedException($"Unsupported type: {typeof(T)}");

			if (numBits == 0)  // No bits to write
				return;

			if (numBits < 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");

			if (LogWrites) {
				if (typeof(T) == typeof(byte))
					MagicStorageMod.Instance.Logger.Info($"WRITE [byte]: {value:X02} ({numBits} bits)");
				else if (typeof(T) == typeof(ushort))
					MagicStorageMod.Instance.Logger.Info($"WRITE [ushort]: {value:X04} ({numBits} bits)");
				else if (typeof(T) == typeof(uint))
					MagicStorageMod.Instance.Logger.Info($"WRITE [uint]: {value:X08} ({numBits} bits)");
				else if (typeof(T) == typeof(ulong))
					MagicStorageMod.Instance.Logger.Info($"WRITE [ulong]: {value:X016} ({numBits} bits)");
			}

			_bits.SetVariant(value, ref _head, (byte)numBits);

			CheckBits();
		}

		public void WriteSigned<T>(T value, int numBits) where T : ISignedNumber<T>, IBinaryInteger<T>, IComparisonOperators<T, T, bool> {
			if (typeof(T) == typeof(sbyte)) {
				if (numBits > BitBuffer128.MAX_BYTE)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_BYTE}");
			} else if (typeof(T) == typeof(short)) {
				if (numBits > BitBuffer128.MAX_SHORT)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_SHORT}");
			} else if (typeof(T) == typeof(int)) {
				if (numBits > BitBuffer128.MAX_INT)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_INT}");
			} else if (typeof(T) == typeof(long)) {
				if (numBits > BitBuffer128.MAX_LONG)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count must be less than or equal to {BitBuffer128.MAX_LONG}");
			} else
				throw new NotSupportedException($"Unsupported type: {typeof(T)}");

			if (numBits == 0)  // No bits to write
				return;

			if (numBits < 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");

			if (LogWrites) {
				if (typeof(T) == typeof(sbyte))
					MagicStorageMod.Instance.Logger.Info($"WRITE [sbyte]: {value:X02} ({numBits} bits)");
				else if (typeof(T) == typeof(short))
					MagicStorageMod.Instance.Logger.Info($"WRITE [short]: {value:X04} ({numBits} bits)");
				else if (typeof(T) == typeof(int))
					MagicStorageMod.Instance.Logger.Info($"WRITE [int]: {value:X08} ({numBits} bits)");
				else if (typeof(T) == typeof(long))
					MagicStorageMod.Instance.Logger.Info($"WRITE [long]: {value:X016} ({numBits} bits)");
			}

			if (typeof(T) == typeof(sbyte))
				_bits.SetVariant((byte)Unsafe.As<T, sbyte>(ref value), ref _head, (byte)numBits);
			else if (typeof(T) == typeof(short))
				_bits.SetVariant((ushort)Unsafe.As<T, short>(ref value), ref _head, (byte)numBits);
			else if (typeof(T) == typeof(int))
				_bits.SetVariant((uint)Unsafe.As<T, int>(ref value), ref _head, (byte)numBits);
			else if (typeof(T) == typeof(long))
				_bits.SetVariant((ulong)Unsafe.As<T, long>(ref value), ref _head, (byte)numBits);

			CheckBits();
		}

		public void Write(byte value, int numBits) => WriteUnsigned(value, numBits);

		public void Write(sbyte value, int numBits) => WriteSigned(value, numBits);

		public void Write(ushort value, int numBits) => WriteUnsigned(value, numBits);

		public void Write(short value, int numBits) => WriteSigned(value, numBits);

		public void Write(uint value, int numBits) => WriteUnsigned(value, numBits);

		public void Write(int value, int numBits) => WriteSigned(value, numBits);

		public void Write(ulong value, int numBits) => WriteUnsigned(value, numBits);

		public void Write(long value, int numBits) => WriteSigned(value, numBits);

		public void WriteBytes(byte[] bytes) {
			if (bytes is null)
				throw new ArgumentNullException(nameof(bytes), "Value cannot be null");

			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info("WRITE START [byte[]]");

			Write7BitEncodedInt(bytes.Length);

			for (int i = 0; i < bytes.Length; i++)
				Write(bytes[i], BitBuffer128.MAX_BYTE);

			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE FINISH [byte[]]: {bytes.Length} bytes");
		}

		public void WriteBytesNoLength(byte[] bytes) {
			if (bytes is null)
				throw new ArgumentNullException(nameof(bytes), "Value cannot be null");

			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE START [byte[]/nl]: {bytes.Length} bytes");
			
			for (int i = 0; i < bytes.Length; i++)
				Write(bytes[i], BitBuffer128.MAX_BYTE);
			
			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info($"WRITE FINISH [byte[]/nl]: {bytes.Length} bytes");
		}

		public void Write7BitEncodedInt(int value) {
			if (LogWrites)
				MagicStorageMod.Instance.Logger.Info("WRITE START [7BitEncodedInt]");

			uint num = (uint)value;

			while (num >= 128u) {
				if (LogWrites)
					MagicStorageMod.Instance.Logger.Info($"WRITE [7BitEncodedInt/byte]: {num & 0x7F:X02} (continuing)");

				using (FlagSwitch.Create(ref LogWrites, false))  {
					Write((byte)(num & 0x7F), BitBuffer128.MAX_BYTE - 1);
					Write(true);
				}
				num >>= 7;
			}

			using (FlagSwitch.Create(ref LogWrites, false)) {
				Write((byte)num, BitBuffer128.MAX_BYTE - 1);
				Write(false);
			}

			if (LogWrites) {
				MagicStorageMod.Instance.Logger.Info($"WRITE [7BitEncodedInt/byte]: {num:X02} (final)");
				MagicStorageMod.Instance.Logger.Info($"WRITE FINISH [7BitEncodedInt]: {value:X08}");
			}
		}
	}
}
