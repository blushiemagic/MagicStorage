using System.Collections.Generic;
using System.Linq;
using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.Debugging;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace MagicStorage.Components
{
	public class StorageConnector : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileSolid[Type] = false;
			TileObjectData.newTile.Width = 1;
			TileObjectData.newTile.Height = 1;
			TileObjectData.newTile.Origin = new Point16(0, 0);
			TileObjectData.newTile.CoordinateHeights = new[] { 16 };
			TileObjectData.newTile.CoordinateWidth = 16;
			TileObjectData.newTile.CoordinatePadding = 2;
			TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(CanPlace, -1, 0, true);
			TileObjectData.newTile.UsesCustomCanPlace = true;
			TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(Hook_AfterPlacement, -1, 0, false);
			TileObjectData.addTile(Type);

			// We don't need to call SetDefault() on CreateMapEntryName()'s return value if we have .hjson files.
			AddMapEntry(new Color(153, 107, 61), CreateMapEntryName());

			DustType = 7;

			//RegisterItemDrop new to 1.4.4
			RegisterItemDrop(ModContent.ItemType<Items.StorageConnector>());
   
			// Make the tile count as a door for housing purposes (like how platforms work)
			AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
		}

		public static int CanPlace(int i, int j, int type, int style, int direction, int alternative)
		{
			TileScanSettings scanSettings = new() {
				AllowLocalCenterScanningShortcut = true
			};

			TileScanResult result = TileNetworkScanner.ScanForStorageCenters(new Point16(i, j), scanSettings);

			return result == TileScanResult.TooManyCenters ? -1 : (int)result;
		}

		public static int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternative)
		{
			// CHANGE: v0.7.1 - placing a connector checks for existing networks instead of forcing the adjacent network to be recalculated

			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				NetMessage.SendTileSquare(Main.myPlayer, i, j, 1, 1);
				//NetHelper.SendSearchAndRefresh(i, j);
				NetHelper.SendNetworkConnectionsUpdateOnPlacement(new Point16(i, j));
				return 0;
			}

			//TEStorageComponent.SearchAndRefreshNetwork(new Point16(i, j));
			TileNetworkScanner.SmartlyConnectAdjacentNetworks(new Point16(i, j));
			return 0;
		}

		public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
		{
			int frameX = 0;
			int frameY = 0;
			if (CanMergeWith(i - 1, j))
				frameX += 18;
			if (CanMergeWith(i + 1, j))
				frameX += 36;
			if (CanMergeWith(i, j - 1))
				frameY += 18;
			if (CanMergeWith(i, j + 1))
				frameY += 36;
			Main.tile[i, j].TileFrameX = (short) frameX;
			Main.tile[i, j].TileFrameY = (short) frameY;
			return false;
		}

		private static bool CanMergeWith(int i, int j) {
			if (!WorldGen.InWorld(i, j))
				return false;

			Tile tile = Main.tile[i, j];
			return tile.HasTile && TileLoader.GetTile(tile.TileType) is StorageComponent or StorageConnector;
		}

		public override bool CanExplode(int i, int j)
		{
			bool explodable = TileNetworkScanner.ScanComponents(new Point16(i, j), TileScanSettings.Default).FirstOrDefault() is not TEStorageComponent component || SecuritySystem.CanDestroyTile(component);

			if (!explodable)
			{
				using var debugging = DebugMessage.CreateIf(DebugControls.Names.StorageComponentDestruction);

				if (debugging.IsDebugging)
					debugging.Report(false, "Blocked tile destruction from explosive for {0} at {1}", FullName, new Point16(i, j).DebugString());
			}

			return explodable;
		}

		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
		{
			if (fail || effectOnly)
				return;

			if (TileNetworkScanner.ScanComponents(new Point16(i, j), TileScanSettings.Default).FirstOrDefault() is TEStorageComponent component && !SecuritySystem.CanDestroyTile(component))
			{
				fail = true;
				effectOnly = true;
				noItem = true;

				using var debugging = DebugMessage.CreateIf(DebugControls.Names.StorageComponentDestruction);

				if (debugging.IsDebugging)
					debugging.Report(false, "Blocked tile destruction for {0} at {1} - reason: inaccessible network", FullName, new Point16(i, j).DebugString());
			}
			else
			{
				TileNetworkScanner.SmartlyDisconnectComponents(new Point16(i, j), TileNetworkScanner.GetLocalNeighbors1x1());
			}
		}
	}
}
