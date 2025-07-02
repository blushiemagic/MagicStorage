using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MagicStorage.Common.IO {
	[StructLayout(LayoutKind.Explicit, Pack = 16, Size = 16)]
	public struct BitBuffer128 {
		public const int MAX_BYTE = 8;
		public const int MAX_SHORT = 16;
		public const int MAX_INT = 32;
		public const int MAX_LONG = 64;

		[FieldOffset(0)] private byte _byte0;
		[FieldOffset(0)] private ushort _word0;
		[FieldOffset(0)] private uint _dword0;
		[FieldOffset(0)] private ulong _qword0;

		[FieldOffset(8)] private byte _byte8;
		[FieldOffset(8)] private ushort _word4;
		[FieldOffset(8)] private uint _dword2;
		[FieldOffset(8)] private ulong _qword1;

		public void Clear() {
			_qword0 = 0;
			_qword1 = 0;
		}

		private void GetDataAndHead(out RefContainer<ulong> data, ref int head, int numBits) {
			if (head < 0)
				throw new ArgumentOutOfRangeException(nameof(head), "Write head must be non-negative");
			if (head > 2 * MAX_LONG - numBits)
				throw new ArgumentOutOfRangeException(nameof(head), $"Expected at least {numBits} bits of space, only {2 * MAX_LONG - head} were present");

			data = default;

			if (head >= 64) {
				data.Assign(ref _qword1);
				head -= 64;
			} else if (head > 0) {
				data.Assign(ref Unsafe.AddByteOffset(ref _qword0, (nint)head >> 3));
				head &= 7;
			} else
				data.Assign(ref _qword0);
		}

		public void FlushBytes(BinaryWriter writer, ref int head, bool writeLastBits = true) {
			while (head >= MAX_BYTE)
				writer.Write(GetByte(ref head));

			if (writeLastBits && head > 0)
				writer.Write(GetByte(ref head, (byte)head));
		}

		public bool GetBoolean(ref int head) {
			if (head == 0)
				throw new InvalidOperationException("No more bits to read");

			bool shiftOut = (_qword0 & 1) == 1;
			_qword0 = (_qword0 >> 1) | (_qword1 & 1) << 63;
			_qword1 >>= 1;
			head--;
			return shiftOut;
		}

		public void Set(bool value, ref int head) {
			int localHead = head;
			GetDataAndHead(out var dataRef, ref localHead, numBits: 1);

			if (value)
				dataRef.Value |= 1uL << localHead;
			else
				dataRef.Value &= ~(1uL << localHead);
			
			head++;
		}

		public T GetVariant<T>(ref int head, byte numBits) where T : IUnsignedNumber<T>, IBinaryInteger<T> {
			if (typeof(T) == typeof(byte)) {
				if (numBits > MAX_BYTE)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count ({numBits}) must be less than or equal to {MAX_BYTE}");
			} else if (typeof(T) == typeof(ushort)) {
				if (numBits > MAX_SHORT)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count ({numBits}) must be less than or equal to {MAX_SHORT}");
			} else if (typeof(T) == typeof(uint)) {
				if (numBits > MAX_INT)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count ({numBits}) must be less than or equal to {MAX_INT}");
			} else if (typeof(T) == typeof(ulong)) {
				if (numBits > MAX_LONG)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count ({numBits}) must be less than or equal to {MAX_LONG}");
			} else
				throw new InvalidOperationException($"Unsupported type: {typeof(T).FullName}");

			if (numBits == 0)
				return T.Zero;

			if (head < numBits)
				throw new InvalidOperationException($"Expected {numBits} bits, found only {head}");

			ulong mask = (1uL << numBits) - 1;
			ulong shiftOut = _qword0 & mask;
			_qword0 = (_qword0 >> numBits) | ((_qword1 & mask) << (MAX_LONG - numBits));
			_qword1 >>= numBits;
			head -= numBits;

			if (typeof(T) == typeof(byte)) {
				byte ret = (byte)shiftOut;
				return Unsafe.As<byte, T>(ref ret);
			} else if (typeof(T) == typeof(ushort)) {
				ushort ret = (ushort)shiftOut;
				return Unsafe.As<ushort, T>(ref ret);
			} else if (typeof(T) == typeof(uint)) {
				uint ret = (uint)shiftOut;
				return Unsafe.As<uint, T>(ref ret);
			} else if (typeof(T) == typeof(ulong)) {
				ulong ret = shiftOut;
				return Unsafe.As<ulong, T>(ref ret);
			}

			return default;  // Unreachable, but required for compilation
		}

		public void SetVariant<T>(T value, ref int head, byte numBits) where T : IUnsignedNumber<T>, IBinaryInteger<T> {
			if (typeof(T) == typeof(byte)) {
				if (numBits > MAX_BYTE)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count ({numBits}) must be less than or equal to {MAX_BYTE}");
			} else if (typeof(T) == typeof(ushort)) {
				if (numBits > MAX_SHORT)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count ({numBits}) must be less than or equal to {MAX_SHORT}");
			} else if (typeof(T) == typeof(uint)) {
				if (numBits > MAX_INT)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count ({numBits}) must be less than or equal to {MAX_INT}");
			} else if (typeof(T) == typeof(ulong)) {
				if (numBits > MAX_LONG)
					throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count ({numBits}) must be less than or equal to {MAX_LONG}");
			} else
				throw new InvalidOperationException($"Unsupported type: {typeof(T).FullName}");

			if (numBits == 0)
				return;

			int localHead = head;
			GetDataAndHead(out var dataRef, ref localHead, numBits);

			if (typeof(T) == typeof(byte)) {
				byte mask = (byte)(byte.MaxValue >> (MAX_BYTE - numBits));
				dataRef.Value &= ~((ulong)mask << localHead);
				dataRef.Value |= (ulong)(Unsafe.As<T, byte>(ref value) & mask) << localHead;
			} else if (typeof(T) == typeof(ushort)) {
				ushort mask = (ushort)(ushort.MaxValue >> (MAX_SHORT - numBits));
				dataRef.Value &= ~((ulong)mask << localHead);
				dataRef.Value |= (ulong)(Unsafe.As<T, ushort>(ref value) & mask) << localHead;
			} else if (typeof(T) == typeof(uint)) {
				uint mask = uint.MaxValue >> (MAX_INT - numBits);
				dataRef.Value &= ~((ulong)mask << localHead);
				dataRef.Value |= (ulong)(Unsafe.As<T, uint>(ref value) & mask) << localHead;
			} else if (typeof(T) == typeof(ulong)) {
				ulong mask = ulong.MaxValue >> (MAX_LONG - numBits);
				dataRef.Value &= ~(mask << localHead);
				dataRef.Value |= (Unsafe.As<T, ulong>(ref value) & mask) << localHead;
			}

			head += numBits;
		}

		public byte GetByte(ref int head, byte numBits = MAX_BYTE) => GetVariant<byte>(ref head, numBits);

		public void Set(byte value, ref int head, byte numBits = MAX_BYTE) => SetVariant(value, ref head, numBits);

		public ushort GetUInt16(ref int head, byte numBits = MAX_SHORT) => GetVariant<ushort>(ref head, numBits);

		public void Set(ushort value, ref int head, byte numBits = MAX_SHORT) => SetVariant(value, ref head, numBits);

		public uint GetUInt32(ref int head, byte numBits = MAX_INT) => GetVariant<uint>(ref head, numBits);

		public void Set(uint value, ref int head, byte numBits = MAX_INT) => SetVariant(value, ref head, numBits);

		public ulong GetUInt64(ref int head, byte numBits = MAX_LONG) => GetVariant<ulong>(ref head, numBits);

		public void Set(ulong value, ref int head, byte numBits = MAX_LONG) => SetVariant(value, ref head, numBits);
	}
}
