using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MagicStorage.Edits;
using MagicStorage.Sorting;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage
{
	// TODO: think of a better name
	public class MagicSystem : ModSystem
	{
		public static Dictionary<int, List<Recipe>> hasIngredient;
		public static Dictionary<int, List<Recipe>> hasTile;

		internal static Dictionary<int, Func<Item, Item, bool>> canCombineByType;

		internal static List<Recipe> EnabledRecipes;

		public static bool CanCombine(Item item1, Item item2) => ItemData.Matches(item1, item2) && (!canCombineByType.TryGetValue(item1.type, out var func) || func(item1, item2));

		internal static bool CanCombineIgnoreType(Item item1, Item item2) => !canCombineByType.TryGetValue(item1.type, out var func) || func(item1, item2);

		public override void Load() {
			canCombineByType = new();
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			InterfaceHelper.ModifyInterfaceLayers(layers);
		}

		public override void PostUpdateInput()
		{
			if (!Main.instance.IsActive)
				return;

			StorageGUI.Update(null);
			CraftingGUI.Update(null);
		}

		public override void PostSetupContent() {
			SortingCache.dictionary.Fill();
		}

		public override void PostSetupRecipes() {
			hasIngredient?.Clear();
			hasIngredient = new();

			hasTile?.Clear();
			hasTile = new();

			//Initialize the lookup tables
			for (int i = 0; i < Recipe.numRecipes; i++) {
				Recipe recipe = Main.recipe[i];

				foreach (var item in recipe.requiredItem) {
					if (!hasIngredient.TryGetValue(item.type, out var list))
						hasIngredient[item.type] = list = new();

					list.Add(recipe);
				}

				foreach (var tile in recipe.requiredTile) {
					if (!hasTile.TryGetValue(tile, out var list))
						hasTile[tile] = list = new();

					list.Add(recipe);
				}
			}

			EnabledRecipes = Main.recipe.AsParallel().Take(Recipe.numRecipes).Where(r => !r.Disabled).ToList();
		}
	}
}
