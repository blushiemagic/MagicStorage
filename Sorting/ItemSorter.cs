using System;
using System.Collections.Generic;
using System.Linq;
using MagicStorage.Common.Systems;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.ModLoader;
using Terraria.ID;
using MagicStorage.CrossMod;
using System.Threading;
using System.Runtime.CompilerServices;

namespace MagicStorage.Sorting
{
	public static class ItemSorter
	{
		public class AggregateContext {
			public IEnumerable<Item> items;
			public IEnumerable<List<Item>> sourceItems;
			public ConditionalWeakTable<Item, byte[]> savedItemTagIO;
			internal List<List<Item>> enumeratedSource;

			public AggregateContext(IEnumerable<Item> items) {
				this.items = items;
				sourceItems = enumeratedSource = new();
				savedItemTagIO = new();
			}
		}

		public static IEnumerable<Item> SortAndFilter(StorageGUI.ThreadContext thread, int? takeCount = null, bool aggregate = true)
		{
			try {
				thread.context.items = DoFiltering(thread, thread.context.items, i => i);

				if (takeCount is int take)
					thread.context.items = thread.context.items.Take(take);

				thread.context.items = Aggregate(thread.context, thread.token, aggregate);

				thread.context.sourceItems = DoFiltering(thread, thread.context.sourceItems, i => i[0]);

				if (takeCount is int take2)
					thread.context.sourceItems = thread.context.sourceItems.Take(take2);

				thread.context.sourceItems = DoSorting(thread, thread.context.sourceItems, i => i[0]);

				return DoSorting(thread, thread.context.items, i => i);
			} catch when (thread.token.IsCancellationRequested) {
				thread.context.items = Array.Empty<Item>();
				return Array.Empty<Item>();
			}
		}

		public static IEnumerable<T> DoFiltering<T>(StorageGUI.ThreadContext thread, IEnumerable<T> source, Func<T, Item> objToItem) {
			try {
				ArgumentNullException.ThrowIfNull(objToItem);

				var filter = FilteringOptionLoader.Get(thread.filterMode)?.Filter;

				if (filter is null)
					throw new ArgumentOutOfRangeException(nameof(thread) + "." + nameof(thread.filterMode), "Filtering ID was invalid or its definition had a null filter");

				return source.Where(x => filter(objToItem(x)) && FilterBySearchText(objToItem(x), thread.searchText, thread.modSearch));
			} catch when (thread.token.IsCancellationRequested) {
				return Array.Empty<T>();
			}
		}

		public static IEnumerable<T> DoSorting<T>(StorageGUI.ThreadContext thread, IEnumerable<T> source, Func<T, Item> objToItem) {
			try {
				ArgumentNullException.ThrowIfNull(objToItem);

				if (thread.sortMode < 0)
					return source;

				//Apply "fuzzy" sorting since it's faster, but less accurate
				IOrderedEnumerable<T> orderedItems = SortingCache.dictionary.SortFuzzy(source, objToItem, thread.sortMode);

				var sorter = SortingOptionLoader.Get(thread.sortMode);

				if (!sorter.CacheFuzzySorting || sorter.SortAgainAfterFuzzy) {
					var sortFunc = sorter.Sorter.AsSafe(x => $"{x.Name} | ID: {x.type} | Mod: {x.ModItem?.Mod.Name ?? "Terraria"}");

					orderedItems = orderedItems.OrderByDescending(x => objToItem(x), sortFunc);
				}

				return orderedItems.ThenByDescending(x => objToItem(x).type).ThenByDescending(x => objToItem(x).value);
			} catch when (thread.token.IsCancellationRequested) {
				return Array.Empty<T>();
			}
		}

