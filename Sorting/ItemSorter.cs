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

			public bool uniqueSlotPerItemStack;

			public AggregateContext(IEnumerable<Item> items) {
				this.items = items;
				sourceItems = enumeratedSource = new();
				savedItemTagIO = new();
			}
		}

		private static Item SelectFirstItem(List<Item> items) => items[0];

		public static IEnumerable<Item> SortAndFilter(StorageGUI.ThreadContext thread, int? takeCount = null)
		{
			try {
				thread.context.items = DoFiltering(thread, thread.context.items);

				thread.CompleteOneTask();

				if (takeCount is int take)
					thread.context.items = thread.context.items.Take(take);

				thread.context.items = Aggregate(thread.context, thread.token);

				thread.CompleteOneTask();

				thread.context.sourceItems = DoFiltering(thread, thread.context.sourceItems, SelectFirstItem);

				thread.CompleteOneTask();

				if (takeCount is int take2)
					thread.context.sourceItems = thread.context.sourceItems.Take(take2);

				thread.context.sourceItems = DoSorting(thread, thread.context.sourceItems, SelectFirstItem);

				thread.CompleteOneTask();

				var items = DoSorting(thread, thread.context.items);

				thread.CompleteOneTask();

				return items;
			} catch when (thread.token.IsCancellationRequested) {
				thread.context.items = Array.Empty<Item>();
				return Array.Empty<Item>();
			}
		}

		public static IEnumerable<T> DoFiltering<T>(StorageGUI.ThreadContext thread, IEnumerable<T> source, Func<T, Item> objToItem) {
			try {
				ArgumentNullException.ThrowIfNull(objToItem);
				return source.Filter(thread, objToItem);
			} catch when (thread.token.IsCancellationRequested) {
				return Array.Empty<T>();
			}
		}

		public static IEnumerable<Item> DoFiltering(StorageGUI.ThreadContext thread, IEnumerable<Item> source) {
			try {
				return source.Filter(thread);
			} catch when (thread.token.IsCancellationRequested) {
				return Array.Empty<Item>();
			}
		}

		public static IEnumerable<T> DoSorting<T>(StorageGUI.ThreadContext thread, IEnumerable<T> source, Func<T, Item> objToItem) {
			try {
				ArgumentNullException.ThrowIfNull(objToItem);
				return new ThreadSortOrderedEnumerable<T>(thread, source, objToItem);
			} catch when (thread.token.IsCancellationRequested) {
				return Array.Empty<T>();
			}
		}

		public static IEnumerable<Item> DoSorting(StorageGUI.ThreadContext thread, IEnumerable<Item> source) {
			try {
				return new ThreadSortOrderedItemEnumerable(thread, source);
			} catch when (thread.token.IsCancellationRequested) {
				return Array.Empty<Item>();
			}
		}

		//Formerly returned IEnumerable<Item> for lazy evaluation
		//Needs to return a collection so that "context.enumeratedSource" is properly assigned
		public static List<Item> Aggregate(AggregateContext context, CancellationToken token)
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

					bool combiningPermitted = StorageAggregator.CanCombineItems(item, lastItem, checkPrefix: true, strict: true, savedItemTagIO: context.savedItemTagIO);
					if (combiningPermitted && (context.uniqueSlotPerItemStack || lastItem.stack + item.stack > 0))
					{
						if (!context.uniqueSlotPerItemStack)
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
						else
						{
							aggregate.Add(lastItem);
							lastItem = item.Clone();
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

		internal static Item GetRecipeResult(Recipe recipe) => recipe.createItem;

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

				thread.CompleteOneTask();

				var filteredRecipes = recipes
					.AsParallel()
					.AsOrdered()
					.Filter(thread, GetRecipeResult);

				thread.CompleteOneTask();

				return filteredRecipes;
			} catch when (thread.token.IsCancellationRequested) {
				return Array.Empty<Recipe>().AsParallel();
			}
		}

		public static ParallelQuery<int> GetShimmerItems(StorageGUI.ThreadContext thread) {
			try {
				IEnumerable<Item> items;

				FilteringOption filterOption = FilteringOptionLoader.Get(thread.filterMode);

				if (filterOption is null)
					items = Array.Empty<Item>();  // Failsafe
				else if (filterOption.UsesFilterCache)
					items = MagicCache.FilteredItemsCache[thread.filterMode];
				else {
					var filter = filterOption.Filter;

					items = MagicCache.ItemSamples.Where(i => filter(i));
				}

				thread.CompleteOneTask();

				var filteredItems = items
					.AsParallel()
					.AsOrdered()
					.Filter(thread);

				thread.CompleteOneTask();

				return filteredItems.Select(item => item.type);
			} catch when (thread.token.IsCancellationRequested) {
				return Array.Empty<int>().AsParallel();
			}
		}

		public static bool RecipePassesFilter(Recipe recipe, StorageGUI.ThreadContext thread) => PassesFilter(recipe, thread, static r => r.createItem);

		public static bool ItemPassesFilter(int item, StorageGUI.ThreadContext thread) => PassesFilter(item, thread, Utility.GetItemSample);

		public static bool PassesFilter<T>(T value, StorageGUI.ThreadContext thread, Func<T, Item> objToItem) {
			FilteringOption filterOption = FilteringOptionLoader.Get(thread.filterMode);

			if (filterOption is null)
				return true;  // Failsafe

			var item = objToItem(value);

			return filterOption.Filter(item) && FilterBySearchText(item, thread.searchText, thread.modSearch);
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

			long num2 = 30;
			int numLines = 1;
			string[] array = new string[num2];
			bool[] array2 = new bool[num2];
			bool[] array3 = new bool[num2];
			for (int i = 0; i < num2; i++) {
				array2[i] = false;
				array3[i] = false;
			}
			string[] tooltipNames = new string[num2];

			Main.MouseText_DrawItemTooltip_GetLinesInfo(item, ref yoyoLogo, ref researchLine, knockBack, ref numLines, array, array2, array3, tooltipNames, out _);

			// Fix a bug where item knockback grows to infinity
			hoverItem.knockBack = knockBack;

			if (Main.npcShop > 0 && hoverItem.value >= 0 && (hoverItem.type < ItemID.CopperCoin || hoverItem.type > ItemID.PlatinumCoin)) {
				Main.LocalPlayer.GetItemExpectedPrice(hoverItem, out long calcForSelling, out long calcForBuying);

				long num5 = (hoverItem.isAShopItem || hoverItem.buyOnce) ? calcForBuying : calcForSelling;
				if (hoverItem.shopSpecialCurrency != -1) {
					tooltipNames[numLines] = "SpecialPrice";
					CustomCurrencyManager.GetPriceText(hoverItem.shopSpecialCurrency, array, ref numLines, num5);
				} else if (num5 > 0) {
					string text = "";
					long num6 = 0;
					long num7 = 0;
					long num8 = 0;
					long num9 = 0;
					long num10 = num5 * hoverItem.stack;
					if (!hoverItem.buy) {
						num10 = num5 / 5;
						if (num10 < 1)
							num10 = 1;

						long num11 = num10;
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

			List<TooltipLine> lines = ItemLoader.ModifyTooltips(item, ref numLines, tooltipNames, ref array, ref array2, ref array3, ref yoyoLogo, out _, 0);

			return lines.Select(line => line.Text);
		}
	}
}
