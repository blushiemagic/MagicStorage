using MagicStorage.Modules;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;

namespace MagicStorage.Common.Threading.Refreshing {
	public interface IProcessedStorageItemsProvider {
		ProcessedStorageItems ProcessedStorageItems { get; }
	}

	public class ProcessedStorageItems {
		public List<Item> allModuleItems;
		/// <summary>
		/// <b>NOTE:</b> Value doesn't matter; the item is from a module if, and only if, this table has it as a key
		/// </summary>
		public readonly WeakTableProvider<Item, object> wasModuleItem;
		/// <summary>
		/// <b>NOTE:</b> Value doesn't matter; the item is from the player's main inventory if, and only if, this table has it as a key
		/// </summary>
		public readonly WeakTableProvider<Item, object> moduleItemWasFromInventory;
		public readonly ListProvider<Item> resultItems;
		public readonly ListOfListsProvider<Item> resultItemGroups;
		public readonly ListProvider<Item> resultItemsFromModules;
		public readonly DictionaryProvider<int, int> itemCounts;
		public readonly DictionaryOfDictionariesProvider<int, int, int> itemCountsByPrefix;

		public ProcessedStorageItems(
			ConditionalWeakTable<Item, object> staticWasModuleItemTable,
			ConditionalWeakTable<Item, object> staticModuleItemWasFromInventoryTable,
			List<Item> staticResultItemsList,
			List<List<Item>> staticResultItemGroupsList,
			List<Item> staticResultItemsFromModulesList,
			Dictionary<int, int> staticCountsDictionary,
			Dictionary<int, Dictionary<int, int>> staticCountsByPrefixDictionary
		) {
			wasModuleItem = new(staticWasModuleItemTable);
			moduleItemWasFromInventory = new(staticModuleItemWasFromInventoryTable);
			resultItems = new(staticResultItemsList);
			resultItemGroups = new(staticResultItemGroupsList);
			resultItemsFromModules = new(staticResultItemsFromModulesList);
			itemCounts = new(staticCountsDictionary);
			itemCountsByPrefix = new(staticCountsByPrefixDictionary);
		}

		public void CollectObjects(RefreshThread thread) {
			var sandbox = new EnvironmentSandbox(Main.LocalPlayer, thread.Heart);

			allModuleItems = [];

			foreach (var module in thread.Heart.GetModules()) {
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
		}

		public void CopyFromStaticCollectionsAndFields() {
			wasModuleItem.CopyFromStatic();
			moduleItemWasFromInventory.CopyFromStatic();
			resultItems.CopyFromStatic();
			resultItemGroups.CopyFromStatic();
			resultItemsFromModules.CopyFromStatic();
			itemCounts.CopyFromStatic();
			itemCountsByPrefix.CopyFromStatic();
		}

		public void CopyToStaticCollectionsAndFields() {
			wasModuleItem.OverwriteStatic();
			moduleItemWasFromInventory.OverwriteStatic();
			resultItems.OverwriteStatic();
			resultItemGroups.OverwriteStatic();
			resultItemsFromModules.OverwriteStatic();
			itemCounts.OverwriteStatic();
			itemCountsByPrefix.OverwriteStatic();
		}

		public void ClearStaticCollections() {
			wasModuleItem.ClearStatic();
			moduleItemWasFromInventory.ClearStatic();
			resultItems.ClearStatic();
			resultItemGroups.ClearStatic();
			resultItemsFromModules.ClearStatic();
			itemCounts.ClearStatic();
			itemCountsByPrefix.ClearStatic();
		}
	}
}
