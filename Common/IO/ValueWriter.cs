using System;
using System.IO;

namespace MagicStorage.Common.IO {
	public class ValueWriter {
		private BitBuffer128 _bits;
		private int _head;
		private readonly BinaryWriter _stream;

		public ValueWriter(BinaryWriter stream) {
			_bits = new BitBuffer128();
			_head = 0;
			_stream = stream;
		}

		private void CheckBits() {
			if (_head >= 64)
				_bits.FlushBytes(_stream, ref _head, writeLastBits: false);
		}

		public void Flush() {
			_bits.FlushBytes(_stream, ref _head, writeLastBits: true);
		}

		public void Write(bool value) {
			_bits.Set(value, ref _head);
			CheckBits();
		}

		public void Write(byte value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");

			_bits.Set(value, ref _head, (byte)numBits);
			CheckBits();
		}

		public void Write(sbyte value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");

			_bits.Set((byte)value, ref _head, (byte)numBits);
			CheckBits();
		}

		public void Write(ushort value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");

			_bits.Set(value, ref _head, (byte)numBits);
			CheckBits();
		}

		public void Write(short value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");

			_bits.Set((ushort)value, ref _head, (byte)numBits);
			CheckBits();
		}

		public void Write(uint value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");

			_bits.Set(value, ref _head, (byte)numBits);
			CheckBits();
		}

		public void Write(int value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");

			_bits.Set((uint)value, ref _head, (byte)numBits);
			CheckBits();
		}

		public void Write(ulong value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");

			_bits.Set(value, ref _head, (byte)numBits);
			CheckBits();
		}

		public void Write(long value, int numBits) {
			if (numBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), "Bit count must be greater than 0");

			_bits.Set((ulong)value, ref _head, (byte)numBits);
			CheckBits();
		}

		public void WriteBytes(byte[] bytes) {
			Write7BitEncodedInt(bytes.Length);

			for (int i = 0; i < bytes.Length; i++)
				Write(bytes[i], BitBuffer128.MAX_BYTE);
		}

		public void Write7BitEncodedInt(int value) {
			uint num = (uint)value;
			
			while (num >= 128u) {
				Write((byte)(num | 128u), BitBuffer128.MAX_BYTE);
				num >>= 7;
			}

			Write((byte)num, BitBuffer128.MAX_BYTE);
		}
	}
}
