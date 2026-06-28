using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.Debugging;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
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

			// We don't need to call SetDefault() on CreateMapEntryName()'s return value if we have .hjson files.
			AddMapEntry(new Color(153, 107, 61), CreateMapEntryName());

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
			// NOTE: Assumes TileObjectData.Origin is (1, 1)
			Point16 scanOrigin = new Point16(i - 1, j - 1);

			TileScanSettings scanSettings = new() {
				InvokingActor = GetTileEntity(),
				AllowLocalCenterScanningShortcut = true
			};

			TileScanResult result = TileNetworkScanner.ScanForStorageCenters(scanOrigin, scanSettings);

			return result == TileScanResult.TooManyCenters ? -1 : (int)result;
		}

		public override bool CanExplode(int i, int j)
		{
			if (Main.tile[i, j].TileFrameX % 36 == 18)
				i--;
			if (Main.tile[i, j].TileFrameY % 36 == 18)
				j--;

			if (GetTileEntity() is not null && !SecuritySystem.CanDestroyTile(i, j))
			{
				// The player causing the explosion shouldn't be allowed to destroy the tile

				using var debugging = DebugMessage.CreateIf(DebugControls.Names.StorageComponentDestruction);

				if (debugging.IsDebugging)
					debugging.Report(false, "Blocked tile destruction from explosive for {0} at {1}", FullName, new Point16(i, j).DebugString());

				return false;
			}

			return true;
		}

		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
		{
			if (fail || effectOnly)
				return;

			if (Main.tile[i, j].TileFrameX % 36 == 18)
				i--;
			if (Main.tile[i, j].TileFrameY % 36 == 18)
				j--;

			if (GetTileEntity() is not null)
			{
				if (new Point16(i, j).ResolveToTileEntity() is not TEStorageComponent component)
				{
					// Entity was deleted, prevent the tile from being destroyed
					fail = true;
					effectOnly = true;
					noItem = true;

					using var debugging = DebugMessage.CreateIf(DebugControls.Names.StorageComponentDestruction);

					if (debugging.IsDebugging)
						debugging.Report(false, "Blocked tile destruction for {0} at {1} - reason: missing component entity", FullName, new Point16(i, j).DebugString());
				}
				else if (!SecuritySystem.CanDestroyTile(component))
				{
					// The player causing the destruction shouldn't be allowed to destroy the tile
					fail = true;
					effectOnly = true;
					noItem = true;

					using var debugging = DebugMessage.CreateIf(DebugControls.Names.StorageComponentDestruction);

					if (debugging.IsDebugging)
						debugging.Report(false, "Blocked tile destruction for {0} at {1} - reason: inaccessible network", FullName, new Point16(i, j).DebugString());
				}
			}
		}

		public override void KillMultiTile(int i, int j, int frameX, int frameY)
		{
			if (GetTileEntity() is ModTileEntity entity) {
				// TEStorageComponent.OnKill calls SmartlyDisconnectComponents
				entity.Kill(i, j);
			} else
				TileNetworkScanner.SmartlyDisconnectComponents(new Point16(i, j), TileNetworkScanner.GetLocalNeighbors2x2());
		}

		public override void MouseOver(int i, int j) {
			if (Main.tile[i, j].TileFrameX % 36 == 18)
				i--;
			if (Main.tile[i, j].TileFrameY % 36 == 18)
				j--;

			if (TileEntity.ByPosition.TryGetValue(new Point16(i, j), out TileEntity te) && te is TEStorageComponent component && component.GetHeart()?.storageName is { Length: >0 } heartName) {
				// Extra space added when an icon is present
				string text = heartName;
				if (Main.LocalPlayer.cursorItemIconID > ItemID.None)
					text = "    " + text;

				Main.instance.MouseText(text);
			}
		}
	}
}
