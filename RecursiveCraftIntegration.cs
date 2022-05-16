#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MagicStorage.Components;
using RecursiveCraft;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using OnPlayer = On.Terraria.Player;

namespace MagicStorage
{
#if !TML_2022_04
	[JITWhenModsEnabled(RecursiveCraftModName)]
#endif
	public sealed class RecursiveCraftIntegration : ModSystem
	{
		private const string RecursiveCraftModName = "RecursiveCraft";
		public static bool Enabled { get; private set; }

		public override void Load()
		{
			Enabled = ModLoader.HasMod(RecursiveCraftModName);
			if (Enabled)
				StrongRef_Load(); // Move that logic into another method to prevent this.
		}

		// Be aware of inlining. Inlining can happen at the whim of the runtime. Without this Attribute, this mod happens to crash the 2nd time it is loaded on Linux/Mac. (The first call isn't inlined just by chance.) This can cause headaches.
		// To avoid TypeInitializationException (or ReflectionTypeLoadException) problems, we need to specify NoInlining on methods like this to prevent inlining (methods containing or accessing Types in the Weakly referenced assembly).
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void StrongRef_Load()
		{
			OnPlayer.QuickSpawnItem_IEntitySource_int_int += OnPlayerQuickSpawnItem_IEntitySource_int_int;

			Members.RecipeInfoCache = new();
		}

