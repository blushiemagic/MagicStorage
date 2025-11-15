using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.UI;

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
			CheckScopeOverflow(1);

			CheckBits(1);

			bool ret = _bits.GetBoolean(ref _head);

			IncrementScopeBits(1);

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

			CheckScopeOverflow(numBits);

			CheckBits(numBits);

			T ret = _bits.GetVariant<T>(ref _head, (byte)numBits);

			IncrementScopeBits(numBits);

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

			CheckScopeOverflow(numBits);

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

			IncrementScopeBits(numBits);

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
			int length = Read7BitEncodedInt();
			byte[] bytes = GC.AllocateUninitializedArray<byte>(length);

			for (int i = 0; i < length; i++)
				bytes[i] = ReadByte(BitBuffer128.MAX_BYTE);

			return bytes;
		}

		public byte[] ReadBytes(int count) {
			byte[] bytes = GC.AllocateUninitializedArray<byte>(count);
			for (int i = 0; i < count; i++)
				bytes[i] = ReadByte(BitBuffer128.MAX_BYTE);
			
			return bytes;
		}

		public int Read7BitEncodedInt() {
			int read = 0;
			int shift = 0;

			while (shift < BitBuffer128.MAX_INT) {
				byte b;
				bool more;

				b = ReadByte(BitBuffer128.MAX_BYTE - 1);
				read |= (b & 0x7F) << shift;
				shift += BitBuffer128.MAX_BYTE - 1;
				
				more = ReadBoolean();

				if (!more)
					return read;
			}

			throw new FormatException("Invalid 7-bit encoded integer");
		}

		// Specialized methods for handling item data

		internal void ReadModData(Item item, LengthCompressor<uint> lengthReader) {
			if (ReadBoolean() && item.ModItem is { } modItem) {
				using (ReadScope(lengthReader, optimizeForBytes: true)) {
					uint byteCount = _activeScope.ExpectedBitCount / 8;
					byte[] bytes = ReadBytes((int)byteCount);

					var ms = new MemoryStream(bytes, 0, bytes.Length, writable: false, publiclyVisible: true);
					modItem.NetReceive(new BinaryReader(ms));

					if ((uint)ms.Position != byteCount)
						throw new InvalidOperationException($"Read underflow {ms.Position} of {byteCount} bytes caused by {modItem.Name} from the {modItem.Mod.Name} mod.");
				}
			}
		}

		internal void ReadGlobalModData(Item item, LengthCompressor<uint> lengthReader, DeserializedNetItem readData) {
			var enumerator = ItemLoader.HookNetReceive.Enumerate(item);

			uint count = ReadBoolean() ? lengthReader.ReadFrom(this) : 0;

			if (count != (uint)enumerator.baseGlobals.Length) {
				// Skip past all of the data
				for (uint i = 0; i < count; i++) {
					using (ReadScope(lengthReader, optimizeForBytes: true))
						_activeScope.ReadToEnd();
				}

				throw new InvalidOperationException($"Expected {count} GlobalItem instances, but found {enumerator.baseGlobals.Length}");
			} else if (count > 0) {
				UnloadedGlobalItem unloadedGlobalItem = null;
				List<Exception> errors = [];

				foreach (var globalItem in enumerator) {
					if (globalItem is UnloadedGlobalItem unloaded)
						unloadedGlobalItem = unloaded;

					IDisposable scope = null;

					try {
						scope = ReadScope(lengthReader, optimizeForBytes: true);

						uint byteCount = _activeScope.ExpectedBitCount / 8;
						byte[] bytes = ReadBytes((int)byteCount);

						var ms = new MemoryStream(bytes, 0, bytes.Length, writable: false, publiclyVisible: true);
						globalItem.NetReceive(item, new BinaryReader(ms));

						if ((uint)ms.Position != byteCount)
							throw new InvalidOperationException($"Read underflow {ms.Position} of {byteCount} bytes caused by {globalItem.Name} from the {globalItem.Mod.Name} mod.");
					} catch (Exception ex) {
						errors.Add(ex);
					} finally {
						try {
							scope?.Dispose();
						} catch (Exception ex) {
							errors.Add(ex);
						}
					}
				}

				if (unloadedGlobalItem is not null) {
					readData.modPrefixMod = unloadedGlobalItem.ModPrefixMod;
					readData.modPrefixName = unloadedGlobalItem.ModPrefixName;
				}

				if (errors.Count > 0) {
					if (errors.Count == 1)
						throw errors[0];
					else
						throw new AggregateException(errors);
				}
			}
		}

		// Data deserialization safeguards

		private Scope _activeScope;

		public IDisposable ReadScope(LengthCompressor<uint> lengthReader, bool optimizeForBytes) {
			uint expectedBits = ReadBoolean() ? lengthReader.ReadFrom(this) : 0;
			if (optimizeForBytes)
				expectedBits *= 8;

			_activeScope = new Scope(this, _activeScope, expectedBits);
			return _activeScope;
		}

		private void IncrementScopeBits(int numBits) {
			if (_activeScope is { disposed: false } scope)
				scope.readBitCount += (uint)numBits;
		}

		private void CheckScopeOverflow(int numBits) {
			if (_activeScope is { disposed: false } scope)
				scope.CheckOverflow(numBits);
		}

		private static readonly string[] _byteSuffixes = [ "", ".125", ".25", ".375", ".5", ".625", ".75", ".875" ];

		private static string GetByteString(uint bits) => $"{bits >> 3}{_byteSuffixes[bits & 0b111]}";

		private class Scope : IDisposable {
			private readonly ValueReader _baseReader;
			private readonly Scope _enclosingScope;

			public uint readBitCount;
			private readonly uint _expectedBitCount;

			internal bool disposed;

			public uint ExpectedBitCount => _expectedBitCount;

			public Scope(ValueReader baseReader, Scope enclosingScope, uint expectedBitCount) {
				_baseReader = baseReader;
				_enclosingScope = enclosingScope;
				_expectedBitCount = expectedBitCount;
			}

			public void CheckOverflow(int numBits) {
				uint afterRead = readBitCount + (uint)numBits;
				if (afterRead > _expectedBitCount)
					throw new InvalidOperationException($"Read overflow {GetByteString(afterRead)} of {GetByteString(_expectedBitCount)} bytes");
			}

			public void ReadToEnd() {
				// Scan until the end of the scope has been reached
				int bitsToSkip = (int)(_expectedBitCount - readBitCount);

				if (bitsToSkip > 0) {
					while (bitsToSkip >= 8) {
						_baseReader.ReadByte(8);
						bitsToSkip -= 8;
					}

					if (bitsToSkip > 0)
						_baseReader.ReadByte(bitsToSkip);

					readBitCount = _expectedBitCount;
				}
				
				if (_enclosingScope is { } scope)
					scope.readBitCount += readBitCount;
			}

			public void Dispose() {
				if (disposed)
					throw new ObjectDisposedException(nameof(Scope), "Scope has already been closed.");

				disposed = true;

				uint read = readBitCount;
				bool readToEnd = read == _expectedBitCount;

				ReadToEnd();

				_baseReader._activeScope = _enclosingScope;

				if (!readToEnd)
					throw new InvalidOperationException($"Read underflow {GetByteString(read)} of {GetByteString(_expectedBitCount)} bytes");
			}
		}
	}
}
