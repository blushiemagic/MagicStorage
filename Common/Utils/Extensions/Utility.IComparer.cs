using System;
using System.Collections.Generic;

namespace MagicStorage {
	partial class Utility {
		public static SafeOrdering<T> AsSafe<T>(this IComparer<T> comparer, Func<T, string> reportObjectFunc) => new(comparer, reportObjectFunc);
	}
}
