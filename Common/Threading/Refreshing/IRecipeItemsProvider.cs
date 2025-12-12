using MagicStorage.Common.Systems.RecurrentRecipes;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;

namespace MagicStorage.Common.Threading.Refreshing {
	public interface IRecipeItemsProvider {
		RecipeItems RecipeItems { get; }
	}

	public abstract class RecipeItems {
		public readonly ItemInfoListProvider storedIngredients;
		public IModuleItemResolver itemResolver;

		public RecipeItems(
			List<Item> staticStoredIngredientsList,
			List<ItemInfo> staticStoredIngredientsInfoList
		) {
			storedIngredients = new(
				staticItemsList: staticStoredIngredientsList,
				staticInfoList: staticStoredIngredientsInfoList
			);
		}

		public abstract string GetItemCountsReport();

		public void AddStoredIngredient(Item item) {
			storedIngredients.items.Add(item);
			storedIngredients.info.Add(item);
		}

		public virtual void CompactCollections(RefreshThread thread) {
			CraftingGUI.CompactItemList(
				thread,
				storedIngredients,
				itemResolver.IsModuleItem,
				"Stored Ingredients"
			);
		}

		public virtual void CopyFromStaticCollections() {
			storedIngredients.items.CopyFromStatic();
			storedIngredients.info.CopyFromStatic();
		}

		public virtual void CopyToStaticCollections() {
			storedIngredients.items.OverwriteStatic();
			storedIngredients.info.OverwriteStatic();
		}

		public virtual void ClearStaticCollections() {
			storedIngredients.items.ClearStatic();
			storedIngredients.info.ClearStatic();
		}

		public abstract void SetResultItem(Item item);
	}
}
