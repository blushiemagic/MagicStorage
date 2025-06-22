using MagicStorage.Items;
using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace MagicStorage.Common.Systems {
	/// <summary>
	/// An interface for an alterative to <see cref="ModType.ValidateType"/> that's called later in the mod loading process
	/// </summary>
	public interface IValidateAtPostSetupContent : ILoadable {
		/// <summary>
		/// Called during <see cref="ModSystem.PostSetupContent"/>
		/// </summary>
		void ValidateType();
	}

	/// <summary>
	/// An interface for an alterative to <see cref="ModType.ValidateType"/> that's called later in the mod loading process
	/// </summary>
	public interface IValidateAtPostSetupRecipes : ILoadable {
		/// <summary>
		/// Called during <see cref="ModSystem.PostSetupRecipes"/>
		/// </summary>
		void ValidateType();
	}

	internal class LateTypeValidator : ModSystem {
		public override void PostSetupContent() {
			LoaderUtils.ForEachAndAggregateExceptions(ModContent.GetContent<IValidateAtPostSetupContent>(), validator => validator.ValidateType());
		}

		public override void PostSetupRecipes() {
			LoaderUtils.ForEachAndAggregateExceptions(ModContent.GetContent<IValidateAtPostSetupRecipes>(), validator => validator.ValidateType());

			// No recipes can be added for any Storage Core item
			var cores = Main.recipe
				.Take(Recipe.numRecipes)
				.Where(r => !r.Disabled)
				.Select(r => r.createItem.ModItem)
				.OfType<BaseStorageCore>()
				.Select(core => core.FullName)
				.ToList();

			if (cores.Count > 0)
				throw new Exception($"Recipes cannot have a Storage Core as the result item.\n  {string.Join("\n  ", cores)}");
		}
	}
}
