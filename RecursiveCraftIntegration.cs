using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MagicStorage.Components;
using RecursiveCraft;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;
using OnPlayer = On.Terraria.Player;

namespace MagicStorage
{
	public static class RecursiveCraftIntegration
	{
		// Here we store a reference to the RecursiveCraft Mod instance. We can use it for many things. 
		// You can call all the Mod methods on it just like we do with our own Mod instance: RecursiveCraftMod.ItemType("ExampleItem")
		private static Mod RecursiveCraftMod;

		// Here we define a bool property to quickly check if RecursiveCraft is loaded. 
		public static bool Enabled => RecursiveCraftMod != null;

		public static void Load()
		{
			RecursiveCraftMod = ModLoader.GetMod("RecursiveCraft");
			if (Enabled)
				Initialize(); // Move that logic into another method to prevent this.
		}

		// Be aware of inlining. Inlining can happen at the whim of the runtime. Without this Attribute, this mod happens to crash the 2nd time it is loaded on Linux/Mac. (The first call isn't inlined just by chance.) This can cause headaches. 
		// To avoid TypeInitializationException (or ReflectionTypeLoadException) problems, we need to specify NoInlining on methods like this to prevent inlining (methods containing or accessing Types in the Weakly referenced assembly). 
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void Initialize()
		{
			// This method will only be called when Enable is true, preventing TypeInitializationException
			Members.recipeCache = new Dictionary<Recipe, RecipeInfo>();
			OnPlayer.QuickSpawnItem_int_int += OnPlayerOnQuickSpawnItem_int_int;
		}

		public static void InitRecipes()
		{
			if (Enabled)
				InitRecipes_Inner();
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void InitRecipes_Inner()
		{
			Members.compoundRecipe = new CompoundRecipe(RecursiveCraftMod);
			Members.threadCompoundRecipe = new CompoundRecipe(RecursiveCraftMod);
		}

		public static void Unload()
		{
			if (Enabled) // Here we properly unload, making sure to check Enabled before setting RecursiveCraftMod to null.
				Unload_Inner(); // Once again we must separate out this logic.
			RecursiveCraftMod = null; // Make sure to null out any references to allow Garbage Collection to work.
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void Unload_Inner()
		{
			Members.recipeCache = null;
			Members.compoundRecipe = null;
			Members.threadCompoundRecipe = null;
			OnPlayer.QuickSpawnItem_int_int -= OnPlayerOnQuickSpawnItem_int_int;
		}

		private static void OnPlayerOnQuickSpawnItem_int_int(OnPlayer.orig_QuickSpawnItem_int_int orig, Player self, int type, int stack)
		{
			if (CraftingGUI.compoundCrafting)
			{
				var item = new Item();
				item.SetDefaults(type);
				item.stack = stack;
				CraftingGUI.compoundCraftSurplus.Add(item);
				return;
			}

			orig(self, type, stack);
		}

		private static Dictionary<int, int> FlatDict(IEnumerable<Item> items)
		{
			var dictionary = new Dictionary<int, int>();
			foreach (Item item in items)
				if (dictionary.ContainsKey(item.type))
					dictionary[item.type] += item.stack;
				else
					dictionary[item.type] = item.stack;
			return dictionary;
		}

		// Make sure to extract the .dll from the .tmod and then add them to your .csproj as references.
		// As a convention, I rename the .dll file ModName_v1.2.3.4.dll and place them in Mod Sources/Mods/lib. 
		// I do this for organization and so the .csproj loads properly for others using the GitHub repository. 
		// Remind contributors to download the referenced mod itself if they wish to build the mod.
		public static void RecursiveRecipes()
		{
			if (Main.rand == null)
				Main.rand = new UnifiedRandom((int) DateTime.UtcNow.Ticks);
			Dictionary<int, int> storedItems = GetStoredItems();
			if (storedItems == null)
				return;

			lock (Members.recipeCache)
			{
				var recursiveSearch = new RecursiveSearch(storedItems, GuiAsCraftingSource());
				Members.recipeCache.Clear();
				foreach (int i in RecursiveCraft.RecursiveCraft.SortedRecipeList)
				{
					Recipe recipe = Main.recipe[i];
					if (recipe is CompoundRecipe compound)
						SingleSearch(recursiveSearch, compound.OverridenRecipe);
					else
						SingleSearch(recursiveSearch, recipe);
				}
			}
		}

		private static Dictionary<int, int> GetStoredItems()
		{
			Player player = Main.LocalPlayer;
			var modPlayer = player.GetModPlayer<StoragePlayer>();
			TEStorageHeart heart = modPlayer.GetStorageHeart();
			if (heart == null)
				return null;
			return FlatDict(heart.GetStoredItems());
		}

		private static CraftingSource GuiAsCraftingSource() => new CraftingSource
		{
			AdjTile = CraftingGUI.adjTiles,
			AdjWater = CraftingGUI.adjWater,
			AdjHoney = CraftingGUI.adjHoney,
			AdjLava = CraftingGUI.adjLava,
			ZoneSnow = CraftingGUI.zoneSnow,
			AlchemyTable = CraftingGUI.alchemyTable
		};

		private static void SingleSearch(RecursiveSearch recursiveSearch, Recipe recipe)
		{
			RecipeInfo recipeInfo;
			lock (BlockRecipes.activeLock)
			{
				BlockRecipes.active = false;
				recipeInfo = recursiveSearch.FindIngredientsForRecipe(recipe);
				BlockRecipes.active = true;
			}

			if (recipeInfo != null && recipeInfo.RecipeUsed.Count > 1)
				Members.recipeCache.Add(recipe, recipeInfo);
		}

		public static bool IsCompoundRecipe(Recipe recipe) => recipe is CompoundRecipe;

		public static Recipe GetOverriddenRecipe(Recipe recipe) => recipe is CompoundRecipe compound ? compound.OverridenRecipe : recipe;

		public static bool UpdateRecipe(Recipe recipe)
		{
			if (recipe is CompoundRecipe compound)
				recipe = compound.OverridenRecipe;
			else
				return false;

			Dictionary<int, int> storedItems = GetStoredItems();
			if (storedItems != null)
				lock (Members.recipeCache)
				{
					Members.recipeCache.Remove(recipe);
					var recursiveSearch = new RecursiveSearch(storedItems, GuiAsCraftingSource());
					SingleSearch(recursiveSearch, recipe);
				}

			return Members.recipeCache.ContainsKey(recipe);
		}

		public static Recipe ApplyCompoundRecipe(Recipe recipe)
		{
			if (recipe is CompoundRecipe compound)
				recipe = compound.OverridenRecipe;
			if (Members.recipeCache.TryGetValue(recipe, out RecipeInfo recipeInfo))
			{
				int index = Array.IndexOf(Main.recipe, recipe);
				Members.compoundRecipe.Apply(index, recipeInfo);
				return Members.compoundRecipe;
			}

			return recipe;
		}

		public static Recipe ApplyThreadCompoundRecipe(Recipe recipe)
		{
			if (Members.recipeCache.TryGetValue(recipe, out RecipeInfo recipeInfo))
			{
				int index = Array.IndexOf(Main.recipe, recipe);
				Members.threadCompoundRecipe.Apply(index, recipeInfo);
				return Members.threadCompoundRecipe;
			}

			return recipe;
		}

		private static class Members
		{
			public static Dictionary<Recipe, RecipeInfo> recipeCache;
			public static CompoundRecipe compoundRecipe;
			public static CompoundRecipe threadCompoundRecipe;
		}
	}
}
