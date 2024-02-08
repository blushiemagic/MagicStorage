using System;
using System.Runtime.CompilerServices;

namespace MagicStorage.Common {
	/// <summary>
	/// A wrapper structure over an integer used to facilitate overflow checks when calculating a sum or difference
	/// </summary>
	public readonly record struct ClampedArithmetic(int Value) : IEquatable<ClampedArithmetic>, IComparable<ClampedArithmetic> {
		public static readonly ClampedArithmetic Min = new ClampedArithmetic(int.MinValue);
		public static readonly ClampedArithmetic Max = new ClampedArithmetic(int.MaxValue);

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static int AddWithOverflowCheck(int a, int b, out bool overflowFlag) {
			unchecked {
				int c = a + b;
				overflowFlag = ((a ^ b) >= 0) & ((a ^ c) < 0);
				return c;
			}
		}

		public int CompareTo(ClampedArithmetic other) {
			return Value.CompareTo(other.Value);
		}

		public static ClampedArithmetic operator+(ClampedArithmetic a, ClampedArithmetic b) {
			int newValue = AddWithOverflowCheck(a.Value, b.Value, out bool overflowFlag);
			if (overflowFlag)
				return a.Value < 0 ? Min : Max;
			return new ClampedArithmetic(newValue);
		}

		public static ClampedArithmetic operator+(ClampedArithmetic a, int b) {
			int newValue = AddWithOverflowCheck(a.Value, b, out bool overflowFlag);
			if (overflowFlag)
				return a.Value < 0 ? Min : Max;
			return new ClampedArithmetic(newValue);
		}

		public static ClampedArithmetic operator+(int a, ClampedArithmetic b) {
			int newValue = AddWithOverflowCheck(a, b.Value, out bool overflowFlag);
			if (overflowFlag)
				return a < 0 ? Min : Max;
			return new ClampedArithmetic(newValue);
		}

		public static ClampedArithmetic operator-(ClampedArithmetic a, ClampedArithmetic b) {
			int newValue = AddWithOverflowCheck(a.Value, -b.Value, out bool overflowFlag);
			if (overflowFlag)
				return a.Value < 0 ? Min : Max;
			return new ClampedArithmetic(newValue);
		}

		public static ClampedArithmetic operator-(ClampedArithmetic a, int b) {
			int newValue = AddWithOverflowCheck(a.Value, -b, out bool overflowFlag);
			if (overflowFlag)
				return a.Value < 0 ? Min : Max;
			return new ClampedArithmetic(newValue);
		}

		public static ClampedArithmetic operator-(int a, ClampedArithmetic b) {
			int newValue = AddWithOverflowCheck(a, -b.Value, out bool overflowFlag);
			if (overflowFlag)
				return a < 0 ? Min : Max;
			return new ClampedArithmetic(newValue);
		}

		public static implicit operator ClampedArithmetic(int value) => new ClampedArithmetic(value);

		public static implicit operator int(ClampedArithmetic sum) => sum.Value;
	}

	/// <summary>
	/// A wrapper structure over a long integer used to facilitate overflow checks when calculating a sum or difference
	/// </summary>
	/// <param name="Value"></param>
	public readonly record struct ClampedLongArithmetic(long Value) : IEquatable<ClampedLongArithmetic>, IComparable<ClampedLongArithmetic> {
		public static readonly ClampedLongArithmetic Min = new ClampedLongArithmetic(long.MinValue);
		public static readonly ClampedLongArithmetic Max = new ClampedLongArithmetic(long.MaxValue);

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static long AddWithOverflowCheck(long a, long b, out bool overflowFlag) {
			unchecked {
				long c = a + b;
				overflowFlag = ((a ^ b) >= 0) & ((a ^ c) < 0);
				return c;
			}
		}

		public int CompareTo(ClampedLongArithmetic other) {
			return Value.CompareTo(other.Value);
		}

		public static ClampedLongArithmetic operator+(ClampedLongArithmetic a, ClampedLongArithmetic b) {
			long newValue = AddWithOverflowCheck(a.Value, b.Value, out bool overflowFlag);
			if (overflowFlag)
				return a.Value < 0 ? Min : Max;
			return new ClampedLongArithmetic(newValue);
		}

		public static ClampedLongArithmetic operator+(ClampedLongArithmetic a, long b) {
			long newValue = AddWithOverflowCheck(a.Value, b, out bool overflowFlag);
			if (overflowFlag)
				return a.Value < 0 ? Min : Max;
			return new ClampedLongArithmetic(newValue);
		}

		public static ClampedLongArithmetic operator+(long a, ClampedLongArithmetic b) {
			long newValue = AddWithOverflowCheck(a, b.Value, out bool overflowFlag);
			if (overflowFlag)
				return a < 0 ? Min : Max;
			return new ClampedLongArithmetic(newValue);
		}

		public static ClampedLongArithmetic operator-(ClampedLongArithmetic a, ClampedLongArithmetic b) {
			long newValue = AddWithOverflowCheck(a.Value, -b.Value, out bool overflowFlag);
			if (overflowFlag)
				return a.Value < 0 ? Min : Max;
			return new ClampedLongArithmetic(newValue);
		}

		public static ClampedLongArithmetic operator-(ClampedLongArithmetic a, long b) {
			long newValue = AddWithOverflowCheck(a.Value, -b, out bool overflowFlag);
			if (overflowFlag)
				return a.Value < 0 ? Min : Max;
			return new ClampedLongArithmetic(newValue);
		}

		public static ClampedLongArithmetic operator-(long a, ClampedLongArithmetic b) {
			long newValue = AddWithOverflowCheck(a, -b.Value, out bool overflowFlag);
			if (overflowFlag)
				return a < 0 ? Min : Max;
			return new ClampedLongArithmetic(newValue);
		}

		public static implicit operator ClampedLongArithmetic(long value) => new ClampedLongArithmetic(value);

		public static implicit operator long(ClampedLongArithmetic sum) => sum.Value;
	}
}
