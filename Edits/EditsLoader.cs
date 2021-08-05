using MagicStorage.Edits.Detours;
using On.Terraria;

namespace MagicStorage.Edits
{
	//Handles loading/unloading any method detours and IL edits
	internal static class EditsLoader
	{
		internal static bool MessageTileEntitySyncing;

		public static void Load()
		{
			NetMessage.SendData += Vanilla.NetMessage_SendData;

			MessageBuffer.GetData += Vanilla.MessageBuffer_GetData;
		}

		public static void Unload()
		{
			NetMessage.SendData -= Vanilla.NetMessage_SendData;

			MessageBuffer.GetData -= Vanilla.MessageBuffer_GetData;
		}
	}
}
