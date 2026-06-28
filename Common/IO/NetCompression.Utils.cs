using Ionic.Zlib;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace MagicStorage.Common.IO {
	partial class NetCompression {
		/// <summary>
		/// Compresses <paramref name="data"/> at the provided <paramref name="level"/>
		/// </summary>
		/// <param name="data">The decompressed byte array</param>
		/// <param name="level">The compression level</param>
		/// <returns>The compressed byte array</returns>
		public static byte[] Compress(byte[] data, CompressionLevel level) {
			using MemoryStream decompressed = new(data);
			using DeflateStream compression = new(decompressed, CompressionMode.Compress, level);
			using MemoryStream compressed = new();
			compression.CopyTo(compressed);
			return compressed.ToArray();
		}

		/// <summary>
		/// Decompresses <paramref name="data"/> at the provided <paramref name="level"/>
		/// </summary>
		/// <param name="data">The compressed byte array</param>
		/// <param name="level">The compression level</param>
		/// <returns>The decompressed byte array</returns>
		public static byte[] Decompress(byte[] data, CompressionLevel level) {
			using MemoryStream compressed = new(data);
			using DeflateStream decompression = new(compressed, CompressionMode.Decompress, level);
			using MemoryStream decompressed = new();
			decompression.CopyTo(decompressed);
			return decompressed.ToArray();
		}

		/// <summary>
		/// Gets the minimum number of bits required to send <paramref name="value"/> over the network
		/// </summary>
		public static int GetBitSize(byte value) => BitOperations.Log2(value) + 1;

		/// <summary>
		/// Gets the minimum number of bits required to send <paramref name="value"/> as an unsigned value over the network
		/// </summary>
		public static int GetBitSize(sbyte value) => BitOperations.Log2((byte)value) + 1;
		
		/// <summary>
		/// Gets the minimum number of bits required to send <paramref name="value"/> over the network
		/// </summary>
		public static int GetBitSize(ushort value) => BitOperations.Log2(value) + 1;
		
		/// <summary>
		/// Gets the minimum number of bits required to send <paramref name="value"/> as an unsigned value over the network
		/// </summary>
		public static int GetBitSize(short value) => BitOperations.Log2((ushort)value) + 1;
		
		/// <summary>
		/// Gets the minimum number of bits required to send <paramref name="value"/> over the network
		/// </summary>
		public static int GetBitSize(uint value) => BitOperations.Log2(value) + 1;
		
		/// <summary>
		/// Gets the minimum number of bits required to send <paramref name="value"/> as an unsigned value over the network
		/// </summary>
		public static int GetBitSize(int value) => BitOperations.Log2((uint)value) + 1;
		
		/// <summary>
		/// Gets the minimum number of bits required to send <paramref name="value"/> over the network
		/// </summary>
		public static int GetBitSize(ulong value) => BitOperations.Log2(value) + 1;
		
		/// <summary>
		/// Gets the minimum number of bits required to send <paramref name="value"/> as an unsigned value over the network
		/// </summary>
		public static int GetBitSize(long value) => BitOperations.Log2((ulong)value) + 1;
		
		/// <summary>
		/// Gets the minimum number of bits required to send <paramref name="value"/> as an unsigned value over the network<br/>
		/// This method assumes that the type argument is one of the primitive integer types; otherwise, an exception is thrown
		/// </summary>
		/// <exception cref="NotSupportedException"/>
		public static int GetBitSize<T>(T value) {
			if (typeof(T) == typeof(byte))
				return GetBitSize(Unsafe.As<T, byte>(ref value));
			else if (typeof(T) == typeof(sbyte))
				return GetBitSize(Unsafe.As<T, sbyte>(ref value));
			else if (typeof(T) == typeof(ushort))
				return GetBitSize(Unsafe.As<T, ushort>(ref value));
			else if (typeof(T) == typeof(short))
				return GetBitSize(Unsafe.As<T, short>(ref value));
			else if (typeof(T) == typeof(uint))
				return GetBitSize(Unsafe.As<T, uint>(ref value));
			else if (typeof(T) == typeof(int))
				return GetBitSize(Unsafe.As<T, int>(ref value));
			else if (typeof(T) == typeof(ulong))
				return GetBitSize(Unsafe.As<T, ulong>(ref value));
			else if (typeof(T) == typeof(long))
				return GetBitSize(Unsafe.As<T, long>(ref value));
			
			throw new NotSupportedException($"Unsupported type: {typeof(T)}");
		}

		/// <summary>
		/// Gets the minimum number of bytes required to send <paramref name="value"/> via <see cref="BinaryWriter.Write7BitEncodedInt(int)"/> over the network
		/// </summary>
		public static int GetByteSize(int value) {
			// BinaryWriter.Write7BitEncodedInt()
			uint write = (uint)value;
			int size = 1;

			while (write > 0x7Fu) {
				write >>= 7;
				size++;
			}

			return size;
		}
	}
}
