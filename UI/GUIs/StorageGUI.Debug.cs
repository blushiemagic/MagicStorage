using MagicStorage.Components;
using System.Collections.Generic;
using System.Reflection;
using System;
using Terraria.ID;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;
using Terraria.ModLoader;
using Terraria;
using System.Linq;

namespace MagicStorage {
	partial class StorageGUI {
		internal static List<Item> showcaseItems;

		internal static void DepositShowcaseItemsToCurrentStorage() {
			if (GetHeart() is not TEStorageHeart heart || heart.GetStoredItems().Any())
				return;

			if (showcaseItems is not null)
				goto DepositTheItems;

			showcaseItems = new();
			HashSet<int> addedTypes = new();

			void MakeItemsForUnloadedDataShowcase(int type, int stack) {
				showcaseItems.Add(new Item(type, stack));

				Item item = new(type, stack);
				AddRandomUnloadedItemDataToItem(item);

				showcaseItems.Add(item);

				addedTypes.Add(type);
			}

			void MakeItemsForSellingShowcase(int type) {
				HashSet<int> rolledPrefixes = new();
				int tries = 200;

				for (int i = 0; i < 5; i++) {
					Item item = new(type);
					item.Prefix(-1);

					if (!rolledPrefixes.Add(item.prefix)) {
						i--;
						tries--;

						if (tries <= 0)
							break;
					}

					showcaseItems.Add(item);
					tries = 200;
				}

				addedTypes.Add(type);
			}

			void MakeCoinsForStackingShowcase() {
				void MakeCoins(int type, int total) {
					while (total > 0) {
						showcaseItems.Add(new Item(type, Math.Min(100, total)));
						total -= 100;
					}

					addedTypes.Add(type);
				}

				MakeCoins(ItemID.CopperCoin, 135);
				MakeCoins(ItemID.SilverCoin, 215);
				MakeCoins(ItemID.GoldCoin, 180);
				MakeCoins(ItemID.PlatinumCoin, 60);
			}

			bool MakeRandomItem(ref int i, Func<Item, bool> condition, Action onValidItemFail = null) {
				int type = Main.rand.Next(0, ItemLoader.ItemCount);

				Item item = new(type);

				if (item.IsAir) {
					i--;
					return false;
				}

				item.stack = Main.rand.Next(1, item.maxStack + 1);

				if (!condition(item)) {
					onValidItemFail?.Invoke();

					i--;
					return false;
				}

				if (!addedTypes.Add(type)) {
					i--;
					return false;
				}

				showcaseItems.Add(item);
				return true;
			}

			void MakeItem(int type) {
				if (addedTypes.Add(type)) {
					Item item = new(type);
					item.stack = item.maxStack;

					showcaseItems.Add(item);
				}
			}

			void MakeResearchedItems(bool researched) {
				bool CheckIsValidResearchableItemForShowcase(Item item) {
					if (item.dye > 0 || item.maxStack == 1)
						return false;

					return researched != Utility.IsFullyResearched(item.type, true);
				}

				int tries = 1000;

				for (int i = 0; i < 10; i++) {
					if (MakeRandomItem(ref i, CheckIsValidResearchableItemForShowcase, () => tries--))
						tries = 1000;
					else if (tries <= 0)
						return;
				}
			}

			void MakeIngredientItems() {
				bool CheckIsValidMaterialAndMaximizeStack(Item item) {
					if (item.maxStack == 1 || !item.material || item.type == ItemID.DirtBlock || item.createTile >= TileID.Dirt || item.dye > 0 || item.createWall > WallID.None)
						return false;

					item.stack = item.maxStack;
					return true;
				}

				for (int i = 0; i < 50; i++)
					MakeRandomItem(ref i, CheckIsValidMaterialAndMaximizeStack);

				MakeItem(ItemID.Wood);
				MakeItem(ItemID.StoneBlock);
				MakeItem(ItemID.Torch);
				MakeItem(ItemID.ClayBlock);
				MakeItem(ItemID.SandBlock);
				MakeItem(ItemID.Glass);
				MakeItem(ItemID.Cobweb);
			}

			MakeItemsForUnloadedDataShowcase(ItemID.WoodenSword, 1);
			MakeItemsForUnloadedDataShowcase(ItemID.CopperHelmet, 1);
			MakeItemsForUnloadedDataShowcase(ItemID.FlamingArrow, 100);
			MakeItemsForSellingShowcase(ItemID.Megashark);
			MakeItemsForSellingShowcase(ItemID.LightningBoots);
			MakeCoinsForStackingShowcase();
			MakeResearchedItems(true);
			MakeResearchedItems(false);
			MakeIngredientItems();

			Main.NewText("Showcase initialized.  Item count: " + showcaseItems.Count);

			DepositTheItems:
			heart.TryDeposit(showcaseItems.Select(i => i.Clone()).ToList());
			SetRefresh();
		}

		internal static readonly FieldInfo UnloadedGlobalItem_data = typeof(UnloadedGlobalItem).GetField("data", BindingFlags.NonPublic | BindingFlags.Instance);

		private static void AddRandomUnloadedItemDataToItem(Item item)
		{
			if (item is null || item.IsAir || item.ModItem is UnloadedItem)
				return;

			// NOTE: this method will always fail, but i can't be bothered to update it since users can't use it anyway
			if (TEStorageHeart.Item_globalItems.GetValue(item) is not Ref<GlobalItem>[] globalItems || globalItems.Length == 0)
				return;

			// Create the data
			TagCompound modData = new()
			{
				["mod"] = "MagicStorage",
				["name"] = "ShowcaseItemData",
				["data"] = new TagCompound()
				{
					["randomData"] = Main.rand.Next()
				}
			};

			if (item.TryGetGlobalItem(out UnloadedGlobalItem obj))
				(UnloadedGlobalItem_data.GetValue(obj) as IList<TagCompound>).Add(modData);
			else
			{
				Ref<GlobalItem>[] array = (Ref<GlobalItem>[])globalItems.Clone();
				int index = array.Length;

				Array.Resize(ref array, array.Length + 1);

				// Create the instance
				obj = new UnloadedGlobalItem();
				UnloadedGlobalItem_data.SetValue(obj, new List<TagCompound>() { modData });

				Ref<GlobalItem> newItemRef = new Ref<GlobalItem>();
				newItemRef.Value = obj;

				array[^1] = newItemRef;

				TEStorageHeart.Item_globalItems.SetValue(item, array);
			}
		}
	}
}
