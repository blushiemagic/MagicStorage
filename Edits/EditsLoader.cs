using MagicStorage.Edits.Detours;
using On.Terraria;

namespace MagicStorage.Edits
{
	//Handles loading/unloading any method detours and IL edits
	internal static class EditsLoader
	{
		public static void Load()
		{
			Recipe.FindRecipes += Vanilla.Recipe_FindRecipes;
		}
	}
}
