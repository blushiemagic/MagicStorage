using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
	public abstract class TEStorageCenter : TEStorageComponent
	{
		public List<Point16> storageUnits = new();

		private int eatingWaitDuration = -1;

		public void AskToEatItem(int duration) {
			duration += 10;

			if (eatingWaitDuration < duration)
				eatingWaitDuration = 10;
		}

		internal void UpdateItemEatingTime() {
			if (eatingWaitDuration >= 0) {
				if (eatingWaitDuration == 0)
					SoundEngine.PlaySound(SoundID.Grab, Position.ToWorldCoordinates(16, 16));

				eatingWaitDuration--;
			}
		}

		protected virtual void CheckMapSections() {
			//Force a map section send for each unique map section that has one of this storage center's storage units
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				foreach (Point16 unit in storageUnits.DistinctBy(p => new Point16(Netplay.GetSectionX(p.X), Netplay.GetSectionY(p.Y))))
					NetHelper.ClientRequestSection(unit);
			}
		}

		public void ResetAndSearch()
		{
			NetHelper.Report(true, "TEStorageCenter.ResetAndSearch invoked.  Current unit count: " + storageUnits.Count);

			CheckMapSections();

			List<Point16> oldStorageUnits = new(storageUnits);
			storageUnits.Clear();
			HashSet<Point16> hashStorageUnits = new();
			HashSet<Point16> explored = new()
			{
				Position
			};
			Queue<Point16> toExplore = new();
			foreach (Point16 point in AdjacentComponents())
				toExplore.Enqueue(point);

			NetHelper.StartUpdateQueue();

			while (toExplore.Count > 0)
			{
				Point16 explore = toExplore.Dequeue();
				if (!explored.Contains(explore) && explore != StorageComponent.killTile)
				{
					explored.Add(explore);
					if (ByPosition.TryGetValue(explore, out TileEntity te) && te is TEAbstractStorageUnit storageUnit)
					{
						storageUnit.Link(Position);
						NetHelper.SendTEUpdate(storageUnit.ID, storageUnit.Position);

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
					if (ByPosition.TryGetValue(oldStorageUnit, out TileEntity te) && te is TEAbstractStorageUnit storageUnit)
					{
						storageUnit.Unlink();
						NetHelper.SendTEUpdate(storageUnit.ID, storageUnit.Position);
					}
				}

			NetHelper.Report(true, "TEStorageCenter.ResetAndSearch finished.  New unit count: " + storageUnits.Count);

			TEStorageHeart heart = GetHeart();
			heart?.ResetCompactStage();
			NetHelper.SendTEUpdate(ID, Position);

			if (heart is not null)
				NetHelper.SendTEUpdate(heart.ID, heart.Position);

			NetHelper.ProcessUpdateQueue();
		}

		public override void OnPlace()
		{
			ResetAndSearch();
		}

		public override void OnKill()
		{
			foreach (Point16 storageUnit in storageUnits)
			{
				if (!ByPosition.TryGetValue(storageUnit, out var te) || te is not TEAbstractStorageUnit unit)
					continue;
				
				unit.Unlink();
				NetHelper.SendTEUpdate(unit.ID, unit.Position);
			}
		}

		public abstract TEStorageHeart GetHeart();

		public static bool IsStorageCenter(Point16 point) => ByPosition.TryGetValue(point, out TileEntity te) && te is TEStorageCenter;

		public static bool HeartsMatch(Point16 center, Point16 heart) {
			if (!TileEntity.ByPosition.TryGetValue(center, out TileEntity entity) || entity is not TEStorageCenter centerEntity)
				return false;

			return centerEntity.GetHeart()?.Position == heart;
		}

		public override void SaveData(TagCompound tag)
		{
			List<TagCompound> tagUnits = new();
			foreach (Point16 storageUnit in storageUnits)
			{
				TagCompound tagUnit = new();
				tagUnit.Set("X", storageUnit.X);
				tagUnit.Set("Y", storageUnit.Y);
				tagUnits.Add(tagUnit);
			}

			tag.Set("StorageUnits", tagUnits);
		}

		public override void LoadData(TagCompound tag)
		{
			foreach (TagCompound tagUnit in tag.GetList<TagCompound>("StorageUnits"))
				storageUnits.Add(new Point16(tagUnit.GetShort("X"), tagUnit.GetShort("Y")));
		}

		public override void NetSend(BinaryWriter writer)
		{
			writer.Write((short) storageUnits.Count);
			foreach (Point16 storageUnit in storageUnits)
			{
				writer.Write(storageUnit.X);
				writer.Write(storageUnit.Y);
			}
		}

		public override void NetReceive(BinaryReader reader)
		{
			int count = reader.ReadInt16();
			for (int k = 0; k < count; k++)
				storageUnits.Add(new Point16(reader.ReadInt16(), reader.ReadInt16()));

			CheckMapSections();
		}
	}
}
