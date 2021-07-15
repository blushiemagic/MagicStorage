using System.Collections.Generic;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace MagicStorageExtra.Components
{
	public abstract class TEStorageCenter : TEStorageComponent
	{
		public List<Point16> storageUnits = new List<Point16>();

		public void ResetAndSearch()
		{
			var oldStorageUnits = new List<Point16>(storageUnits);
			storageUnits.Clear();
			var hashStorageUnits = new HashSet<Point16>();
			var explored = new HashSet<Point16> {Position};
			var toExplore = new Queue<Point16>();
			foreach (Point16 point in AdjacentComponents())
				toExplore.Enqueue(point);
			bool changed = false;

			while (toExplore.Count > 0)
			{
				Point16 explore = toExplore.Dequeue();
				if (!explored.Contains(explore) && explore != StorageComponent.killTile)
				{
					explored.Add(explore);
					if (ByPosition.ContainsKey(explore) && ByPosition[explore] is TEAbstractStorageUnit)
					{
						var storageUnit = (TEAbstractStorageUnit) ByPosition[explore];
						if (storageUnit.Link(Position))
						{
							NetHelper.SendTEUpdate(storageUnit.ID, storageUnit.Position);
							changed = true;
						}

						storageUnits.Add(explore);
						hashStorageUnits.Add(explore);
					}

					foreach (Point16 point in AdjacentComponents(explore))
						toExplore.Enqueue(point);
				}
			}

			foreach (Point16 oldStorageUnit in oldStorageUnits)
				if (!hashStorageUnits.Contains(oldStorageUnit))
				{
					if (ByPosition.ContainsKey(oldStorageUnit) && ByPosition[oldStorageUnit] is TEAbstractStorageUnit)
					{
						TileEntity storageUnit = ByPosition[oldStorageUnit];
						((TEAbstractStorageUnit) storageUnit).Unlink();
						NetHelper.SendTEUpdate(storageUnit.ID, storageUnit.Position);
					}

					changed = true;
				}

			if (changed)
			{
				TEStorageHeart heart = GetHeart();
				heart?.ResetCompactStage();
				NetHelper.SendTEUpdate(ID, Position);
			}
		}

		public override void OnPlace()
		{
			ResetAndSearch();
		}

		public override void OnKill()
		{
			foreach (Point16 storageUnit in storageUnits)
			{
				var unit = (TEAbstractStorageUnit) ByPosition[storageUnit];
				unit.Unlink();
				NetHelper.SendTEUpdate(unit.ID, unit.Position);
			}
		}

		public abstract TEStorageHeart GetHeart();

		public static bool IsStorageCenter(Point16 point) => ByPosition.ContainsKey(point) && ByPosition[point] is TEStorageCenter;

		public override TagCompound Save()
		{
			var tag = new TagCompound();
			var tagUnits = new List<TagCompound>();
			foreach (Point16 storageUnit in storageUnits)
			{
				var tagUnit = new TagCompound();
				tagUnit.Set("X", storageUnit.X);
				tagUnit.Set("Y", storageUnit.Y);
				tagUnits.Add(tagUnit);
			}

			tag.Set("StorageUnits", tagUnits);
			return tag;
		}

		public override void Load(TagCompound tag)
		{
			foreach (TagCompound tagUnit in tag.GetList<TagCompound>("StorageUnits"))
				storageUnits.Add(new Point16(tagUnit.GetShort("X"), tagUnit.GetShort("Y")));
		}

		public override void NetSend(BinaryWriter writer, bool lightSend)
		{
			writer.Write((short) storageUnits.Count);
			foreach (Point16 storageUnit in storageUnits)
			{
				writer.Write(storageUnit.X);
				writer.Write(storageUnit.Y);
			}
		}

		public override void NetReceive(BinaryReader reader, bool lightReceive)
		{
			int count = reader.ReadInt16();
			for (int k = 0; k < count; k++)
				storageUnits.Add(new Point16(reader.ReadInt16(), reader.ReadInt16()));
		}
	}
}
