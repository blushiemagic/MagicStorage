using System;
using System.IO;
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
		[FieldOffset(0)] private ulong _data0;

		[FieldOffset(8)] private byte _byte8;
		[FieldOffset(8)] private ushort _word4;
		[FieldOffset(8)] private uint _dword2;
		[FieldOffset(8)] private ulong _data1;

		public void Clear() {
			_data0 = 0;
			_data1 = 0;
		}

		private void GetDataAndHead(out RefContainer<ulong> data, ref int head, int numBits) {
			if (head < 0)
				throw new ArgumentOutOfRangeException(nameof(head), "Write head must be non-negative");
			if (head > 2 * MAX_LONG - numBits)
				throw new ArgumentOutOfRangeException(nameof(head), $"Expected at least {numBits} bits of space, only {2 * MAX_LONG - head} were present");

			data = default;

			if (head >= 64) {
				data.Assign(ref _data1);
				head -= 64;
			} else if (head > 0) {
				data.Assign(ref Unsafe.AddByteOffset(ref _data0, (nint)head >> 3));
				head &= 7;
			} else
				data.Assign(ref _data0);
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

			bool shiftOut = (_data0 & 1) == 1;
			_data0 = (_data0 >> 1) | (_data1 & 1) << 63;
			_data1 >>= 1;
			head--;
			return shiftOut;
		}

		public void Set(bool value, ref int head) {
			int _head = head;
			GetDataAndHead(out var dataRef, ref head, numBits: 1);

			if (value)
				dataRef.Value |= 1uL << head;
			else
				dataRef.Value &= ~(1uL << head);

			head = _head + 1;
		}

		public byte GetByte(ref int head, byte numBits = MAX_BYTE) {
			if (numBits == 0)
				return 0;
			if (numBits > MAX_BYTE)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count ({numBits}) must be less than or equal to {MAX_BYTE}");
			if (head < numBits)
				throw new InvalidOperationException($"Expected {numBits} bits, found only {head}");

			byte mask = (byte)((1u << numBits) - 1);
			byte shiftOut = (byte)(_byte0 & mask);
			_data0 = (_data0 >> numBits) | ((ulong)_byte8 & mask);
			_data1 >>= numBits;
			head -= numBits;
			return shiftOut;
		}

		public void Set(byte value, ref int head, byte numBits = MAX_BYTE) {
			if (numBits == 0)
				return;
			if (numBits > MAX_BYTE)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"numBits must be less than or equal to {MAX_BYTE}");

			int _head = head;
			GetDataAndHead(out var dataRef, ref head, numBits);

			byte mask = (byte)(byte.MaxValue >> (MAX_BYTE - numBits));
			dataRef.Value &= ~((ulong)mask << head);
			dataRef.Value |= (ulong)(value & mask) << head;
			
			head = _head + numBits;
		}

		public ushort GetUInt16(ref int head, byte numBits = MAX_SHORT) {
			if (numBits == 0)
				return 0;
			if (numBits > MAX_SHORT)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count ({numBits}) must be less than or equal to {MAX_SHORT}");
			if (head < numBits)
				throw new InvalidOperationException($"Expected {numBits} bits, found only {head}");

			ushort mask = (ushort)((1u << numBits) - 1);
			ushort shiftOut = (ushort)(_word0 & mask);
			_data0 = (_data0 >> numBits) | ((ulong)_word4 & mask);
			_data1 >>= numBits;
			head -= numBits;
			return shiftOut;
		}

		public void Set(ushort value, ref int head, byte numBits = MAX_SHORT) {
			if (numBits == 0)
				return;
			if (numBits > MAX_SHORT)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"numBits must be less than or equal to {MAX_SHORT}");

			int _head = head;
			GetDataAndHead(out var dataRef, ref head, numBits);

			ushort mask = (ushort)(ushort.MaxValue >> (MAX_SHORT - numBits));
			dataRef.Value &= ~((ulong)mask << head);
			dataRef.Value |= (ulong)(value & mask) << head;
			
			head = _head + numBits;
		}

		public uint GetUInt32(ref int head, byte numBits = MAX_INT) {
			if (numBits == 0)
				return 0;
			if (numBits > MAX_INT)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count ({numBits}) must be less than or equal to {MAX_INT}");
			if (head < numBits)
				throw new InvalidOperationException($"Expected {numBits} bits, found only {head}");

			uint mask = (1u << numBits) - 1;
			uint shiftOut = _dword0 & mask;
			_data0 = (_data0 >> numBits) | (_dword2 & mask);
			_data1 >>= numBits;
			head -= numBits;
			return shiftOut;
		}

		public void Set(uint value, ref int head, byte numBits = MAX_INT) {
			if (numBits == 0)
				return;
			if (numBits > MAX_INT)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"numBits must be less than or equal to {MAX_INT}");

			int _head = head;
			GetDataAndHead(out var dataRef, ref head, numBits);

			uint mask = uint.MaxValue >> (MAX_INT - numBits);
			dataRef.Value &= ~((ulong)mask << head);
			dataRef.Value |= (ulong)(value & mask) << head;
			
			head = _head + numBits;
		}

		public ulong GetUInt64(ref int head, byte numBits = MAX_LONG) {
			if (numBits == 0)
				return 0;
			if (numBits > MAX_LONG)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"Bit count ({numBits}) must be less than or equal to {MAX_LONG}");
			if (head < numBits)
				throw new InvalidOperationException($"Expected {numBits} bits, found only {head}");

			ulong mask = (1uL << numBits) - 1;
			ulong shiftOut = _data0 & mask;
			_data0 = (_data0 >> numBits) | (_data1 & mask);
			_data1 >>= numBits;
			head -= numBits;
			return shiftOut;
		}

		public void Set(ulong value, ref int head, byte numBits = MAX_LONG) {
			if (numBits == 0)
				return;
			if (numBits > MAX_LONG)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"numBits must be less than or equal to {MAX_LONG}");

			int _head = head;
			GetDataAndHead(out var dataRef, ref head, numBits);

			ulong mask = ulong.MaxValue >> (MAX_LONG - numBits);
			dataRef.Value &= ~(mask << head);
			dataRef.Value |= (value & mask) << head;
			
			head = _head + numBits;
		}
	}
}
