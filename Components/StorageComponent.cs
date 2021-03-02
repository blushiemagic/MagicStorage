using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace MagicStorage.Components
{
	public class StorageComponent : ModTile
	{
		public static Point16 killTile = new Point16(-1, -1);

		// Use StorageComponent_Highlight as the default highlight mask for subclasses
		public override string HighlightTexture => typeof(StorageComponent).FullName.Replace('.', '/') + "_Highlight";

		public override void SetDefaults() {
			Main.tileSolidTop[Type] = true;
			Main.tileFrameImportant[Type] = true;
			TileObjectData.newTile.Width = 2;
			TileObjectData.newTile.Height = 2;
			TileObjectData.newTile.Origin = new Point16(1, 1);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
			TileObjectData.newTile.CoordinateWidth = 16;
			TileObjectData.newTile.CoordinatePadding = 2;
			TileObjectData.newTile.HookCheck = new PlacementHook(CanPlace, -1, 0, true);
			TileObjectData.newTile.UsesCustomCanPlace = true;
			ModifyObjectData();
			ModTileEntity tileEntity = GetTileEntity();
			if (tileEntity != null)
				TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(tileEntity.Hook_AfterPlacement, -1, 0, false);
			else
				TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(TEStorageComponent.Hook_AfterPlacement_NoEntity, -1, 0, false);
			TileObjectData.addTile(Type);
			ModTranslation text = CreateMapEntryName();
			text.SetDefault("Magic Storage");
			AddMapEntry(new Color(153, 107, 61), text);
			dustType = 7;
			disableSmartCursor = true;
			TileID.Sets.HasOutlines[Type] = HasSmartInteract();
		}

		public virtual void ModifyObjectData() {
		}

		public virtual ModTileEntity GetTileEntity() {
			return null;
		}

		public virtual int ItemType(int frameX, int frameY) {
			return mod.ItemType("StorageComponent");
		}

		public static bool IsStorageComponent(Point16 point) {
			Tile tile = Main.tile[point.X, point.Y];
			return tile.active() && TileLoader.GetTile(tile.type) is StorageComponent;
		}

		public int CanPlace(int i, int j, int type, int style, int direction) {
			int count = 0;
			if (GetTileEntity() != null && GetTileEntity() is TEStorageCenter)
				count++;

			var startSearch = new Point16(i - 1, j - 1);
			var explored = new HashSet<Point16>();
			explored.Add(startSearch);
			var toExplore = new Queue<Point16>();
			foreach (Point16 point in TEStorageComponent.AdjacentComponents(startSearch))
				toExplore.Enqueue(point);

			while (toExplore.Count > 0) {
				Point16 explore = toExplore.Dequeue();
				if (!explored.Contains(explore) && explore != killTile) {
					explored.Add(explore);
					if (TEStorageCenter.IsStorageCenter(explore)) {
						count++;
						if (count >= 2)
							return -1;
					}
					foreach (Point16 point in TEStorageComponent.AdjacentComponents(explore))
						toExplore.Enqueue(point);
				}
			}
			return count;
		}

		public override void KillMultiTile(int i, int j, int frameX, int frameY) {
			Item.NewItem(i * 16, j * 16, 32, 32, ItemType(frameX, frameY));
			killTile = new Point16(i, j);
			ModTileEntity tileEntity = GetTileEntity();
			if (tileEntity != null) {
				tileEntity.Kill(i, j);
			}
			else {
				if (Main.netMode == NetmodeID.MultiplayerClient)
					NetHelper.SendSearchAndRefresh(killTile.X, killTile.Y);
				else
					TEStorageComponent.SearchAndRefreshNetwork(killTile);
			}
			killTile = new Point16(-1, -1);
		}
	}
}
