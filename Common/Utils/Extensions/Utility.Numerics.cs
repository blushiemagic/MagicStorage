namespace MagicStorage {
	partial class Utility {
		public static T Repeat<T>(this T number, T max) where T : System.Numerics.INumberBase<T>, System.Numerics.IComparisonOperators<T, T, bool>, System.Numerics.IModulusOperators<T, T, T>
			=> number.Repeat(T.Zero, max);

		public static T Repeat<T>(this T number, T min, T max) where T : System.Numerics.INumberBase<T>, System.Numerics.IComparisonOperators<T, T, bool>, System.Numerics.IModulusOperators<T, T, T> {
			// Modulus, but keeps the result between "min" and "max"
			// I.e. Repeat(-1, 0, 10) = 9
			T range = max - min;
			T result = (number - min) % range;
			return result + (result < min ? range : min);
		}
	}
}