		public override void PostAddRecipes()
		{
			if (Enabled)
				StrongRef_PostAddRecipes();
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void StrongRef_PostAddRecipes()
		{
			Members.CompoundRecipes = new CompoundRecipe[Recipe.maxRecipes];
			Members.RecipeToCompoundRecipe = new();

			for (int i = 0; i < Recipe.maxRecipes; i++)
			{
				CompoundRecipe compoundRecipe = new(Mod);

				Members.CompoundRecipes[i] = compoundRecipe;
				Members.RecipeToCompoundRecipe[compoundRecipe.Compound] = compoundRecipe;
			}
		}

		public override void Unload()
		{
			if (Enabled)            // Here we properly unload, making sure to check Enabled before unloading anything.
				StrongRef_Unload(); // Once again we must separate out this logic.
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void StrongRef_Unload()
		{
			OnPlayer.QuickSpawnItem_IEntitySource_int_int -= OnPlayerQuickSpawnItem_IEntitySource_int_int;

			Members.RecipeInfoCache = null!;
			Members.CompoundRecipes = null!;
			Members.RecipeToCompoundRecipe = null!;
		}

		private static int OnPlayerQuickSpawnItem_IEntitySource_int_int(OnPlayer.orig_QuickSpawnItem_IEntitySource_int_int orig,
			Player self, IEntitySource source, int type, int stack)
		{
			if (CraftingGUI.compoundCrafting)
			{
				Item item = new(type, stack);
				CraftingGUI.compoundCraftSurplus.Add(item);
				return -1; // return invalid value since this should never be used
			}

			return orig(self, source, type, stack);
		}

		private static Dictionary<int, int> FlatDict(IEnumerable<Item> items)
		{
			ArgumentNullException.ThrowIfNull(items);

			Dictionary<int, int> flatDict = new();

			foreach (Item item in items)
			{
				int type = item.type;
				int stack = item.stack;

				if (flatDict.ContainsKey(type))
				{
					flatDict[type] += stack;
				}
				else
				{
					flatDict.Add(type, stack);
				}
			}

			return flatDict;
		}

		// Make sure to extract the .dll from the .tmod and then add them to your .csproj as references.
		// As a convention, I rename the .dll file ModName_v1.2.3.4.dll and place them in Mod Sources/Mods/lib.
		// I do this for organization and so the .csproj loads properly for others using the GitHub repository.
		// Remind contributors to download the referenced mod itself if they wish to build the mod.
		public static void RefreshRecursiveRecipes()
		{
			CraftingGUI.ExecuteInCraftingGuiEnvironment(() =>
			{
				var storedItems = GetStoredItems();
				if (storedItems is null)
					return;

				FindRecipes(storedItems);
			});
		}

		private static void FindRecipes(Dictionary<int, int> inventory)
		{
			Members.RecipeInfoCache.Clear();
			RecursiveSearch recursiveSearch = new(inventory);

			for (int i = 0; i < Recipe.numRecipes; i++)
			{
				Recipe recipe = Main.recipe[i];

				if (Members.RecipeToCompoundRecipe.TryGetValue(recipe, out var compoundRecipe))
				{
					ArgumentNullException.ThrowIfNull(compoundRecipe.OverridenRecipe);
					recipe = compoundRecipe.OverridenRecipe;
				}

				RecipeInfo? recipeInfo = recursiveSearch.FindIngredientsForRecipe(recipe);
				if (recipeInfo != null)
				{
					if (recipeInfo.RecipeUsed.Count > 1)
						Members.RecipeInfoCache.Add(recipe, recipeInfo);
				}
			}
		}

		private static Dictionary<int, int>? GetStoredItems()
		{
			Player player = Main.LocalPlayer;
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
			TEStorageHeart heart = modPlayer.GetStorageHeart();
			if (heart is null)
				return null;

			return FlatDict(heart.GetStoredItems());
		}

		public static bool IsCompoundRecipe(Recipe recipe) => Members.RecipeToCompoundRecipe.ContainsKey(recipe);

		public static bool HasCompoundVariant(Recipe recipe)
		{
			if (Members.RecipeToCompoundRecipe.TryGetValue(recipe, out var compoundRecipe))
			{
				ArgumentNullException.ThrowIfNull(compoundRecipe.OverridenRecipe);
				recipe = compoundRecipe.OverridenRecipe;
			}

			return Members.RecipeInfoCache.ContainsKey(recipe);
		}

		public static Recipe GetOverriddenRecipe(Recipe recipe)
		{
			if (Members.RecipeToCompoundRecipe.TryGetValue(recipe, out var compoundRecipe))
			{
				ArgumentNullException.ThrowIfNull(compoundRecipe.OverridenRecipe);
				return compoundRecipe.OverridenRecipe;
			}

			return recipe;
		}

		public static Recipe ApplyCompoundRecipe(Recipe recipe)
		{
			if (Members.RecipeToCompoundRecipe.TryGetValue(recipe, out var compoundRecipe))
			{
				ArgumentNullException.ThrowIfNull(compoundRecipe.OverridenRecipe);
				recipe = compoundRecipe.OverridenRecipe;
			}

			if (!Members.RecipeInfoCache.TryGetValue(recipe, out var recipeInfo))
				return recipe;

			int index = Array.IndexOf(Main.recipe, recipe);
			compoundRecipe = Members.CompoundRecipes[index];
			compoundRecipe.Apply(index, recipeInfo);

			return compoundRecipe.Compound;
		}

		/*
		public static Recipe ApplyThreadCompoundRecipe(Recipe recipe)
		{
			if (recipe == Members.ThreadCompoundRecipe.Compound)
				recipe = Members.ThreadCompoundRecipe.OverridenRecipe!;

			if (!Members.RecipeInfoCache.TryGetValue(recipe, out RecipeInfo recipeInfo))
				return recipe;

			int index = Array.IndexOf(Main.recipe, recipe);
			Members.ThreadCompoundRecipe.Apply(index, recipeInfo);
			return Members.ThreadCompoundRecipe.Compound;
		}
		*/

		// TODO: test if the new tml JIT system allows these to be regular fields
#if !TML_2022_04
		[JITWhenModsEnabled(RecursiveCraftModName)]
#endif
		private static class Members
		{
			public static Dictionary<Recipe, RecipeInfo> RecipeInfoCache = null!;
			public static CompoundRecipe[] CompoundRecipes = null!;
			public static Dictionary<Recipe, CompoundRecipe> RecipeToCompoundRecipe = null!;
		}
	}
}
