using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
    public abstract class TEAbstractStorageUnit : TEStorageComponent
    {
        private bool inactive;
        private Point16 center;

        public bool Inactive
        {
            get
            {
                return inactive;
            }
            set
            {
                inactive = value;
            }
        }

        public abstract bool IsFull
        {
            get;
        }

        public bool Link(Point16 pos)
        {
            bool changed = pos != center;
            center = pos;
            return changed;
        }

        public bool Unlink()
        {
            return Link(new Point16(-1, -1));
        }

        public TEStorageHeart GetHeart()
        {
            if (center != new Point16(-1, -1) && TileEntity.ByPosition.ContainsKey(center) && TileEntity.ByPosition[center] is TEStorageCenter entity)
            {
                return entity.GetHeart();
            }
            return null;
        }

        public abstract bool HasSpaceInStackFor(Item check, bool locked = false);

        public abstract  bool HasItem(Item check, bool locked = false);

        public abstract IEnumerable<Item> GetItems();

        public abstract void DepositItem(Item toDeposit, bool locked = false);

        public abstract Item TryWithdraw(Item lookFor, bool locked = false);

        public override TagCompound Save()
        {
            TagCompound tag = new TagCompound();
            tag.Set("Inactive", inactive);
            TagCompound tagCenter = new TagCompound();
            tagCenter.Set("X", center.X);
            tagCenter.Set("Y", center.Y);
            tag.Set("Center", tagCenter);
            return tag;
        }

        public override void Load(TagCompound tag)
        {
            inactive = tag.GetBool("Inactive");
            TagCompound tagCenter = tag.GetCompound("Center");
            center = new Point16(tagCenter.GetShort("X"), tagCenter.GetShort("Y"));
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(inactive);
            writer.Write(center.X);
            writer.Write(center.Y);
        }

        public override void NetReceive(BinaryReader reader)
        {
            inactive = reader.ReadBoolean();
            center = new Point16(reader.ReadInt16(), reader.ReadInt16());
        }
    }
}