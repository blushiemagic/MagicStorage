using System;
using System.IO;

namespace MagicStorage.Common.IO {
	public class ValueReader {
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
				byte b = _stream.ReadByte();
				_bits.Set(b, ref _head);
			}
		}

		public bool ReadBoolean() {
			CheckBits(1);
			return _bits.GetBoolean(ref _head);
		}

		public byte ReadByte(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");

			CheckBits(Math.Min(numBits, BitBuffer128.MAX_BYTE));
			return _bits.GetByte(ref _head, (byte)numBits);
		}

		public sbyte ReadSByte(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			
			CheckBits(Math.Min(numBits, BitBuffer128.MAX_BYTE));
			return (sbyte)_bits.GetByte(ref _head, (byte)numBits);
		}

		public ushort ReadUInt16(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			
			CheckBits(Math.Min(numBits, BitBuffer128.MAX_SHORT));
			return _bits.GetUInt16(ref _head, (byte)numBits);
		}

		public short ReadInt16(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			
			CheckBits(Math.Min(numBits, BitBuffer128.MAX_SHORT));
			return (short)_bits.GetUInt16(ref _head, (byte)numBits);
		}

		public uint ReadUInt32(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			
			CheckBits(Math.Min(numBits, BitBuffer128.MAX_INT));
			return _bits.GetUInt32(ref _head, (byte)numBits);
		}

		public int ReadInt32(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			
			CheckBits(Math.Min(numBits, BitBuffer128.MAX_INT));
			return (int)_bits.GetUInt32(ref _head, (byte)numBits);
		}

		public ulong ReadUInt64(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			
			CheckBits(Math.Min(numBits, BitBuffer128.MAX_LONG));
			return _bits.GetUInt64(ref _head, (byte)numBits);
		}

		public long ReadInt64(int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");
			
			CheckBits(Math.Min(numBits, BitBuffer128.MAX_LONG));
			return (long)_bits.GetUInt64(ref _head, (byte)numBits);
		}

		public byte[] ReadBytes() {
			int length = Read7BitEncodedInt();
			byte[] bytes = new byte[length];

			for (int i = 0; i < length; i++)
				bytes[i] = ReadByte(8);

			return bytes;
		}

		public int Read7BitEncodedInt() {
			int read = 0;
			int shift = 0;

			while (shift < 32) {
				byte b = ReadByte(8);
				read |= (b & 0x7F) << shift;
				shift += 7;

				if ((b & 0x80) == 0)
					return read;
			}

			throw new FormatException("Invalid 7-bit encoded integer");
		}
	}
}
