using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Edits {
	internal class RecipeCheckingILEdit : Edit {
		public override void LoadEdits() {
			DirectDetourManager.ILHook(typeof(RecipeLoader).GetCachedMethod("AddRecipes"), typeof(RecipeCheckingILEdit).GetCachedMethod(nameof(RecipeLoader_Hook)));
			DirectDetourManager.ILHook(typeof(RecipeLoader).GetCachedMethod("PostAddRecipes"), typeof(RecipeCheckingILEdit).GetCachedMethod(nameof(RecipeLoader_Hook)));
			DirectDetourManager.ILHook(typeof(RecipeLoader).GetCachedMethod("PostSetupRecipes"), typeof(RecipeCheckingILEdit).GetCachedMethod(nameof(RecipeLoader_Hook)));
		}

		public override void UnloadEdits() { }

		private static void RecipeLoader_Hook(ILContext il) {
			ILHelper.CommonPatchingWrapper(il, DoCommonPatching);
		}

		private static bool DoCommonPatching(ILCursor c, ref string badReturnReason) {
			int patchNum = 1;

			//Get the local num for "Mod mod"
			int modLocalNum = -1;
			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdloc(out _),
				i => i.MatchLdloc(out _),
				i => i.MatchLdelemRef(),
				i => i.MatchStloc(out modLocalNum)))
				goto bad_il;

			patchNum++;

			if (!c.TryGotoNext(MoveType.Before, i => i.MatchLeaveS(out _)))
				goto bad_il;

			patchNum++;
			c.Emit(OpCodes.Ldloc, modLocalNum);  //Mod mod = CurrentMod;
			c.EmitDelegate(CheckRecipes);

			return true;

			bad_il:
			badReturnReason += "\nReason: Could not find instruction sequence for patch #" + patchNum;
			return false;
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
						$"Problem Recipe:  {result} @ {tile}}");
				}
			}
		}
	}
}
