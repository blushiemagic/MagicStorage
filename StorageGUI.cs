using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MagicStorage.Common.Systems;
using MagicStorage.Components;
using MagicStorage.CrossMod;
using MagicStorage.Sorting;
using MagicStorage.UI;
using MagicStorage.UI.States;
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

		public const int WAIT_PANEL_MINIMUM_TICKS = 45;

		//Legacy properties required for UISearchBar to function properly
		public static bool MouseClicked => curMouse.LeftButton == ButtonState.Pressed && oldMouse.LeftButton == ButtonState.Released;
		public static bool RightMouseClicked => curMouse.RightButton == ButtonState.Pressed && oldMouse.RightButton == ButtonState.Released;

		internal static void Unload() {
			showcaseItems = null;
		}

		public static TEStorageHeart GetHeart() => StoragePlayer.LocalPlayer.GetStorageHeart();

		// Field included for backwards compatibility, but made Obsolete to encourage modders to use the new API
		[Obsolete("Use the SetRefresh() method or RefreshStorageUI property instead", error: true)]
		public static bool needRefresh;

		[Obsolete]
		private static ref bool Obsolete_needRefresh() => ref needRefresh;

		private static bool _refreshUI;
		public static bool RefreshUI {
			get => _refreshUI;
			set => _refreshUI |= value;
		}

		public static bool CurrentlyRefreshing { get; internal set; }

		public static event Action OnRefresh;

		// Specialized collection for making only certain item types get recalculated
		private static HashSet<int> itemTypesToUpdate;
		private static bool forceFullRefresh;

		public static bool ForceNextRefreshToBeFull {
			get => forceFullRefresh || Obsolete_needRefresh();
			set => forceFullRefresh |= value;
		}

		/// <summary>
		/// Shorthand for setting <see cref="RefreshUI"/> to <see langword="true"/> and also setting <see cref="ForceNextRefreshToBeFull"/>
		/// </summary>
		public static void SetRefresh(bool forceFullRefresh = false) {
			RefreshUI = true;
			StorageGUI.forceFullRefresh = forceFullRefresh;
		}

		public static int CurrentThreadingDuration { get; private set; }

		internal static void CheckRefresh() {
			if (RefreshUI)
				RefreshItems();

			if (activeThread?.Running is true)
				CurrentThreadingDuration++;
			else
				CurrentThreadingDuration = 0;
		}

		internal static void InvokeOnRefresh() => OnRefresh?.Invoke();

		public class ThreadContext {
			public ItemSorter.AggregateContext context;
			private readonly CancellationTokenSource tokenSource;
			public readonly CancellationToken token;
			public TEStorageHeart heart;
			public int sortMode, filterMode;
			public string searchText;
			public bool onlyFavorites;
			public int modSearch;
			private readonly Action<ThreadContext> work;
			private readonly Action<ThreadContext> afterWork;
			public object state;
			private readonly ManualResetEvent cancelWait = new(false);

			public ThreadContext(CancellationTokenSource tokenSource, Action<ThreadContext> work, Action<ThreadContext> afterWork) {
				ArgumentNullException.ThrowIfNull(tokenSource);
				ArgumentNullException.ThrowIfNull(work);

				this.tokenSource = tokenSource;
				token = tokenSource.Token;
				this.work = work;
				this.afterWork = afterWork;
			}

			public ThreadContext Clone(int? newSortMode = null, int? newFilterMode = null, string newSearchText = null, int? newModSearch = null) {
				return new ThreadContext(tokenSource, work, afterWork) {
					context = context,
					heart = heart,
					sortMode = newSortMode ?? sortMode,
					filterMode = newFilterMode ?? filterMode,
					searchText = newSearchText ?? searchText,
					onlyFavorites = onlyFavorites,
					modSearch = newModSearch ?? modSearch,
					state = state
				};
			}

			public bool Running { get; private set; }

			public static void Begin(ThreadContext incoming) {
				activeThread?.Stop();

				if (incoming.Running)
					throw new ArgumentException("Incoming thread state was already running");

				activeThread = incoming;
				activeThread.Running = true;

				// Variable capturing
				ThreadContext ctx = incoming;

				NetHelper.Report(true, "Threading logic started");

				Task.Run(() => {
					try {
						ctx.work(ctx);
						NetHelper.Report(true, "Main work for thread finished");

						ctx.afterWork?.Invoke(ctx);
						if (ctx.afterWork is not null)
							NetHelper.Report(true, "Final work for thread finished");
					} catch when (ctx.token.IsCancellationRequested) {
						NetHelper.Report(true, "Thread work was cancelled");
					} finally {
						ctx.cancelWait.Set();
					}
				});
			}

			public void Stop() {
				if (!Running)
					return;

				Running = false;
				tokenSource.Cancel();
				cancelWait.WaitOne();

				NetHelper.Report(true, "Current thread halted");
			}
		}

		internal static ThreadContext activeThread;

		public static void SetNextItemTypeToRefresh(int itemType) {
			itemTypesToUpdate ??= new();
			itemTypesToUpdate.Add(itemType);
		}

		public static void SetNextItemTypesToRefresh(IEnumerable<int> itemTypes) {
			itemTypesToUpdate ??= new();
			
			foreach (int id in itemTypes)
				itemTypesToUpdate.Add(id);
		}

		public static void RefreshItems()
		{
			// Moved to the start of the logic since CheckRefresh() might be called multiple times during refreshing otherwise
			_refreshUI = false;
			Obsolete_needRefresh() = false;

			if (forceFullRefresh)
				itemTypesToUpdate = null;

			// No refreshing required
			if (StoragePlayer.IsStorageEnvironment()) {
				itemTypesToUpdate = null;
				forceFullRefresh = false;
				return;
			}

			if (StoragePlayer.IsStorageCrafting()) {
				CraftingGUI.RefreshItems();
				itemTypesToUpdate = null;
				forceFullRefresh = false;
				return;
			}

			var storagePage = MagicUI.storageUI.GetPage<StorageUIState.StoragePage>("Storage");

			storagePage?.RequestThreadWait(waiting: true);

			if (CurrentlyRefreshing) {
				activeThread?.Stop();
				activeThread = null;
			}

			if (itemTypesToUpdate is null)
				RefreshAllItems(storagePage);
			else
				RefreshSpecificItems(storagePage);

			itemTypesToUpdate = null;
			forceFullRefresh = false;
		}

		private static void RefreshAllItems(StorageUIState.StoragePage storagePage) {
			if (InitializeThreadContext(storagePage, true) is not ThreadContext thread)
				return;
			
			// Assign the thread context
			AdjustItemCollectionAndAssignToThread(thread, thread.heart.GetStoredItems());

			// Start the thread
			ThreadContext.Begin(thread);
		}

		private static void RefreshSpecificItems(StorageUIState.StoragePage storagePage) {
			if (InitializeThreadContext(storagePage, false) is not ThreadContext thread)
				return;

			thread.state = itemTypesToUpdate;

			// Get the items that need to be updated
			IEnumerable<Item> itemsToUpdate = thread.heart.GetStoredItems().Where(ShouldItemUpdate);

			IEnumerable<Item> source;
			if (thread.filterMode == FilteringOptionLoader.Definitions.Recent.Type) {
				// Recent filter needs the entire storage as context, but the existing collection only has the results from the previous filter
				// If items are removed, this can cause the displayed amount to be not 100, which is undesirable
				// Using the entire storage is fine since only the first 100 items are used
				source = thread.heart.GetStoredItems();
			} else {
				// Reuse the existing collection for better sorting/filtering time
				source = items;
			}

			// Remove the types to update from the collection, then append the items to update
			IEnumerable<Item> collection = source.Where(static i => !ShouldItemUpdate(i)).Concat(itemsToUpdate);

			// Assign the thread context
			AdjustItemCollectionAndAssignToThread(thread, collection);

			// Start the thread
			ThreadContext.Begin(thread);
		}

		private static ThreadContext InitializeThreadContext(StorageUIState.StoragePage storagePage, bool clearItemLists) {
			if (clearItemLists) {
				didMatCheck.Clear();
				items.Clear();
				sourceItems.Clear();
			}

			TEStorageHeart heart = GetHeart();
			if (heart == null) {
				itemTypesToUpdate = null;
				storagePage?.RequestThreadWait(waiting: false);

				InvokeOnRefresh();
				return null;
			}

			NetHelper.Report(true, $"Refreshing {(itemTypesToUpdate is null ? "all" : $"{itemTypesToUpdate.Count}")} storage items");

			CurrentlyRefreshing = true;

			int sortMode = MagicUI.storageUI.GetPage<SortingPage>("Sorting").option;
			int filterMode = MagicUI.storageUI.GetPage<FilteringPage>("Filtering").option;

			string searchText = storagePage.searchBar.Text;
			bool onlyFavorites = storagePage.filterFavorites.Value;
			int modSearch = storagePage.modSearchBox.ModIndex;

			return new ThreadContext(new CancellationTokenSource(), SortAndFilter, AfterSorting) {
				heart = heart,
				sortMode = sortMode,
				filterMode = filterMode,
				searchText = searchText,
				onlyFavorites = onlyFavorites,
				modSearch = modSearch,
				state = itemTypesToUpdate
			};
		}

		private static bool ShouldItemUpdate(Item item) {
			if (activeThread?.state is not HashSet<int> toUpdate)
				return true;

			return toUpdate.Contains(item.type);
		}

		private static void AdjustItemCollectionAndAssignToThread(ThreadContext thread, IEnumerable<Item> source) {
			// Adjust the thread context based on the filter mode
			if (thread.filterMode == FilteringOptionLoader.Definitions.Recent.Type) {
				Dictionary<int, Item> stored = source.GroupBy(x => x.type).ToDictionary(x => x.Key, x => x.First());

				IEnumerable<Item> toFilter = thread.heart.UniqueItemsPutHistory.Reverse().Where(x => stored.ContainsKey(x.type)).Select(x => stored[x.type]);

				thread.context = new(toFilter);
			} else {
				thread.context = new(source);
			}
		}

		private static void SortAndFilter(ThreadContext thread) {
			DoFiltering(thread);
			
			bool didDefault = false;

			// now if nothing found we disable filters one by one
			if (thread.searchText.Trim().Length > 0)
			{
				if (items.Count == 0 && thread.filterMode != FilteringOptionLoader.Definitions.All.Type)
				{
					NetHelper.Report(true, "No items passed the filter.  Attempting filter with All setting");

					// search all categories
					thread.filterMode = FilteringOptionLoader.Definitions.All.Type;

					MagicUI.lastKnownSearchBarErrorReason = Language.GetTextValue("Mods.MagicStorage.Warnings.StorageDefaultToAllItems");
					didDefault = true;

					DoFiltering(thread);
				}

				if (items.Count == 0 && thread.modSearch != ModSearchBox.ModIndexAll)
				{
					NetHelper.Report(true, "No items passed the filter.  Attempting filter with All Mods setting");

					// search all mods
					thread.modSearch = ModSearchBox.ModIndexAll;

					MagicUI.lastKnownSearchBarErrorReason = Language.GetTextValue("Mods.MagicStorage.Warnings.StorageDefaultToAllMods");
					didDefault = true;

					DoFiltering(thread);
				}
			}

			if (!didDefault)
				MagicUI.lastKnownSearchBarErrorReason = null;
		}

		private static bool filterOutFavorites;

		private static void DoFiltering(ThreadContext thread)
		{
			try {
				NetHelper.Report(true, "Applying item filters...");

				if (thread.filterMode == FilteringOptionLoader.Definitions.Recent.Type)
				{
					if (thread.sortMode == SortingOptionLoader.Definitions.Default.Type)
						thread.sortMode = -1;

					thread.filterMode = FilteringOptionLoader.Definitions.All.Type;

					thread.context.items = ItemSorter.SortAndFilter(thread, 100);
				}
				else
				{
					thread.context.items = ItemSorter.SortAndFilter(thread);
				}

				if (MagicStorageConfig.CraftingFavoritingEnabled) {
					thread.context.items = thread.context.items.OrderByDescending(static x => x.favorited ? 1 : 0);
					thread.context.sourceItems = thread.context.sourceItems.OrderByDescending(static x => x[0].favorited ? 1 : 0);
				}

				filterOutFavorites = thread.onlyFavorites;

				if (thread.state is not null) {
					// Specific IDs were refreshed, meaning the lists weren't cleared.  Clear them now
					items.Clear();
					sourceItems.Clear();
					didMatCheck.Clear();
				}

				items.AddRange(thread.context.items.Where(static x => !MagicStorageConfig.CraftingFavoritingEnabled || !filterOutFavorites || x.favorited));

				sourceItems.AddRange(thread.context.sourceItems.Where(static x => !MagicStorageConfig.CraftingFavoritingEnabled || !filterOutFavorites || x[0].favorited));

				NetHelper.Report(true, "Filtering applied.  Item count: " + items.Count);
			} catch when (thread.token.IsCancellationRequested) {
				items.Clear();
				sourceItems.Clear();
				didMatCheck.Clear();
				throw;
			}
		}

		private static void AfterSorting(ThreadContext thread) {
			// Refresh logic in the UIs will only run when this is false
			if (!thread.token.IsCancellationRequested)
				CurrentlyRefreshing = false;

			for (int k = 0; k < items.Count; k++)
				didMatCheck.Add(false);

			// Ensure that race conditions with the UI can't occur
			// QueueMainThreadAction will execute the logic in a very specific place
			Main.QueueMainThreadAction(InvokeOnRefresh);

			MagicUI.storageUI.GetPage<StorageUIState.StoragePage>("Storage")?.RequestThreadWait(waiting: false);
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
					else {
						Utility.CallOnStackHooks(Main.mouseItem, result, result.stack);

						Main.mouseItem.stack += result.stack;
					}

					SetRefresh();
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
			SetRefresh();
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

			SetRefresh();
			SetNextItemTypeToRefresh(sourceItems[slot][0].type);
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
				int oldType = item.type;
				TEStorageHeart heart = wrapper.Heart;
				heart.TryDeposit(item);

				if (oldStack != item.stack) {
					if (GetHeart()?.Position == heart.Position)
						SetNextItemTypeToRefresh(oldType);

					return true;
				}
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
			int oldType = item.type;
			TEStorageHeart heart = GetHeart();
			heart.TryDeposit(item);

			if (oldStack != item.stack) {
				SetNextItemTypeToRefresh(oldType);
				return true;
			}

			return false;
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
				int[] types = items.Select(static i => i.type).ToArray();

				if (heart.TryDeposit(items)) {
					if (GetHeart()?.Position == heart.Position)
						SetNextItemTypesToRefresh(types);
					return true;
				}

				return false;
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

				int[] types = items.Select(static i => i.type).ToArray();

				if (heart.TryDeposit(items)) {
					if (GetHeart()?.Position == heart.Position)
						SetNextItemTypesToRefresh(types);
					return true;
				}

				return false;
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
			int[] types = items.Select(static i => i.type).ToArray();
			
			if (heart.TryDeposit(items)) {
				SetNextItemTypesToRefresh(types);
				return true;
			}

			return false;
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

			int[] types = items.Select(static i => i.type).ToArray();

			if (heart.TryDeposit(items)) {
				SetNextItemTypesToRefresh(types);
				return true;
			}

			return false;
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
				Item withdrawn = heart.TryWithdraw(item, keepOneIfFavorite, toInventory);

				if (!withdrawn.IsAir && GetHeart()?.Position == heart.Position)
					SetNextItemTypeToRefresh(withdrawn.type);

				return withdrawn;
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
			Item withdrawn = heart.TryWithdraw(item, keepOneIfFavorite, toInventory);

			if (!withdrawn.IsAir)
				SetNextItemTypeToRefresh(withdrawn.type);

			return withdrawn;
		}
	}
}
