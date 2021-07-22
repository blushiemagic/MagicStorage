using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Components
{
    public abstract class TEStorageComponent : ModTileEntity
    {
        public override bool ValidTile(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            return tile.IsActive && ValidTile(tile);
        }

        public abstract bool ValidTile(Tile tile);

        public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternative)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetHelper.SendComponentPlace(i - 1, j - 1, Type);
                return -1;
            }
            int id = Place(i - 1, j - 1);
            ((TEStorageComponent)TileEntity.ByID[id]).OnPlace();
            return id;
        }

#pragma warning disable IDE0060
        public static int Hook_AfterPlacement_NoEntity(int i, int j, int type, int style, int direction, int alternative)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendTileSquare(Main.myPlayer, i - 1, j - 1, 2, 2);
                NetHelper.SendSearchAndRefresh(i - 1, j - 1);
                return 0;
            }
            SearchAndRefreshNetwork(new Point16(i - 1, j - 1));
            return 0;
        }
#pragma warning restore IDE0060

        public virtual void OnPlace()
        {
            SearchAndRefreshNetwork(Position);
        }

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetHelper.SendSearchAndRefresh(Position.X, Position.Y);
            }
            else
            {
                TEStorageComponent.SearchAndRefreshNetwork(Position);
            }
        }

        private static readonly IEnumerable<Point16> checkNeighbors = new Point16[]
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

        private static readonly IEnumerable<Point16> checkNeighbors1x1 = new Point16[]
        {
            new Point16(-1, 0),
            new Point16(0, -1),
            new Point16(1, 0),
            new Point16(0, 1)
        };

        public IEnumerable<Point16> AdjacentComponents()
        {
            return AdjacentComponents(Position);
        }

        public static IEnumerable<Point16> AdjacentComponents(Point16 point)
        {
            List<Point16> points = new List<Point16>();
            bool isConnector = Main.tile[point.X, point.Y].type == ModContent.TileType<StorageConnector>();
            foreach (Point16 add in (isConnector ? checkNeighbors1x1 : checkNeighbors))
            {
                int checkX = point.X + add.X;
                int checkY = point.Y + add.Y;
                Tile tile = Main.tile[checkX, checkY];
                if (!tile.IsActive)
                {
                    continue;
                }
                if (TileLoader.GetTile(tile.type) is StorageComponent)
                {
                    if (tile.frameX % 36 == 18)
                    {
                        checkX--;
                    }
                    if (tile.frameY % 36 == 18)
                    {
                        checkY--;
                    }
                    Point16 check = new Point16(checkX, checkY);
                    if (!points.Contains(check))
                    {
                        points.Add(check);
                    }
                }
                else if (tile.type == ModContent.TileType<StorageConnector>())
                {
                    Point16 check = new Point16(checkX, checkY);
                    if (!points.Contains(check))
                    {
                        points.Add(check);
                    }
                }
            }
            return points;
        }

        public static Point16 FindStorageCenter(Point16 startSearch)
        {
			HashSet<Point16> explored = new HashSet<Point16>(){
				startSearch
			};
			Queue<Point16> toExplore = new Queue<Point16>();
            foreach (Point16 point in AdjacentComponents(startSearch))
            {
                toExplore.Enqueue(point);
            }

            while (toExplore.Count > 0)
            {
                Point16 explore = toExplore.Dequeue();
                if (!explored.Contains(explore) && explore != StorageComponent.killTile)
                {
                    explored.Add(explore);
                    if (TEStorageCenter.IsStorageCenter(explore))
                    {
                        return explore;
                    }
                    foreach (Point16 point in AdjacentComponents(explore))
                    {
                        toExplore.Enqueue(point);
                    }
                }
            }
            return new Point16(-1, -1);
        }

        public override void OnNetPlace()
        {
            OnPlace();
            NetHelper.SendTEUpdate(ID, Position);
        }

        public static void SearchAndRefreshNetwork(Point16 position)
        {
            Point16 center = FindStorageCenter(position);
            if (center.X >= 0 && center.Y >= 0)
            {
                TEStorageCenter centerEnt = (TEStorageCenter)TileEntity.ByPosition[center];
                centerEnt.ResetAndSearch();
            }
        }
    }
}