using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using MagicStorage.Common.Global;
using MagicStorage.Common.Systems;
using MagicStorage.Components;
using MagicStorage.CrossMod;
using MagicStorage.Sorting;
using MagicStorage.UI;
using MagicStorage.UI.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;

namespace MagicStorage
{
	public static class StorageGUI
	{
		public const int padding = 4;
		public const int numColumns = 10;
		public const float inventoryScale = 0.85f;
		public const int startMaxRightClickTimer = 20;

		public static MouseState curMouse;
		public static MouseState oldMouse;

		internal static int slotFocus = -1;

		private static int rightClickTimer;
		private static int maxRightClickTimer = startMaxRightClickTimer;

		internal static readonly float scrollBarViewSize = 1f;
		internal static float scrollBarMaxViewSize = 2f;

		internal static readonly List<Item> items = new();
		internal static readonly List<List<Item>> sourceItems = new();
		internal static readonly List<bool> didMatCheck = new();

		//Legacy properties required for UISearchBar to function properly
		public static bool MouseClicked => curMouse.LeftButton == ButtonState.Pressed && oldMouse.LeftButton == ButtonState.Released;
		public static bool RightMouseClicked => curMouse.RightButton == ButtonState.Pressed && oldMouse.RightButton == ButtonState.Released;

		internal static void Unload() {
			showcaseItems = null;
		}

		public static TEStorageHeart GetHeart() => StoragePlayer.LocalPlayer.GetStorageHeart();

		public static bool needRefresh;

		public static event Action OnRefresh;

		internal static void CheckRefresh() {
			if (needRefresh)
				RefreshItems();
		}

		internal static void InvokeOnRefresh() => OnRefresh?.Invoke();

		public static void RefreshItems()
		{
			if (StoragePlayer.IsStorageCrafting())
			{
				CraftingGUI.RefreshItems();
				return;
			}

			if (StoragePlayer.IsStorageEnvironment())
				return;

			items.Clear();
			didMatCheck.Clear();
			sourceItems.Clear();
			TEStorageHeart heart = GetHeart();
			if (heart == null)
				return;

			NetHelper.Report(true, "Refreshing storage items");

			int sortMode = MagicUI.storageUI.GetPage<SortingPage>("Sorting").option;
			int filterMode = MagicUI.storageUI.GetPage<FilteringPage>("Filtering").option;

			var storagePage = MagicUI.storageUI.GetPage<StorageUIState.StoragePage>("Storage");

			string searchText = storagePage.searchBar.Text;
			bool onlyFavorites = storagePage.filterFavorites.Value;
			int modSearch = storagePage.modSearchBox.ModIndex;

			void DoFiltering()
			{
				ItemSorter.AggregateContext context;

				if (filterMode == FilteringOptionLoader.Definitions.Recent.Type)
				{
					Dictionary<int, Item> stored = heart.GetStoredItems().GroupBy(x => x.type).ToDictionary(x => x.Key, x => x.First());

					IEnumerable<Item> toFilter = heart.UniqueItemsPutHistory.Reverse().Where(x => stored.ContainsKey(x.type)).Select(x => stored[x.type]);

					context = new(toFilter);

					context.items = ItemSorter.SortAndFilter(context, sortMode == SortingOptionLoader.Definitions.Default.Type ? -1 : sortMode, FilteringOptionLoader.Definitions.All.Type, searchText, modSearch, 100);
				}
				else
				{
					context = new(heart.GetStoredItems());
					context.items = ItemSorter.SortAndFilter(context, sortMode, filterMode, searchText, modSearch);
				}

				if (MagicStorageConfig.CraftingFavoritingEnabled) {
					context.items = context.items.OrderByDescending(x => x.favorited ? 1 : 0);
					context.sourceItems = context.sourceItems.OrderByDescending(x => x[0].favorited ? 1 : 0);
				}

				items.AddRange(context.items.Where(x => !MagicStorageConfig.CraftingFavoritingEnabled || !onlyFavorites || x.favorited));

				sourceItems.AddRange(context.sourceItems.Where(x => !MagicStorageConfig.CraftingFavoritingEnabled || !onlyFavorites || x[0].favorited));

				NetHelper.Report(false, "Filtering applied.  Item count: " + items.Count);
			}

			DoFiltering();

			// now if nothing found we disable filters one by one
			if (searchText.Trim().Length > 0)
			{
				if (items.Count == 0 && filterMode != FilteringOptionLoader.Definitions.All.Type)
				{
					// search all categories
					filterMode = FilteringOptionLoader.Definitions.All.Type;

					Main.NewText(Language.GetTextValue("Mods.MagicStorage.Warnings.StorageDefaultToAllItems"), Color.Red);

					DoFiltering();
				}

				if (items.Count == 0 && modSearch != ModSearchBox.ModIndexAll)
				{
					// search all mods
					modSearch = ModSearchBox.ModIndexAll;

					Main.NewText(Language.GetTextValue("Mods.MagicStorage.Warnings.StorageDefaultToAllMods"), Color.Red);

					DoFiltering();
				}
			}

			for (int k = 0; k < items.Count; k++)
				didMatCheck.Add(false);

			OnRefresh?.Invoke();
			needRefresh = false;
		}

