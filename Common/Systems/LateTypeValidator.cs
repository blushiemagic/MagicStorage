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
		}
	}
}
