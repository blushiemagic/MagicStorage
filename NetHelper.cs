using System;
using System.IO;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using MagicStorage.Components;
using MagicStorage.Edits;

namespace MagicStorage
{
    public static class NetHelper
    {
        private static bool queueUpdates = false;
        private static Queue<int> updateQueue = new Queue<int>();
        private static HashSet<int> updateQueueContains = new HashSet<int>();

        public static void HandlePacket(BinaryReader reader, int sender)
        {
            MessageType type = (MessageType)reader.ReadByte();

            /*
            if (Main.netMode == NetmodeID.MultiplayerClient)
                Main.NewText($"Receiving Message Type \"{Enum.GetName(typeof(MessageType), type)}\"");
            else if(Main.netMode == NetmodeID.Server)
                Console.WriteLine($"Receiving Message Type \"{Enum.GetName(typeof(MessageType), type)}\"");
            */

            if (type == MessageType.SearchAndRefreshNetwork)
            {
                ReceiveSearchAndRefresh(reader);
            }
            else if (type == MessageType.TryStorageOperation)
            {
                ReceiveStorageOperation(reader, sender);
            }
            else if (type == MessageType.StorageOperationResult)
            {
                ReceiveOperationResult(reader);
            }
            else if (type == MessageType.RefreshNetworkItems)
            {
                ReceiveRefreshNetworkItems(reader);
            }
            else if (type == MessageType.ClientSendTEUpdate)
            {
                ReceiveClientSendTEUpdate(reader, sender);
            }
            else if (type == MessageType.TryStationOperation)
            {
                ReceiveStationOperation(reader, sender);
            }
            else if (type == MessageType.StationOperationResult)
            {
                ReceiveStationResult(reader);
            }
            else if (type == MessageType.ResetCompactStage)
            {
                ReceiveResetCompactStage(reader);
            }
            else if (type == MessageType.CraftRequest)
            {
                ReceiveCraftRequest(reader, sender);
            }
            else if (type == MessageType.CraftResult)
            {
                ReceiveCraftResult(reader);
            }
            else if (type == MessageType.NetWorkaround)
            {
                ReceiveNetWorkaround(reader);
            }
        }

        public static void SendComponentPlace(int i, int j, int type)
        {
            if (Main.netMode == 1)
            {
                NetMessage.SendTileRange(Main.myPlayer, i, j, 2, 2);
                NetMessage.SendData(MessageID.TileEntityPlacement, -1, -1, null, i, j, type);
            }
        }

        public static void StartUpdateQueue()
        {
            queueUpdates = true;
        }

        public static void SendTEUpdate(int id, Point16 position)
        {
            if (Main.netMode != 2)
            {
                return;
            }
            if (queueUpdates)
            {
                if (!updateQueueContains.Contains(id))
                {
                    updateQueue.Enqueue(id);
                    updateQueueContains.Add(id);
                }
            }
            else
            {
                NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, id, position.X, position.Y);
            }
        }

