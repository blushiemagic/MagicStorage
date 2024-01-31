using System.Collections.Generic;
using System.Linq;
using MagicStorage.Common.Systems;
using MagicStorage.Components;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ID;

namespace MagicStorage
{
	// Method implementations can also be found in UI/GUIs/StorageGUI.X.cs
	public static partial class StorageGUI
	{
		public const int padding = 4;
		public const int numColumns = 10;
		public const float inventoryScale = 0.85f;
		public const int startMaxRightClickTimer = 20;

		public static MouseState curMouse;
		public static MouseState oldMouse;

		internal static readonly float scrollBarViewSize = 1f;
		internal static float scrollBarMaxViewSize = 2f;

		public const int WAIT_PANEL_MINIMUM_TICKS = 45;

		//Legacy properties required for UISearchBar to function properly
		public static bool MouseClicked => curMouse.LeftButton == ButtonState.Pressed && oldMouse.LeftButton == ButtonState.Released;
		public static bool RightMouseClicked => curMouse.RightButton == ButtonState.Pressed && oldMouse.RightButton == ButtonState.Released;

		public static TEStorageHeart GetHeart() => StoragePlayer.LocalPlayer.GetStorageHeart();

		internal static void InvokeOnRefresh() => OnRefresh?.Invoke();

		internal static void Unload() => ClearAllCollections();

		private static void ClearAllCollections() {
			itemTypesToUpdate.Clear();
			items.Clear();
			sourceItems.Clear();
			didMatCheck.Clear();
			ResetRefreshCache();
		}

		internal static void FavoriteItem(int slot) {
			if (slot < 0 || slot >= items.Count)
				return;

			//Favorite all of the source items
			bool doFavorite = !sourceItems[slot][0].favorited;

			foreach (var item in sourceItems[slot])
				item.favorited = doFavorite;

			MagicUI.SetNextCollectionsToRefresh(sourceItems[slot][0].type);
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
						MagicUI.SetNextCollectionsToRefresh(oldType);

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
				MagicUI.SetNextCollectionsToRefresh(oldType);
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
						MagicUI.SetNextCollectionsToRefresh(types);
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
						MagicUI.SetNextCollectionsToRefresh(types);
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
			if (GetHeart() is not TEStorageHeart heart)
				return false;

			int[] types = items.Select(static i => i.type).ToArray();
			
			if (heart.TryDeposit(items)) {
				MagicUI.SetNextCollectionsToRefresh(types);
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
