using MagicStorage.Common.Systems.RecurrentRecipes;
using System.Collections.Generic;
using Terraria;

namespace MagicStorage.Common.Threading.Refreshing {
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
}
