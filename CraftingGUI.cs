using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Threading.UI;
using MagicStorage.Components;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static ReLogic.Peripherals.RGB.Corsair.CorsairDeviceGroup;

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
			ClearAllCollections();
			PlayerZoneCache.FreeCache(true);
		}

		internal static void ClearAllCollections() {
			recipes.Clear();
			recipeAvailable.Clear();
			storageItems.Clear();
			storageItemInfo.Clear();
			items.Clear();
			itemGroups.Clear();
			itemCounts.Clear();
			itemCountsByPrefix.Clear();
			sourceItemsFromModules.Clear();
			wasModuleItem.Clear();
			moduleItemWasFromInventory.Clear();
			blockStorageItems.Clear();
			isItemInfinite.Clear();
			selectedRecipe = null;
			result = null;
			ResetRecentRecipeCache();
			ResetRefreshCache();
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

		public static bool MeetsIngredientRequirement(Recipe recipe, Dictionary<int, int> countsDictionary, int ingredientType, int requiredStack) {
			if (MeetsIngredientRequirement_CheckCounts(countsDictionary, ingredientType, ref requiredStack))
				return true;

			foreach (int group in recipe.acceptedGroups) {
				RecipeGroup recipeGroup = RecipeGroup.recipeGroups[group];

				if (recipeGroup.ContainsItem(ingredientType)) {
					foreach (int groupItemType in recipeGroup.ValidItems) {
						if (MeetsIngredientRequirement_CheckCounts(countsDictionary, groupItemType, ref requiredStack))
							return true;
					}
				}
			}

			return false;
		}

		private static bool MeetsIngredientRequirement_CheckCounts(Dictionary<int, int> countsDictionary, int itemType, ref int requiredStack) {
			if (countsDictionary.TryGetValue(itemType, out int quantity)) {
				if (quantity >= requiredStack)
					return true;

				requiredStack -= quantity;
			}

			return false;
		}

		internal static void SetSelectedRecipe(Recipe recipe)
		{
			ArgumentNullException.ThrowIfNull(recipe);

			NetHelper.Report(true, "Reassigning current recipe and refreshing recipe panel...");

			craftAmountTarget = 1;
			blockStorageItems.Clear();

			CreateSelectedRecipeRefreshThread(recipe, caller: nameof(SetSelectedRecipe)).Start();
		}

		internal static RecipeInfoPanelRefreshThread CreateSelectedRecipeRefreshThread(Recipe selectedRecipe, string caller) {
			GetCommonRefreshThreadParameters(out _, out var showAllIngredients, out var blockedStoredIngredients, out var craftAmountTarget);

			var thread = new RecipeInfoPanelRefreshThread(
				controls: CreateRefreshThreadControls(),
				selectedRecipe: selectedRecipe,
				showAllIngredients: showAllIngredients,
				blockedStoredIngredients: blockedStoredIngredients,
				craftAmountTarget: craftAmountTarget
			);
			thread.SetDebugName($"CraftingGUI.{caller}() thread");

			return thread;
		}

		/// <summary>
		/// Attempts to craft a certain amount of items from a Crafting Interface
		/// </summary>
		/// <param name="craftingAccess">The tile entity for the Crafting Interface to craft items from</param>
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

		internal static Dictionary<int, int> GetItemCountsWithBlockedItemsRemoved(bool cloneIfBlockEmpty = false) {
			Dictionary<int, int> counts;
			Dictionary<int, Dictionary<int, int>> countsByPrefix;
			List<ItemData> blockedIngredients;

			if (MagicUI.HasActiveThread(out CommonCraftingThread thread)) {
				counts = thread.itemCounts;
				countsByPrefix = thread.itemCountsByPrefix;
				blockedIngredients = thread.blockStorageItems;
			} else {
				counts = itemCounts;
				countsByPrefix = itemCountsByPrefix;
				blockedIngredients = blockStorageItems;
			}

			if (!cloneIfBlockEmpty && blockedIngredients.Count == 0)
				return counts;

			counts = new(counts);

			foreach (var data in blockedIngredients) {
				if (counts.TryGetValue(data.Type, out int quantity)
				&& countsByPrefix.TryGetValue(data.Type, out var prefixCounts)
				&& prefixCounts.TryGetValue(data.Prefix, out int prefixQuantity)
				&& prefixQuantity > 0) {
					quantity -= prefixQuantity;
					if (quantity <= 0)
						counts.Remove(data.Type);
					else
						counts[data.Type] = quantity;
				}
			}

			return counts;
		}

		internal static bool TryGetIngredientQuantity(Recipe recipe, Dictionary<int, int> storageQuantity, HashSet<int> infiniteItems, int requiredIngredient, out int totalQuantity) {
			if (infiniteItems.Contains(requiredIngredient)) {
				totalQuantity = int.MaxValue;
				return false;
			}

			ClampedArithmetic total = 0;

			if (storageQuantity.TryGetValue(requiredIngredient, out int quantity))
				total += quantity;

			if (recipe is null)
				goto SkipRecipeGroupsCheck;

			foreach (int group in recipe.acceptedGroups) {
				RecipeGroup recipeGroup = RecipeGroup.recipeGroups[group];

				if (recipeGroup.ContainsItem(requiredIngredient)) {
					foreach (int groupItemType in recipeGroup.ValidItems) {
						if (infiniteItems.Contains(groupItemType)) {
							totalQuantity = int.MaxValue;
							return false;
						}

						if (storageQuantity.TryGetValue(groupItemType, out int groupItemQuantity))
							total += groupItemQuantity;
					}
				}
			}

			SkipRecipeGroupsCheck:

			totalQuantity = total;
			return true;
		}

		public static AvailableRecipeObjects GetCurrentInventory(bool cloneIfBlockEmpty = false) {
			var inventory = GetItemCountsWithBlockedItemsRemoved(cloneIfBlockEmpty);

			bool[] adjTiles;
			bool[] recipeConditionsMetSnapshot;
			HashSet<int> infiniteItems;
			bool creativeUnitPresent;

			if (MagicUI.HasActiveThread(out CommonCraftingThread commonThread)) {
				infiniteItems = commonThread.infiniteItems;
				creativeUnitPresent = commonThread.creativeUnitPresent;

				if (commonThread is CraftingControlsRefreshThread controlsThread) {
					adjTiles = controlsThread.adjTiles;

					if (controlsThread is CraftingRefreshThread craftingThread)
						recipeConditionsMetSnapshot = craftingThread.recipeConditionsMetSnapshot;
					else
						recipeConditionsMetSnapshot = null;
				} else {
					adjTiles = CraftingGUI.adjTiles;
					recipeConditionsMetSnapshot = null;
				}
			} else {
				adjTiles = CraftingGUI.adjTiles;
				recipeConditionsMetSnapshot = null;
				infiniteItems = [.. isItemInfinite];
				creativeUnitPresent = allItemsAreInfinite;
			}

			return new AvailableRecipeObjects(adjTiles, inventory, recipeConditionsMetSnapshot, infiniteItems, creativeUnitPresent);
		}

		internal static List<Item> HandleCraftWithdrawAndDeposit(TEStorageHeart heart, List<Item> toWithdraw, List<Item> results)
		{
			NetHelper.Report(true, $"Withdrawing {toWithdraw.Count} items...");

			var items = new List<Item>();
			foreach (Item tryWithdraw in toWithdraw)
			{
				NetHelper.Report(false, $"  {tryWithdraw.IdentifierAndStack()}");

				int expectedStack = tryWithdraw.stack;
				Item withdrawn = heart.TryWithdraw(tryWithdraw, false);
				if (!withdrawn.IsAir) {
					items.Add(withdrawn);
					NetHelper.Report(false, $"    SUCCESS: Withdrew {withdrawn.stack} items");
				}
				if (withdrawn.stack < expectedStack)
				{
					// There weren't enough of this item to withdraw, deposit what was already withdrawn
					NetHelper.Report(false, $"    FAILED: Stack requirement not met ({withdrawn.stack} < {expectedStack}), aborting procedure");

					for (int k = 0; k < items.Count; k++)
					{
						heart.DepositItem(items[k]);
						if (items[k].IsAir)
						{
							items.RemoveAt(k);
							k--;
						}
					}

					goto ReturnFromMethod;
				}
			}

			NetHelper.Report(false, $"Withdrew {items.Count} items");

			NetHelper.Report(false, $"Depositing {results.Count} items...");

			items.Clear();
			foreach (Item result in results)
			{
				NetHelper.Report(false, $"  {result.IdentifierAndStack()}");

				int stack = result.stack;
				heart.DepositItem(result);

				if (result.stack != stack) {
					int deposited = stack - result.stack;
					NetHelper.Report(false, $"    SUCCESS: Deposited {deposited} items");
				} else
					NetHelper.Report(false, $"    FAILED");

				if (!result.IsAir)
					items.Add(result);
			}

			ReturnFromMethod:
			if (items.Count > 0)
				NetHelper.Report(false, $"Operation had {items.Count} leftover items");

			return items;
		}

		internal static bool TryDepositResult(Item item)
		{
			int oldStack = item.stack;
			TEStorageHeart heart = GetHeart();

			if (heart is null)
				return false;

			heart.TryDeposit(item, accessingPlayer: Main.LocalPlayer);

			return oldStack != item.stack;
		}

		internal static Item DoWithdrawResult(int amountToWithdraw, bool toInventory = false)
		{
			TEStorageHeart heart = GetHeart();
			if (heart is null)
				return new Item();

			using var _ = SecuritySystem.CreateAccessContext();

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
				withdrawn = TryToWithdrawFromModuleItems(clone, true);

			return withdrawn;
		}

		internal static Item TryToWithdrawFromModuleItems(Item toWithdraw, bool wasAlreadyCloned) {
			Item withdrawn;
			if (sourceItemsFromModules.Count > 0) {
				//Heart did not contain the item; try to withdraw from the module items
				Item item = wasAlreadyCloned ? toWithdraw : toWithdraw.Clone();

				TEStorageUnit.WithdrawFromItemCollection(sourceItemsFromModules, item, out withdrawn,
					onItemRemoved: k => {
						int index = k + items.Count - sourceItemsFromModules.Count;
						
						items.RemoveAt(index);
					},
					onItemStackReduced: (k, stack) => {
						int index = k + items.Count - sourceItemsFromModules.Count;

						Item item = items[index];
						itemCounts[item.type] -= stack;
						itemCountsByPrefix[item.type][item.prefix] -= stack;
					});

				if (!withdrawn.IsAir) {
					MagicUI.SetRefresh();
					SetNextDefaultRecipeCollectionToRefresh(withdrawn.type);
				}
			} else
				withdrawn = new Item();

			return withdrawn;
		}
	}
}
