using MagicStorage.Edits.Detours;
using On.Terraria;
using On.Terraria.DataStructures;

namespace MagicStorage.Edits
{
	//Handles loading/unloading any method detours and IL edits
	internal static class EditsLoader
	{
		internal static bool MessageTileEntitySyncing;

		internal static bool LightSend;
		internal static bool LightReceive;

		public static void Load()
		{
			NetMessage.SendData += Vanilla.NetMessage_SendData;

			TileEntity.WriteInner += Vanilla.TileEntity_WriteInner;
			TileEntity.ReadInner += Vanilla.TileEntity_ReadInner;

			MessageBuffer.GetData += Vanilla.MessageBuffer_GetData;

			Recipe.FindRecipes += Vanilla.Recipe_FindRecipes;
		}
	}
}
