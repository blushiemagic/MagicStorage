using System.Collections.Generic;
using Terraria;

namespace MagicStorage.Common.Threading.Refreshing {
	public interface IIngredientControlsProvider {
		IngredientControls IngredientControls { get; }
	}

	public class IngredientControls {
		public readonly IReadOnlyValueProvider<bool> showAllPossibleIngredients;
		public readonly HashSetProvider<int> infiniteItems;
		public readonly ListProvider<ItemData> blockStorageItems;
		public readonly IValueProvider<bool> creativeUnitPresent;

		public IngredientControls(
			IReadOnlyValueProvider<bool> staticShowAllIngredientsField,
			HashSet<int> staticInfiniteItemsSet,
			List<ItemData> staticBlockedList,
			IValueProvider<bool> staticCreativeUnitField
		) {
			showAllPossibleIngredients = staticShowAllIngredientsField;
			infiniteItems = new HashSetProvider<int>(staticInfiniteItemsSet);
			blockStorageItems = new ListProvider<ItemData>(staticBlockedList);
			creativeUnitPresent = staticCreativeUnitField;
		}

		public bool IsInfiniteIngredient(int item) => creativeUnitPresent.Value || infiniteItems.Contains(item);

		public bool IsIngredientBlocked(Item ingredient) => blockStorageItems.Contains(ingredient);

		public void CollectObjects(RefreshThread thread) {
			var sandbox = new EnvironmentSandbox(Main.LocalPlayer, thread.Heart);

			creativeUnitPresent.Value = sandbox.HeartHasCreativeUnit();
			
			infiniteItems.Clear();
			foreach (int item in sandbox.LoadInfiniteItems())
				infiniteItems.Add(item);

			blockStorageItems.CopyFromStatic();  // Always copy
		}

		public void CopyFromStaticCollectionsAndFields() {
			showAllPossibleIngredients.CopyFromStatic();
			infiniteItems.CopyFromStatic();
			blockStorageItems.CopyFromStatic();
			creativeUnitPresent.CopyFromStatic();
		}

		public void CopyToStaticCollectionsAndFields() {
			showAllPossibleIngredients.OverwriteStatic();
			infiniteItems.OverwriteStatic();
			blockStorageItems.OverwriteStatic();
			creativeUnitPresent.OverwriteStatic();
		}

		public void ClearStaticCollections() {
			showAllPossibleIngredients.ClearStatic();
			infiniteItems.ClearStatic();
		//	blockStorageItems.ClearStatic();
			creativeUnitPresent.ClearStatic();
		}
	}
}