		//Formerly returned IEnumerable<Item> for lazy evaluation
		//Needs to return a collection so that "context.enumeratedSource" is properly assigned
		public static List<Item> Aggregate(AggregateContext context, CancellationToken token, bool actuallyAggregate = true)
		{
			try
			{
				Item lastItem = null;

				int sourceIndex = 0;

				List<Item> aggregate = new();

				foreach (Item item in context.items.OrderBy(i => i.type).ThenBy(i => i.prefix))
				{
					if (lastItem is null)
					{
						lastItem = item.Clone();
						context.enumeratedSource.Add(new() { item });
						continue;
					}

					bool combiningPermitted = ItemCombining.CanCombineItems(item, lastItem, checkPrefix: true, savedItemTagIO: context.savedItemTagIO);
					if (combiningPermitted && (!actuallyAggregate || lastItem.stack + item.stack > 0))
					{
						if (actuallyAggregate)
						{
							if (item.favorited)
							{
								lastItem.favorited = true;

								foreach (var source in context.enumeratedSource[sourceIndex])
									source.favorited = true;
							}

							Utility.CallOnStackHooks(lastItem, item, item.stack);

							lastItem.stack += item.stack;
						}

						context.enumeratedSource[sourceIndex].Add(item);
					}
					else
					{
						Item next = item.Clone();

						// Transfer stack from current item to "next item"
						if (combiningPermitted)
						{
							int transfer = int.MaxValue - lastItem.stack;

							Utility.CallOnStackHooks(lastItem, item, transfer);

							next.stack -= transfer;
							lastItem.stack = int.MaxValue;
						}

						aggregate.Add(lastItem);
						lastItem = next;
						context.enumeratedSource.Add(new() { item });
						sourceIndex++;
					}
				}

				if (lastItem is not null)
					aggregate.Add(lastItem);

				return aggregate;
			}
			catch when (token.IsCancellationRequested)
			{
				context.enumeratedSource.Clear();

				return new();
			}
			catch (Exception e) {
				MagicStorageMod.Instance.Logger.Error(e);
				return new();
			}
		}

		public static ParallelQuery<Recipe> GetRecipes(StorageGUI.ThreadContext thread) {
			try {
				IEnumerable<Recipe> recipes;

				FilteringOption filterOption = FilteringOptionLoader.Get(thread.filterMode);

				if (filterOption is null)
					recipes = Array.Empty<Recipe>();  // Failsafe
				else if (filterOption.UsesFilterCache)
					recipes = MagicCache.FilteredRecipesCache[thread.filterMode];
				else {
					var filter = filterOption.Filter;

					recipes = MagicCache.EnabledRecipes.Where(r => filter(r.createItem));
				}

				return recipes
					.AsParallel()
					.AsOrdered()
					.Where(recipe => FilterBySearchText(recipe.createItem, thread.searchText, thread.modSearch));
			} catch when (thread.token.IsCancellationRequested) {
				return Array.Empty<Recipe>().AsParallel();
			}
		}

		internal static bool FilterBySearchText(Item item, string filter, int modSearchIndex, bool modSearched = false) {
			if (!modSearched && modSearchIndex != ModSearchBox.ModIndexAll)
				filter += "@" + ModSearchBox.GetNameFromIndex(modSearchIndex);

			if (string.IsNullOrWhiteSpace(filter))
				return true;
			
			filter = filter.Trim();

			char first = filter[0];

			if (first == '#') {
				//First character is a "#"?  Treat the search as a tooltip search
				if (filter.Length > 1) {
					filter = filter[1..];
					try
					{
						return GetItemTooltipLines(item).Any(line => line.Contains(filter, StringComparison.OrdinalIgnoreCase));
					}
					catch
					{
						return false;
					}
				} else
					return true;  //Empty tooltip = anything is valid
			} else if (first == '@' && !modSearched) {
				//First character is a "@"?  Treat the first "word" as a mod search and the rest as a normal search
				if (filter.Length > 1) {
					string mod = filter[1..];
					string remaining;
					int space;
					
					if ((space = mod.IndexOf(' ')) > -1) {
						remaining = mod[space..];
						mod = mod[..space];
					} else
						remaining = "";

					return (item.ModItem?.Mod.Name ?? "Terraria").Contains(mod, StringComparison.OrdinalIgnoreCase) && FilterBySearchText(item, remaining, modSearchIndex, modSearched: true);
				} else
					return true;  //Empty mod name = anything is valid
			}

			return item.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);
		}

