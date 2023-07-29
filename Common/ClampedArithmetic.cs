using System;
using System.Runtime.CompilerServices;

namespace MagicStorage.Common {
	/// <summary>
	/// A wrapper structure over an integer used to facilitate overflow checks when calculating a sum or difference
	/// </summary>
	public readonly record struct ClampedArithmetic(int Value) : IEquatable<ClampedArithmetic>, IComparable<ClampedArithmetic> {
		public static readonly ClampedArithmetic Min = new ClampedArithmetic(int.MinValue);
		public static readonly ClampedArithmetic Max = new ClampedArithmetic(int.MaxValue);

		public int CompareTo(ClampedArithmetic other) {
			return Value.CompareTo(other.Value);
		}

		public static ClampedArithmetic operator+(ClampedArithmetic a, ClampedArithmetic b) {
			return ProcessWithPotentialOverflow(a.Value, b.Value, true);
		}

		public static ClampedArithmetic operator+(ClampedArithmetic a, int b) {
			return ProcessWithPotentialOverflow(a.Value, b, true);
		}

		public static ClampedArithmetic operator+(int a, ClampedArithmetic b) {
			return ProcessWithPotentialOverflow(a, b.Value, true);
		}

		public static ClampedArithmetic operator-(ClampedArithmetic a, ClampedArithmetic b) {
			return ProcessWithPotentialOverflow(a.Value, b.Value, false);
		}

		public static ClampedArithmetic operator-(ClampedArithmetic a, int b) {
			return ProcessWithPotentialOverflow(a.Value, b, false);
		}

		public static ClampedArithmetic operator-(int a, ClampedArithmetic b) {
			return ProcessWithPotentialOverflow(a, b.Value, false);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static ClampedArithmetic ProcessWithPotentialOverflow(int a, int b, bool add) {
			if (add) {
				// Overflow from positive to negative
				if (a > 0 && b > 0 && a + b < 0)
					return Max;

				// Underflow from negative to positive
				if (a < 0 && b < 0 && a + b > 0)
					return Min;

				return a + b;
			} else {
				// Underflow from negative to positive
				if (a < 0 && b > 0 && a - b > 0)
					return Min;

				// Overflow from positive to negative
				if (a > 0 && b < 0 && a - b < 0)
					return Max;

				return a - b;
			}
		}

		public static implicit operator ClampedArithmetic(int value) => new ClampedArithmetic(value);

		public static implicit operator int(ClampedArithmetic sum) => sum.Value;
	}
}
