using MagicStorage.Components;
using MagicStorage.CrossMod;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.UI.Selling {
	internal static class DuplicateItemFinder {
		private class DuplicateContext {
			public Item keep;
			public readonly List<Item> duplicates = new();

			public DuplicateContext(Item keep) {
				this.keep = keep;
			}
		}

		public static List<Item> GetDuplicateItems(TEStorageHeart heart, SellingDuplicateFinder finder) {
			Dictionary<int, DuplicateContext> contextByType = new();

			NetHelper.Report(true, $"Detecting duplicates to sell from storage heart (X: {heart.Position.X}, Y: {heart.Position.X})...");

			IEnumerable<Item> items = heart.ComponentManager.GetRealStorageUnitEntities().SelectMany(static u => u.GetItems()).Where(static i => !i.IsAir);

			foreach (Item item in items) {
				// Only select duplicates of unstackable items
				if (item.maxStack > 1)
					continue;

				// Ignore favorited items
				if (item.favorited)
					continue;

				if (!contextByType.TryGetValue(item.type, out var context)) {
					contextByType[item.type] = context = new DuplicateContext(item);
					continue;
				}

				if (finder.IsValidForDuplicateSelling(context.keep, item)) {
					Item betterItem = finder.GetBetterItem(context.keep, item);
					
					if (object.ReferenceEquals(betterItem, context.keep))
						context.duplicates.Add(item);
					else if (object.ReferenceEquals(betterItem, item)) {
						context.duplicates.Add(context.keep);
						context.keep = item;
					} else
						throw new InvalidOperationException($"{finder.GetType().Name} returned an item via GetBetterItem that wasn't one of the provided items");
				}
			}

			return contextByType.Values.SelectMany(static c => c.duplicates).ToList();
		}
	}
}
