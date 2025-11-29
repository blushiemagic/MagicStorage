using SerousCommonLib.API.ModCall;
using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;

namespace MagicStorage.CrossMod.Calls.Functions {
	internal class AddExtraCraftDrop : BaseCallFunction {
		// "Add Extra Craft Drop", Recipe recipe, IItemDropRule rule
		// "Add Extra Craft Drop", Func<Recipe, bool> condition, IItemDropRule rule

		public override object Call(ReadOnlySpan<object> args) {
			ThrowIfNotEnoughArgs(args, 2);

			return Handle(
				GetOrThrowIfNotEither<Recipe, Func<Recipe, bool>>(args, 0),
				GetOrThrowIfNot<IItemDropRule>(args, 1)
			);
		}

		private static object Handle(Either<Recipe, Func<Recipe, bool>> recipe, IItemDropRule rule) {
			if (recipe.IsFirstOption)
				ExtraCraftItemsSystem.RegisterDrop(recipe.FirstOption, rule);
			else if (recipe.IsSecondOption)
				ExtraCraftItemsSystem.RegisterDrop(recipe.SecondOption, rule);

			return null;
		}
	}
}
