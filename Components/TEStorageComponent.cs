using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorageExtra.Components
{
	public abstract class TEStorageComponent : ModTileEntity
	{

		private static readonly IEnumerable<Point16> checkNeighbors = new[] {
			new Point16(-1, 0),
			new Point16(0, -1),
			new Point16(1, -1),
			new Point16(2, 0),
			new Point16(2, 1),
			new Point16(1, 2),
			new Point16(0, 2),
			new Point16(-1, 1)
		};

		private static readonly IEnumerable<Point16> checkNeighbors1x1 = new[] {
			new Point16(-1, 0),
			new Point16(0, -1),
			new Point16(1, 0),
			new Point16(0, 1)
		};

		public override bool ValidTile(int i, int j) {
			Tile tile = Main.tile[i, j];
			return tile.active() && ValidTile(tile);
		}

		public abstract bool ValidTile(Tile tile);

		public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				NetHelper.SendComponentPlace(i - 1, j - 1, Type);
				return -1;
			}
			int id = Place(i - 1, j - 1);
			((TEStorageComponent)ByID[id]).OnPlace();
			return id;
		}

		public static int Hook_AfterPlacement_NoEntity(int i, int j, int type, int style, int direction) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				NetMessage.SendTileRange(Main.myPlayer, i - 1, j - 1, 2, 2);
				NetHelper.SendSearchAndRefresh(i - 1, j - 1);
				return 0;
			}
			SearchAndRefreshNetwork(new Point16(i - 1, j - 1));
			return 0;
		}

		public virtual void OnPlace() {
			SearchAndRefreshNetwork(Position);
		}

		public override void OnKill() {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				NetHelper.SendSearchAndRefresh(Position.X, Position.Y);
			else
				SearchAndRefreshNetwork(Position);
		}

		public IEnumerable<Point16> AdjacentComponents() {
			return AdjacentComponents(Position);
		}

		public static IEnumerable<Point16> AdjacentComponents(Point16 point) {
			var points = new List<Point16>();
			bool isConnector = Main.tile[point.X, point.Y].type == MagicStorageExtra.Instance.TileType("StorageConnector");
			foreach (Point16 add in isConnector ? checkNeighbors1x1 : checkNeighbors) {
				int checkX = point.X + add.X;
				int checkY = point.Y + add.Y;
				Tile tile = Main.tile[checkX, checkY];
				if (!tile.active())
					continue;
				if (TileLoader.GetTile(tile.type) is StorageComponent) {
					if (tile.frameX % 36 == 18)
						checkX--;
					if (tile.frameY % 36 == 18)
						checkY--;
					var check = new Point16(checkX, checkY);
					if (!points.Contains(check))
						points.Add(check);
				}
				else if (tile.type == MagicStorageExtra.Instance.TileType("StorageConnector")) {
					var check = new Point16(checkX, checkY);
					if (!points.Contains(check))
						points.Add(check);
				}
			}
			return points;
		}

		public static Point16 FindStorageCenter(Point16 startSearch) {
			var explored = new HashSet<Point16>();
			explored.Add(startSearch);
			var toExplore = new Queue<Point16>();
			foreach (Point16 point in AdjacentComponents(startSearch))
				toExplore.Enqueue(point);

			while (toExplore.Count > 0) {
				Point16 explore = toExplore.Dequeue();
				if (!explored.Contains(explore) && explore != StorageComponent.killTile) {
					explored.Add(explore);
					if (TEStorageCenter.IsStorageCenter(explore))
						return explore;
					foreach (Point16 point in AdjacentComponents(explore))
						toExplore.Enqueue(point);
				}
			}
			return new Point16(-1, -1);
		}

		public override void OnNetPlace() {
			OnPlace();
			NetHelper.SendTEUpdate(ID, Position);
		}

		public static void SearchAndRefreshNetwork(Point16 position) {
			Point16 center = FindStorageCenter(position);
			if (center.X >= 0 && center.Y >= 0) {
				var centerEnt = (TEStorageCenter)ByPosition[center];
				centerEnt.ResetAndSearch();
			}
		}
	}
}
