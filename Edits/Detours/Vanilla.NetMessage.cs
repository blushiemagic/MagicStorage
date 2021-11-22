using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using On.Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using System.Diagnostics;

namespace MagicStorage.Edits.Detours
{
	internal static partial class Vanilla
	{
		static Stopwatch w = new Stopwatch();

		internal static void NetMessage_SendData(NetMessage.orig_SendData orig, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number,
			float number2, float number3, float number4, int number5, int number6, int number7)
		{
			orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
		}

		internal static void MessageBuffer_GetData(MessageBuffer.orig_GetData orig, Terraria.MessageBuffer self, int start, int length, out int messageType)
		{
			orig(self, start, length, out messageType);
		}
	}
}
