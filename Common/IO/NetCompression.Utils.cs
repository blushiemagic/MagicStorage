using Ionic.Zlib;
using MagicStorage.Items.ErrorDisplay;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

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

		public static int GetBitSize(byte value) => BitOperations.Log2(value) + 1;

		public static int GetBitSize(sbyte value) => BitOperations.Log2((byte)value) + 1;

		public static int GetBitSize(ushort value) => BitOperations.Log2(value) + 1;

		public static int GetBitSize(short value) => BitOperations.Log2((ushort)value) + 1;

		public static int GetBitSize(uint value) => BitOperations.Log2(value) + 1;

		public static int GetBitSize(int value) => BitOperations.Log2((uint)value) + 1;

		public static int GetBitSize(ulong value) => BitOperations.Log2(value) + 1;

		public static int GetBitSize(long value) => BitOperations.Log2((ulong)value) + 1;

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
	}
}
