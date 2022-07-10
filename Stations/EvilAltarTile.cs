﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace MagicStorage.Stations
{
	public class EvilAltarTile : ModTile
	{
		public override string Texture => $"Terraria/Images/Tiles_{TileID.DemonAltar}";

		public override void SetStaticDefaults()
		{
			Main.tileSolid[Type] = false;
			Main.tileLavaDeath[Type] = false;
			Main.tileFrameImportant[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.StyleWrapLimit = 36;
			TileObjectData.newTile.Origin = new Point16(1, 1);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
			TileObjectData.addTile(Type);

			AdjTiles = new int[] { Type, TileID.DemonAltar };

			AddMapEntry(Color.MediumPurple, CreateMapEntryName());
		}

		public override void KillMultiTile(int i, int j, int frameX, int frameY)
		{
			int type = frameX / 54 == 0 ? ModContent.ItemType<DemonAltar>() : ModContent.ItemType<CrimsonAltar>();

			var source = new EntitySource_TileBreak(i, j);
			Item.NewItem(source, i * 16, j * 16, 48, 32, type);
		}
	}
}
