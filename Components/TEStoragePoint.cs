using System.Collections.Generic;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
	public abstract class TEStoragePoint : TEStorageComponent
	{
		private Point16 center;

		public void ResetAndSearch() {
			Point16 oldCenter = center;
			center = new Point16(-1, -1);

			var explored = new HashSet<Point16>();
			explored.Add(Position);
			var toExplore = new Queue<Point16>();
			foreach (Point16 point in AdjacentComponents())
				toExplore.Enqueue(point);

			while (toExplore.Count > 0) {
				Point16 explore = toExplore.Dequeue();
				if (!explored.Contains(explore) && explore != StorageComponent.killTile) {
					explored.Add(explore);
					if (TEStorageCenter.IsStorageCenter(explore)) {
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

		public override void OnPlace() {
			ResetAndSearch();
		}

		public bool Link(Point16 pos) {
			bool changed = pos != center;
			center = pos;
			return changed;
		}

		public bool Unlink() {
			return Link(new Point16(-1, -1));
		}

		public TEStorageHeart GetHeart() {
			if (center != new Point16(-1, -1))
				return ((TEStorageCenter)ByPosition[center]).GetHeart();
			return null;
		}

		public static bool IsStoragePoint(Point16 point) {
			return ByPosition.ContainsKey(point) && ByPosition[point] is TEStoragePoint;
		}

		public override TagCompound Save() {
			TagCompound tag = new TagCompound();
			TagCompound tagCenter = new TagCompound();
			tagCenter.Set("X", center.X);
			tagCenter.Set("Y", center.Y);
			tag.Set("Center", tagCenter);
			return tag;
		}

		public override void Load(TagCompound tag) {
			TagCompound tagCenter = tag.GetCompound("Center");
			center = new Point16(tagCenter.GetShort("X"), tagCenter.GetShort("Y"));
		}

		public override void NetSend(BinaryWriter writer, bool lightSend) {
			writer.Write(center.X);
			writer.Write(center.Y);
		}

		public override void NetReceive(BinaryReader reader, bool lightReceive) {
			center = new Point16(reader.ReadInt16(), reader.ReadInt16());
		}
	}
}
