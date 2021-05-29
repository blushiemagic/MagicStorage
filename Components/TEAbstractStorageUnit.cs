using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace MagicStorageExtra.Components
{
	public abstract class TEAbstractStorageUnit : TEStorageComponent
	{
		private Point16 center;

		public bool Inactive { get; set; }

		public abstract bool IsFull { get; }

		public bool Link(Point16 pos)
		{
			bool changed = pos != center;
			center = pos;
			return changed;
		}

		public bool Unlink() => Link(new Point16(-1, -1));

		public TEStorageHeart GetHeart()
		{
			if (center != new Point16(-1, -1) && ByPosition.ContainsKey(center) && ByPosition[center] is TEStorageCenter)
				return ((TEStorageCenter) ByPosition[center]).GetHeart();
			return null;
		}

		public abstract bool HasSpaceInStackFor(Item check, bool locked = false);

		public abstract bool HasItem(Item check, bool locked = false, bool ignorePrefix = false);

		public abstract IEnumerable<Item> GetItems();

		public abstract void DepositItem(Item toDeposit, bool locked = false);

		public abstract Item TryWithdraw(Item lookFor, bool locked = false, bool keepOneIfFavorite = false);

		public override TagCompound Save()
		{
			var tag = new TagCompound();
			tag.Set("Inactive", Inactive);
			var tagCenter = new TagCompound();
			tagCenter.Set("X", center.X);
			tagCenter.Set("Y", center.Y);
			tag.Set("Center", tagCenter);
			return tag;
		}

		public override void Load(TagCompound tag)
		{
			Inactive = tag.GetBool("Inactive");
			TagCompound tagCenter = tag.GetCompound("Center");
			center = new Point16(tagCenter.GetShort("X"), tagCenter.GetShort("Y"));
		}

		public override void NetSend(BinaryWriter writer, bool lightSend)
		{
			writer.Write(Inactive);
			writer.Write(center.X);
			writer.Write(center.Y);
		}

		public override void NetReceive(BinaryReader reader, bool lightReceive)
		{
			Inactive = reader.ReadBoolean();
			center = new Point16(reader.ReadInt16(), reader.ReadInt16());
		}
	}
}