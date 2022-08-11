using System;
using System.Collections.Generic;

namespace MagicStorage {
	//.NET will sometimes have a stroke when using OrderBy and two entries end up being equal
	//The solution is just to ensure that equal values are handled properly
	// See: https://ayende.com/blog/188865-C/bad-sorting-and-other-pitfalls
	public sealed class SafeOrdering<T> : IComparer<T> {
		public readonly IComparer<T> orig;

		public readonly Func<T, string> reportObjectFunc;

		public SafeOrdering(IComparer<T> orig, Func<T, string> reportObjectFunc) {
			this.orig = orig;
			this.reportObjectFunc = reportObjectFunc;
		}

		public int Compare(T x, T y) {
			int order = orig.Compare(x, y);
			int reverse = orig.Compare(y, x);

			int sign = Math.Sign(order), signR = Math.Sign(reverse);

			//Throw a proper error here if the reverse comparison isn't the negative of "order"
			if ((sign != 0 && signR == 0) || (sign == 0 && signR != 0) || (sign != -signR))
				throw new ArgumentException($"Comparer returned inconsistent results (x.Compare(y): {sign}, y.Compare(x): {signR})\n" +
					$"Object #1: {reportObjectFunc(x)}\n" +
					$"Object #2: {reportObjectFunc(y)}");

			return order;
		}
	}
}