		internal static void ResetSlotFocus()
		{
			slotFocus = -1;
			rightClickTimer = 0;
			maxRightClickTimer = startMaxRightClickTimer;
		}

		internal static void SlotFocusLogic()
		{
			if (slotFocus >= items.Count ||
				!Main.mouseItem.IsAir && (!ItemCombining.CanCombineItems(Main.mouseItem, items[slotFocus]) || Main.mouseItem.stack >= Main.mouseItem.maxStack))
			{
				ResetSlotFocus();
			}
			else
			{
				if (rightClickTimer <= 0)
				{
					rightClickTimer = maxRightClickTimer;
					maxRightClickTimer = maxRightClickTimer * 3 / 4;
					if (maxRightClickTimer <= 0)
						maxRightClickTimer = 1;
					Item toWithdraw = items[slotFocus].Clone();
					toWithdraw.stack = 1;
					Item result = DoWithdraw(toWithdraw);
					if (Main.mouseItem.IsAir)
						Main.mouseItem = result;
					else
						Main.mouseItem.stack += result.stack;

					RefreshItems();
					SoundEngine.PlaySound(SoundID.MenuTick);
				}

				rightClickTimer--;
			}
		}

		internal static List<Item> showcaseItems;

		internal static void DepositShowcaseItemsToCurrentStorage() {
			if (GetHeart() is not TEStorageHeart heart || heart.GetStoredItems().Any())
				return;

			if (showcaseItems is not null)
				goto DepositTheItems;

			showcaseItems = new();
			HashSet<int> addedTypes = new();

			void MakeItemsForUnloadedDataShowcase(int type, int stack) {
				showcaseItems.Add(new Item(type, stack));

				Item item = new(type, stack);
				AddRandomUnloadedItemDataToItem(item);

				showcaseItems.Add(item);

				addedTypes.Add(type);
			}

			void MakeItemsForSellingShowcase(int type) {
				HashSet<int> rolledPrefixes = new();
				int tries = 200;

				for (int i = 0; i < 5; i++) {
					Item item = new(type);
					item.Prefix(-1);

					if (!rolledPrefixes.Add(item.prefix)) {
						i--;
						tries--;

						if (tries <= 0)
							break;
					}

					showcaseItems.Add(item);
					tries = 200;
				}

				addedTypes.Add(type);
			}

			void MakeCoinsForStackingShowcase() {
				void MakeCoins(int type, int total) {
					while (total > 0) {
						showcaseItems.Add(new Item(type, Math.Min(100, total)));
						total -= 100;
					}

					addedTypes.Add(type);
				}

				MakeCoins(ItemID.CopperCoin, 135);
				MakeCoins(ItemID.SilverCoin, 215);
				MakeCoins(ItemID.GoldCoin, 180);
				MakeCoins(ItemID.PlatinumCoin, 60);
			}

			bool MakeRandomItem(ref int i, Func<Item, bool> condition, Action onValidItemFail = null) {
				int type = Main.rand.Next(0, ItemLoader.ItemCount);

				Item item = new(type);

				if (item.IsAir) {
					i--;
					return false;
				}

				item.stack = Main.rand.Next(1, item.maxStack + 1);

				if (!condition(item)) {
					onValidItemFail?.Invoke();

					i--;
					return false;
				}

				if (!addedTypes.Add(type)) {
					i--;
					return false;
				}

				showcaseItems.Add(item);
				return true;
			}

			void MakeItem(int type) {
				if (addedTypes.Add(type)) {
					Item item = new(type);
					item.stack = item.maxStack;

					showcaseItems.Add(item);
				}
			}

			void MakeResearchedItems(bool researched) {
				bool CheckIsValidResearchableItemForShowcase(Item item) {
					if (item.dye > 0 || item.maxStack == 1)
						return false;

					return researched != Utility.IsFullyResearched(item.type, true);
				}

				int tries = 1000;

				for (int i = 0; i < 10; i++) {
					if (MakeRandomItem(ref i, CheckIsValidResearchableItemForShowcase, () => tries--))
						tries = 1000;
					else if (tries <= 0)
						return;
				}
			}

			void MakeIngredientItems() {
				bool CheckIsValidMaterialAndMaximizeStack(Item item) {
					if (item.maxStack == 1 || !item.material || item.type == ItemID.DirtBlock || item.createTile >= TileID.Dirt || item.dye > 0 || item.createWall > WallID.None)
						return false;

					item.stack = item.maxStack;
					return true;
				}

				for (int i = 0; i < 50; i++)
					MakeRandomItem(ref i, CheckIsValidMaterialAndMaximizeStack);

				MakeItem(ItemID.Wood);
				MakeItem(ItemID.StoneBlock);
				MakeItem(ItemID.Torch);
				MakeItem(ItemID.ClayBlock);
				MakeItem(ItemID.SandBlock);
				MakeItem(ItemID.Glass);
				MakeItem(ItemID.Cobweb);
			}

			MakeItemsForUnloadedDataShowcase(ItemID.WoodenSword, 1);
			MakeItemsForUnloadedDataShowcase(ItemID.CopperHelmet, 1);
			MakeItemsForUnloadedDataShowcase(ItemID.FlamingArrow, 100);
			MakeItemsForSellingShowcase(ItemID.Megashark);
			MakeItemsForSellingShowcase(ItemID.LightningBoots);
			MakeCoinsForStackingShowcase();
			MakeResearchedItems(true);
			MakeResearchedItems(false);
			MakeIngredientItems();

			Main.NewText("Showcase initialized.  Item count: " + showcaseItems.Count);

			DepositTheItems:
			heart.TryDeposit(showcaseItems.Select(i => i.Clone()).ToList());
			needRefresh = true;
		}

