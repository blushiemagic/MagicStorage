using MagicStorage.Common.Algorithms;
using MagicStorage.Common.Systems.Debugging;
using MagicStorage.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common {
	public enum TileScanResult {
		NoCentersFound,
		CenterExists,
		TooManyCenters
	}

	public readonly ref struct TileScanSettings {
		public object InvokingActor { get; init; }

		public bool AllowLocalCenterScanningShortcut { get; init; }

		public bool IgnoreOriginPoint { get; init; }

		public static TileScanSettings Default => default;

		public static TileScanSettings FastDefault => new() { AllowLocalCenterScanningShortcut = true };
	}

	public static class TileNetworkScanner {
		public static TileScanResult ScanForStorageCenters(Point16 origin, TileScanSettings settings) {
			using var debugging = DebugMessage.CreateIf(DebugControls.Names.StorageNetworkScanning);

			if (debugging.IsDebugging)
				debugging.Report(true, "Scanning for storage centers...");

			int count = settings.InvokingActor is TEStorageCenter ? 1 : 0;

			foreach (var component in ScanComponents(origin, settings)) {
				if (component is TEStorageCenter) {
					count++;

					if (count >= 2) {
						if (debugging.IsDebugging)
							debugging.Report(true, "More than one storage center was found, aborting");

						return TileScanResult.TooManyCenters;
					}
				}
			}

			if (debugging.IsDebugging)
				debugging.Report(true, $"Scan complete, found {count} storage center(s)");

			return (TileScanResult)count;
		}

		public static bool TryFindStorageCenter(Point16 origin, TileScanSettings settings, out TEStorageCenter center) {
			if (ScanComponents(origin, settings).OfType<TEStorageCenter>().FirstOrDefault() is TEStorageCenter foundCenter) {
				center = foundCenter;
				return true;
			}

			center = default;
			return false;
		}

		public static IEnumerable<TEStorageComponent> ScanComponents(Point16 origin, TileScanSettings settings) {
			HashSet<Point16> visited = [];

			if (origin == StorageComponent.killTile && !settings.IgnoreOriginPoint)
				settings = settings with { IgnoreOriginPoint = true };

			Queue<Point16> exploring = [];
			exploring.Enqueue(origin);

			while (exploring.TryDequeue(out Point16 explore)) {
				if (settings.IgnoreOriginPoint && explore == origin)
					continue;

				if (!WorldGen.InWorld(explore.X, explore.Y))
					continue;

				Tile tile = Main.tile[explore];

				if (!tile.HasTile)
					continue;

				ModTile modTile = TileLoader.GetTile(tile.TileType);

				// It's safe to assume that Connectors will always be 1x1 and any component is 2x2
				// This will have to be revisted should support for other component sizes is implemented
				IEnumerable<Point16> exploreNeighbors;

				if (modTile is StorageConnector) {
					// Connectors are 1x1 tiles
					exploreNeighbors = GetLocalNeighbors1x1();
				} else if (modTile is StorageComponent) {
					// Components are 2x2 tiles
					exploreNeighbors = GetLocalNeighbors2x2();

					// Iterative positions should already be adjusted, but they're adjusted again here just in case
					AdjustDefiniteComponentCoordinate(ref explore);

					// Enumerate the found component
					if (visited.Add(explore) && explore.ResolveToTileEntity() is TEStorageComponent component) {
						yield return component;

						if (settings.AllowLocalCenterScanningShortcut && component.GetLocalCenter() is TEStorageCenter center) {
							// Enumerate the local center
							Point16 position = center.Position;
							if (visited.Add(position))
								yield return center;

							// Enumerate the center's connected components
							foreach (var connectedComponent in center.ComponentManager.GetDirectlyConnectableComponentEntities()) {
								position = connectedComponent.Position;

								if (visited.Add(position))
									yield return connectedComponent;
							}
						}
					}
				} else {
					// Not a storage component, ignore
					continue;
				}

				foreach (Point16 offset in exploreNeighbors) {
					Point16 neighbor = explore + offset;

					AdjustComponentCoordinate(ref neighbor);

					if (!visited.Contains(neighbor))
						exploring.Enqueue(explore + offset);
				}
			}
		}

		public static IEnumerable<Point16> IterateAdjacentComponents(Point16 origin) => IterateAdjacentComponents(origin, []);

		public static IEnumerable<Point16> IterateAdjacentComponents(Point16 origin, HashSet<Point16> visited) {
			if (!WorldGen.InWorld(origin.X, origin.Y))
				yield break;

			Tile originTile = Main.tile[origin];

			if (!originTile.HasTile)
				yield break;

			ModTile modTile = TileLoader.GetTile(originTile.TileType);

			foreach (Point16 neighbor in IterateNeighbors(origin, modTile, visited))
				yield return neighbor;
		}

		private static IEnumerable<Point16> IterateNeighbors(Point16 origin, ModTile originTile, HashSet<Point16> visited) {
			IEnumerable<Point16> neighbors;
			
			// It's safe to assume that Connectors will always be 1x1 and any component is 2x2
			// This will have to be revisted should support for other component sizes is implemented
			if (originTile is StorageConnector) {
				neighbors = GetLocalNeighbors1x1();
			} else if (originTile is StorageComponent) {
				AdjustComponentCoordinate(ref origin);
				neighbors = GetLocalNeighbors2x2();
			} else
				yield break;

			visited ??= [];

			foreach (Point16 relative in neighbors) {
				Point16 neighbor = origin + relative;

				AdjustComponentCoordinate(ref neighbor);

				if (visited.Add(neighbor))
					yield return neighbor;
			}
		}

		public static void AdjustComponentCoordinate(ref Point16 position) {
			if (!WorldGen.InWorld(position.X, position.Y))
				return;

			if (TileLoader.GetTile(Main.tile[position].TileType) is not StorageComponent)
				return;

			AdjustDefiniteComponentCoordinate(ref position);
		}

		private static void AdjustDefiniteComponentCoordinate(ref Point16 position) {
			int x = position.X, y = position.Y;

			if (Main.tile[x, y].TileFrameX % 36 == 18)
				x--;
			if (Main.tile[x, y].TileFrameY % 36 == 18)
				y--;

			position = new Point16(x, y);
		}

		public static Point16 AdjustComponentCoordinate(Point16 position) {
			AdjustComponentCoordinate(ref position);
			return position;
		}

		public static IEnumerable<Point16> GetLocalNeighbors1x1() {
			return [
				new Point16(0, -1),
				new Point16(1, 0),
				new Point16(0, 1),
				new Point16(-1, 0)
			];
		}

		public static IEnumerable<Point16> GetLocalNeighbors2x2() {
			return [
				new Point16(0, -1),
				new Point16(1, -1),
				new Point16(2, 0),
				new Point16(2, 1),
				new Point16(1, 2),
				new Point16(0, 2),
				new Point16(-1, 1),
				new Point16(-1, 0)
			];
		}

		public static IEnumerable<Point16> GetLocalNeighbors(int areaWidth, int areaHeight) {
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(areaWidth);
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(areaHeight);

			// Top edge

			for (int x = 0; x < areaWidth; x++)
				yield return new Point16(x, -1);

			// Right edge

			for (int y = 0; y < areaHeight; y++)
				yield return new Point16(areaWidth, y);

			// Bottom edge

			for (int x = areaWidth - 1; x >= 0; x--)
				yield return new Point16(x, areaHeight);

			// Left edge

			for (int y = areaHeight - 1; y >= 0; y--)
				yield return new Point16(-1, y);
		}

		public static void SmartlyDisconnectComponents(Point16 destroyedComponent, IEnumerable<Point16> initialScanNeighbors) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				NetHelper.SendNetworkConnectionsUpdateOnDestruction(destroyedComponent, initialScanNeighbors);
				return;
			}

			using var debugging = DebugMessage.CreateIfAny(DebugControls.Names.StorageNetworkRecalculate, DebugControls.Names.StorageNetworkScanning);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Starting smart component disconnecting from origin point {0}...", destroyedComponent.DebugString())
					.Indent();
			}

			StorageComponent.killTile = destroyedComponent;

			var tree = new SmartDisconnectIterationTree(initialScanNeighbors);

			tree.Scan(destroyedComponent);

			if (tree.foundStorageCenter.ResolveToTileEntity() is TEStorageCenter center) {
				var manager = center.ComponentManager;

				foreach (var position in tree.EnumerateDisjointFrom(tree.foundStorageCenter)) {
					if (tree.IsFoundComponent(position)) {
						if (debugging.IsDebugging) {
							debugging
								.Report(false, "Component at location {0} is no longer connected", position.DebugString())
								.Indent();
						}

						manager.Unlink(position);

						if (debugging.IsDebugging)
							debugging.Unindent();
					}
				}
			}

			StorageComponent.killTile = Point16.NegativeOne;
		}

		private class SmartDisconnectIterationTree(IEnumerable<Point16> initialLocalNeighbors) : BreadthFirstSearchConnectionsGraph<Point16> {
			private readonly IEnumerable<Point16> _initialLocalNeighbors = initialLocalNeighbors;
			private ModTile _enumeratingTile;

			public Point16 foundStorageCenter = Point16.NegativeOne;
			private readonly HashSet<Point16> _foundComponents = [];

			public bool IsFoundComponent(Point16 value) => _foundComponents.Contains(value);

			protected override void Initialize() {
				_enumeratingTile = null;
				foundStorageCenter = Point16.NegativeOne;
				_foundComponents.Clear();
			}

			protected override void PopulateInitialQueue(Queue<Point16> queue, Point16 origin) {
				// The origin point is being destroyed and should not be enumerated
				//queue.Enqueue(origin);

				// The initial neighbors are manually specified in case the origin tile isn't valid
				foreach (Point16 relative in _initialLocalNeighbors)
					queue.Enqueue(origin + relative);
			}

			protected override bool IsValidEntry(Point16 entry) {
				_enumeratingTile = null;

				if (entry == StorageComponent.killTile)
					return false;

				if (!WorldGen.InWorld(entry.X, entry.Y))
					return false;

				Tile tile = Main.tile[entry];
				
				if (!tile.HasTile)
					return false;

				// Remember the ModTile for faster future references
				_enumeratingTile = TileLoader.GetTile(tile.TileType);
				return _enumeratingTile is StorageConnector or StorageComponent;
			}

			protected override void TransformEntry(ref Point16 entry) {
				if (_enumeratingTile is StorageComponent)
					AdjustDefiniteComponentCoordinate(ref entry);
			}

			protected override void OnEntryVisited(Point16 value) {
				if (_enumeratingTile is StorageComponent) {
					var entity = value.ResolveToTileEntity();

					if (entity is TEStorageCenter)
						foundStorageCenter = value;
					else if (entity is TEStorageComponent)
						_foundComponents.Add(value);
				}
			}

			protected override IEnumerable<Point16> EnumerateNextEntries(Point16 current) => IterateNeighbors(current, _enumeratingTile, null);
		}

		public static void SmartlyConnectAdjacentNetworks(Point16 placedComponent) {
			// Instead of performing multiple searches, just remember which components have already been iterated

			using var debugging = DebugMessage.CreateIfAny(DebugControls.Names.StorageNetworkRecalculate, DebugControls.Names.StorageNetworkScanning);

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "Starting smart component linking from origin point {0}...", placedComponent.DebugString())
					.Indent();
			}

			List<TEStorageComponent> waitingList = [];
			TEStorageCenter foundCenter = null;

			TileScanSettings scanSettings = new() {
				InvokingActor = placedComponent.ResolveToTileEntity<TEStorageComponent>(),
				AllowLocalCenterScanningShortcut = true
			};

			foreach (var component in ScanComponents(placedComponent, scanSettings)) {
				if (component is TEStorageCenter center) {
					if (debugging.IsDebugging)
						debugging.Report(false, "Storage center was found at location {0}", center.Position.DebugString());

					foundCenter = center;

					// Link the components waiting to be linked
					var manager = center.ComponentManager;

					foreach (var waitingComponent in waitingList) {
						if (debugging.IsDebugging) {
							debugging
								.Report(false, "Linking component at location {0}", waitingComponent.Position.DebugString())
								.Indent();
						}

						manager.Link(waitingComponent);

						if (debugging.IsDebugging)
							debugging.Unindent();
					}

					waitingList.Clear();
				} else if (foundCenter is not null) {
					if (component.StorageCenter != foundCenter.Position) {
						// Link the component
						if (debugging.IsDebugging) {
							debugging
								.Report(false, "Changing linked center for component at location {0}", component.Position.DebugString())
								.Indent();
						}

						foundCenter.ComponentManager.Link(component);

						if (debugging.IsDebugging)
							debugging.Unindent();
					}
				} else {
					// Delay component linking
					waitingList.Add(component);
				}
			}
		}
	}
}
