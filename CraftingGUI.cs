using System;
using System.Collections.Generic;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Components;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage
{
	// Method implementations can also be found in UI/GUIs/CraftingGUI.X.cs
	public static partial class CraftingGUI
	{
		public const int RecipeButtonsAvailableChoice = 0;
		//Button location could either be the third (2) or fourth (3) option depending on if the favoriting config is enabled
		public static int RecipeButtonsBlacklistChoice => MagicStorageConfig.CraftingFavoritingEnabled ? 3 : 2;
		public const int RecipeButtonsFavoritesChoice = 2;
		public const int Padding = 4;
		public const int RecipeColumns = 10;
		public const int IngredientColumns = 7;
		public const float InventoryScale = 0.85f;
		public const float SmallScale = 0.7f;
		public const int StartMaxCraftTimer = 20;
		public const int StartMaxRightClickTimer = 20;
		public const float ScrollBar2ViewSize = 1f;
		public const float RecipeScrollBarViewSize = 1f;

		internal static Recipe selectedRecipe;

		[ThreadStatic]
		public static bool CatchDroppedItems;
		[ThreadStatic]
		public static List<Item> DroppedItems;

		internal static void Unload()
		{
			selectedRecipe = null;
		}

		internal static void Reset() {
			Campfire = false;
			craftTimer = 0;
			maxCraftTimer = StartMaxCraftTimer;
			craftAmountTarget = 1;
		}

		internal static TEStorageHeart GetHeart() => StoragePlayer.LocalPlayer.GetStorageHeart();

		internal static TECraftingAccess GetCraftingEntity() => StoragePlayer.LocalPlayer.GetCraftingAccess();

		internal static List<Item> GetCraftingStations() => GetCraftingEntity()?.stations ?? new();

		/// <summary>
		/// Returns the recursion crafting tree for <paramref name="recipe"/> if it exists and recursion is enabled, or <see langword="null"/> otherwise.
		/// </summary>
		/// <param name="recipe">The recipe</param>
		/// <param name="toCraft">The quantity of the final recipe's crafted item to create</param>
		/// <param name="blockedSubrecipeIngredient">An optional item ID representing ingredient trees that should be ignored</param>
		public static OrderedRecipeTree GetCraftingTree(Recipe recipe, int toCraft = 1, int blockedSubrecipeIngredient = 0) {
			if (!MagicStorageConfig.IsRecursionEnabled || !recipe.TryGetRecursiveRecipe(out RecursiveRecipe recursiveRecipe))
				return null;

			return recursiveRecipe.GetCraftingTree(toCraft, available: GetCurrentInventory(), blockedSubrecipeIngredient);
		}

		public static bool RecipeGroupMatch(Recipe recipe, int inventoryType, int requiredType)
		{
			foreach (int num in recipe.acceptedGroups)
			{
				RecipeGroup recipeGroup = RecipeGroup.recipeGroups[num];
				if (recipeGroup.ContainsItem(inventoryType) && recipeGroup.ContainsItem(requiredType))
					return true;
			}

			return false;
		}

		internal static void SetSelectedRecipe(Recipe recipe)
		{
			ArgumentNullException.ThrowIfNull(recipe);

			NetHelper.Report(true, "Reassigning current recipe...");

			selectedRecipe = recipe;
			RefreshStorageItems();
			blockStorageItems.Clear();

			NetHelper.Report(true, "Successfully reassigned current recipe!");
		}

		/// <summary>
		/// Attempts to craft a certain amount of items from a Crafting Access
		/// </summary>
		/// <param name="craftingAccess">The tile entity for the Crafting Access to craft items from</param>
		/// <param name="toCraft">How many items should be crafted</param>
		public static void Craft(TECraftingAccess craftingAccess, int toCraft) {
			if (craftingAccess is null)
				return;

			StoragePlayer.StorageHeartAccessWrapper wrapper = new(craftingAccess);

			//OpenStorage() handles setting the CraftingGUI to use the new storage and Dispose()/CloseStorage() handles reverting it back
			if (wrapper.Valid) {
				using (wrapper.OpenStorage())
					Craft(toCraft);
			}
		}

		private static Dictionary<int, int> GetItemCountsWithBlockedItemsRemoved(bool cloneIfBlockEmpty = false) {
			if (!cloneIfBlockEmpty && blockStorageItems.Count == 0)
				return itemCounts;

			Dictionary<int, int> counts = new(itemCounts);

			foreach (var data in blockStorageItems)
				counts.Remove(data.Type);

			return counts;
		}

		public static AvailableRecipeObjects GetCurrentInventory(bool cloneIfBlockEmpty = false) {
			bool[] availableRecipes = currentlyThreading && StorageGUI.activeThread.state is ThreadState { recipeConditionsMetSnapshot: bool[] snapshot } ? snapshot : null;
			return new AvailableRecipeObjects(adjTiles, GetItemCountsWithBlockedItemsRemoved(cloneIfBlockEmpty), availableRecipes);
		}

		internal static List<Item> HandleCraftWithdrawAndDeposit(TEStorageHeart heart, List<Item> toWithdraw, List<Item> results)
		{
			var items = new List<Item>();
			foreach (Item tryWithdraw in toWithdraw)
			{
				Item withdrawn = heart.TryWithdraw(tryWithdraw, false);
				if (!withdrawn.IsAir)
					items.Add(withdrawn);
				if (withdrawn.stack < tryWithdraw.stack)
				{
					for (int k = 0; k < items.Count; k++)
					{
						heart.DepositItem(items[k]);
						if (items[k].IsAir)
						{
							items.RemoveAt(k);
							k--;
						}
					}

					return items;
				}
			}

			items.Clear();
			foreach (Item result in results)
			{
				heart.DepositItem(result);
				if (!result.IsAir)
					items.Add(result);
			}

			return items;
		}

		internal static bool TryDepositResult(Item item)
		{
			int oldStack = item.stack;
			int oldType = item.type;
			TEStorageHeart heart = GetHeart();

			if (heart is null)
				return false;

			heart.TryDeposit(item);

			if (oldStack != item.stack) {
				SetNextDefaultRecipeCollectionToRefresh(oldType);

				return true;
			}

			return false;
		}

		internal static Item DoWithdrawResult(int amountToWithdraw, bool toInventory = false)
		{
			TEStorageHeart heart = GetHeart();
			if (heart is null)
				return new Item();

			Item clone = result.Clone();
			clone.stack = Math.Min(amountToWithdraw, clone.maxStack);

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = heart.PrepareClientRequest(toInventory ? TEStorageHeart.Operation.WithdrawToInventoryThenTryModuleInventory : TEStorageHeart.Operation.WithdrawThenTryModuleInventory);
				ItemIO.Send(clone, packet, true, true);
				packet.Send();
				return new Item();
			}

			Item withdrawn = heart.Withdraw(clone, false);

			if (withdrawn.IsAir)
				withdrawn = TryToWithdrawFromModuleItems(amountToWithdraw);

			return withdrawn;
		}

		internal static Item TryToWithdrawFromModuleItems(int amountToWithdraw) {
			Item withdrawn;
			if (items.Count != numItemsWithoutSimulators) {
				//Heart did not contain the item; try to withdraw from the module items
				Item item = result.Clone();
				item.stack = Math.Min(amountToWithdraw, item.maxStack);

				TEStorageUnit.WithdrawFromItemCollection(sourceItemsFromModules, item, out withdrawn,
					onItemRemoved: k => {
						int index = k + numItemsWithoutSimulators;
						
						items.RemoveAt(index);
					},
					onItemStackReduced: (k, stack) => {
						int index = k + numItemsWithoutSimulators;

						Item item = items[index];
						itemCounts[item.type] -= stack;
					});

				if (!withdrawn.IsAir) {
					StorageGUI.SetRefresh();
					SetNextDefaultRecipeCollectionToRefresh(withdrawn.type);
				}
			} else
				withdrawn = new Item();

			return withdrawn;
		}
	}
}
