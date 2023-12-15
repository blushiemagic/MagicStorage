using System.Collections.Generic;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
	public abstract class TEStoragePoint : TEStorageComponent
	{
		internal Point16 center;

		public override Point16 StorageCenter {
			get => center;
			set => center = value;
		}

		public void ResetAndSearch()
		{
			Point16 oldCenter = center;
			center = Point16.NegativeOne;

			HashSet<Point16> explored = new()
			{
				Position
			};
			Queue<Point16> toExplore = new();
			foreach (Point16 point in AdjacentComponents())
				toExplore.Enqueue(point);

			while (toExplore.Count > 0)
			{
				Point16 explore = toExplore.Dequeue();
				if (!explored.Contains(explore) && explore != StorageComponent.killTile)
				{
					explored.Add(explore);
					if (TEStorageCenter.IsStorageCenter(explore))
					{
						center = explore;
						break;
					}

					foreach (Point16 point in AdjacentComponents(explore))
						toExplore.Enqueue(point);
				}
			}

			if (center != oldCenter)
				NetHelper.SendTEUpdate(ID, Position);
		}

		public override void OnPlace()
		{
			ResetAndSearch();
		}

		public static bool IsStoragePoint(Point16 point) => ByPosition.TryGetValue(point, out TileEntity te) && te is TEStoragePoint;

		public override void SaveData(TagCompound tag)
		{
			TagCompound tagCenter = new();
			tagCenter.Set("X", center.X);
			tagCenter.Set("Y", center.Y);
			tag.Set("Center", tagCenter);
		}

		public override void LoadData(TagCompound tag)
		{
			TagCompound tagCenter = tag.GetCompound("Center");
			center = new Point16(tagCenter.GetShort("X"), tagCenter.GetShort("Y"));
		}

		public override void NetSend(BinaryWriter writer)
		{
			writer.Write(center.X);
			writer.Write(center.Y);
		}

		public override void NetReceive(BinaryReader reader)
		{
			center = new Point16(reader.ReadInt16(), reader.ReadInt16());
		}
	}
}
