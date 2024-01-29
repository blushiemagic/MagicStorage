using MagicStorage.Common.Systems;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
	public abstract class TEStorageComponent : ModTileEntity
	{
		private static readonly Point16[] checkNeighbors = new[]
		{
			new Point16(-1, 0),
			new Point16(0, -1),
			new Point16(1, -1),
			new Point16(2, 0),
			new Point16(2, 1),
			new Point16(1, 2),
			new Point16(0, 2),
			new Point16(-1, 1)
		};

		private static readonly Point16[] checkNeighbors1x1 = new[]
		{
			new Point16(-1, 0),
			new Point16(0, -1),
			new Point16(1, 0),
			new Point16(0, 1)
		};

		protected Point16 _storageCenter;

		public virtual Point16 StorageCenter
		{
			get => _storageCenter;
			set => _storageCenter = value;
		}

		public virtual TEStorageHeart GetHeart() {
			Point16 center = StorageCenter;

			if (center.X < 0 || center.Y < 0)
				return null;

			if (ByPosition.TryGetValue(center, out TileEntity te) && te is TEStorageHeart heart)
				return heart;

			return null;
		}

		public override bool IsTileValidForEntity(int x, int y)
		{
			Tile tile = Main.tile[x, y];
			return tile.HasTile && ValidTile(tile);
		}

		public bool Link(Point16 center)
		{
			Point16 oldCenter = StorageCenter;
			StorageCenter = center;
			return oldCenter != center;
		}

		public bool Unlink() => Link(Point16.NegativeOne);

		public abstract bool ValidTile(in Tile tile);

		public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				NetMessage.SendTileSquare(Main.myPlayer, i - 1, j - 2, 2, 4);
				NetMessage.SendTileSquare(Main.myPlayer, i - 2, j - 1, 4, 2);
				NetHelper.SendComponentPlace(i - 1, j - 1, Type);
				NetHelper.SendComponentPlacement(new Point16(i - 1, j - 1));
				return -1;
			}

			int id = Place(i - 1, j - 1);
			((TEStorageComponent) ByID[id]).OnPlace();
			UpdateNearbyConnectors(i - 1, j - 1);
			return id;
		}

		public static int Hook_AfterPlacement_NoEntity(int i, int j, int type, int style, int direction, int alternate)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				NetMessage.SendTileSquare(Main.myPlayer, i - 1, j - 2, 2, 4);
				NetMessage.SendTileSquare(Main.myPlayer, i - 2, j - 1, 4, 2);
				NetHelper.SendSearchAndRefresh(i - 1, j - 1);
				NetHelper.SendComponentPlacement(new Point16(i - 1, j - 1));
				return 0;
			}

			SearchAndRefreshNetwork(new Point16(i - 1, j - 1));
			UpdateNearbyConnectors(i - 1, j - 1);
			return 0;
		}

		private static void UpdateNearbyConnectors(int x, int y)
		{
			// Forcibly update the frames of adjacent Connectors
			for (int i = -1; i < 3; i++) {
				for (int j = -1; j < 3; j++) {
					// Ignore corners
					if ((i == -1 || i == 2) && (j == -1 || j == 2))
						continue;

					int tx = x + i, ty = y + j;
					if (!WorldGen.InWorld(tx, ty))
						continue;

					// Only update connector tiles
					if (TileLoader.GetTile(Main.tile[tx, ty].TileType) is not StorageConnector)
						continue;

					WorldGen.TileFrame(tx, ty, resetFrame: true, noBreak: true);
				}
			}
		}

		public override void Update() {
			GetHeart()?.ComponentManager.LinkIfNotExists(this);
		}

		public virtual void OnPlace()
		{
			SearchAndRefreshNetwork(Position);
		}

		public override void OnKill()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				NetHelper.SendSearchAndRefresh(Position.X, Position.Y);
				NetHelper.SendComponentDestruction(Position);
			} else
				SearchAndRefreshNetwork(Position);
		}

		public IEnumerable<Point16> AdjacentComponents() => AdjacentComponents(Position);

		public static IEnumerable<Point16> AdjacentComponents(Point16 point)
		{
			List<Point16> points = new();
			bool isConnector = Main.tile[point.X, point.Y].TileType == ModContent.TileType<StorageConnector>();
			foreach (Point16 add in isConnector ? checkNeighbors1x1 : checkNeighbors)
			{
				int checkX = point.X + add.X;
				int checkY = point.Y + add.Y;
				Tile tile = Main.tile[checkX, checkY];
				if (!tile.HasTile)
					continue;

				if (TileLoader.GetTile(tile.TileType) is StorageComponent)
				{
					if (tile.TileFrameX % 36 == 18)
						checkX--;
					if (tile.TileFrameY % 36 == 18)
						checkY--;
					Point16 check = new(checkX, checkY);
					if (!points.Contains(check))
						points.Add(check);
				}
				else if (tile.TileType == ModContent.TileType<StorageConnector>())
				{
					Point16 check = new(checkX, checkY);
					if (!points.Contains(check))
						points.Add(check);
				}
			}

			return points;
		}

		public static Point16 FindStorageCenter(Point16 startSearch)
		{
			HashSet<Point16> explored = new()
			{
				startSearch
			};
			Queue<Point16> toExplore = new();
			foreach (Point16 point in AdjacentComponents(startSearch))
				toExplore.Enqueue(point);

			while (toExplore.Count > 0)
			{
				Point16 explore = toExplore.Dequeue();
				if (!explored.Contains(explore) && explore != StorageComponent.killTile)
				{
					explored.Add(explore);
					if (TEStorageCenter.IsStorageCenter(explore))
						return explore;
					foreach (Point16 point in AdjacentComponents(explore))
						toExplore.Enqueue(point);
				}
			}

			return Point16.NegativeOne;
		}

		public override void OnNetPlace()
		{
			OnPlace();
			NetHelper.SendTEUpdate(ID, Position);
		}

		public static void SearchAndRefreshNetwork(Point16 position)
		{
			Point16 center = FindStorageCenter(position);
			if (center != Point16.NegativeOne && ByPosition.TryGetValue(center, out var te) && te is TEStorageCenter centerEnt) {
				centerEnt.ResetAndSearch();

				if (Main.netMode != NetmodeID.Server && StoragePlayer.LocalPlayer.ViewingStorage().X >= 0 && centerEnt.GetHeart()?.Position == StoragePlayer.LocalPlayer.GetStorageHeart().Position)
					MagicUI.SetRefresh(forceFullRefresh: false);
			}
		}

		public override void NetSend(BinaryWriter writer) {
			writer.Write(_storageCenter);
		}

		public override void NetReceive(BinaryReader reader) {
			_storageCenter = reader.ReadPoint16();
		}

		public override void SaveData(TagCompound tag) {
			tag["center"] = _storageCenter;
		}

		public override void LoadData(TagCompound tag) {
			if (tag.TryGet("center", out Point16 center))
				_storageCenter = center;
			else
				_storageCenter = Point16.NegativeOne;
		}
	}
}
