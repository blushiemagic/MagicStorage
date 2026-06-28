using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
	public abstract class TEStoragePoint : TEStorageComponent
	{
		internal Point16 center = Point16.NegativeOne;

		public override Point16 StorageCenter {
			get => center;
			set => center = value;
		}

		public static bool IsStoragePoint(Point16 point) => ByPosition.TryGetValue(point, out TileEntity te) && te is TEStoragePoint;

		public override void SaveData(TagCompound tag)
		{
			base.SaveData(tag);

			TagCompound tagCenter = new();
			tagCenter.Set("X", center.X);
			tagCenter.Set("Y", center.Y);
			tag.Set("Center", tagCenter);
		}

		public override void LoadData(TagCompound tag)
		{
			base.LoadData(tag);

			TagCompound tagCenter = tag.GetCompound("Center");
			center = new Point16(tagCenter.GetShort("X"), tagCenter.GetShort("Y"));
		}

		public override void NetSend(BinaryWriter writer)
		{
			base.NetSend(writer);

			writer.Write(center.X);
			writer.Write(center.Y);
		}

		public override void NetReceive(BinaryReader reader)
		{
			base.NetReceive(reader);

			center = new Point16(reader.ReadInt16(), reader.ReadInt16());
		}
	}
}
