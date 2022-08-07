using System.Collections.Generic;

namespace MagicStorage {
	//.NET will sometimes have a stroke when using OrderBy and two entries end up being equal
	//The solution is just to ensure that equal values are handled properly
	// See: https://ayende.com/blog/188865-C/bad-sorting-and-other-pitfalls
	public sealed class SafeOrdering<T> : IComparer<T> {
		public readonly IComparer<T> orig;

		public SafeOrdering(IComparer<T> orig) {
			this.orig = orig;
		}

		public int Compare(T x, T y) {
			int order = orig.Compare(x, y);

			if (order == 0)
				return 1;

			return order;
		}
	}
}