		internal static readonly FieldInfo UnloadedGlobalItem_data = typeof(UnloadedGlobalItem).GetField("data", BindingFlags.NonPublic | BindingFlags.Instance);

		private static void AddRandomUnloadedItemDataToItem(Item item) {
			if (item is null || item.IsAir || item.ModItem is UnloadedItem)
				return;

			if (TEStorageHeart.Item_globalItems.GetValue(item) is not Instanced<GlobalItem>[] globalItems || globalItems.Length == 0)
				return;

			//Create the data
			TagCompound modData = new() {
				["mod"] = "MagicStorage",
				["name"] = "ShowcaseItemData",
				["data"] = new TagCompound() {
					["randomData"] = Main.rand.Next()
				}
			};

			if (item.TryGetGlobalItem(out UnloadedGlobalItem obj))
				(UnloadedGlobalItem_data.GetValue(obj) as IList<TagCompound>).Add(modData);
			else {
				Instanced<GlobalItem>[] array = (Instanced<GlobalItem>[])globalItems.Clone();
				int index = array.Length;

				Array.Resize(ref array, array.Length + 1);
			
				//Create the instance
				obj = new();

				UnloadedGlobalItem_data.SetValue(obj, new List<TagCompound>() { modData });

				array[^1] = new((ushort)index, obj);

				TEStorageHeart.Item_globalItems.SetValue(item, array);
			}
		}

		internal static void FavoriteItem(int slot) {
			if (slot < 0 || slot >= items.Count)
				return;

			//Favorite all of the source items
			bool doFavorite = !sourceItems[slot][0].favorited;

			foreach (var item in sourceItems[slot])
				item.favorited = doFavorite;

			needRefresh = true;
		}

		/// <summary>
		/// Simulates a deposit attempt into a storage center (Storage Heart, Storage Access, etc.).
		/// </summary>
		/// <param name="center">The tile entity used to attempt to retrieve a Storage Heart</param>
		/// <param name="item">The item to deposit</param>
		/// <returns>Whether the deposit was successful</returns>
		public static bool TryDespoit(TEStorageCenter center, Item item) {
			if (center is null)
				return false;

			StoragePlayer.StorageHeartAccessWrapper wrapper = new(center);

			if (wrapper.Valid) {
				int oldStack = item.stack;
				TEStorageHeart heart = wrapper.Heart;
				heart.TryDeposit(item);
				return oldStack != item.stack;
			}
			
			return false;
		}

		/// <summary>
		/// Simulates a deposit attempt into the currently assigned Storage Heart.
		/// </summary>
		/// <param name="item">The item to deposit</param>
		/// <returns>Whether the deposit was successful</returns>
		public static bool TryDeposit(Item item)
		{
			int oldStack = item.stack;
			TEStorageHeart heart = GetHeart();
			heart.TryDeposit(item);
			return oldStack != item.stack;
		}

