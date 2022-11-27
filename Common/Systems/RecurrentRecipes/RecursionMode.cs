namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public enum RecursionMode {
		/// <summary>
		/// Logic is kept in line with how it worked in the Recursive Craft integration.<br/>
		/// That is, recursive recipes are "combined" into one singular recipe.
		/// Simplicity at the cost of more processing required and less control.
		/// </summary>
		Legacy,
		/// <summary>
		/// Each sub-recipe in a recursive recipe processes its ingredients and conditions separately.<br/>
		/// Sub-recipes can be swapped out in a special UI in the Crafting UI.
		/// Less processing required and more control at the cost of more complexity.
		/// </summary>
		Modern
	}
}
