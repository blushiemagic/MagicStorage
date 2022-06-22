using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace MagicStorage.Stations
{
	public abstract class CombinedStationsTile<TItem> : ModTile where TItem : ModItem
	{
		public abstract Color MapColor { get; }

		public abstract int[] GetAdjTiles();

		public abstract void GetTileDimensions(out int width, out int height);

		public virtual void SafeSetStaticDefaults()
		{
		}

		public sealed override void SetStaticDefaults()
		{
			SafeSetStaticDefaults();

			Main.tileSolid[Type] = false;
			Main.tileLavaDeath[Type] = false;
			Main.tileFrameImportant[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
			GetTileDimensions(out int width, out int height);
			TileObjectData.newTile.Width = width;
			TileObjectData.newTile.Height = height;
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.StyleWrapLimit = 36;
			TileObjectData.newTile.Origin = new Point16((width - 1) / 2, height - 1);
			TileObjectData.newTile.CoordinateHeights = CreateArrayFromLength(height);
			TileObjectData.addTile(Type);

			AdjTiles = GetAdjTiles();

			AddMapEntry(MapColor);
		}

		private static int[] CreateArrayFromLength(int length)
		{
			int[] arr = new int[length];

			Array.Fill(arr, 16);

			arr[^1] = 18;

			return arr;
		}

		public sealed override void KillMultiTile(int i, int j, int frameX, int frameY)
		{
			GetTileDimensions(out int width, out int height);
			var source = new EntitySource_TileBreak(i, j);
			Item.NewItem(source, i * 16, j * 16, width * 16, height * 16, ModContent.ItemType<TItem>());
		}
	}
}
