namespace MagicStorage.Edits
{
	//Handles loading/unloading any method detours and IL edits
	internal static class EditsLoader
	{
		internal static bool MessageTileEntitySyncing;

		public static void Load()
		{
			On.Terraria.NetMessage.SendData += Detours.Vanilla.NetMessage_SendData;

			On.Terraria.MessageBuffer.GetData += Detours.Vanilla.MessageBuffer_GetData;

			On.Terraria.Recipe.FindRecipes += Detours.Vanilla.Recipe_FindRecipes;
		}
	}
}
