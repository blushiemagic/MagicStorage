using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public struct ItemInfo {
		public readonly int itemType;
		public readonly int itemStack;

		public ItemInfo(int type, int stack) {
			itemType = type;
			itemStack = stack;
		}

		public void Deconstruct(out int type, out int stack) {
			type = itemType;
			stack = itemStack;
		}
	}

	public class RecurrentRecipeInfo {
		public readonly Recipe recipe;

		public readonly List<ItemInfo> excessResults;

		internal RecurrentRecipeInfo(Recipe recipe) {
			this.recipe = recipe;
			excessResults = new();
		}
	}

	public class RecursiveRecipe {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				recipeToRecursiveRecipe.Clear();
			}
		}

		/// <summary>
		/// The base recipe object
		/// </summary>
		public readonly Recipe original;

		// All possible recipes from the original
		private List<RecurrentRecipeInfo> recurrentRecipes;

		public readonly RecursionTree tree;

		internal static readonly ConditionalWeakTable<Recipe, RecursiveRecipe> recipeToRecursiveRecipe = new();

		private bool recalculateRecipes;

		public RecursiveRecipe(Recipe recipe) {
			original = recipe;
			tree = new RecursionTree(recipe);
		}

		public static void RecalculateAllRecursiveRecipes() {
			foreach (var (_, recursive) in recipeToRecursiveRecipe) {
				recursive.recalculateRecipes = true;
				recursive.tree.Reset();
			}

			RecursionTree.NodePool.ClearNodes();
		}

		/// <summary>
		/// In <see cref="RecursionMode.Legacy"/>, this method returns a list of recipes consisting of the combinations of sub-recipes.<br/>
		/// In <see cref="RecursionMode.Modern"/>, the original recipe is returned instead.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<RecurrentRecipeInfo> GetRecurrentRecipes() {
			if (recalculateRecipes) {
				recurrentRecipes = null;
				recalculateRecipes = false;
			}

			if (MagicStorageConfig.RecipeRecursionMode == RecursionMode.Modern) {
				// Singular recipes aren't supported in this mode, just return the original recipe
				return new RecurrentRecipeInfo[] { new(original) };
			}

			if (recurrentRecipes is not null)
				return recurrentRecipes;

			CalculateRecurrentRecipes();

			return recurrentRecipes;
		}

		private class CraftingInfo {
			public readonly List<ItemInfo> consumedItems = new();
			public readonly List<ItemInfo> excessResults = new();
			public readonly HashSet<int> usedStations = new();
			public readonly HashSet<Condition> usedConditions = new(ReferenceEqualityComparer.Instance);
		}

		private void CalculateRecurrentRecipes() {
			recurrentRecipes = new();

			// Ensure that the tree is calculated
			tree.CalculateTree();

			// For every recipe in the tree, make a recipe using the combinations of the infinite recipes
		}

		private Recipe InitRecurrentRecipe() {
			Recipe recurrentRecipe = Recipe.Create(0);
			BuildCopyDelegate().Invoke(original, recurrentRecipe);
			return recurrentRecipe;
		}

		internal static void UnloadDelegates() {
			copyRecipeIndexDelegate = null;
			getConsumeItemCallbackDelegate = null;
			getOnCraftCallbackDelegate = null;
		}

		private delegate void CopyRecipeIndex(Recipe copyFrom, Recipe copyTo);
		private static CopyRecipeIndex copyRecipeIndexDelegate;

		private static CopyRecipeIndex BuildCopyDelegate() {
			if (copyRecipeIndexDelegate is not null)
				return copyRecipeIndexDelegate;
			
			DynamicMethod dmd = new(typeof(RecursiveRecipe).FullName + ".BuildCopyDelegate.<>DMD", null, new Type[] { typeof(Recipe), typeof(Recipe) }, typeof(MagicStorageMod).Module, skipVisibility: true);
			ILGenerator il = dmd.GetILGenerator();

			PropertyInfo Recipe_RecipeIndex = typeof(Recipe).GetProperty(nameof(Recipe.RecipeIndex), BindingFlags.Public | BindingFlags.Instance);
			MethodInfo Recipe_get_RecipeIndex = Recipe_RecipeIndex.GetGetMethod();
			MethodInfo Recipe_set_RecipeIndex = Recipe_RecipeIndex.GetSetMethod(nonPublic: true);

			/*
			Desired resulting method:

			arg_1.RecipeIndex = arg_0.RecipeIndex;
			*/

			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_0);
			il.EmitCall(OpCodes.Call, Recipe_get_RecipeIndex, null);
			il.EmitCall(OpCodes.Call, Recipe_set_RecipeIndex, null);
			il.Emit(OpCodes.Ret);

			copyRecipeIndexDelegate = dmd.CreateDelegate<CopyRecipeIndex>();

			return copyRecipeIndexDelegate;
		}

		private delegate Recipe.ConsumeItemCallback GetConsumeItemCallback(Recipe recipe);
		private static GetConsumeItemCallback getConsumeItemCallbackDelegate;

		private static GetConsumeItemCallback BuildConsumeItemCallbackDelegate() {
			if (getConsumeItemCallbackDelegate is not null)
				return getConsumeItemCallbackDelegate;
			
			DynamicMethod dmd = new(typeof(RecursiveRecipe).FullName + ".BuildConsumeItemCallbackDelegate.<>DMD", typeof(Recipe.ConsumeItemCallback), new Type[] { typeof(Recipe) }, typeof(MagicStorageMod).Module, skipVisibility: true);
			ILGenerator il = dmd.GetILGenerator();

			PropertyInfo Recipe_ConsumeItemHooks = typeof(Recipe).GetProperty("ConsumeItemHooks", BindingFlags.NonPublic | BindingFlags.Instance);
			MethodInfo Recipe_get_ConsumeItemHooks = Recipe_ConsumeItemHooks.GetGetMethod(nonPublic: true);

			/*
			Desired resulting method:

			return arg_0.ConsumeItemHooks;
			*/

			il.Emit(OpCodes.Ldarg_0);
			il.EmitCall(OpCodes.Call, Recipe_get_ConsumeItemHooks, null);
			il.Emit(OpCodes.Ret);

			getConsumeItemCallbackDelegate = dmd.CreateDelegate<GetConsumeItemCallback>();

			return getConsumeItemCallbackDelegate;
		}

		private delegate Recipe.OnCraftCallback GetOnCraftCallback(Recipe recipe);
		private static GetOnCraftCallback getOnCraftCallbackDelegate;

		private static GetOnCraftCallback BuildOnCraftCallbackDelegate() {
			if (getOnCraftCallbackDelegate is not null)
				return getOnCraftCallbackDelegate;
			
			DynamicMethod dmd = new(typeof(RecursiveRecipe).FullName + ".BuildOnCraftCallbackDelegate.<>DMD", typeof(Recipe.OnCraftCallback), new Type[] { typeof(Recipe) }, typeof(MagicStorageMod).Module, skipVisibility: true);
			ILGenerator il = dmd.GetILGenerator();

			PropertyInfo Recipe_OnCraftHooks = typeof(Recipe).GetProperty("OnCraftHooks", BindingFlags.NonPublic | BindingFlags.Instance);
			MethodInfo Recipe_get_OnCraftHooks = Recipe_OnCraftHooks.GetGetMethod(nonPublic: true);

			/*
			Desired resulting method:

			return arg_0.OnCraftHooks;
			*/

			il.Emit(OpCodes.Ldarg_0);
			il.EmitCall(OpCodes.Call, Recipe_get_OnCraftHooks, null);
			il.Emit(OpCodes.Ret);

			getOnCraftCallbackDelegate = dmd.CreateDelegate<GetOnCraftCallback>();

			return getOnCraftCallbackDelegate;
		}
	}
}
