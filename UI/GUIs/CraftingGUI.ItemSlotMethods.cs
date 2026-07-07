using MagicStorage.Common.Systems;
using MagicStorage.Common;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Terraria.Localization;
using Terraria;
using MagicStorage.UI;

namespace MagicStorage {
	partial class CraftingGUI {
		internal static Item GetStation(int slot, ref int context)
		{
			List<Item> stations = GetCraftingStations();
			if (stations is not null && slot < stations.Count)
				return stations[slot];
			return new Item();
		}

		internal static Item GetHeader(int slot, ref int context)
		{
			return (selectedRecipe?.createItem.Clone()) ?? new Item();
		}

		internal static Item GetIngredient(int slot, ref int context)
		{
			if (selectedRecipe == null || slot >= selectedRecipe.requiredItem.Count)
				return new Item();

			Item item = selectedRecipe.requiredItem[slot].Clone();
			if (selectedRecipe.HasRecipeGroup(RecipeGroupID.Wood) && item.type == ItemID.Wood)
				item.SetNameOverride(Language.GetText("LegacyMisc.37").Value + " " + Lang.GetItemNameValue(ItemID.Wood));
			if (selectedRecipe.HasRecipeGroup(RecipeGroupID.Sand) && item.type == ItemID.SandBlock)
				item.SetNameOverride(Language.GetText("LegacyMisc.37").Value + " " + Lang.GetItemNameValue(ItemID.SandBlock));
			if (selectedRecipe.HasRecipeGroup(RecipeGroupID.IronBar) && item.type == ItemID.IronBar)
				item.SetNameOverride(Language.GetText("LegacyMisc.37").Value + " " + Lang.GetItemNameValue(ItemID.IronBar));
			if (selectedRecipe.HasRecipeGroup(RecipeGroupID.Fragment) && item.type == ItemID.FragmentSolar)
				item.SetNameOverride(Language.GetText("LegacyMisc.37").Value + " " + Language.GetText("LegacyMisc.51").Value);
			if (selectedRecipe.HasRecipeGroup(RecipeGroupID.PressurePlate) && item.type == ItemID.GrayPressurePlate)
				item.SetNameOverride(Language.GetText("LegacyMisc.37").Value + " " + Language.GetText("LegacyMisc.38").Value);
			if (ProcessGroupsForText(selectedRecipe, item.type, out string nameOverride))
				item.SetNameOverride(nameOverride);

			// OPTIMIZATION: Use itemCounts dictionary for O(1) lookups instead of O(n*m*k*p) nested loops through storageItems
			// This fixes severe lag when player has multiple stacks of certain items (stone, mud, etc.)
			
			int directItemCount = itemCounts.TryGetValue(item.type, out int count) ? count : 0;
			
			ClampedArithmetic totalGroupStack = 0;
			// Calculate recipe group totals by dictionary lookup instead of iterating storageItems
			foreach (RecipeGroup rec in selectedRecipe.acceptedGroups.Select(index => RecipeGroup.recipeGroups[index])) {
				if (rec.ContainsItem(item.type)) {
					foreach (int type in rec.ValidItems) {
						if (itemCounts.TryGetValue(type, out int groupItemCount)) {
							totalGroupStack += groupItemCount;

							if (totalGroupStack >= item.stack)
								goto StopCheckingRecipeGroups;
						}
					}
				}
			}

			StopCheckingRecipeGroups:

			if (!item.IsAir) {
				if (directItemCount == 0 && (int)totalGroupStack == 0)
					context = MagicSlotContext.IngredientNotCraftable;
				else if (directItemCount < item.stack && totalGroupStack < item.stack)
					context = MagicSlotContext.IngredientPartiallyInStock;

				if (context != MagicSlotContext.Normal) {
					bool craftable;

				//	using (FlagSwitch.Create(ref disableNetPrintingForIsAvailable, true))
					craftable = MagicCache.ResultToRecipe.TryGetValue(item.type, out var r) && r.Any(recipe => IsAvailable(recipe, true));

					if (craftable)
						context = MagicSlotContext.IngredientCraftable;
				}
			}

			return item;
		}

		internal static bool ProcessGroupsForText(Recipe recipe, int type, out string theText)
		{
			foreach (int num in recipe.acceptedGroups)
				if (RecipeGroup.recipeGroups[num].ContainsItem(type))
				{
					theText = RecipeGroup.recipeGroups[num].GetText();
					return true;
				}

			theText = "";
			return false;
		}

		internal static Item GetResult(int slot, ref int context) => slot == 0 && result is not null ? result : new Item();
	}
}
