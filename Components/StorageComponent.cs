using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace MagicStorage.Components
{
    public class StorageComponent : ModTile
    {
        public static Point16 killTile = new Point16(-1, -1);

        // Use StorageComponent_Highlight as the default highlight mask for subclasses
        public override string HighlightTexture { get { return typeof(StorageComponent).FullName.Replace('.', '/') + "_Highlight"; } }

        public override void SetStaticDefaults()
        {
            Main.tileSolidTop[Type] = true;
            Main.tileFrameImportant[Type] = true;
            TileObjectData.newTile.Width = 2;
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.Origin = new Point16(1, 1);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(CanPlace, -1, 0, true);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            ModifyObjectData();
            ModTileEntity tileEntity = GetTileEntity();
            if (tileEntity != null)
            {
                TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(tileEntity.Hook_AfterPlacement, -1, 0, false);
            }
            else
            {
                TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(TEStorageComponent.Hook_AfterPlacement_NoEntity, -1, 0, false);
            }
            TileObjectData.addTile(Type);
            ModTranslation text = CreateMapEntryName();
            text.SetDefault("Magic Storage");
            AddMapEntry(new Color(153, 107, 61), text);
            DustType = 7;
            TileID.Sets.HasOutlines[Type] = HasSmartInteract();
        }

        public virtual void ModifyObjectData()
        {
        }

        public virtual ModTileEntity GetTileEntity()
        {
            return null;
        }

        public virtual int ItemType(int frameX, int frameY)
        {
            return ModContent.ItemType<Items.StorageComponent>();
        }

        public static bool IsStorageComponent(Point16 point)
        {
            Tile tile = Main.tile[point.X, point.Y];
            return tile.IsActive && TileLoader.GetTile(tile.type) is StorageComponent;
        }

        public int CanPlace(int i, int j, int type, int style, int direction, int alternative)
        {
            int count = 0;
            if (GetTileEntity() != null && GetTileEntity() is TEStorageCenter)
            {
                count++;
            }

            Point16 startSearch = new Point16(i - 1, j - 1);
			HashSet<Point16> explored = new HashSet<Point16>(){
				startSearch
			};
			Queue<Point16> toExplore = new Queue<Point16>();
            foreach (Point16 point in TEStorageComponent.AdjacentComponents(startSearch))
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
                        count++;
                        if (count >= 2)
                        {
                            return -1;
                        }
                    }
                    foreach (Point16 point in TEStorageComponent.AdjacentComponents(explore))
                    {
                        toExplore.Enqueue(point);
                    }
                }
            }
            return count;
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            Item.NewItem(i * 16, j * 16, 32, 32, ItemType(frameX, frameY));
            killTile = new Point16(i, j);
            ModTileEntity tileEntity = GetTileEntity();
            if (tileEntity != null)
            {
                tileEntity.Kill(i, j);
            }
            else
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetHelper.SendSearchAndRefresh(killTile.X, killTile.Y);
                }
                else
                {
                    TEStorageComponent.SearchAndRefreshNetwork(killTile);
                }
            }
            killTile = new Point16(-1, -1);
        }
    }
}