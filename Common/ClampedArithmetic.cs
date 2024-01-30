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
}
