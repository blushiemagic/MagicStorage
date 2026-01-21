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

		internal static void ClearAllCollections() {
			items.Clear();
			itemToSourceItems.Clear();
			ResetRefreshCache();
			hasAnyErrorItems = false;
		}

		internal static void FavoriteItem(int slot) {
			if (slot < 0 || slot >= items.Count)
				return;

			// Favorite all of the source items
			Item shownItem = items[slot];
			var sourceItems = itemToSourceItems.TryGetValue(shownItem, out var list) ? list : [];
			bool doFavorite = !shownItem.favorited;

			foreach (var item in sourceItems)
				item.favorited = doFavorite;

			MagicUI.SetNextCollectionsToRefresh(shownItem.type);
		}

		/// <summary>
		/// Attempts to deposit the provided <paramref name="item"/> into the currently accessed Storage Heart.
		/// </summary>
		/// <param name="item">The item to deposit</param>
		/// <returns>Whether the deposit was successful</returns>
		public static bool TryDeposit(Item item)
		{
			if (GetHeart() is not TEStorageHeart heart)
				return false;

			using var _ = SecuritySystem.CreateAccessContext();

			int oldStack = item.stack;
			int oldType = item.type;
			heart.TryDeposit(item, accessingPlayer: Main.LocalPlayer);

			if (oldStack != item.stack) {
				MagicUI.SetNextCollectionsToRefresh(oldType);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Attempts to deposit the provided <paramref name="items"/> into the currently accessed Storage Heart.
		/// </summary>
		/// <param name="items">The items to deposit</param>
		/// <returns>Whether the deposit was successful</returns>
		public static bool TryDeposit(List<Item> items)
		{
			if (GetHeart() is not TEStorageHeart heart)
				return false;

			using var _ = SecuritySystem.CreateAccessContext();

			int[] types = items.Select(static i => i.type).ToArray();
			
			if (heart.TryDeposit(items, accessingPlayer: Main.LocalPlayer)) {
				MagicUI.SetNextCollectionsToRefresh(types);
				return true;
			}

			return false;
		}

		internal static bool TryDepositAll(bool quickStack)
		{
			if (GetHeart() is not TEStorageHeart heart)
				return false;

			using var _ = SecuritySystem.CreateAccessContext();

			Player player = Main.LocalPlayer;

			var items = new List<Item>();

			for (int k = 10; k < 50; k++)
			{
				Item item = player.inventory[k];
				if (!item.IsAir && !item.favorited && (!quickStack || heart.HasItem(item, true)))
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
			if (GetHeart() is null)
				return false;

			using var _ = SecuritySystem.CreateAccessContext();

			Player player = Main.LocalPlayer;
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
			if (GetHeart() is not TEStorageHeart heart)
				return new Item();

			using var _ = SecuritySystem.CreateAccessContext();

			Item withdrawn = heart.TryWithdraw(item, keepOneIfFavorite, accessingPlayer: Main.LocalPlayer, toInventory);

			if (!withdrawn.IsAir)
				SetNextItemTypeToRefresh(withdrawn.type);

			return withdrawn;
		}
	}
}