		private static IEnumerable<string> GetItemTooltipLines(Item item) {
			Item hoverItem = item;
			int yoyoLogo = -1;
			int researchLine = -1;
			int rare = hoverItem.rare;
			float knockBack = hoverItem.knockBack;
			float num = 1f;
			if (hoverItem.CountsAsClass(DamageClass.Melee) && Main.LocalPlayer.kbGlove)
				num += 1f;

			if (Main.LocalPlayer.kbBuff)
				num += 0.5f;

			if (num != 1f)
				hoverItem.knockBack *= num;

			if (hoverItem.CountsAsClass(DamageClass.Ranged) && Main.LocalPlayer.shroomiteStealth)
				hoverItem.knockBack *= 1f + (1f - Main.LocalPlayer.stealth) * 0.5f;

			int num2 = 30;
			int numLines = 1;
			string[] array = new string[num2];
			bool[] array2 = new bool[num2];
			bool[] array3 = new bool[num2];
			for (int i = 0; i < num2; i++) {
				array2[i] = false;
				array3[i] = false;
			}
			string[] tooltipNames = new string[num2];

			Main.MouseText_DrawItemTooltip_GetLinesInfo(item, ref yoyoLogo, ref researchLine, knockBack, ref numLines, array, array2, array3, tooltipNames);

			// Fix a bug where item knockback grows to infinity
			hoverItem.knockBack = knockBack;

			if (Main.npcShop > 0 && hoverItem.value >= 0 && (hoverItem.type < ItemID.CopperCoin || hoverItem.type > ItemID.PlatinumCoin)) {
				Main.LocalPlayer.GetItemExpectedPrice(hoverItem, out int calcForSelling, out int calcForBuying);

				int num5 = (hoverItem.isAShopItem || hoverItem.buyOnce) ? calcForBuying : calcForSelling;
				if (hoverItem.shopSpecialCurrency != -1) {
					tooltipNames[numLines] = "SpecialPrice";
					CustomCurrencyManager.GetPriceText(hoverItem.shopSpecialCurrency, array, ref numLines, num5);
				} else if (num5 > 0) {
					string text = "";
					int num6 = 0;
					int num7 = 0;
					int num8 = 0;
					int num9 = 0;
					int num10 = num5 * hoverItem.stack;
					if (!hoverItem.buy) {
						num10 = num5 / 5;
						if (num10 < 1)
							num10 = 1;

						int num11 = num10;
						num10 *= hoverItem.stack;
						int amount = Main.shopSellbackHelper.GetAmount(hoverItem);
						if (amount > 0)
							num10 += (-num11 + calcForBuying) * Math.Min(amount, hoverItem.stack);
					}

					if (num10 < 1)
						num10 = 1;

					if (num10 >= 1000000) {
						num6 = num10 / 1000000;
						num10 -= num6 * 1000000;
					}

					if (num10 >= 10000) {
						num7 = num10 / 10000;
						num10 -= num7 * 10000;
					}

					if (num10 >= 100) {
						num8 = num10 / 100;
						num10 -= num8 * 100;
					}

					if (num10 >= 1)
						num9 = num10;

					if (num6 > 0)
						text = text + num6 + " " + Lang.inter[15].Value + " ";

					if (num7 > 0)
						text = text + num7 + " " + Lang.inter[16].Value + " ";

					if (num8 > 0)
						text = text + num8 + " " + Lang.inter[17].Value + " ";

					if (num9 > 0)
						text = text + num9 + " " + Lang.inter[18].Value + " ";

					if (!hoverItem.buy)
						array[numLines] = Lang.tip[49].Value + " " + text;
					else
						array[numLines] = Lang.tip[50].Value + " " + text;

					tooltipNames[numLines] = "Price";
					numLines++;
				} else if (hoverItem.type != ItemID.DefenderMedal) {
					array[numLines] = Lang.tip[51].Value;
					tooltipNames[numLines] = "Price";
					numLines++;
				}
			}

			List<TooltipLine> lines = ItemLoader.ModifyTooltips(item, ref numLines, tooltipNames, ref array, ref array2, ref array3, ref yoyoLogo, out _);

			return lines.Select(line => line.Text);
		}
	}
}
