namespace MagicStorage.Edits
{
	//Handles loading/unloading any method detours and IL edits
	internal static class EditsLoader
	{
		internal static bool MessageTileEntitySyncing;

		public static void Load()
		{
			// 1.4 have Recipe.FindRecipes += Vanilla.Recipe_FindRecipes;
		}
	}
}
