using MagicStorageExtra.Edits.Detours;

namespace MagicStorageExtra.Edits
{
	//Handles loading/unloading any method detours and IL edits
	internal static class EditsLoader
	{
		internal static bool MessageTileEntitySyncing;

		public static void Load()
		{
			On.Terraria.NetMessage.SendData += Vanilla.NetMessage_SendData;

			On.Terraria.MessageBuffer.GetData += Vanilla.MessageBuffer_GetData;
		}

		public static void Unload()
		{
			On.Terraria.NetMessage.SendData -= Vanilla.NetMessage_SendData;

			On.Terraria.MessageBuffer.GetData -= Vanilla.MessageBuffer_GetData;
		}
	}
}
