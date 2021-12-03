using System.IO;
using Terraria.DataStructures;
using OnTileEntity = On.Terraria.DataStructures.TileEntity;

namespace MagicStorage.Edits.Detours
{
	internal static partial class Vanilla
	{
		public static void TileEntity_WriteInner(OnTileEntity.orig_WriteInner orig, TileEntity self, BinaryWriter writer, bool networkSend, bool lightSend)
		{
			EditsLoader.LightSend = lightSend;
			orig(self, writer, networkSend, lightSend);
		}

		public static void TileEntity_ReadInner(OnTileEntity.orig_ReadInner orig, TileEntity self, BinaryReader reader, bool networkSend, bool lightReceive)
		{
			EditsLoader.LightReceive = lightReceive;
			orig(self, reader, networkSend, lightReceive);
		}
	}
}
