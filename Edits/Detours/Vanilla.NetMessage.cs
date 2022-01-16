using ReLogic.OS;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Edits.Detours
{
	internal static class Vanilla
	{
		internal static void NetMessage_SendData(On.Terraria.NetMessage.orig_SendData orig, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
		{
			orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
		}

		internal static void MessageBuffer_GetData(On.Terraria.MessageBuffer.orig_GetData orig, MessageBuffer self, int start, int length, out int messageType)
		{
			orig(self, start, length, out messageType);
		}
	}
}