		/// <summary>
		/// Simulates a deposit attempt into a storage center (Storage Heart, Storage Access, etc.).
		/// </summary>
		/// <param name="center">The tile entity used to attempt to retrieve a Storage Heart</param>
		/// <param name="items">The items to deposit</param>
		/// <returns>Whether the deposit was successful</returns>
		public static bool TryDeposit(TEStorageCenter center, List<Item> items)
		{
			if (center is null)
				return false;

			StoragePlayer.StorageHeartAccessWrapper wrapper = new(center);

			if (wrapper.Valid) {
				TEStorageHeart heart = wrapper.Heart;
				return heart.TryDeposit(items);
			}

			return false;
		}

		/// <summary>
		/// Simulates a deposit attempt into a storage center (Storage Heart, Storage Access, etc.).
		/// </summary>
		/// <param name="center">The tile entity used to attempt to retrieve a Storage Heart</param>
		/// <param name="items">The items to deposit</param>
		/// <param name="quickStack">Whether the operation is a quick stack</param>
		/// <returns>Whether the deposit was successful</returns>
		public static bool TryDeposit(TEStorageCenter center, List<Item> items, bool quickStack = false)
		{
			if (center is null)
				return false;

			StoragePlayer.StorageHeartAccessWrapper wrapper = new(center);

			if (wrapper.Valid) {
				TEStorageHeart heart = wrapper.Heart;

				if (quickStack)
					items = new(items.Where(i => heart.HasItem(i, ignorePrefix: true)));

				return heart.TryDeposit(items);
			}

			return false;
		}

		/// <summary>
		/// Simulates a deposit attempt into the currently assigned Storage Heart.
		/// </summary>
		/// <param name="items">The items to deposit</param>
		/// <returns>Whether the deposit was successful</returns>
		public static bool TryDeposit(List<Item> items)
		{
			TEStorageHeart heart = GetHeart();
			return heart.TryDeposit(items);
		}

		internal static bool TryDepositAll(bool quickStack)
		{
			Player player = Main.LocalPlayer;
			TEStorageHeart heart = GetHeart();

			bool filter(Item item) => !item.IsAir && !item.favorited && (!quickStack || heart.HasItem(item, true));
			var items = new List<Item>();

			for (int k = 10; k < 50; k++)
			{
				Item item = player.inventory[k];
				if (filter(item))
					items.Add(item);
			}

			return heart.TryDeposit(items);
		}

		internal static bool TryRestock()
		{
			Player player = Main.LocalPlayer;
			GetHeart();
			bool changed = false;

			foreach (Item item in player.inventory)
				if (item is not null && !item.IsAir && item.stack < item.maxStack)
				{
					Item toWithdraw = item.Clone();
					toWithdraw.stack = item.maxStack - item.stack;
					toWithdraw = DoWithdraw(toWithdraw, true, true);
					if (!toWithdraw.IsAir)
					{
						item.stack += toWithdraw.stack;
						toWithdraw.TurnToAir();
						changed = true;
					}
				}

			return changed;
		}

		/// <summary>
		/// Attempts to withdraw an item from a storage center (Storage Heart, Storage Access, etc.).
		/// </summary>
		/// <param name="center">The tile entity used to attempt to retrieve a Storage Heart</param>
		/// <param name="item">The item to withdraw</param>
		/// <param name="toInventory">
		/// Whether the item goes into the player inventory.
		/// This parameter is only used if <see cref="Main.netMode"/> is <see cref="NetmodeID.MultiplayerClient"/>
		/// </param>
		/// <param name="keepOneIfFavorite">Whether at least one item should remain in the storage if it's favourited</param>
		/// <returns>A valid item instance if the withdrawal was succesful, an air item otherwise.</returns>
		public static Item DoWithdraw(TEStorageCenter center, Item item, bool toInventory = false, bool keepOneIfFavorite = false)
		{
			if (center is null)
				return new Item();

			StoragePlayer.StorageHeartAccessWrapper wrapper = new(center);

			if (wrapper.Valid) {
				TEStorageHeart heart = wrapper.Heart;
				return heart.TryWithdraw(item, keepOneIfFavorite, toInventory);
			}

			return new Item();
		}

		/// <summary>
		/// Attempts to withdraw an item from the currently assigned Storage Heart.
		/// </summary>
		/// <param name="item">The item to withdraw</param>
		/// <param name="toInventory">
		/// Whether the item goes into the player inventory.
		/// This parameter is only used if <see cref="Main.netMode"/> is <see cref="NetmodeID.MultiplayerClient"/>
		/// </param>
		/// <param name="keepOneIfFavorite">Whether at least one item should remain in the storage if it's favourited</param>
		/// <returns>A valid item instance if the withdrawal was succesful, an air item otherwise.</returns>
		public static Item DoWithdraw(Item item, bool toInventory = false, bool keepOneIfFavorite = false)
		{
			TEStorageHeart heart = GetHeart();
			return heart.TryWithdraw(item, keepOneIfFavorite, toInventory);
		}
	}
}
