﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using SerousCommonLib.API;
using System;
using System.Linq;
using System.Reflection;
 using MonoMod.RuntimeDetour;
 using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Edits {
	internal class RecipeCheckingILEdit : Edit {
		private static readonly MethodInfo RecipeLoader_AddRecipes = typeof(RecipeLoader).GetMethod("AddRecipes", BindingFlags.NonPublic | BindingFlags.Static);
		private static event ILContext.Manipulator IL_RecipeLoader_AddRecipes;
		private static Hook _ilHookRecipeLoaderAddRecipes;

		private static readonly MethodInfo RecipeLoaderPostAddRecipes = typeof(RecipeLoader).GetMethod("PostAddRecipes", BindingFlags.NonPublic | BindingFlags.Static);
		private static event ILContext.Manipulator IL_RecipeLoader_PostAddRecipes;
		private static Hook _ilHookRecipeLoaderPostAddRecipes;

		private static readonly MethodInfo RecipeLoader_PostSetupRecipes = typeof(RecipeLoader).GetMethod("PostSetupRecipes", BindingFlags.NonPublic | BindingFlags.Static);
		private static event ILContext.Manipulator IL_RecipeLoader_PostSetupRecipes;
		private static Hook ILHook_RecipeLoader_PostSetupRecipes;

		public override void LoadEdits() {
			try {
				IL_RecipeLoader_AddRecipes += RecipeLoader_Hook;
				//_ilHookRecipeLoaderAddRecipes = new Hook(RecipeLoader_AddRecipes, new ILContext.Manipulator(RecipeLoader_Hook));
				IL_RecipeLoader_PostAddRecipes += RecipeLoader_Hook;
				//_ilHookRecipeLoaderPostAddRecipes = new Hook(RecipeLoaderPostAddRecipes, new ILContext.Manipulator(RecipeLoader_Hook));
				IL_RecipeLoader_PostSetupRecipes += RecipeLoader_Hook;
				//ILHook_RecipeLoader_PostSetupRecipes = new Hook(RecipeLoader_PostSetupRecipes, new ILContext.Manipulator(RecipeLoader_Hook));
			} catch (Exception ex) when (BuildInfo.IsDev) {
				// Swallow exceptions on dev builds
				MagicStorageMod.Instance.Logger.Error($"An edit for \"{nameof(RecipeCheckingILEdit)}\" failed", ex);
			}
		}

		public override void UnloadEdits() {
			IL_RecipeLoader_AddRecipes -= RecipeLoader_Hook;
			//_ilHookRecipeLoaderAddRecipes.Dispose();
			IL_RecipeLoader_PostAddRecipes -= RecipeLoader_Hook;
			//_ilHookRecipeLoaderPostAddRecipes.Dispose();
			IL_RecipeLoader_PostSetupRecipes -= RecipeLoader_Hook;
			//ILHook_RecipeLoader_PostSetupRecipes.Dispose();
		}

		private static void RecipeLoader_Hook(ILContext il) {
			ILHelper.CommonPatchingWrapper(il, MagicStorageMod.Instance, DoCommonPatching);
		}

		private static bool DoCommonPatching(ILCursor c, ref string badReturnReason) {
			//Get the local num for "Mod mod"
			int modLocalNum = -1;
			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdloc(out _),
				i => i.MatchLdloc(out _),
				i => i.MatchLdelemRef(),
				i => i.MatchStloc(out modLocalNum))) {
				badReturnReason = "Could not find instruction sequence for initializing Mod local";
				return false;
			}

			if (!c.TryGotoNext(MoveType.Before, i => i.MatchLeaveS(out _))) {
				badReturnReason = "Could not find end of handler clause";
				return false;
			}

			c.Emit(OpCodes.Ldloc, modLocalNum);  //Mod mod = CurrentMod;
			c.EmitDelegate(CheckRecipes);

			return true;
		}

		private static void CheckRecipes(Mod mod) {
			for (int i = 0; i < Recipe.numRecipes; i++) {
				Recipe recipe = Main.recipe[i];

				if (recipe.Disabled)
					continue;  //Ignore since Magic Storage can't use it anyway

				if (recipe.requiredItem.Any(i => i.type <= ItemID.None || i.stack <= 0)) {
					string result = recipe.createItem.IsAir ? "<result not set>" : $"{Lang.GetItemNameValue(recipe.createItem.type)} ({recipe.createItem.stack})";
					string tile = recipe.requiredTile.Count == 0 ? "hand" : string.Join(", ", recipe.requiredTile.Select(t => TileID.Search.TryGetName(t, out string s) ? s : "<unknown>"));

					throw new Exception($"Mod \"{mod.Name}\" added or modified a recipe to be in an invalid state.\n" +
						"Reason: An ingredient had a stack size of zero or less.\n" +
						$"Problem Recipe:  {result} @ {tile}");
				}
			}
		}
	}
}