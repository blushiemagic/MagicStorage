using System;
using System.Numerics;

namespace MagicStorage {
	partial class Utility {
		/// <summary>
		/// Rounds a value up to the next multiple of the specified size.
		/// </summary>
		/// <typeparam name="T">The unsigned numeric type.</typeparam>
		/// <param name="value">The value to round.</param>
		/// <param name="size">The multiple size.</param>
		/// <returns><paramref name="value" /> rounded up to a multiple of <paramref name="size" />.</returns>
		public static T CeilingMultiple<T>(T value, T size) where T : IUnsignedNumber<T> {
			ArgumentOutOfRangeException.ThrowIfZero(size);
			return value == T.Zero ? T.Zero : ((value - T.One) / size + T.One) * size;
		}
	}
}
