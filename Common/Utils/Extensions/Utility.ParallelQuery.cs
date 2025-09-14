using MagicStorage.Sorting;
using System;
using System.Linq;
using Terraria;

namespace MagicStorage {
	partial class Utility {
		internal static ParallelQuery<T> Filter<T>(this ParallelQuery<T> query, StorageGUI.ThreadContext thread, Func<T, Item> objToItem) {
			ArgumentNullException.ThrowIfNull(query);
			ArgumentNullException.ThrowIfNull(thread);
			ArgumentNullException.ThrowIfNull(objToItem);

			return new ThreadFilterParallelEnumerator<T>(thread, query, objToItem).GetQuery();
		}

		internal static ParallelQuery<Item> Filter(this ParallelQuery<Item> query, StorageGUI.ThreadContext thread) {
			ArgumentNullException.ThrowIfNull(query);
			ArgumentNullException.ThrowIfNull(thread);

			return new ThreadFilterParallelItemEnumerator(thread, query).GetQuery();
		}
	}
}
