using System.Collections.Generic;
using System.IO;
using On.Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Edits.Detours
{
	internal static class Vanilla
	{
		internal static void NetMessage_SendData(NetMessage.orig_SendData orig, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
		{
			//TileSection (10) doesn't set "networkSend" to true in TileEntity.Write, so this needs to be kept track of manually
			//Keeping track of this simplifies the workaround code somewhat
			EditsLoader.MessageTileEntitySyncing = msgType == MessageID.TileSection;

			orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);

			EditsLoader.MessageTileEntitySyncing = false;

			//Catch any uses of IDs TileSection (10) and send the ModPacket message
			//This is to circumvent the 65535 shorts' worth of data per-message limit and, hopefully, prevent world sections from suddenly disappearing for no reason
			if (msgType == MessageID.TileSection)
			{
				ModPacket packet = MagicStorage.Instance.GetPacket();

				//Get the entities in the section.  Keep writing until the next entity written would make the size go over 65535
				int startX = number;
				int startY = (int) number2;
				short width = (short) number3;
				short height = (short) number4;

				var ids = new Queue<int>();

				//Only process tile entities from Magic Storage
				foreach (KeyValuePair<Point16, TileEntity> item in TileEntity.ByPosition)
				{
					Point16 pos = item.Key;
					if (pos.X >= startX && pos.X < startX + width && pos.Y >= startY && pos.Y < startY + height)
					{
						if (ModTileEntity.GetTileEntity(item.Value.type)?.mod == MagicStorage.Instance)
						{
							ids.Enqueue(item.Value.ID);
						}
					}
				}

				using (var ms = new MemoryStream())
				using (var ms2 = new MemoryStream())
				using (var msWriter = new BinaryWriter(ms))
				using (var msWriter2 = new BinaryWriter(ms2))
				{
					int written = 0, total = 0, packetCount = 1;

					while (ids.Count > 0)
						WriteNetWorkaround(msWriter, ms, msWriter2, ms2, ids, ref written, ref total, ref packetCount, ref packet, remoteClient, ignoreClient, false);

					if (written > 0)
						//Write the remaining information
					{
						WriteNetWorkaround(msWriter, ms, msWriter2, ms2, ids, ref written, ref total, ref packetCount, ref packet, remoteClient, ignoreClient, true);
					}

					/*
					if (Main.netMode == NetmodeID.Server && total > 0)
						Console.WriteLine($"Magic Storage: Wrote {packetCount} packets for {total} entities, {(packetCount - 1) * 65535 + ms.Position} bytes written");
					*/
					msWriter.Flush();
					msWriter2.Flush();
				}
			}
		}

		private static void WriteNetWorkaround(BinaryWriter msWriter, MemoryStream ms, BinaryWriter msWriter2, MemoryStream ms2, Queue<int> ids, ref int written, ref int total, ref int packetCount, ref ModPacket packet, int remoteClient, int ignoreClient, bool lastSend)
		{
			long start = msWriter.BaseStream.Position, end = start;

			// TODO: why does the last entity in the packet have a bad ID???  also, fix the "read underflow" issues from the other packet types

			if (!lastSend)
			{
				//The last send won't be getting another tile, so just ignore this section
				TileEntity.Write(msWriter2, TileEntity.ByID[ids.Dequeue()]);
				written++;
				total++;

				msWriter2.Flush();

				end += msWriter2.BaseStream.Position;
			}

			byte[] newBytes = ms2.GetBuffer();

			if (end > 65535 || lastSend && written > 0)
			{
				//Too much data for one net message
				// TODO: handle when ONE entity sends too much data, since this assumes that at least 2 would have to be split up across messages
				msWriter.Flush();

				byte[] bytes = ms.GetBuffer();

				//Write the data before the "overflow"
				//If this isn't the last packet, then the actual amount of entities written is "written - 1"
				packet.Write((byte) MessageType.NetWorkaround);
				packet.Write((ushort) (lastSend ? written : written - 1));
				packet.Write(bytes, 0, (int) start);

				packet.Send(remoteClient, ignoreClient);

				//Debugging purposes
				/*
				if (Main.netMode == NetmodeID.Server)
				{
					string path = Path.Combine(Main.SavePath, "MagicStorage Logging");
					Directory.CreateDirectory(path);

					path = Path.Combine(path, $"packet - {DateTime.Now.Ticks}t - {start}b.dat");

					using (BinaryWriter fileWriter = new BinaryWriter(File.Open(path, FileMode.Create)))
						fileWriter.Write(bytes, 0, (int)start);
				}

				if (Main.netMode == NetmodeID.Server)
					Console.WriteLine($"  [written: {written}, total: {total}, packets: {packetCount}, length: {start}]");
				*/

				written = 0;

				if (!lastSend)
				{
					//Reset the packet
					packet = MagicStorage.Instance.GetPacket();

					packetCount++;

					//Reset the stream
					ms.Position = 0;
					ms.SetLength(0);
					ms.Capacity = 0;

					//Still need to write data for one more entity
					written = 1;
				}
			}

			if (!lastSend)
			{
				//Copy over the new bytes
				msWriter.Write(newBytes, 0, (int) (end - start));

				ms2.Position = 0;
				ms2.SetLength(0);
				ms2.Capacity = 0;
			}
		}

		internal static void MessageBuffer_GetData(MessageBuffer.orig_GetData orig, Terraria.MessageBuffer self, int start, int length, out int messageType)
		{
			orig(self, start, length, out messageType);

			//Set to true in Mod.HijackGetData if the message ID is TileSection (10)
			EditsLoader.MessageTileEntitySyncing = false;
		}
	}
}
