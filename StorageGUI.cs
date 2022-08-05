using System;
using System.Collections.Generic;
using System.Linq;
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
		internal static readonly List<bool> didMatCheck = new();

		//Legacy properties required for UISearchBar to function properly
		public static bool MouseClicked => curMouse.LeftButton == ButtonState.Pressed && oldMouse.LeftButton == ButtonState.Released;
		public static bool RightMouseClicked => curMouse.RightButton == ButtonState.Pressed && oldMouse.RightButton == ButtonState.Released;

		public static TEStorageHeart GetHeart() => StoragePlayer.LocalPlayer.GetStorageHeart();

		internal static bool needRefresh;

		internal static event Action OnRefresh;

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
			TEStorageHeart heart = GetHeart();
			if (heart == null)
				return;

			NetHelper.Report(true, "Refreshing storage items");

			int sortMode = MagicUI.storageUI.GetPage<SortingPage>("Sorting").option;
			int filterMode = MagicUI.storageUI.GetPage<FilteringPage>("Filtering").option;

			var storagePage = MagicUI.storageUI.GetPage<StorageUIState.StoragePage>("Storage");

			string searchText = storagePage.searchBar.Text;
			bool onlyFavorites =storagePage.filterFavorites.Value;

			void DoFiltering()
			{
				IEnumerable<Item> itemsLocal;
				if (filterMode == FilteringOptionLoader.Definitions.Recent.Type)
				{
					Dictionary<int, Item> stored = heart.GetStoredItems().GroupBy(x => x.type).ToDictionary(x => x.Key, x => x.First());

					IEnumerable<Item> toFilter = heart.UniqueItemsPutHistory.Reverse().Where(x => stored.ContainsKey(x.type)).Select(x => stored[x.type]);
					itemsLocal = ItemSorter.SortAndFilter(toFilter, sortMode == SortingOptionLoader.Definitions.Default.Type ? -1 : sortMode, FilteringOptionLoader.Definitions.All.Type, searchText, 100);
				}
				else
				{
					itemsLocal = ItemSorter.SortAndFilter(heart.GetStoredItems(), sortMode, filterMode, searchText).OrderBy(x => x.favorited ? 0 : 1);
				}

				items.AddRange(itemsLocal.Where(x => !MagicStorageConfig.CraftingFavoritingEnabled || !onlyFavorites || x.favorited));

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

					NetHelper.Report(false, "No items displayed even though items exist in storage.  Defaulting to \"All\" filter mode");

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
				!Main.mouseItem.IsAir && (!ItemData.Matches(Main.mouseItem, items[slotFocus]) || Main.mouseItem.stack >= Main.mouseItem.maxStack))
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
