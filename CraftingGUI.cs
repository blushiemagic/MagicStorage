using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.Components;
using MagicStorage.UI.States;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage
{
	// Method implementations can also be found in UI/GUIs/CraftingGUI.X.cs
	public static partial class CraftingGUI
	{
		internal static readonly CraftingRefreshThread NullThread = null;

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
			// NOTE: updating this during a refresh thread may cause race conditions in UI, so don't set it null here!
		//	selectedRecipe = null;
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
			
			CreateSelectedRecipeRefreshThread(recipe, 1, caller: "CraftingGUI.SetSelectedRecipe()").Start();
		}

		public static RefreshThread CreateFullRefreshThread(string caller) {
			var thread = FullRefreshBuilder.Instance.CreateThread();
			thread.SetDebugName($"{caller} thread");
			return thread;
		}

		public static RefreshThread CreateSelectedRecipeRefreshThread(Recipe selectedRecipe, int craftAmountTarget, string caller) {
			var thread = new RecipeInfoPanelRefreshThread(
				controls: CreateRefreshThreadControls(MagicUI.craftingUI),
				processedStorage: new(
					staticWasModuleItemTable: wasModuleItem,
					staticModuleItemWasFromInventoryTable: moduleItemWasFromInventory,
					staticResultItemsList: items,
					staticResultItemGroupsList: itemGroups,
					staticResultItemsFromModulesList: sourceItemsFromModules,
					staticCountsDictionary: itemCounts,
					staticCountsByPrefixDictionary: itemCountsByPrefix
				),
				ingredientControls: new(
					staticShowAllIngredientsField: new ShowAllIngredientsProvider(((CraftingUIState)MagicUI.craftingUI).recursionButton.IsOn),
					staticInfiniteItemsSet: isItemInfinite,
					staticBlockedList: blockStorageItems,
					staticCreativeUnitField: new CreativeUnitPresentProvider()
				),
				craftingObject: new(
					selection: new SelectionProvider(selectedRecipe),
					craftAmountTarget: new CraftAmountTargetProvider(craftAmountTarget)
				)
			);

			thread.SetDebugName($"{caller} thread");

			return thread;
		}

		public static RefreshThread CreateRecipeListRefreshThread(string caller) {
			// Force all recipes to be recalculated
			if (MagicUI.ForceNextRefreshToBeFull)
				recipesToRefreshByIndex = null;

			var thread = new RecipeListRefreshThread(
				controls: CreateRefreshThreadControls(MagicUI.craftingUI),
				processedStorage: new(
					staticWasModuleItemTable: wasModuleItem,
					staticModuleItemWasFromInventoryTable: moduleItemWasFromInventory,
					staticResultItemsList: items,
					staticResultItemGroupsList: itemGroups,
					staticResultItemsFromModulesList: sourceItemsFromModules,
					staticCountsDictionary: itemCounts,
					staticCountsByPrefixDictionary: itemCountsByPrefix
				),
				mainZoneControls: new(
					zoneObjectFilterChoice: MagicUI.craftingUI.GetDefaultPage<CraftingUIState.RecipesPage>().recipeButtons.Choice,
					favorited: StoragePlayer.LocalPlayer.FavoritedRecipes,
					hidden: StoragePlayer.LocalPlayer.HiddenRecipes,
					configBlacklist: MagicStorageConfig.GlobalRecipeBlacklist
				),
				mainZoneResults: new(
					objectsToRefresh: CollectRefreshingRecipes(),
					staticObjectList: recipes,
					staticAvailableList: recipeAvailable
				),
				ingredientControls: new(
					staticShowAllIngredientsField: new ShowAllIngredientsProvider(((CraftingUIState)MagicUI.craftingUI).recursionButton.IsOn),
					staticInfiniteItemsSet: isItemInfinite,
					staticBlockedList: blockStorageItems,
					staticCreativeUnitField: new CreativeUnitPresentProvider()
				),
				craftingObject: new(
					selection: new SelectionProvider(),
					craftAmountTarget: new CraftAmountTargetProvider()
				),
				availableCache: new(
					staticTable: recipeToAvailableLookup
				)
			);

			thread.SetDebugName($"{caller} thread");

			return thread;
		}

		internal static Dictionary<int, int> GetItemCountsWithBlockedItemsRemoved(bool cloneIfBlockEmpty = false) => GetItemCountsWithBlockedItemsRemoved(NullThread, cloneIfBlockEmpty);

		internal static Dictionary<int, int> GetItemCountsWithBlockedItemsRemoved<T>(T thread, bool cloneIfBlockEmpty = false)
			where T : IProcessedStorageItemsProvider, IIngredientControlsProvider
		{
			Dictionary<int, int> counts = thread?.ProcessedStorageItems.itemCounts.Value ?? itemCounts;
			Dictionary<int, Dictionary<int, int>> countsByPrefix = thread?.ProcessedStorageItems.itemCountsByPrefix.Value ?? itemCountsByPrefix;
			IEnumerable<ItemData> blockedIngredients = thread?.IngredientControls.blockStorageItems.Value ?? blockStorageItems;

			if (!cloneIfBlockEmpty && !blockedIngredients.Any())
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

		/// <summary>
		/// Gets an object representing information used to determine if a recipe can be crafted.<br/>
		/// <b>NOTE:</b> if a <see cref="RefreshThread"/> is currently active, this method will ignore its controls.
		/// </summary>
		/// <param name="cloneIfBlockEmpty">If <see langword="true"/>, the returned inventory dictionary will be a clone even if there are no blocked items.</param>
		public static AvailableRecipeObjects GetCurrentInventory(bool cloneIfBlockEmpty = false) {
			return GetCurrentInventory(NullThread, cloneIfBlockEmpty);
		}

		/// <summary>
		/// Gets an object representing information used to determine if a recipe can be crafted.
		/// </summary>
		/// <param name="thread">The <see cref="RefreshThread"/> from which to gather controls.</param>
		/// <param name="cloneIfBlockEmpty">If <see langword="true"/>, the returned inventory dictionary will be a clone even if there are no blocked items.</param>
		public static AvailableRecipeObjects GetCurrentInventory<T>(T thread, bool cloneIfBlockEmpty = false)
			where T : RefreshThread, IProcessedStorageItemsProvider, IIngredientControlsProvider, IMainZoneFilterControlsProvider, IRecipeSnapshotsProvider
		{
			var inventory = GetItemCountsWithBlockedItemsRemoved(thread, cloneIfBlockEmpty);

			bool[] adjTiles = thread?.MainZoneObjectsFilterControls.adjTiles ?? CraftingGUI.adjTiles;
			bool[] recipeConditionsMetSnapshot = thread?.RecipeSnapshots.ConditionsMet;
			HashSet<int> infiniteItems = thread?.IngredientControls.infiniteItems.Value ?? [.. isItemInfinite];
			bool creativeUnitPresent = thread?.IngredientControls.creativeUnitPresent.Value ?? allItemsAreInfinite;

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

			if (result is null)
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
				withdrawn = TryToWithdrawFromModuleItems(heart, clone, true);

			return withdrawn;
		}

		internal static Item TryToWithdrawFromModuleItems(TEStorageHeart heart, Item toWithdraw, bool wasAlreadyCloned) {
			Item withdrawn;
			List<Item> moduleItems = GetModuleItems(heart);
			if (moduleItems.Count > 0) {
				// Heart did not contain the item; try to withdraw from the module items
				Item item = wasAlreadyCloned ? toWithdraw : toWithdraw.Clone();

				TEStorageUnit.WithdrawFromItemCollection(moduleItems, item, out withdrawn);

				if (!withdrawn.IsAir)
					MagicUI.SetNextCollectionsToRefresh(withdrawn.type);
			} else
				withdrawn = new Item();

			return withdrawn;
		}

		private static List<Item> GetModuleItems(TEStorageHeart heart) {
			EnvironmentSandbox sandbox = new(Main.LocalPlayer, heart);
			List<Item> items = [];

			foreach (var module in heart.GetModules()) {
				var moduleItems = module.GetAdditionalItems(sandbox);

				if (moduleItems is not null)
					items.AddRange(moduleItems);
			}

			return items;
		}
	}
}
