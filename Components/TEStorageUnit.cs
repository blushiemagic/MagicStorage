using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using MagicStorage.Edits;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
    public class TEStorageUnit : TEAbstractStorageUnit
    {
        private IList<Item> items = new List<Item>();
        private readonly Queue<UnitOperation> netQueue = new Queue<UnitOperation>();
        private bool receiving = false;

        //metadata
        private HashSet<ItemData> hasSpaceInStack = new HashSet<ItemData>();
        private HashSet<ItemData> hasItem = new HashSet<ItemData>();

        public int Capacity
        {
            get
            {
                int style = Main.tile[Position.X, Position.Y].frameY / 36;
                if (style == 8)
                {
                    return 4;
                }
                if (style > 1)
                {
                    style--;
                }
                int capacity = style + 1;
                if (capacity > 4)
                {
                    capacity++;
                }
                if (capacity > 6)
                {
                    capacity++;
                }
                if (capacity > 8)
                {
                    capacity += 7;
                }
                return 40 * capacity;
            }
        }

        public override bool IsFull
        {
            get
            {
                return items.Count >= Capacity;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return items.Count == 0;
            }
        }

        public int NumItems
        {
            get
            {
                return items.Count;
            }
        }

        public override bool ValidTile(Tile tile)
        {
            return tile.type == ModContent.TileType<StorageUnit>() && tile.frameX % 36 == 0 && tile.frameY % 36 == 0;
        }

        public override bool HasSpaceInStackFor(Item check, bool locked = false)
        {
            if (Main.netMode == NetmodeID.Server && !locked)
            {
                GetHeart().EnterReadLock();
            }
            try
            {
                ItemData data = new ItemData(check);
                return hasSpaceInStack.Contains(data);
            }
            finally
            {
                if (Main.netMode == NetmodeID.Server && !locked)
                {
                    GetHeart().ExitReadLock();
                }
            }
        }

        public bool HasSpaceFor(Item check, bool locked = false)
        {
            return !IsFull || HasSpaceInStackFor(check, locked);
        }

        public override bool HasItem(Item check, bool locked = false)
        {
            if (Main.netMode == NetmodeID.Server && !locked)
            {
                GetHeart().EnterReadLock();
            }
            try
            {
                ItemData data = new ItemData(check);
                return hasItem.Contains(data);
            }
            finally
            {
                if (Main.netMode == NetmodeID.Server && !locked)
                {
                    GetHeart().ExitReadLock();
                }
            }
        }

        public override IEnumerable<Item> GetItems()
        {
            return items;
        }

        public override void DepositItem(Item toDeposit, bool locked = false)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
            {
                return;
            }
            if (Main.netMode == NetmodeID.Server && !locked)
            {
                GetHeart().EnterWriteLock();
            }
            try
            {
                Item original = toDeposit.Clone();
                bool finished = false;
                bool hasChange = false;
                foreach (Item item in items)
                {
                    if (ItemData.Matches(toDeposit, item) && item.stack < item.maxStack)
                    {
                        int total = item.stack + toDeposit.stack;
                        int newStack = total;
                        if (newStack > item.maxStack)
                        {
                            newStack = item.maxStack;
                        }
                        item.stack = newStack;
                        hasChange = true;
                        toDeposit.stack = total - newStack;
                        if (toDeposit.stack <= 0)
                        {
                            toDeposit.SetDefaults(0, true);
                            finished = true;
                            break;
                        }
                    }
                }
                if (!finished && !IsFull)
                {
                    Item item = toDeposit.Clone();
                    item.newAndShiny = false;
                    item.favorited = false;
                    items.Add(item);
                    toDeposit.SetDefaults(0, true);
                    hasChange = true;
                }
                if (hasChange && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (Main.netMode == NetmodeID.Server)
                    {
                        netQueue.Enqueue(UnitOperation.Deposit.Create(original));
                    }
                    PostChangeContents();
                }
            }
            finally
            {
                if (Main.netMode == NetmodeID.Server && !locked)
                {
                    GetHeart().ExitWriteLock();
                }
            }
        }

        public override Item TryWithdraw(Item lookFor, bool locked = false)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
            {
                return new Item();
            }
            if (Main.netMode == NetmodeID.Server && !locked)
            {
                GetHeart().EnterWriteLock();
            }
            try
            {
                Item original = lookFor.Clone();
                Item result = lookFor.Clone();
                result.stack = 0;
                for (int k = 0; k < items.Count; k++)
                {
                    Item item = items[k];
                    if (ItemData.Matches(lookFor, item))
                    {
                        int withdraw = Math.Min(lookFor.stack, item.stack);
                        item.stack -= withdraw;
                        if (item.stack <= 0)
                        {
                            items.RemoveAt(k);
                            k--;
                        }
                        result.stack += withdraw;
                        lookFor.stack -= withdraw;
                        if (lookFor.stack <= 0)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                if (Main.netMode == NetmodeID.Server)
                                {
                                    netQueue.Enqueue(UnitOperation.Withdraw.Create(original));
                                }
                                PostChangeContents();
                            }
                            return result;
                        }
                    }
                }
                if (result.stack == 0)
                {
                    return new Item();
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (Main.netMode == NetmodeID.Server)
                    {
                        netQueue.Enqueue(UnitOperation.Withdraw.Create(original));
                    }
                    PostChangeContents();
                }
                return result;
            }
            finally
            {
                if (Main.netMode == NetmodeID.Server && !locked)
                {
                    GetHeart().ExitWriteLock();
                }
            }
        }

        public bool UpdateTileFrame(bool locked = false)
        {
            Tile topLeft = Main.tile[Position.X, Position.Y];
            int oldFrame = topLeft.frameX;
            int style;
            if (Main.netMode == NetmodeID.Server && !locked)
            {
                GetHeart().EnterReadLock();
            }
            if (IsEmpty)
            {
                style = 0;    
            }
            else if (IsFull)
            {
                style = 2;
            }
            else
            {
                style = 1;
            }
            if (Main.netMode == NetmodeID.Server && !locked)
            {
                GetHeart().ExitReadLock();
            }
            if (Inactive)
            {
                style += 3;
            }
            style *= 36;
            topLeft.frameX = (short)style;
            Main.tile[Position.X, Position.Y + 1].frameX = (short)style;
            Main.tile[Position.X + 1, Position.Y].frameX = (short)(style + 18);
            Main.tile[Position.X + 1, Position.Y + 1].frameX = (short)(style + 18);
            return oldFrame != style;
        }

        public void UpdateTileFrameWithNetSend(bool locked = false)
        {
            if (UpdateTileFrame(locked))
            {
                NetMessage.SendTileSquare(-1, Position.X, Position.Y, 2, 2);
            }
        }

        //precondition: lock is already taken
        internal static void SwapItems(TEStorageUnit unit1, TEStorageUnit unit2)
        {
            IList<Item> items = unit1.items;
            unit1.items = unit2.items;
            unit2.items = items;
            HashSet<ItemData> dict = unit1.hasSpaceInStack;
            unit1.hasSpaceInStack = unit2.hasSpaceInStack;
            unit2.hasSpaceInStack = dict;
            dict = unit1.hasItem;
            unit1.hasItem = unit2.hasItem;
            unit2.hasItem = dict;
            if (Main.netMode == NetmodeID.Server)
            {
                unit1.netQueue.Clear();
                unit2.netQueue.Clear();
                unit1.netQueue.Enqueue(UnitOperation.FullSync.Create());
                unit2.netQueue.Enqueue(UnitOperation.FullSync.Create());
            }
            unit1.PostChangeContents();
            unit2.PostChangeContents();
        }

        //precondition: lock is already taken
        internal Item WithdrawStack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
            {
                return new Item();
            }
            Item item = items[items.Count - 1];
            items.RemoveAt(items.Count - 1);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    netQueue.Enqueue(UnitOperation.WithdrawStack.Create());
                }
                PostChangeContents();
            }
            return item;
        }

        public override TagCompound Save()
        {
            TagCompound tag = base.Save();
            List<TagCompound> tagItems = new List<TagCompound>();
            foreach (Item item in items)
            {
                tagItems.Add(ItemIO.Save(item));
            }
            tag.Set("Items", tagItems);
            return tag;
        }

        public override void Load(TagCompound tag)
        {
            base.Load(tag);
            ClearItemsData();
            foreach (TagCompound tagItem in tag.GetList<TagCompound>("Items"))
            {
                Item item = ItemIO.Load(tagItem);
                items.Add(item);
                ItemData data = new ItemData(item);
                if (item.stack < item.maxStack)
                {
                    hasSpaceInStack.Add(data);
                }
                hasItem.Add(data);
            }
            if (Main.netMode == NetmodeID.Server)
            {
                netQueue.Enqueue(UnitOperation.FullSync.Create());
            }
        }

        public override void NetSend(BinaryWriter trueWriter)
        {
            //If the workaround is active, then the entity isn't being sent via the NetWorkaround packet or is being saved to a world file
            if (EditsLoader.MessageTileEntitySyncing)
            {
                trueWriter.Write((byte)0);
                base.NetSend(trueWriter);
                return;
            }

            trueWriter.Write((byte)1);

            /* Recreate a BinaryWriter writer */
            MemoryStream buffer = new MemoryStream(65536);
            DeflateStream compressor = new DeflateStream(buffer, CompressionMode.Compress, true);
            BufferedStream writerBuffer = new BufferedStream(compressor, 65536);
            BinaryWriter writer = new BinaryWriter(writerBuffer);

            /* Original code */
            base.NetSend(writer);
            if (netQueue.Count > Capacity / 2)
            {
                netQueue.Clear();
                netQueue.Enqueue(UnitOperation.FullSync.Create());
            }
            writer.Write((ushort)netQueue.Count);
            while (netQueue.Count > 0)
            {
                netQueue.Dequeue().Send(writer, this);
            }
            
            /* Forces data to be flushed into the compressed buffer */
            writerBuffer.Flush(); compressor.Close();

            /* Sends the buffer through the network */
            trueWriter.Write((ushort)buffer.Length);
            trueWriter.Write(buffer.ToArray());

            /* Compression stats and debugging code (server side) */
            /*
            if (false)
            {
                MemoryStream decompressedBuffer = new MemoryStream(65536);
                DeflateStream decompressor = new DeflateStream(buffer, CompressionMode.Decompress, true);
                decompressor.CopyTo(decompressedBuffer);
                decompressor.Close(); 

                Console.WriteLine("Magic Storage Data Compression Stats: " + decompressedBuffer.Length + " => " + buffer.Length);
                decompressor.Dispose(); decompressedBuffer.Dispose();
            }
            */

            /* Dispose all objects */
            writer.Dispose(); writerBuffer.Dispose(); compressor.Dispose(); buffer.Dispose();
        }

        public override void NetReceive(BinaryReader trueReader)
        {
            //If the workaround is active, then the entity isn't being sent via the NetWorkaround packet
            byte workaround = trueReader.ReadByte();

            if (EditsLoader.MessageTileEntitySyncing || workaround != 1)
            {
                base.NetReceive(trueReader);
                return;
            }

            /* Reads the buffer off the network */
            MemoryStream buffer = new MemoryStream(65536);
            BinaryWriter bufferWriter = new BinaryWriter(buffer);

            bufferWriter.Write(trueReader.ReadBytes(trueReader.ReadUInt16()));
            buffer.Position = 0;

            /* Recreate the BinaryReader reader */
            DeflateStream decompressor = new DeflateStream(buffer, CompressionMode.Decompress, true);
            BinaryReader reader = new BinaryReader(decompressor);

            /* Original code */
            base.NetReceive(reader);
            if (TileEntity.ByPosition.ContainsKey(Position) && TileEntity.ByPosition[Position] is TEStorageUnit unit)
            {
                TEStorageUnit other = unit;
                items = other.items;
                hasSpaceInStack = other.hasSpaceInStack;
                hasItem = other.hasItem;
            }
            receiving = true;
            int count = reader.ReadUInt16();
            bool flag = false;
            for (int k = 0; k < count; k++)
            {
                if (UnitOperation.Receive(reader, this))
                {
                    flag = true;
                }
            }
            if (flag)
            {
                RepairMetadata();
            }
            receiving = false;
            
            /* Dispose all objects */
            reader.Dispose(); decompressor.Dispose(); bufferWriter.Dispose(); buffer.Dispose();
        }

        private void ClearItemsData()
        {
            items.Clear();
            hasSpaceInStack.Clear();
            hasItem.Clear();
        }

        private void RepairMetadata()
        {
            hasSpaceInStack.Clear();
            hasItem.Clear();
            foreach (Item item in items)
            {
                ItemData data = new ItemData(item);
                if (item.stack < item.maxStack)
                {
                    hasSpaceInStack.Add(data);
                }
                hasItem.Add(data);
            }
        }

        private void PostChangeContents()
        {
            RepairMetadata();
            UpdateTileFrameWithNetSend(true);
            NetHelper.SendTEUpdate(ID, Position);
        }

        abstract class UnitOperation
        {
            public static readonly UnitOperation FullSync = new FullSync();
            public static readonly UnitOperation Deposit = new DepositOperation();
            public static readonly UnitOperation Withdraw = new WithdrawOperation();
            public static readonly UnitOperation WithdrawStack = new WithdrawStackOperation();
            private static readonly List<UnitOperation> types = new List<UnitOperation>();

            static UnitOperation()
            {
                types.Add(FullSync);
                types.Add(Deposit);
                types.Add(Withdraw);
                types.Add(WithdrawStack);
                for (int k = 0; k < types.Count; k++)
                {
                    types[k].id = (byte)k;
                }
            }

            protected byte id;
            protected Item data;

            public UnitOperation Create()
            {
                return (UnitOperation)MemberwiseClone();
            }

            public UnitOperation Create(Item item)
            {
                UnitOperation clone = Create();
                clone.data = item;
                return clone;
            }

            public void Send(BinaryWriter writer, TEStorageUnit unit)
            {
                writer.Write(id);
                SendData(writer, unit);
            }

            protected abstract void SendData(BinaryWriter writer, TEStorageUnit unit);

            public static bool Receive(BinaryReader reader, TEStorageUnit unit)
            {
                byte id = reader.ReadByte();
                if (id >= 0 && id < types.Count)
                {
                    return types[id].ReceiveData(reader, unit);
                }
                return false;
            }

            protected abstract bool ReceiveData(BinaryReader reader, TEStorageUnit unit);
        }

        class FullSync : UnitOperation
        {
            protected override void SendData(BinaryWriter writer, TEStorageUnit unit)
            {
                writer.Write(unit.items.Count);
                foreach (Item item in unit.items)
                {
                    ItemIO.Send(item, writer, true, false);
                }
            }

            protected override bool ReceiveData(BinaryReader reader, TEStorageUnit unit)
            {
                unit.ClearItemsData();
                int count = reader.ReadInt32();
                for (int k = 0; k < count; k++)
                {
                    Item item = ItemIO.Receive(reader, true, false);
                    unit.items.Add(item);
                    ItemData data = new ItemData(item);
                    if (item.stack < item.maxStack)
                    {
                        unit.hasSpaceInStack.Add(data);
                    }
                    unit.hasItem.Add(data);
                }
                return false;
            }
        }

        class DepositOperation : UnitOperation
        {
            protected override void SendData(BinaryWriter writer, TEStorageUnit unit)
            {
                ItemIO.Send(data, writer, true, false);
            }

            protected override bool ReceiveData(BinaryReader reader, TEStorageUnit unit)
            {
                unit.DepositItem(ItemIO.Receive(reader, true, false));
                return true;
            }
        }

        class WithdrawOperation : UnitOperation
        {
            protected override void SendData(BinaryWriter writer, TEStorageUnit unit)
            {
                ItemIO.Send(data, writer, true, false);
            }

            protected override bool ReceiveData(BinaryReader reader, TEStorageUnit unit)
            {
                unit.TryWithdraw(ItemIO.Receive(reader, true, false));
                return true;
            }
        }

        class WithdrawStackOperation : UnitOperation
        {
            protected override void SendData(BinaryWriter writer, TEStorageUnit unit)
            {
            }

            protected override bool ReceiveData(BinaryReader reader, TEStorageUnit unit)
            {
                unit.WithdrawStack();
                return true;
            }
        }
    }
}
