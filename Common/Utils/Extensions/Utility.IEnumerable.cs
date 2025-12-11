using MagicStorage.Common;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.Sorting;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;

namespace MagicStorage {
	partial class Utility {
		/// <summary>
		/// Forces an enumeration to be evaluated, then returns the result
		/// </summary>
		public static IEnumerable<T> Evaluate<T>(this IEnumerable<T> enumerable)
			=> enumerable.ToArray();

		/// <summary>
		/// Flattens enumeration of enumerations into a single enumeration
		/// </summary>
		public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> @this) => @this.SelectMany(Identity);

		private static IEnumerable<T> Identity<T>(this IEnumerable<T> @this) => @this;

		internal static int ConstrainedSum(this IEnumerable<int> source) {
			ClampedArithmetic sum = 0;

			foreach (int i in source)
				sum += i;
			
			return sum;
		}

		public static IEnumerable<T> TakeIfLimitExists<T>(this IEnumerable<T> source, int? limit) => limit is int lim ? source.Take(lim) : source;

		public static IEnumerable<T> TakeLastIfLimitExists<T>(this IEnumerable<T> source, int? limit) => limit is int lim ? source.TakeLast(lim) : source;

		public static IEnumerable<TileEntity> ResolveTileEntities(this IEnumerable<Point16> positions) => positions.Select(ResolveToTileEntity).OfType<TileEntity>();

		public static IEnumerable<T> ResolveTileEntities<T>(this IEnumerable<Point16> position) where T : TileEntity => position.Select(ResolveToTileEntity).OfType<T>();
	}
}
