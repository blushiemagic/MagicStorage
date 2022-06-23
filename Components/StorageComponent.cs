﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace MagicStorage.Components
{
	public class StorageComponent : ModTile
	{
		public static Point16 killTile = Point16.NegativeOne;

		// Use StorageComponent_Highlight as the default highlight mask for subclasses
		public override string HighlightTexture => typeof(StorageComponent).FullName!.Replace('.', '/') + "_Highlight";

		public override void SetStaticDefaults()
		{
			Main.tileSolidTop[Type] = true;
			Main.tileFrameImportant[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
			TileObjectData.newTile.Origin = new Point16(1, 1);
			TileObjectData.newTile.LavaDeath = false;
			TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(CanPlace, -1, 0, true);
			ModifyObjectData();
			ModTileEntity tileEntity = GetTileEntity();
			if (tileEntity is not null)
				TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(tileEntity.Hook_AfterPlacement, -1, 0, false);
			else
				TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(TEStorageComponent.Hook_AfterPlacement_NoEntity, -1, 0, false);

			TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
			TileObjectData.newAlternate.AnchorBottom = AnchorData.Empty;
			TileObjectData.addAlternate(0);

			TileObjectData.addTile(Type);

			ModTranslation text = CreateMapEntryName();
			text.SetDefault("Magic Storage");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "魔法储存元件");
			AddMapEntry(new Color(153, 107, 61), text);
			DustType = 7;
			TileID.Sets.DisableSmartCursor[Type] = true;
			// Old: TileID.Sets.HasOutlines[Type] = HasSmartInteract();
			// New: HasSmartInteract() just enables the highlights to be drawn, thus this Sets just has to be true regardless
			TileID.Sets.HasOutlines[Type] = true;
		}

		public virtual void ModifyObjectData()
		{
		}

		public virtual ModTileEntity GetTileEntity() => null;

		public virtual int ItemType(int frameX, int frameY) => ModContent.ItemType<Items.StorageComponent>();

		public static bool IsStorageComponent(Point16 point)
		{
			Tile tile = Main.tile[point.X, point.Y];
			return tile.HasTile && TileLoader.GetTile(tile.TileType) is StorageComponent;
		}

		public int CanPlace(int i, int j, int type, int style, int direction, int alternative)
		{
			int count = 0;
			if (GetTileEntity() is TEStorageCenter)
				count++;

			Point16 startSearch = new(i - 1, j - 1);
			HashSet<Point16> explored = new() { startSearch };
			Queue<Point16> toExplore = new();
			foreach (Point16 point in TEStorageComponent.AdjacentComponents(startSearch))
				toExplore.Enqueue(point);

			while (toExplore.Count > 0)
			{
				Point16 explore = toExplore.Dequeue();
				if (!explored.Contains(explore) && explore != killTile)
				{
					explored.Add(explore);
					if (TEStorageCenter.IsStorageCenter(explore))
					{
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

		public override void KillMultiTile(int i, int j, int frameX, int frameY)
		{
			var source = new EntitySource_TileBreak(i, j);
			Item.NewItem(source, i * 16, j * 16, 32, 32, ItemType(frameX, frameY));
			killTile = new Point16(i, j);
			ModTileEntity tileEntity = GetTileEntity();
			if (tileEntity is not null)
			{
				tileEntity.Kill(i, j);
			}
			else
			{
				if (Main.netMode == NetmodeID.MultiplayerClient)
					NetHelper.SendSearchAndRefresh(killTile.X, killTile.Y);
				else
					TEStorageComponent.SearchAndRefreshNetwork(killTile);
			}

			killTile = Point16.NegativeOne;
		}
	}
}
