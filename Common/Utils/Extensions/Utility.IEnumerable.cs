using MagicStorage.Common;
using System.Collections.Generic;
using System.Linq;
using Terraria.DataStructures;

namespace MagicStorage {
	partial class Utility {
		/// <summary>
		/// Forces an enumeration to be evaluated, then returns the result
		/// </summary>
		/// <typeparam name="T">The element type.</typeparam>
		/// <param name="enumerable">The sequence to evaluate.</param>
		/// <returns>The evaluated sequence.</returns>
		public static IEnumerable<T> Evaluate<T>(this IEnumerable<T> enumerable)
			=> enumerable.ToArray();

		/// <summary>
		/// Flattens enumeration of enumerations into a single enumeration
		/// </summary>
		/// <typeparam name="T">The element type.</typeparam>
		/// <param name="this">The nested sequence to flatten.</param>
		/// <returns>A flattened sequence containing every nested element.</returns>
		public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> @this) => @this.SelectMany(Identity);

		private static IEnumerable<T> Identity<T>(this IEnumerable<T> @this) => @this;

		internal static int ConstrainedSum(this IEnumerable<int> source) {
			ClampedArithmetic sum = 0;

			foreach (int i in source)
				sum += i;
			
			return sum;
		}

		/// <summary>
		/// Applies <see cref="Enumerable.Take{TSource}(IEnumerable{TSource}, int)" /> when a limit is present.
		/// </summary>
		/// <typeparam name="T">The element type.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <param name="limit">The optional item limit.</param>
		/// <returns>The limited sequence, or the original sequence when no limit is present.</returns>
		public static IEnumerable<T> TakeIfLimitExists<T>(this IEnumerable<T> source, int? limit) => limit is int lim ? source.Take(lim) : source;

		/// <summary>
		/// Applies <see cref="Enumerable.TakeLast{TSource}(IEnumerable{TSource}, int)" /> when a limit is present.
		/// </summary>
		/// <typeparam name="T">The element type.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <param name="limit">The optional item limit.</param>
		/// <returns>The tail-limited sequence, or the original sequence when no limit is present.</returns>
		public static IEnumerable<T> TakeLastIfLimitExists<T>(this IEnumerable<T> source, int? limit) => limit is int lim ? source.TakeLast(lim) : source;

		/// <summary>
		/// Resolves tile positions to existing tile entities.
		/// </summary>
		/// <param name="positions">The tile positions to resolve.</param>
		/// <returns>The tile entities found at the specified positions.</returns>
		public static IEnumerable<TileEntity> ResolveTileEntities(this IEnumerable<Point16> positions) => positions.Select(ResolveToTileEntity).OfType<TileEntity>();

		/// <summary>
		/// Resolves tile positions to existing tile entities of the specified type.
		/// </summary>
		/// <typeparam name="T">The tile entity type to return.</typeparam>
		/// <param name="position">The tile positions to resolve.</param>
		/// <returns>The matching tile entities found at the specified positions.</returns>
		public static IEnumerable<T> ResolveTileEntities<T>(this IEnumerable<Point16> position) where T : TileEntity => position.Select(ResolveToTileEntity).OfType<T>();
	}
}
