using MagicStorage.Common.Systems;
using MagicStorage.Common.Threading.Refreshing;
using MagicStorage.CrossMod;
using System.Collections.Generic;
using Terraria;

namespace MagicStorage {
	partial class StorageGUI {
		private class StorageRefreshThread : RefreshThread, IStorageItemsPovider {
			public readonly HashSet<int> targetItemTypes;
			public bool uniqueSlotPerItemStack;

			public override bool IsPartialThread => false;

			public override bool HasCompleteData => true;

			public override IRefreshThreadBuilder FullRefreshBuilder => null;

			public StorageItems StorageItems { get; } = new();

			public StorageRefreshThread(
				StorageViewControls controls,
				ActionMode currentMode,
				HashSet<int> itemTypesToUpdate
			) : base(MagicUI.storageUI, HijackControls(controls, currentMode)) {
				targetItemTypes = itemTypesToUpdate is null ? null : [.. itemTypesToUpdate];
				uniqueSlotPerItemStack = currentMode is ActionMode.Deletion;
			}

			private static StorageViewControls HijackControls(StorageViewControls original, ActionMode currentMode) {
				if (currentMode is ActionMode.Deletion) {
					// Item Deletion Mode needs to always show all items
					return original.CreateCopy(
						filteringOptionOverride: FilteringOptionLoader.Definitions.All.Type,
						generalFiltersOverride: []
					);
				} else
					return original;
			}

			protected override void CollectObjects() {
				StorageItems.CollectObjects(this);
			}

			protected override void Execute() {
				IEnumerable<Item> items;

				if (targetItemTypes is not { Count: > 0 }) {
					// Use the items as they are in storage
					items = StorageItems.allStoredItems;
				} else {
					// Order the items to where items that don't need to update will be in the same general order
					// This should reduce the execution time when sorting
					items = AdjustToUpdateSet(StorageItems.allStoredItems, targetItemTypes);
				}

				// Adjust further based on the filter setting
				if (base.controls.filteringOption == FilteringOptionLoader.Definitions.Recent.Type) {
					items = AdjustToDepositHistory(this, items);
					base.workingCounter = RECENT_FILTER_ITEM_COUNT;
				} else
					base.workingCounter = StorageItems.allStoredItems.Count;

				base.workingItemList = items;
				base.workingFlag = uniqueSlotPerItemStack;

				SortAndFilter(this);

				MagicUI.lastKnownSearchBarErrorReason = base.searchBarError;
			}

			protected override void Cleanup() { }

			public override void ClearStaticCollections() => StorageGUI.ClearAllCollections();

			// Unused due to being a full thread
			public override void PrepareUIZones() { }

			public override void PopulateUIZones() { }
		}
	}
}