        public static void ProcessUpdateQueue()
        {
            if (queueUpdates)
            {
                queueUpdates = false;
                while (updateQueue.Count > 0)
                {
                    NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, updateQueue.Dequeue());
                }
                updateQueueContains.Clear();
            }
        }

        public static void SendSearchAndRefresh(int i, int j)
        {
            if (Main.netMode == 1)
            {
                ModPacket packet = MagicStorage.Instance.GetPacket();
                packet.Write((byte)MessageType.SearchAndRefreshNetwork);
                packet.Write((short)i);
                packet.Write((short)j);
                packet.Send();
            }
        }

        private static void ReceiveSearchAndRefresh(BinaryReader reader)
        {
            Point16 point = new Point16(reader.ReadInt16(), reader.ReadInt16());
            TEStorageComponent.SearchAndRefreshNetwork(point);
        }

        private static ModPacket PrepareStorageOperation(int ent, byte op)
        {
            ModPacket packet = MagicStorage.Instance.GetPacket();
            packet.Write((byte)MessageType.TryStorageOperation);
            packet.Write(ent);
            packet.Write(op);
            return packet;
        }

        private static ModPacket PrepareOperationResult(byte op)
        {
            ModPacket packet = MagicStorage.Instance.GetPacket();
            packet.Write((byte)MessageType.StorageOperationResult);
            packet.Write(op);
            return packet;
        }

        public static void SendDeposit(int ent, Item item)
        {
            if (Main.netMode == 1)
            {
                ModPacket packet = PrepareStorageOperation(ent, 0);
                ItemIO.Send(item, packet, true);
                packet.Send();
            }
        }

        public static void SendWithdraw(int ent, Item item, bool toInventory = false)
        {
            if (Main.netMode == 1)
            {
                ModPacket packet = PrepareStorageOperation(ent, (byte)(toInventory ? 3 : 1));
                ItemIO.Send(item, packet, true);
                packet.Send();
            }
        }

        public static void SendDepositAll(int ent, List<Item> items)
        {
            if (Main.netMode == 1)
            {
                ModPacket packet = PrepareStorageOperation(ent, 2);
                packet.Write((byte)items.Count);
                foreach (Item item in items)
                {
                    ItemIO.Send(item, packet, true);
                }
                packet.Send();
            }
        }

        public static void ReceiveStorageOperation(BinaryReader reader, int sender)
        {
            int ent = reader.ReadInt32();
            byte op = reader.ReadByte();

            /*
            if (Main.netMode == NetmodeID.Server)
                Console.WriteLine($"Receiving Storage Operation {op} from entity {ent}");
            else
                Main.NewText($"Receiving Storage Operation {op} from entity {ent}");
            */

            if (Main.netMode != 2)
            {
                //The data still needs to be read for exceptions to not be thrown...
                if (op == 0 || op == 1 || op == 3)
                {
                    ItemIO.Receive(reader, true);
                }
                else if (op == 3)
                {
                    int count = reader.ReadByte();
                    for (int i = 0; i < count; i++)
                        ItemIO.Receive(reader, true);
                }

                return;
            }
            if (!TileEntity.ByID.ContainsKey(ent) || !(TileEntity.ByID[ent] is TEStorageHeart))
            {
                return;
            }
            TEStorageHeart heart = (TEStorageHeart)TileEntity.ByID[ent];
            if (op == 0)
            {
                Item item = ItemIO.Receive(reader, true);
                heart.DepositItem(item);
                if (!item.IsAir)
                {
                    ModPacket packet = PrepareOperationResult(op);
                    ItemIO.Send(item, packet, true);
                    packet.Send(sender);
                }
            }
            else if (op == 1 || op == 3)
            {
                Item item = ItemIO.Receive(reader, true);
                item = heart.TryWithdraw(item);
                if (!item.IsAir)
                {
                    ModPacket packet = PrepareOperationResult(op);
                    ItemIO.Send(item, packet, true);
                    packet.Send(sender);
                }
            }
            else if (op == 2)
            {
                int count = reader.ReadByte();
                List<Item> items = new List<Item>();
                StartUpdateQueue();
                for (int k = 0; k < count; k++)
                {
                    Item item = ItemIO.Receive(reader, true);
                    heart.DepositItem(item);
                    if (!item.IsAir)
                    {
                        items.Add(item);
                    }
                }
                ProcessUpdateQueue();
                if (items.Count > 0)
                {
                    ModPacket packet = PrepareOperationResult(op);
                    packet.Write((byte)items.Count);
                    foreach (Item item in items)
                    {
                        ItemIO.Send(item, packet, true);
                    }
                    packet.Send(sender);
                }
            }
            SendRefreshNetworkItems(ent);
        }

        public static void ReceiveOperationResult(BinaryReader reader)
        {
            byte op = reader.ReadByte();

            /*
            if (Main.netMode == NetmodeID.Server)
                Console.WriteLine($"Receiving Operation Result {op}");
            else
                Main.NewText($"Receiving Operation Result {op}");
            */

            if (Main.netMode != 1)
            {
                //The data still needs to be read for exceptions to not be thrown...
                if (op == 0 || op == 1 || op == 3)
                {
                    ItemIO.Receive(reader, true);
                }
                else if (op == 2)
                {
                    int count = reader.ReadByte();
                    for (int i = 0; i < count; i++)
                        ItemIO.Receive(reader, true);
                }

                return;
            }
            if (op == 0 || op == 1 || op == 3)
            {
                Item item = ItemIO.Receive(reader, true);

                /*
                Main.NewText($"Item Read: [type: {item.type}, prefix: {item.prefix}, stack: {item.stack}]");
                Main.NewText($"Reader Position: {reader.BaseStream.Position}");
                */

                StoragePlayer.GetItem(item, op != 3);
            }
            else if (op == 2)
            {
                int count = reader.ReadByte();
                for (int k = 0; k < count; k++)
                {
                    Item item = ItemIO.Receive(reader, true);

                    /*
                    Main.NewText($"Item Read: [type: {item.type}, prefix: {item.prefix}, stack: {item.stack}]");
                    Main.NewText($"Reader Position: {reader.BaseStream.Position}");
                    */

                    StoragePlayer.GetItem(item, false);
                }
            }
        }

        public static void SendRefreshNetworkItems(int ent)
        {
            if (Main.netMode == 2)
            {
                ModPacket packet = MagicStorage.Instance.GetPacket();
                packet.Write((byte)MessageType.RefreshNetworkItems);
                packet.Write(ent);
                packet.Send();
            }
        }

        private static void ReceiveRefreshNetworkItems(BinaryReader reader)
        {
            int ent = reader.ReadInt32();
            if (Main.netMode == 2)
            {
                return;
            }

            if (!TileEntity.ByID.TryGetValue(ent, out _))
            {
                //Nothing would've happened anyway
                return;
            }

            StorageGUI.RefreshItems();
        }

        public static void ClientSendTEUpdate(int id)
        {
            if (Main.netMode == 1)
            {
                ModPacket packet = MagicStorage.Instance.GetPacket();
                packet.Write((byte)MessageType.ClientSendTEUpdate);
                packet.Write(id);
                TileEntity.Write(packet, TileEntity.ByID[id], true);
                packet.Send();
            }
        }

        public static void ReceiveClientSendTEUpdate(BinaryReader reader, int sender)
        {
            if (Main.netMode == 2)
            {
                int id = reader.ReadInt32();
                TileEntity ent = TileEntity.Read(reader, true);
                ent.ID = id;
                TileEntity.ByID[id] = ent;
                TileEntity.ByPosition[ent.Position] = ent;
                if (ent is TEStorageUnit)
                {
                    TEStorageHeart heart = ((TEStorageUnit)ent).GetHeart();
                    if (heart != null)
                    {
                        heart.ResetCompactStage();
                    }
                }
                NetMessage.SendData(MessageID.TileEntitySharing, -1, sender, null, id, ent.Position.X, ent.Position.Y);
            }
            else if (Main.netMode == 1)
            {
                //Still need to read the data
                reader.ReadInt32();
            }
        }

        private static ModPacket PrepareStationOperation(int ent, byte op)
        {
            ModPacket packet = MagicStorage.Instance.GetPacket();
            packet.Write((byte)MessageType.TryStationOperation);
            packet.Write(ent);
            packet.Write(op);
            return packet;
        }

        private static ModPacket PrepareStationResult(byte op)
        {
            ModPacket packet = MagicStorage.Instance.GetPacket();
            packet.Write((byte)MessageType.StationOperationResult);
            packet.Write(op);
            return packet;
        }

        public static void SendDepositStation(int ent, Item item)
        {
            if (Main.netMode == 1)
            {
                ModPacket packet = PrepareStationOperation(ent, 0);
                ItemIO.Send(item, packet, true);
                packet.Send();
            }
        }

        public static void SendWithdrawStation(int ent, int slot)
        {
            if (Main.netMode == 1)
            {
                ModPacket packet = PrepareStationOperation(ent, 1);
                packet.Write((byte)slot);
                packet.Send();
            }
        }

        public static void SendStationSlotClick(int ent, Item item, int slot)
        {
            if (Main.netMode == 1)
            {
                ModPacket packet = PrepareStationOperation(ent, 2);
                ItemIO.Send(item, packet, true);
                packet.Write((byte)slot);
                packet.Send();
            }
        }

        public static void ReceiveStationOperation(BinaryReader reader, int sender)
        {
            int ent = reader.ReadInt32();
            byte op = reader.ReadByte();
            if (Main.netMode != 2)
            {
                //Still need to read the data for exceptions to not be thrown...
                if (op == 0)
                {
                    ItemIO.Receive(reader, true);
                }
                else if(op == 1)
                {
                    reader.ReadByte();
                }
                else if(op == 2)
                {
                    ItemIO.Receive(reader, true);
                    reader.ReadByte();
                }
                return;
            }
            if (!TileEntity.ByID.ContainsKey(ent) || !(TileEntity.ByID[ent] is TECraftingAccess))
            {
                return;
            }
            TECraftingAccess access = (TECraftingAccess)TileEntity.ByID[ent];
            Item[] stations = access.stations;
            if (op == 0)
            {
                Item item = ItemIO.Receive(reader, true);
                access.TryDepositStation(item);
                if (item.stack > 0)
                {
                    ModPacket packet = PrepareStationResult(op);
                    ItemIO.Send(item, packet, true);
                    packet.Send(sender);
                }
            }
            else if (op == 1)
            {
                int slot = reader.ReadByte();
                Item item = access.TryWithdrawStation(slot);
                if (!item.IsAir)
                {
                    ModPacket packet = PrepareStationResult(op);
                    ItemIO.Send(item, packet, true);
                    packet.Send(sender);
                }
            }
            else if (op == 2)
            {
                Item item = ItemIO.Receive(reader, true);
                int slot = reader.ReadByte();
                item = access.DoStationSwap(item, slot);
                if (!item.IsAir)
                {
                    ModPacket packet = PrepareStationResult(op);
                    ItemIO.Send(item, packet, true);
                    packet.Send(sender);
                }
            }
            Point16 pos = access.Position;
            StorageAccess modTile = TileLoader.GetTile(Main.tile[pos.X, pos.Y].type) as StorageAccess;
            if (modTile != null)
            {
                TEStorageHeart heart = modTile.GetHeart(pos.X, pos.Y);
                if (heart != null)
                {
                    SendRefreshNetworkItems(heart.ID);
                }
            }
        }

        public static void ReceiveStationResult(BinaryReader reader)
        {
            //Still need to read the data for exceptions to not be thrown...
            byte op = reader.ReadByte();
            Item item = ItemIO.Receive(reader, true);
            if (Main.netMode != 1)
            {
                return;
            }
            Player player = Main.player[Main.myPlayer];
            if (op == 2 && Main.playerInventory && Main.mouseItem.IsAir)
            {
                Main.mouseItem = item;
                item = new Item();
            }
            else if (op == 2 && Main.playerInventory && Main.mouseItem.type == item.type)
            {
                int total = Main.mouseItem.stack + item.stack;
                if (total > Main.mouseItem.maxStack)
                {
                    total = Main.mouseItem.maxStack;
                }
                int difference = total - Main.mouseItem.stack;
                Main.mouseItem.stack = total;
                item.stack -= total;
            }
            if (item.stack > 0)
            {
                item = player.GetItem(Main.myPlayer, item, false, true);
                if (!item.IsAir)
                {
                    player.QuickSpawnClonedItem(item, item.stack);
                }
            }
        }

        public static void SendResetCompactStage(int ent)
        {
            if (Main.netMode == 1)
            {
                ModPacket packet = MagicStorage.Instance.GetPacket();
                packet.Write((byte)MessageType.ResetCompactStage);
                packet.Write(ent);
                packet.Send();
            }
        }

        public static void ReceiveResetCompactStage(BinaryReader reader)
        {
            if (Main.netMode == 2)
            {
                int ent = reader.ReadInt32();
                if (TileEntity.ByID[ent] is TEStorageHeart)
                {
                    ((TEStorageHeart)TileEntity.ByID[ent]).ResetCompactStage();
                }
            }
            else if (Main.netMode == 1)
            {
                reader.ReadInt32();
            }
        }

        public static void SendCraftRequest(int heart, List<Item> toWithdraw, Item result)
        {
            if (Main.netMode == 1)
            {
                ModPacket packet = MagicStorage.Instance.GetPacket();
                packet.Write((byte)MessageType.CraftRequest);
                packet.Write(heart);
                packet.Write(toWithdraw.Count);
                foreach (Item item in toWithdraw)
                {
                    ItemIO.Send(item, packet, true);
                }
                ItemIO.Send(result, packet, true);
                packet.Send();
            }
        }

        public static void ReceiveCraftRequest(BinaryReader reader, int sender)
        {
            int ent = reader.ReadInt32();
            int count = reader.ReadInt32();
            if (Main.netMode != 2)
            {
                //Still need to read the data for exceptions to not be thrown
                for (int i = 0; i < count; i++)
                    ItemIO.Receive(reader, true);

                ItemIO.Receive(reader, true);
                return;
            }
            if (!TileEntity.ByID.ContainsKey(ent) || !(TileEntity.ByID[ent] is TEStorageHeart))
            {
                return;
            }
            TEStorageHeart heart = (TEStorageHeart)TileEntity.ByID[ent];
            List<Item> toWithdraw = new List<Item>();
            for (int k = 0; k < count; k++)
            {
                toWithdraw.Add(ItemIO.Receive(reader, true));
            }
            Item result = ItemIO.Receive(reader, true);
            List<Item> items = CraftingGUI.DoCraft(heart, toWithdraw, result);
            if (items.Count > 0)
            {
                ModPacket packet = MagicStorage.Instance.GetPacket();
                packet.Write((byte)MessageType.CraftResult);
                packet.Write(items.Count);
                foreach (Item item in items)
                {
                    ItemIO.Send(item, packet, true);
                }
                packet.Send(sender);
            }
            SendRefreshNetworkItems(ent);
        }

        public static void ReceiveCraftResult(BinaryReader reader)
        {
            Player player = Main.player[Main.myPlayer];
            int count = reader.ReadInt32();
            for (int k = 0; k < count; k++)
            {
                Item item = ItemIO.Receive(reader, true);
                player.QuickSpawnClonedItem(item, item.stack);
            }
        }

        public static void ReceiveNetWorkaround(BinaryReader reader)
        {
            EditsLoader.MessageTileEntitySyncing = false;

            int entityCount = reader.ReadUInt16();
            for (int i = 0; i < entityCount; i++)
            {
                /*
                long entStart = reader.BaseStream.Position;

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    byte type = reader.ReadByte();
                    reader.BaseStream.Seek(-1, SeekOrigin.Current);
                    MagicStorage.Instance.Logger.Debug($"Reading entity data of type {type}");
                }
                */

                TileEntity.Read(reader);

                /*
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    MagicStorage.Instance.Logger.Debug($"Bytes read (#{i + 1}): {reader.BaseStream.Position - entStart} (total: {reader.BaseStream.Position})");
                */
            }

            /*
            long end = reader.BaseStream.Position;

            if (Main.netMode == NetmodeID.MultiplayerClient && entityCount > 0)
                MagicStorage.Instance.Logger.Debug($"Received netmessage workaround - {entityCount} entities read, {end} bytes read");
            */
        }
    }

    enum MessageType : byte
    {
        SearchAndRefreshNetwork,
        TryStorageOperation,
        StorageOperationResult,
        RefreshNetworkItems,
        ClientSendTEUpdate,
        TryStationOperation,
        StationOperationResult,
        ResetCompactStage,
        CraftRequest,
        CraftResult,
        NetWorkaround
    }
}