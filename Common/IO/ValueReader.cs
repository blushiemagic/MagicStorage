using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

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

		public T ReadUnsigned<T>(int numBits) where T : IUnsignedNumber<T>, IBinaryInteger<T> {
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

			if (numBits == 0)
				return T.Zero;

			if (numBits < 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");

			CheckBits(numBits);

			T ret = _bits.GetVariant<T>(ref _head, (byte)numBits);

			if (LogReads) {
				if (typeof(T) == typeof(byte))
					MagicStorageMod.Instance.Logger.Info($"READ [byte]: {ret:X02} ({numBits} bits)");
				else if (typeof(T) == typeof(ushort))
					MagicStorageMod.Instance.Logger.Info($"READ [ushort]: {ret:X04} ({numBits} bits)");
				else if (typeof(T) == typeof(uint))
					MagicStorageMod.Instance.Logger.Info($"READ [uint]: {ret:X08} ({numBits} bits)");
				else if (typeof(T) == typeof(ulong))
					MagicStorageMod.Instance.Logger.Info($"READ [ulong]: {ret:X016} ({numBits} bits)");
			}

			return ret;
		}

		public T ReadSigned<T>(int numBits) where T : ISignedNumber<T>, IBinaryInteger<T> {
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

			if (numBits == 0)
				return T.Zero;

			if (numBits < 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");

			CheckBits(numBits);

			T ret = default;
			if (typeof(T) == typeof(sbyte)) {
				byte read = ReadSigned_GetAndSignExtend<byte>(numBits);
				ret = Unsafe.As<byte, T>(ref read);
			} else if (typeof(T) == typeof(short)) {
				ushort read = ReadSigned_GetAndSignExtend<ushort>(numBits);
				ret = Unsafe.As<ushort, T>(ref read);
			} else if (typeof(T) == typeof(int)) {
				uint read = ReadSigned_GetAndSignExtend<uint>(numBits);
				ret = Unsafe.As<uint, T>(ref read);
			} else if (typeof(T) == typeof(long)) {
				ulong read = ReadSigned_GetAndSignExtend<ulong>(numBits);
				ret = Unsafe.As<ulong, T>(ref read);
			}

			if (LogReads) {
				if (typeof(T) == typeof(sbyte))
					MagicStorageMod.Instance.Logger.Info($"READ [sbyte]: {ret:X02} ({numBits} bits)");
				else if (typeof(T) == typeof(short))
					MagicStorageMod.Instance.Logger.Info($"READ [short]: {ret:X04} ({numBits} bits)");
				else if (typeof(T) == typeof(int))
					MagicStorageMod.Instance.Logger.Info($"READ [int]: {ret:X08} ({numBits} bits)");
				else if (typeof(T) == typeof(long))
					MagicStorageMod.Instance.Logger.Info($"READ [long]: {ret:X016} ({numBits} bits)");
			}

			return ret;
		}

		private T ReadSigned_GetAndSignExtend<T>(int numBits) where T : IUnsignedNumber<T>, IBinaryInteger<T> {
			T read = _bits.GetVariant<T>(ref _head, (byte)numBits);
			bool negative = (read & (T.One << (numBits - 1))) != T.Zero;

			if (negative) {
				long mask = -1 << numBits;
				read = Unsafe.As<long, T>(ref mask) | read; // Sign extend
			}

			return read;
		}

		public byte ReadByte(int numBits) => ReadUnsigned<byte>(numBits);

		public sbyte ReadSByte(int numBits) => ReadSigned<sbyte>(numBits);

		public ushort ReadUInt16(int numBits) => ReadUnsigned<ushort>(numBits);

		public short ReadInt16(int numBits) => ReadSigned<short>(numBits);

		public uint ReadUInt32(int numBits) => ReadUnsigned<uint>(numBits);

		public int ReadInt32(int numBits) => ReadSigned<int>(numBits);

		public ulong ReadUInt64(int numBits) => ReadUnsigned<ulong>(numBits);

		public long ReadInt64(int numBits) => ReadSigned<long>(numBits);

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

		public byte[] ReadBytes(int count) {
			if (LogReads)
				MagicStorageMod.Instance.Logger.Info($"READ START [byte[]/c] ({count} bytes)");

			byte[] bytes = GC.AllocateUninitializedArray<byte>(count);
			for (int i = 0; i < count; i++)
				bytes[i] = ReadByte(BitBuffer128.MAX_BYTE);
			
			if (LogReads)
				MagicStorageMod.Instance.Logger.Info($"READ FINISH [byte[]/c]");
			
			return bytes;
		}

		public int Read7BitEncodedInt() {
			int read = 0;
			int shift = 0;

			if (LogReads)
				MagicStorageMod.Instance.Logger.Info("READ START [7BitEncodedInt]");

			while (shift < BitBuffer128.MAX_INT) {
				byte b;
				bool more;

				using (FlagSwitch.Create(ref LogReads, false)) {
					b = ReadByte(BitBuffer128.MAX_BYTE - 1);
					read |= (b & 0x7F) << shift;
					shift += BitBuffer128.MAX_BYTE - 1;
				
					more = ReadBoolean();
				}

				if (!more) {
					if (LogReads) {
						MagicStorageMod.Instance.Logger.Info($"READ [7BitEncodedInt/byte]: {b:X02} (final)");
						MagicStorageMod.Instance.Logger.Info($"READ FINISH [7BitEncodedInt]: {read:X08}");
					}

					return read;
				} else if (LogReads)
					MagicStorageMod.Instance.Logger.Info($"READ [7BitEncodedInt/byte]: {b:X02} (continuing)");
			}

			throw new FormatException("Invalid 7-bit encoded integer");
		}
	}
}
