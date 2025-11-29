using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.Common.Threading.UI;
using MagicStorage.Modules;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader.Config;

namespace MagicStorage {
	partial class CraftingGUI {
		#region CommonCraftingThread
		internal abstract class CommonCraftingThread : RefreshThread {
			public Recipe selectedRecipe;
			/// <summary>
			/// <b>NOTE:</b> Value doesn't matter; the item is from a module if, and only if, this table has it as a key
			/// </summary>
			public readonly ConditionalWeakTable<Item, object> wasModuleItem = [];
			/// <summary>
			/// <b>NOTE:</b> Value doesn't matter; the item is from the player's main inventory if, and only if, this table has it as a key
			/// </summary>
			public readonly ConditionalWeakTable<Item, object> moduleItemWasFromInventory = [];
			public readonly bool showAllPossibleIngredients;
			public EnvironmentSandbox sandbox;
			public bool creativeUnitPresent;
			public HashSet<int> infiniteItems;
			public readonly List<Item> resultItems = [];
			public readonly List<List<Item>> resultItemGroups = [];
			public readonly List<Item> resultItemsFromModules = [];
			public IRecipeItemsHandler recipeItemsHandler;
			public readonly Dictionary<int, int> itemCounts = [];
			public readonly Dictionary<int, Dictionary<int, int>> itemCountsByPrefix = [];
			public readonly List<ItemData> blockStorageItems = [];
			public int craftAmountTarget;

			protected CommonCraftingThread(
				StorageViewControls controls,
				Recipe selectedRecipe,
				bool showAllIngredients,
				IEnumerable<ItemData> blockedStoredIngredients,
				int craftAmountTarget
			) : base(MagicUI.craftingUI, controls) {
				this.selectedRecipe = selectedRecipe;
				showAllPossibleIngredients = showAllIngredients;
				blockStorageItems = [.. blockedStoredIngredients];
				this.craftAmountTarget = craftAmountTarget;
			}

			protected void CopyFromStaticCollectionsAndFields() {
				resultItems.AddRange(CraftingGUI.items);

				foreach (var group in CraftingGUI.itemGroups)
					resultItemGroups.Add([.. group]);

				resultItemsFromModules.AddRange(CraftingGUI.sourceItemsFromModules);

				foreach (var (key, _) in CraftingGUI.wasModuleItem)
					wasModuleItem.TryAdd(key, null);

				foreach (var (key, _) in CraftingGUI.moduleItemWasFromInventory)
					moduleItemWasFromInventory.TryAdd(key, null);

				foreach (var (key, quantity) in CraftingGUI.itemCounts)
					itemCounts[key] = quantity;

				foreach (var (key, dictionary) in CraftingGUI.itemCountsByPrefix) {
					Dictionary<int, int> prefixDictionary = [];

					itemCountsByPrefix[key] = prefixDictionary;
					foreach (var (prefixKey, quantity) in dictionary)
						prefixDictionary[prefixKey] = quantity;
				}

				creativeUnitPresent = CraftingGUI.allItemsAreInfinite;
				infiniteItems = [.. CraftingGUI.isItemInfinite];
			}

