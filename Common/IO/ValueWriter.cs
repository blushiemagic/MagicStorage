using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.IO {
	public class ValueWriter {
		private BitBuffer128 _bits;
		private int _head;
		private readonly Stream _stream;
		private readonly LengthCompressor<uint> _globalArrayLengthWriter;

		public ValueWriter(Stream stream) {
			_bits = new BitBuffer128();
			_head = 0;
			_stream = stream;
			_globalArrayLengthWriter = null;
		}

		public ValueWriter(BinaryWriter writer) : this(writer.BaseStream) { }

		public ValueWriter(Stream stream, LengthCompressor<uint> arrayLengthWriter) : this(stream) {
			_globalArrayLengthWriter = arrayLengthWriter;
		}

		public ValueWriter(BinaryWriter writer, LengthCompressor<uint> arrayLengthWriter) : this(writer.BaseStream, arrayLengthWriter) { }

		private void CheckBits() {
			if (_head >= 64)
				_bits.FlushBytes(_stream, ref _head, writeLastBits: false);
		}

		public void Flush() {
			_bits.FlushBytes(_stream, ref _head, writeLastBits: true);
		}

		public void Write(bool value) {
			if (_activeScope is { disposed: false } scope) {
				scope.writer.Write(value);
				scope.writtenBitCount++;
				return;
			}

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

			WriteUnsigned_Inner(value, numBits);
		}
		
		private void WriteUnsigned_Inner<T>(T value, int numBits) where T : IUnsignedNumber<T>, IBinaryInteger<T> {
			if (_activeScope is { disposed: false } scope) {
				scope.writer.WriteUnsigned_Inner(value, numBits);
				scope.writtenBitCount += (uint)numBits;
				return;
			}

			_bits.SetVariant(value, ref _head, (byte)numBits);

			CheckBits();
		}

		public void WriteSigned<T>(T value, int numBits) where T : ISignedNumber<T>, IBinaryInteger<T> {
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

			WriteSigned_Inner(value, numBits);
		}

		private void WriteSigned_Inner<T>(T value, int numBits) where T : ISignedNumber<T>, IBinaryInteger<T> {
			if (_activeScope is { disposed: false } scope) {
				scope.writer.WriteSigned_Inner(value, numBits);
				scope.writtenBitCount += (uint)numBits;
				return;
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

		public void Write(byte[] bytes) {
			ArgumentNullException.ThrowIfNull(bytes);

			if (_globalArrayLengthWriter is { } lengthWriter)
				lengthWriter.WriteTo(this, (uint)bytes.Length);
			else
				Write7BitEncodedInt(bytes.Length);

			for (int i = 0; i < bytes.Length; i++)
				Write(bytes[i], BitBuffer128.MAX_BYTE);
		}

		public void Write(byte[] bytes, LengthCompressor<uint> lengthWriter) {
			ArgumentNullException.ThrowIfNull(bytes);
			ArgumentNullException.ThrowIfNull(lengthWriter);

			lengthWriter.WriteTo(this, (uint)bytes.Length);
			
			for (int i = 0; i < bytes.Length; i++)
				Write(bytes[i], BitBuffer128.MAX_BYTE);
		}

		public void WriteContents(byte[] bytes) {
			ArgumentNullException.ThrowIfNull(bytes);

			for (int i = 0; i < bytes.Length; i++)
				Write(bytes[i], BitBuffer128.MAX_BYTE);
		}

		public void Write7BitEncodedInt(int value) {
			uint num = (uint)value;

			while (num >= 128u) {
				Write((byte)(num & 0x7F), BitBuffer128.MAX_BYTE - 1);
				Write(true);
				num >>= 7;
			}

			Write((byte)num, BitBuffer128.MAX_BYTE - 1);
			Write(false);
		}

		// Specialized methods for handling item data

		internal void WriteModData(Item item, LengthCompressor<uint> lengthWriter) {
			if (item.ModItem is { } modItem) {
				Write(true);

				using (CreateScope(lengthWriter, optimizeForBytes: true)) {
					var ms = new MemoryStream();
					modItem.NetSend(new BinaryWriter(ms));
					WriteContents(ms.ToArray());
				}
			} else
				Write(false);
		}

		internal void WriteGlobalModData(Item item, LengthCompressor<uint> lengthWriter) {
			var enumerator = ItemLoader.HookNetSend.Enumerate(item);

			if (enumerator.baseGlobals.Length > 0) {
				Write(true);

				lengthWriter.WriteTo(this, (uint)enumerator.baseGlobals.Length);

				foreach (var globalItem in enumerator) {
					using (CreateScope(lengthWriter, optimizeForBytes: true)) {
						var ms = new MemoryStream();
						globalItem.NetSend(item, new BinaryWriter(ms));
						WriteContents(ms.ToArray());
					}
				}
			} else
				Write(false);
		}

		// Data serialization safeguards

		private Scope _activeScope;

		public IDisposable CreateScope(LengthCompressor<uint> lengthWriter, bool optimizeForBytes) {
			_activeScope = new Scope(baseWriter: this, enclosingScope: _activeScope, lengthWriter, optimizeForBytes);
			return _activeScope;
		}

		private class Scope : IDisposable {
			private readonly ValueWriter _baseWriter;
			private readonly Scope _enclosingScope;
			public readonly ValueWriter writer;
			private readonly MemoryStream _stream;

			public uint writtenBitCount;
			private readonly bool _encodeByteLength;
			private LengthCompressor<uint> _lengthWriter;

			internal bool disposed;

			public Scope(ValueWriter baseWriter, Scope enclosingScope, LengthCompressor<uint> lengthWriter, bool encodeByteLength) {
				_baseWriter = baseWriter;
				_enclosingScope = enclosingScope;
				_lengthWriter = lengthWriter;
				_encodeByteLength = encodeByteLength;

				var ms = new MemoryStream();
				_stream = ms;
				writer = new ValueWriter(ms);
			}

			public void Dispose() {
				if (disposed)
					throw new ObjectDisposedException(nameof(Scope), "Scope has already been closed.");

				disposed = true;

				ValueWriter enclosingWriter = _enclosingScope?.writer ?? _baseWriter;

				writer.Flush();

				byte[] bytes = _stream.ToArray();

				// If there's no data, save bits by only writing a flag
				// This will be the case for e.g. ModItems and GlobalItems that don't send data
				if (bytes.Length == 0 || writtenBitCount == 0) {
					enclosingWriter.Write(true);
					return;
				}

				enclosingWriter.Write(false);

				uint bitCount;
				if (_encodeByteLength) {
					bitCount = Utility.CeilingMultiple(writtenBitCount, 8u);
					uint byteCount = bitCount / 8;

					_lengthWriter.WriteTo(enclosingWriter, byteCount);

					for (uint i = 0; i < byteCount; i++)
						enclosingWriter.Write(bytes[i], 8);
				} else {
					bitCount = writtenBitCount;
					uint remaningBits = bitCount;
					int i;

					_lengthWriter.WriteTo(enclosingWriter, remaningBits);

					for (i = 0; i < bytes.Length && remaningBits >= 8; i++, remaningBits -= 8)
						enclosingWriter.Write(bytes[i], 8);

					if (i < bytes.Length && remaningBits > 0)
						enclosingWriter.Write(bytes[i], (int)remaningBits);
				}

				if (_enclosingScope is { } scope)
					scope.writtenBitCount += bitCount;

				_baseWriter._activeScope = _enclosingScope;
			}
		}
	}
}
