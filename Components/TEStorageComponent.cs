using MagicStorage.Common;
using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.Debugging;
using System;
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
		protected Point16 _storageCenter = Point16.NegativeOne;

		public virtual Point16 StorageCenter
		{
			get => _storageCenter;
			set => _storageCenter = value;
		}

		internal int assignedNetwork = -1;

		public bool CanLocalClientAccess() => SecuritySystem.CanPlayerAccessImmediately(Main.LocalPlayer, assignedNetwork);

		public virtual TEStorageHeart GetHeart() {
			Point16 center = StorageCenter;

			if (center.X < 0 || center.Y < 0)
				return null;

			if (ByPosition.TryGetValue(center, out TileEntity te) && te is TEStorageCenter storageCenter)
				return storageCenter.GetHeart();

			return null;
		}

		public TEStorageCenter GetLocalCenter() {
			if (this is TEStorageCenter centerEntity)
				return centerEntity;

			Point16 center = StorageCenter;

			if (center.X < 0 || center.Y < 0)
				return null;

			return center.ResolveToTileEntity<TEStorageCenter>();
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
				NetHelper.SendComponentTilesAndEntityPlacement(i - 1, j - 1, Type);
				NetHelper.SendNetworkConnectionsUpdateOnPlacement(new Point16(i - 1, j - 1));
				return -1;
			}

			int id = Place(i - 1, j - 1);
			((TEStorageComponent) ByID[id]).OnPlace();
			UpdateNearbyConnectors(i - 1, j - 1);
			return id;
		}

		public static int Hook_AfterPlacement_NoEntity(int i, int j, int type, int style, int direction, int alternate)
		{
			// CHANGE: v0.7.1 - placing a component checks for existing networks instead of forcing the adjacent network to be recalculated

			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				NetMessage.SendTileSquare(Main.myPlayer, i - 1, j - 2, 2, 4);
				NetMessage.SendTileSquare(Main.myPlayer, i - 2, j - 1, 4, 2);
				//NetHelper.SendSearchAndRefresh(i - 1, j - 1);
				NetHelper.SendNetworkConnectionsUpdateOnPlacement(new Point16(i - 1, j - 1));
				return 0;
			}

			//SearchAndRefreshNetwork(new Point16(i - 1, j - 1));
			TileNetworkScanner.SmartlyConnectAdjacentNetworks(new Point16(i - 1, j - 1));
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
			if (this is not TEStorageHeart) {
				if (GetLocalCenter() is TEStorageCenter center) {
					// Ensure that connections are complete
					center.ComponentManager.LinkIfNotExists(this);
				} else {
					// No center found, force the component to not be connected to anything
					if (Unlink())
						NetHelper.SendTEUpdate(ID);
				}
			}
		}

		public virtual void OnPlace()
		{
			// CHANGE: v0.7.1 - placing a component checks for existing networks instead of forcing the adjacent network to be recalculated
			//SearchAndRefreshNetwork(Position);
			TileNetworkScanner.SmartlyConnectAdjacentNetworks(Position);
		}

		public override void OnKill()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// This packet also handles unlinking the component on the server
				NetHelper.SendNetworkConnectionsUpdateOnDestruction(Position, TileNetworkScanner.GetLocalNeighbors2x2());
			} else
				TileNetworkScanner.SmartlyDisconnectComponents(Position, TileNetworkScanner.GetLocalNeighbors2x2());
		}

		public IEnumerable<Point16> AdjacentComponents() => AdjacentComponents(Position);

		public static IEnumerable<Point16> AdjacentComponents(Point16 point) => TileNetworkScanner.IterateAdjacentComponents(point);

		public static Point16 FindStorageCenter(Point16 startSearch)
		{
			return TileNetworkScanner.TryFindStorageCenter(startSearch, TileScanSettings.FastDefault, out var foundCenter)
				? foundCenter.Position
				: Point16.NegativeOne;
		}

		public override void OnNetPlace()
		{
			OnPlace();
			NetHelper.SendTEUpdate(ID);
		}

		public static void SearchAndRefreshNetwork(Point16 position)
		{
			using var debugging = DebugMessage.CreateIf(DebugControls.Names.StorageNetworkRecalculate);

			Point16 center = FindStorageCenter(position);

			if (debugging.IsDebugging)
				debugging.Report(true, "Located storage center: {0}", center.DebugString());

			if (center != Point16.NegativeOne && ByPosition.TryGetValue(center, out var te) && te is TEStorageCenter centerEnt) {
				centerEnt.ResetAndSearch();

				if (Main.netMode != NetmodeID.Server && StoragePlayer.LocalPlayer.ViewingStorage().X >= 0) {
					if (centerEnt.GetHeart() is TEStorageHeart centerHeart && StoragePlayer.IsClientViewingHeart(centerHeart)) {
						if (debugging.IsDebugging)
							debugging.Report(false, "Client is viewing the updated storage network, forcing the UI fully refresh...");

						MagicUI.RequestFullRefresh();
					}
				}
			}
		}

		public override void NetSend(BinaryWriter writer) {
			writer.Write(_storageCenter);
			writer.Write(assignedNetwork);
		}

		public override void NetReceive(BinaryReader reader) {
			_storageCenter = reader.ReadPoint16();
			assignedNetwork = reader.ReadInt32();
		}

		public override void SaveData(TagCompound tag) {
			tag["center"] = _storageCenter;
			tag["network"] = assignedNetwork;
		}

		public override void LoadData(TagCompound tag) {
			_storageCenter = tag.TryGet("center", out Point16 center) ? center : Point16.NegativeOne;
			assignedNetwork = tag.TryGet("network", out int network) ? network : -1;
		}
	}
}