			protected void CopyToStaticCollectionsAndFields() {
				CraftingGUI.items.Clear();
				CraftingGUI.items.AddRange(resultItems);

				CraftingGUI.itemGroups.Clear();
				CraftingGUI.itemGroups.AddRange(resultItemGroups);

				CraftingGUI.sourceItemsFromModules.Clear();
				CraftingGUI.sourceItemsFromModules.AddRange(resultItemsFromModules);

				CraftingGUI.blockStorageItems.Clear();
				CraftingGUI.blockStorageItems.AddRange(blockStorageItems);

				CraftingGUI.wasModuleItem.Clear();
				foreach (var (key, _) in wasModuleItem)
					CraftingGUI.wasModuleItem.TryAdd(key, null);

				CraftingGUI.moduleItemWasFromInventory.Clear();
				foreach (var (key, _) in moduleItemWasFromInventory)
					CraftingGUI.moduleItemWasFromInventory.TryAdd(key, null);

				CraftingGUI.itemCounts.Clear();
				foreach (var (key, value) in itemCounts)
					CraftingGUI.itemCounts[key] = value;

				CraftingGUI.itemCountsByPrefix.Clear();
				foreach (var (key, value) in itemCountsByPrefix)
					CraftingGUI.itemCountsByPrefix[key] = value;

				CraftingGUI.isItemInfinite.Clear();
				CraftingGUI.isItemInfinite.UnionWith(infiniteItems);

				recipeItemsHandler?.CopyToStaticCollections();

				CraftingGUI.selectedRecipe = selectedRecipe;
				CraftingGUI.craftAmountTarget = craftAmountTarget;
				CraftingGUI.allItemsAreInfinite = creativeUnitPresent;
				CraftingGUI.showAllPossibleIngredients = showAllPossibleIngredients;
				MagicUI.lastKnownSearchBarErrorReason = base.searchBarError;
				CraftingGUI.lastKnownRecursionErrorForStoredItems = base.storedItemsError;

				CraftingGUI.SetRecipeAndCraftingCaches(this);
			}

			protected override void CollectObjects() {
				sandbox = new EnvironmentSandbox(Main.LocalPlayer, base.Heart);

				CraftingGUI.ResetRecentRecipeCache();
			}

			protected override void Cleanup() {
				wasModuleItem.Clear();
				moduleItemWasFromInventory.Clear();
				recipeItemsHandler = null;
			}

			public override void ClearStaticCollections() {
				CraftingGUI.items.Clear();
				CraftingGUI.itemGroups.Clear();
				CraftingGUI.sourceItemsFromModules.Clear();
				CraftingGUI.wasModuleItem.Clear();
				CraftingGUI.moduleItemWasFromInventory.Clear();
				CraftingGUI.itemCounts.Clear();
				CraftingGUI.itemCountsByPrefix.Clear();
				CraftingGUI.isItemInfinite.Clear();
				CraftingGUI.selectedRecipe = null;
				CraftingGUI.showAllPossibleIngredients = false;
				CraftingGUI.allItemsAreInfinite = false;
			}
		}
		#endregion

		#region CraftingControlsRefreshThread
		internal abstract class CraftingControlsRefreshThread : CommonCraftingThread {
			public readonly bool[] adjTiles;
			public List<Item> allStoredItems;
			public List<Item> allModuleItems;
			public readonly ItemTypeOrderedSet favoritedTypes;
			public readonly ItemTypeOrderedSet hiddenTypes;
			public readonly HashSet<int> globalHiddenTypes;
			public readonly int recipeFilterChoice;
			public IFilterProvider<Recipe> recipeFilterProvider;

			protected CraftingControlsRefreshThread(
				StorageViewControls controls,
				IEnumerable<bool> adjTiles,
				Recipe selectedRecipe,
				bool showAllIngredients,
				int recipeFilter,
				ItemTypeOrderedSet favorited,
				ItemTypeOrderedSet hidden,
				HashSet<ItemDefinition> configBlacklist,
				IEnumerable<ItemData> blockedStoredIngredients,
				int craftAmountTarget
			) : base(controls, selectedRecipe, showAllIngredients, blockedStoredIngredients, craftAmountTarget) {
				this.adjTiles = [.. adjTiles];
				favoritedTypes = favorited.Clone();
				hiddenTypes = hidden.Clone();
				globalHiddenTypes = [.. configBlacklist.Where(x => !x.IsUnloaded).Select(x => x.Type)];
				recipeFilterChoice = recipeFilter;
			}

			public bool IsHidden(int item) => globalHiddenTypes.Contains(item) || hiddenTypes.Contains(item);

			public bool IsInfiniteIngredient(int item) => creativeUnitPresent || infiniteItems.Contains(item);

			protected sealed override void CollectObjects() {
				base.CollectObjects();

				allStoredItems = [.. base.Heart.GetStoredItems()];

				allModuleItems = [];

				foreach (var module in base.Heart.GetModules()) {
					module.PreRefreshRecipes(sandbox);

					var items = module.GetAdditionalItems(sandbox);

					if (items is null || !items.Any())
						continue;
					
					bool inventoryItems = module is UseInventoryModule or UseInventoryNoFavoritesModule;

					foreach (Item item in items) {
						if (item is not { IsAir: false })
							continue;

						if (wasModuleItem.TryAdd(item, null)) {
							if (!inventoryItems || moduleItemWasFromInventory.TryAdd(item, null))
								allModuleItems.Add(item);
						}
					}
				}

				base.creativeUnitPresent = CheckForCreativeUnit(sandbox);

				base.infiniteItems = LoadInfiniteItems(sandbox);

				recipeFilterProvider = new StandardRecipeFilterProvider(this);

				PostItemsFound();

				CollectSnapshots();

				foreach (EnvironmentModule module in base.Heart.GetModules())
					module.ResetPlayer(sandbox);

				PostModuleAccess();
			}

			protected virtual void PostItemsFound() { }

			protected virtual void CollectSnapshots() { }

			protected virtual void PostModuleAccess() { }

			protected void PostRefreshRecipes() {
				foreach (var module in base.Heart.GetModules())
					module.PostRefreshRecipes(sandbox);
			}
		}
		#endregion

		#region CraftingRefreshThread
		internal class CraftingRefreshThread : CraftingControlsRefreshThread {
			public Recipe[] recipesToRefresh;
			public bool[] recipeConditionsMetSnapshot;
			public readonly List<Recipe> resultRecipes = [];
			public readonly List<bool> recipeIsAvailable = [];

			public CraftingRefreshThread(
				StorageViewControls controls,
				IEnumerable<bool> adjTiles,
				Recipe selectedRecipe,
				Recipe[] recipesToRefresh,
				bool showAllIngredients,
				int recipeFilter,
				ItemTypeOrderedSet favorited,
				ItemTypeOrderedSet hidden,
				HashSet<ItemDefinition> configBlacklist,
				IEnumerable<ItemData> blockedStoredIngredients,
				int craftAmountTarget
			) : base(controls, adjTiles, selectedRecipe, showAllIngredients, recipeFilter, favorited, hidden, configBlacklist, blockedStoredIngredients, craftAmountTarget) {
				this.recipesToRefresh = recipesToRefresh;
			}

			protected override void PostItemsFound() {
				AnalyzeIngredients();
			}

			protected override void CollectSnapshots() {
				recipeConditionsMetSnapshot = ExecuteInCraftingGuiEnvironment<bool[]>(() => [.. Main.recipe.Take(Recipe.numRecipes).Select(AvailableForSnapshot)]);
			}

			protected override void PostModuleAccess() {
				if (recipesToRefresh is { Length: > 0 }) {
					// RefreshSpecificRecipes() will manipulate the current lists, so they need to be cached
					resultRecipes.AddRange(CraftingGUI.recipes);
					recipeIsAvailable.AddRange(CraftingGUI.recipeAvailable);
				}
			}

			protected override void Execute() {
				CraftingGUI.SortAndFilter(this);

				base.CopyToStaticCollectionsAndFields();

				CraftingGUI.recipes.Clear();
				CraftingGUI.recipes.AddRange(resultRecipes);

				CraftingGUI.recipeAvailable.Clear();
				CraftingGUI.recipeAvailable.AddRange(recipeIsAvailable);

				base.PostRefreshRecipes();
			}

			public override void ClearStaticCollections() {
				base.ClearStaticCollections();
				CraftingGUI.recipes.Clear();
				CraftingGUI.recipeAvailable.Clear();
			}
		}
		#endregion

		#region RecipeInfoPanelRefreshThread
		internal class RecipeInfoPanelRefreshThread : CommonCraftingThread {
			public RecipeInfoPanelRefreshThread(
				StorageViewControls controls,
				Recipe selectedRecipe,
				bool showAllIngredients,
				IEnumerable<ItemData> blockedStoredIngredients,
				int craftAmountTarget
			) : base(controls, selectedRecipe, showAllIngredients, blockedStoredIngredients, craftAmountTarget)
			{
			}

			protected override void CollectObjects() {
				base.CollectObjects();

				base.CopyFromStaticCollectionsAndFields();
			}

			protected override void Execute() {
				CraftingGUI.RefreshStorageItems(this);

				base.CopyToStaticCollectionsAndFields();
			}
		}
		#endregion

		#region IRecipeItemsHandler
		public interface IRecipeItemsHandler {
			bool FoundStoredResultItem { get; }

			int StoredIngredientCount { get; }

			void AddStoredIngredient(Item item);

			void CompactCollections();

			void CopyToStaticCollections();

			IEnumerable<ItemInfo> GetIngredientsInfo();

			bool IsItemFromModule(Item item);

			void SetResultItem(Item item);
		}
		#endregion

		#region SingleResultItemHandler
		private class SingleResultItemHandler(CommonCraftingThread thread) : IRecipeItemsHandler {
			public readonly CommonCraftingThread _thread = thread;

			public List<Item> storedIngredients = [];
			public List<ItemInfo> storedIngredientsInfo = [];
			public Item resultItem;
			public bool hasStorageResult;

			public bool FoundStoredResultItem => resultItem is { IsAir: false };

			public int StoredIngredientCount => storedIngredients.Count;

			public void AddStoredIngredient(Item item) {
				// Items from modules need to be referenced directly
				if (!_thread.wasModuleItem.ContainsKey(item))
					item = item.Clone();

				storedIngredients.Add(item);
				storedIngredientsInfo.Add(item);
			}

			public void CompactCollections() {
				var stored = CraftingGUI.CompactItemList(_thread, this, storedIngredients);
				if (stored.Count != storedIngredients.Count) {
					storedIngredients = stored;
					storedIngredientsInfo = [.. stored.Select(x => new ItemInfo(x))];
				}
			}

			public void CopyToStaticCollections() {
				CraftingGUI.storageItems.AddRange(storedIngredients);
				CraftingGUI.storageItemInfo.AddRange(storedIngredientsInfo);
				CraftingGUI.result = resultItem ?? new Item(selectedRecipe.createItem.type, 0);
			}

			public IEnumerable<ItemInfo> GetIngredientsInfo() => storedIngredientsInfo;

			public bool IsItemFromModule(Item item) => _thread.wasModuleItem.ContainsKey(item);

			public void SetResultItem(Item item) {
				if (!_thread.wasModuleItem.ContainsKey(item)) {
					// Items from storage
					resultItem = item;
					hasStorageResult = true;
				} else if (!hasStorageResult && !_thread.moduleItemWasFromInventory.ContainsKey(item)) {
					// Items from modules that aren't the Player Inventory modules
					resultItem = item;
				}
			}
		}
		#endregion

		#region IRecipeFilterProvider
		public interface IFilterProvider<T> {
			bool ShowOnlyBlacklisted { get; }

			bool IsHidden(T value);

			internal bool IsVisible(T value) => !IsHidden(value);

			bool IsFavorited(T value);
		}
		#endregion

		#region StandardFilterProvider
		internal abstract class StandardFilterProvider<T> : IFilterProvider<T> {
			private readonly CraftingControlsRefreshThread _thread;

			public bool ShowOnlyBlacklisted { get; }

			public StandardFilterProvider(CraftingControlsRefreshThread thread) {
				_thread = thread;
				ShowOnlyBlacklisted = MagicStorageConfig.RecipeBlacklistEnabled && thread.recipeFilterChoice == CraftingGUI.RecipeButtonsBlacklistChoice;
			}

			public bool IsFavorited(T value) => _thread.favoritedTypes.Contains(GetObjectType(value));

			public bool IsHidden(T value) => _thread.IsHidden(GetObjectType(value));

			protected abstract int GetObjectType(T value);
		}
		#endregion

		#region StandardRecipeFilterProvider
		internal class StandardRecipeFilterProvider(CraftingControlsRefreshThread thread) : StandardFilterProvider<Recipe>(thread) {
			protected override int GetObjectType(Recipe value) => value.createItem.type;
		}
		#endregion
	}
}
