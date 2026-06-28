using MagicStorage.Common;
using MagicStorage.Common.Systems.Debugging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
	public abstract class TEStorageCenter : TEStorageComponent
	{
		public class ConnectedComponentManager {
			private enum ComponentType {
				StorageUnit,
				StorageAccess,
				CraftingAccess,
				EnvironmentAccess,
				RemoteAccess,
				DecraftingAccess,
				StorageHeart,
				ExternalStorageComponent,  // Third-party entities inheriting from TEStorageComponent
				ExternalStoragePoint,      // Third-party entities inheriting from TEStoragePoint
				ExternalStorageCenter,     // Third-party entities inheriting from TEStorageCenter
				Unknown,
				DeferredLoad               // In cases where attempting to use ByPosition won't work
			}

			#region Generic Helper Types
			private interface ITypeResolver<TSelf>
				where TSelf : ITypeResolver<TSelf>
			{
				static abstract ComponentType TypeFilter { get; }

				public static bool Matches(Component component) => component.type == TSelf.TypeFilter;
			}

			private readonly struct StorageUnitResolver : ITypeResolver<StorageUnitResolver> { public static ComponentType TypeFilter => ComponentType.StorageUnit; }
			private readonly struct StorageAccessResolver : ITypeResolver<StorageAccessResolver> { public static ComponentType TypeFilter => ComponentType.StorageAccess; }
			private readonly struct CraftingAccessResolver : ITypeResolver<CraftingAccessResolver> { public static ComponentType TypeFilter => ComponentType.CraftingAccess; }
			private readonly struct EnvironmentAccessResolver : ITypeResolver<EnvironmentAccessResolver> { public static ComponentType TypeFilter => ComponentType.EnvironmentAccess; }
			private readonly struct RemoteAccessResolver : ITypeResolver<RemoteAccessResolver> { public static ComponentType TypeFilter => ComponentType.RemoteAccess; }
			private readonly struct DecraftingAccessResolver : ITypeResolver<DecraftingAccessResolver> { public static ComponentType TypeFilter => ComponentType.DecraftingAccess; }
			private readonly struct StorageHeartResolver : ITypeResolver<StorageHeartResolver> { public static ComponentType TypeFilter => ComponentType.StorageHeart; }
			private readonly struct ExternalStorageComponentResolver : ITypeResolver<ExternalStorageComponentResolver> { public static ComponentType TypeFilter => ComponentType.ExternalStorageComponent; }
			private readonly struct ExternalStoragePointResolver : ITypeResolver<ExternalStoragePointResolver> { public static ComponentType TypeFilter => ComponentType.ExternalStoragePoint; }
			private readonly struct ExternalStorageCenterResolver : ITypeResolver<ExternalStorageCenterResolver> { public static ComponentType TypeFilter => ComponentType.ExternalStorageCenter; }
			private readonly struct UnknownResolver : ITypeResolver<UnknownResolver> { public static ComponentType TypeFilter => ComponentType.Unknown; }
			private readonly struct DeferredLoadResolver : ITypeResolver<DeferredLoadResolver> { public static ComponentType TypeFilter => ComponentType.DeferredLoad; }
			#endregion

			private readonly struct Component {
				public readonly Point16 location;
				public readonly ComponentType type;

				public Component(Point16 location, ComponentType type) {
					this.location = location;
					this.type = type;
				}
			}

			private readonly List<Component> _components = new();
			private readonly TEStorageCenter _center;
			private Point16 _foundHeart;
			private readonly HashSet<int> _unresolvedComponents = [];

			private readonly Dictionary<ComponentType, HashSet<Point16>> _knownComponentLocationCache = new();

			public int Count => _components.Count;

			public Point16 StorageCenter => _center.Position;

			public ConnectedComponentManager(TEStorageCenter center) {
				_center = center;
				if (center is TEStorageHeart heart)
					_foundHeart = heart.Position;
			}

			internal void Reset() {
				_components.Clear();
				_knownComponentLocationCache.Clear();
				_unresolvedComponents.Clear();
				
				if (_center is not TEStorageHeart)
					_foundHeart = Point16.NegativeOne;
			}

			public void LinkIfNotExists(TEStorageComponent component) {
				var type = GetComponentType(component);
				if (!_knownComponentLocationCache.TryGetValue(type, out var set) || !set.Contains(component.Position))
					Link(component);
			}

			public bool IsLinked(TEStorageComponent component) {
				var type = GetComponentType(component);
				return _knownComponentLocationCache.TryGetValue(type, out var set) && set.Contains(component.Position);
			}

			public void Link(TEStorageComponent component) {
				bool debuggingCondition = DebugControls.Get(DebugControls.Names.StorageCenterComponentLinking);

				if (component is TEStorageHeart heart) {
					// Don't link storage hearts to storage storage hearts
					if (_center is TEStorageHeart) {
						using var debuggingFailure = DebugMessage.Create(debuggingCondition);

						if (debuggingFailure.IsDebugging) {
							debuggingFailure
								.Report(false, "Attempted to link two Storage Hearts")
								.Indent()
								.Report(false, "Linking actor: {0} at {1}", _center.FullName, _center.Position.DebugString())
								.Report(false, "Linking target: {0} at {1}", heart.FullName, heart.Position.DebugString());
						}
					} else {
						// Redirect so that the Heart is what's handling the link
						heart.ComponentManager.Link(_center);
					}

					return;
				}

				using var debugging = DebugMessage.Create(debuggingCondition);

				if (debugging.IsDebugging) {
					debugging
						.Report(false, "Linking component ({0}) to Center ({1})...", component.FullName, _center.FullName)
						.Indent()
						.Report(false, "Component location: {0}", component.Position.DebugString())
						.Report(false, "Center location: {0}", _center.Position.DebugString())
						.Unindent();
				}

				if (component.StorageCenter != Point16.NegativeOne) {
					if (component.StorageCenter != _center.Position) {
						if (component.StorageCenter.ResolveToTileEntity() is TEStorageCenter previousCenter) {
							if (debugging.IsDebugging) {
								debugging
									.Report(false, "Component already has a connection at (X: {0}, Y: {1}), unlinking...", previousCenter.Position.X, previousCenter.Position.Y)
									.Indent();
							}

							previousCenter.ComponentManager.Unlink(component.Position);
						}
					} else {
						if (debugging.IsDebugging)
							debugging.Report(false, "Connection already exists, enforcing linked states");
					}
				}

				ComponentType type = ApplyLink(component);

				if (debugging.IsDebugging)
					debugging.Report(false, "Success.  Linked component classification was {0}", type);

				if (_center is TEStorageHeart && component is TEStorageCenter otherCenter)
					otherCenter.ComponentManager.ApplyLink(_center);

				NetHelper.SendTEUpdate(_center.ID);
				NetHelper.SendTEUpdate(component.ID);
			}

			private ComponentType ApplyLink(TEStorageComponent component) {
				ComponentType type = GetComponentType(component);

				if (type == ComponentType.StorageHeart) {
					_foundHeart = component.Position;
					return type;
				}

				if (!_knownComponentLocationCache.TryGetValue(type, out var set))
					_knownComponentLocationCache[type] = set = new HashSet<Point16>();
				
				if (set.Add(component.Position)) {
					_components.Add(new Component(component.Position, type));
					_center.OnConnectComponent(component);
				}

				component.Link(_center.Position);

				return type;
			}

			internal void LinkStorageUnit(Point16 location) {
				if (!_knownComponentLocationCache.TryGetValue(ComponentType.StorageUnit, out var set))
					_knownComponentLocationCache[ComponentType.StorageUnit] = set = new HashSet<Point16>();

				if (set.Contains(location))
					return;

				if (location.ResolveToTileEntity() is TEAbstractStorageUnit unit)
					Link(unit);
			}

			internal void LinkRemoteAccess(Point16 location) {
				if (!_knownComponentLocationCache.TryGetValue(ComponentType.RemoteAccess, out var set))
					_knownComponentLocationCache[ComponentType.RemoteAccess] = set = new HashSet<Point16>();

				if (set.Contains(location))
					return;

				if (location.ResolveToTileEntity() is TERemoteAccess access)
					Link(access);
			}

			internal void LinkEnvironmentAccess(Point16 location) {
				if (!_knownComponentLocationCache.TryGetValue(ComponentType.EnvironmentAccess, out var set))
					_knownComponentLocationCache[ComponentType.EnvironmentAccess] = set = new HashSet<Point16>();

				if (set.Contains(location))
					return;

				if (location.ResolveToTileEntity() is TEEnvironmentAccess access)
					Link(access);
			}

			private static ComponentType GetComponentType(TEStorageComponent component) {
				if (component is TEAbstractStorageUnit)
					return ComponentType.StorageUnit;

				if (component is TEStorageAccess) {
					if (component is TECraftingAccess)
						return ComponentType.CraftingAccess;

					if (component is TEDecraftingAccess)
						return ComponentType.DecraftingAccess;

					return ComponentType.StorageAccess;
				}

				if (component is TEStorageCenter) {
					if (component is TEStorageHeart)
						return ComponentType.StorageHeart;

					if (component is TERemoteAccess)
						return ComponentType.RemoteAccess;

					return ComponentType.ExternalStorageCenter;
				}

				if (component is TEStoragePoint) {
					if (component is TEEnvironmentAccess)
						return ComponentType.EnvironmentAccess;

					return ComponentType.ExternalStoragePoint;
				}

				// Failed to resolve the component to a definite type
				return ComponentType.ExternalStorageComponent;
			}

			public void Unlink(Point16 location) {
				using var debugging = DebugMessage.CreateIfAny(DebugControls.Names.StorageCenterComponentLinking);

				for (int i = _components.Count - 1; i >= 0; i--) {
					var component = _components[i];

					if (component.location == location) {
						if (debugging.IsDebugging) {
							debugging.Report(true, "Link between component and Center has been removed")
								.Indent()
								.Report(false, "Center: {0} at {1}", _center.FullName, _center.Position.DebugString())
								.Report(false, "Component: {0} at {1}", component.type, location.DebugString());
						}

						UnlinkAtIndex(i);
					}
				}
			}

			private void UnlinkAtIndex(int i) {
				if (_components[i].location.ResolveToTileEntity() is TEStorageComponent storageComponent) {
					storageComponent.Unlink();
					_center.OnDisconnectComponent(storageComponent);
					NetHelper.SendTEUpdate(storageComponent.ID);
				}

				_components.RemoveAt(i);

				MarkIndexAsResolved(i);
			}

			private List<Component> ResolveComponents() {
				using var debugging = DebugMessage.CreateIf(DebugControls.Names.StorageCenterComponentLinking);

				if (_unresolvedComponents.Count > 0) {
					// There are still some components that need to be resolved
					List<int> toRemove = [];
					foreach (var index in _unresolvedComponents) {
						var component = _components[index];
						if (component.location.ResolveToTileEntity() is TEStorageComponent storageComponent) {
							if (debugging.IsDebugging) {
								debugging
									.Report(true, "Lazily resolved component {0}", storageComponent.FullName)
									.Indent()
									.Report(false, "Location: {0}", storageComponent.Position.DebugString())
									.Report(false, "Linked center: {0} at {1}", _center.FullName, _center.Position.DebugString())
									.Unindent();
							}

							Link(storageComponent);
							toRemove.Add(index);
						}
					}

					for (int k = toRemove.Count - 1; k >= 0; k--) {
						int index = toRemove[k];
						_components.RemoveAt(index);

						MarkIndexAsResolved(index);
					}
				}

				return _components;
			}

			private void DeferLinking(Point16 location) {
				_unresolvedComponents.Add(_components.Count);
				_components.Add(new Component(location, ComponentType.DeferredLoad));
			}

			private void MarkIndexAsResolved(int i) {
				if (_unresolvedComponents.Remove(i) && _unresolvedComponents.Count > 0) {
					// Shift all unresolved component indices after this one down by 1
					List<int> toAdjust = [];
					foreach (var index in _unresolvedComponents) {
						if (index > i)
							toAdjust.Add(index);
					}

					foreach (var index in toAdjust) {
						_unresolvedComponents.Remove(index);
						_unresolvedComponents.Add(index - 1);
					}
				}
			}

			public void CheckForRemovedEntities() {
				using var debugging = DebugMessage.CreateIf(DebugControls.Names.StorageCenterComponentLinking);

				List<int> toRemove = new();
				for (int i = _components.Count - 1; i >= 0; i--) {
					Component component = _components[i];

					if (component.type == ComponentType.DeferredLoad)
						continue;

					if (component.location.ResolveToTileEntity() is not TEStorageComponent storageComponent) {
						if (debugging.IsDebugging)
							debugging.Report(true, "Component at {0} no longer exists, unlinking", component.location.DebugString());

						toRemove.Add(i);
						continue;
					}

					if (component.type != GetComponentType(storageComponent)) {
						if (debugging.IsDebugging)
							debugging.Report(true, "Component at {0} had an outdated classification, unlinking", component.location.DebugString());

						toRemove.Add(i);
						continue;
					}

					if (storageComponent.StorageCenter != _center.Position) {
						if (debugging.IsDebugging)
							debugging.Report(true, "Component at {0} has a stale linking reference, unlinking", component.location.DebugString());

						toRemove.Add(i);
						continue;
					}
				}

				foreach (int index in toRemove)
					UnlinkAtIndex(index);
			}

			#region GetComponents
			private IEnumerable<Point16> GetComponents<T>()
				where T : ITypeResolver<T>
			{
				return ResolveComponents().Where(ITypeResolver<T>.Matches).Select(static c => c.location);
			}

			private IEnumerable<Point16> GetComponents<T1, T2>()
				where T1 : ITypeResolver<T1>
				where T2 : ITypeResolver<T2>
			{
				return ResolveComponents().Where(static c => ITypeResolver<T1>.Matches(c) || ITypeResolver<T2>.Matches(c)).Select(static c => c.location);
			}

			private IEnumerable<Point16> GetComponents<T1, T2, T3>()
				where T1 : ITypeResolver<T1>
				where T2 : ITypeResolver<T2>
				where T3 : ITypeResolver<T3>
			{
				return ResolveComponents().Where(static c => ITypeResolver<T1>.Matches(c) || ITypeResolver<T2>.Matches(c) || ITypeResolver<T3>.Matches(c)).Select(static c => c.location);
			}

			private IEnumerable<Point16> GetComponentsExcept<T1, T2>()
				where T1 : ITypeResolver<T1>
				where T2 : ITypeResolver<T2>
			{
				return ResolveComponents().Where(static c => !ITypeResolver<T1>.Matches(c) && !ITypeResolver<T2>.Matches(c)).Select(static c => c.location);
			}

			private IEnumerable<Point16> GetComponentsExcept<T1, T2, T3, T4>()
				where T1 : ITypeResolver<T1>
				where T2 : ITypeResolver<T2>
				where T3 : ITypeResolver<T3>
				where T4 : ITypeResolver<T4>
			{
				return ResolveComponents().Where(static c => !ITypeResolver<T1>.Matches(c) && !ITypeResolver<T2>.Matches(c) && !ITypeResolver<T3>.Matches(c) && !ITypeResolver<T4>.Matches(c)).Select(static c => c.location);
			}
			#endregion

			public IEnumerable<Point16> GetStorageUnits() => GetComponents<StorageUnitResolver>();

			public IEnumerable<TEAbstractStorageUnit> GetStorageUnitEntities() => GetStorageUnits().ResolveTileEntities<TEAbstractStorageUnit>();

			public IEnumerable<TEStorageUnit> GetRealStorageUnitEntities() => GetStorageUnits().ResolveTileEntities<TEStorageUnit>();

			public IEnumerable<Point16> GetStorageAccesses() => GetComponents<StorageAccessResolver>();

			public IEnumerable<TEStorageAccess> GetStorageAccessEntities() => GetStorageAccesses().ResolveTileEntities<TEStorageAccess>();

			public IEnumerable<Point16> GetCraftingAccesses() => GetComponents<CraftingAccessResolver>();

			public IEnumerable<TECraftingAccess> GetCraftingAccessEntities() => GetCraftingAccesses().ResolveTileEntities<TECraftingAccess>();

			public IEnumerable<Point16> GetEnvironmentAccesses() => GetComponents<EnvironmentAccessResolver>();

			public IEnumerable<TEEnvironmentAccess> GetEnvironmentAccessEntities() => GetEnvironmentAccesses().ResolveTileEntities<TEEnvironmentAccess>();

			public IEnumerable<Point16> GetRemoteAccesses() => GetComponents<RemoteAccessResolver>();

			public IEnumerable<TERemoteAccess> GetRemoteAccessEntities() => GetRemoteAccesses().ResolveTileEntities<TERemoteAccess>();

			public IEnumerable<Point16> GetDecraftingAccesses() => GetComponents<DecraftingAccessResolver>();

			public IEnumerable<TEDecraftingAccess> GetDecraftingAccessEntities() => GetDecraftingAccesses().ResolveTileEntities<TEDecraftingAccess>();

			public IEnumerable<Point16> GetStoragePoints() => GetComponents<EnvironmentAccessResolver, ExternalStoragePointResolver>();

			public IEnumerable<TEStoragePoint> GetStoragePointEntities() => GetStoragePoints().ResolveTileEntities<TEStoragePoint>();

			public IEnumerable<Point16> GetStorageCenters() => GetComponents<RemoteAccessResolver, ExternalStorageCenterResolver>();

			public IEnumerable<TEStorageCenter> GetStorageCenterEntities() => GetStorageCenters().ResolveTileEntities<TEStorageCenter>();

			public IEnumerable<Point16> GetMiscellaneousComponents() => GetComponents<ExternalStorageComponentResolver, ExternalStoragePointResolver, ExternalStorageCenterResolver>();

			public IEnumerable<TEStorageComponent> GetMiscellaneousComponentEntities() => GetMiscellaneousComponents().ResolveTileEntities<TEStorageComponent>();

			public IEnumerable<Point16> GetAllComponents() => GetComponentsExcept<UnknownResolver, DeferredLoadResolver>();

			public IEnumerable<TEStorageComponent> GetAllComponentEntities() => GetAllComponents().ResolveTileEntities<TEStorageComponent>();

			public IEnumerable<Point16> GetDirectlyConnectableComponents() => GetComponentsExcept<RemoteAccessResolver, ExternalStorageCenterResolver, UnknownResolver, DeferredLoadResolver>();

			public IEnumerable<TEStorageComponent> GetDirectlyConnectableComponentEntities() => GetDirectlyConnectableComponents().ResolveTileEntities<TEStorageComponent>();

			public TEStorageHeart GetStorageHeart() {
				// FIX: v0.7.0.5 - Components attached to a Remote Access try to get the heart through the Remote Access, but that would fail
				ResolveComponents();

				if (_foundHeart.ResolveToTileEntity() is TEStorageHeart heart) {
					heart.ComponentManager.LinkIfNotExists(_center);
					heart.ComponentManager.ResolveComponents();
					return heart;
				}

				// IMPORTANT: RemoteAccess -> StorageHeart may be an actual link, but the StorageHeart may not be loaded yet.
				//            Hence, keep the connection "alive" on clients since it could be loaded later.
				if (Main.netMode != NetmodeID.MultiplayerClient)
					_foundHeart = Point16.NegativeOne;

				return null;
			}

			public void Serialize(BinaryWriter writer) {
				if (_center is not TEStorageHeart)
					writer.Write(_foundHeart);

				writer.Write(_components.Count);
				foreach (Component component in _components)
					writer.Write(component.location);

				using var debugging = DebugMessage.CreateIf(DebugControls.Names.StorageCenterManagerData);

				if (debugging.IsDebugging)
					debugging.Report(true, "Serialized {0} components for " + nameof(TEStorageCenter) + "." + nameof(ConnectedComponentManager), _components.Count);
			}

			public void Deserialize(BinaryReader reader) {
				Reset();

				// FIX: v0.7.0.5 - Assume that the read coordinate is the heart, and defer linking it
				if (_center is not TEStorageHeart) {
					var location = reader.ReadPoint16();

					if (location != Point16.NegativeOne)
						DeferLinking(location);
				}

				int count = reader.ReadInt32();

				using var debugging = DebugMessage.CreateIf(DebugControls.Names.StorageCenterManagerData);

				if (debugging.IsDebugging) {
					debugging
						.Report(true, "Deserializing {0} components for " + nameof(TEStorageCenter) + "." + nameof(ConnectedComponentManager), count)
						.Indent();
				}

				for (int k = 0; k < count; k++) {
					Point16 loc = reader.ReadPoint16();
					if (loc.ResolveToTileEntity() is TileEntity te) {
						if (te is TEStorageComponent component) {
							// The location was valid
							Link(component);
						} else {
							// The location was invalid or is a component that's no longer loaded
							if (debugging.IsDebugging) {
								debugging
									.Report(false, "Entity at location {0} was not a TEStorageComponent", loc.DebugString())
									.Indent()
									.Report(false, "Name: {0}", te is ModTileEntity mte ? mte.FullName : te.GetType().Name)
									.Unindent();
							}

							_components.Add(new Component(loc, ComponentType.Unknown));
						}
					} else {
						if (debugging.IsDebugging)
							debugging.Report(false, "Entity at location {0} could not be found, delaying loading until it can be resolved", loc.DebugString());

						DeferLinking(loc);
					}
				}
			}

			public void Save(TagCompound tag) {
				TagCompound data = new TagCompound() {
					["locations"] = _components.Select(static c => c.location).ToList()
				};

				if (_center is not TEStorageHeart && _foundHeart != Point16.NegativeOne)
					data["heart"] = _foundHeart;

				tag["components"] = data;
			}

			public void Load(TagCompound tag) {
				Reset();

				if (tag.TryGet("components", out TagCompound data)) {
					foreach (Point16 loc in data.GetList<Point16>("components")) {
						if (loc.ResolveToTileEntity() is TileEntity te) {
							if (te is TEStorageComponent component)
								Link(component);
							else
								_components.Add(new Component(loc, ComponentType.Unknown));
						} else if (loc != Point16.NegativeOne)
							DeferLinking(loc);
					}

					// FIX: v0.7.0.5 - Assume that the read coordinate is the heart, and defer linking it
					if (_center is not TEStorageHeart && data.TryGet("heart", out Point16 location) && location != Point16.NegativeOne)
						DeferLinking(location);
				}
			}
		}

		[Obsolete("Use ComponentManager.GetStorageUnits() instead", true)]
		public List<Point16> storageUnits = new();
		[Obsolete]
		internal List<Point16> Obsolete_storageUnits() => storageUnits;

		private ConnectedComponentManager _manager;

		public ConnectedComponentManager ComponentManager => _manager ??= new(this);

		public override TEStorageHeart GetHeart() => ComponentManager.GetStorageHeart();

		public override Point16 StorageCenter {
			get => ComponentManager.StorageCenter;
			set => throw new NotSupportedException(nameof(TEStorageCenter) + "." + nameof(StorageCenter) + " does not support value assignment.");
		}

		private int eatingWaitDuration = -1;

		public void AskToEatItem(int duration) {
			duration += 10;

			if (eatingWaitDuration < duration)
				eatingWaitDuration = duration;
		}

		internal void UpdateItemEatingTime() {
			if (eatingWaitDuration >= 0) {
				if (eatingWaitDuration == 0)
					SoundEngine.PlaySound(SoundID.Grab, Position.ToWorldCoordinates(16, 16));

				eatingWaitDuration--;
			}
		}

		public override void Update() {
			base.Update();

			ComponentManager.CheckForRemovedEntities();

			// Ensure network assignments are up to date
			// NOTE:  For TEStorageHeart and TERemoteAccess, Heart will update Remote in one tick, then Remote will update its components in the next tick
			foreach (var component in ComponentManager.GetAllComponentEntities()) {
				if (component.assignedNetwork != assignedNetwork) {
					component.assignedNetwork = assignedNetwork;
					NetHelper.SyncStorageComponentNetwork(component);
				}
			}
		}

		private void CheckMapSections() {
			//Force a map section send for each unique map section that has one of this storage center's storage units
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				foreach (Point16 unit in ComponentManager.GetAllComponents().DistinctBy(p => new Point16(Netplay.GetSectionX(p.X), Netplay.GetSectionY(p.Y))))
					NetHelper.ClientRequestSection(unit);
			}
		}

		public void ResetAndSearch()
		{
			using var debugging = DebugMessage.CreateIf(DebugControls.Names.StorageNetworkRecalculate);

			ConnectedComponentManager manager = ComponentManager;

			// FIX: v0.7.0.8 - GetAllComponents() may contain RemoteAccess components, which can't be linked directly.  This caused them to always be unlinked when ResetAndSearch() was called.
			List<Point16> oldComponents = manager.GetDirectlyConnectableComponents().ToList();
			// FIX: v0.7.1 - Calling ResetAndSearch() on a Storage Heart would result in Remote Accesses briefly losing connection
			List<Point16> remoteAccess = manager.GetRemoteAccesses().ToList();
			TEStorageHeart assignedHeart = manager.GetStorageHeart();

			if (debugging.IsDebugging) {
				debugging
					.Report(true, "TEStorageCenter.ResetAndSearch(): Scanning for connected components...")
					.Indent()
					.Report(false, "Context:")
					.Indent()
					.Report(false, "Invoking entity: {0}", FullName)
					.Report(false, "Pre-recaculate component count: {0}", manager.Count)
					.Unindent();
			}

			CheckMapSections();

			// NOTE: For Storage Hearts, this call causes Remote Accesses to be unlinked until they try to access their Storage Heart.
			//       Their connections will need to be manually reapplied after the directly-connected components have been found.
			manager.Reset();

			NetHelper.StartUpdateQueue();

			HashSet<Point16> hashComponents = new();

			TileScanSettings scanSettings = new() {
				InvokingActor = this
			};

			foreach (var component in TileNetworkScanner.ScanComponents(Position, scanSettings)) {
				manager.Link(component);
				hashComponents.Add(component.Position);

				if (debugging.IsDebugging)
					debugging.Report(false, "Found component {0} at {1}", component.FullName, component.Position.DebugString());
			}

			foreach (Point16 oldComponent in oldComponents)
			{
				if (!hashComponents.Contains(oldComponent))
				{
					if (oldComponent.ResolveToTileEntity() is TEStorageComponent storageUnit)
						manager.Unlink(oldComponent);
				}
			}

			// Restore the link to any Remote Accesses
			foreach (Point16 access in remoteAccess)
				manager.LinkRemoteAccess(access);

			// Restore the link to the Heart
			if (assignedHeart is not null)
				manager.Link(assignedHeart);

			if (debugging.IsDebugging) {
				debugging
					.Unindent()
					.Report(true, "Scanning has completed.  New component count: " + manager.Count);
			}

			TEStorageHeart heart = GetHeart();
			heart?.ResetCompactStage();
			NetHelper.SendTEUpdate(ID);

			if (heart is not null)
				NetHelper.SendTEUpdate(heart.ID);

			NetHelper.ProcessUpdateQueue();
		}

		protected virtual void OnConnectComponent(TEStorageComponent component) {
			if (component is TEAbstractStorageUnit)
				Obsolete_storageUnits().Add(component.Position);
		}

		protected virtual void OnDisconnectComponent(TEStorageComponent component) {
			if (component is TEAbstractStorageUnit)
				Obsolete_storageUnits().Remove(component.Position);
		}

		public override void OnPlace()
		{
			ResetAndSearch();
		}

		public override void OnKill()
		{
			ConnectedComponentManager manager = ComponentManager;

			NetHelper.StartUpdateQueue();

			foreach (var component in manager.GetAllComponentEntities().ToList())
			{
				manager.Unlink(component.Position);
				NetHelper.SendTEUpdate(component.ID);
			}

			NetHelper.ProcessUpdateQueue();

			manager.Reset();
		}

		public static bool IsStorageCenter(Point16 point) => ByPosition.TryGetValue(point, out TileEntity te) && te is TEStorageCenter;

		public static bool HeartsMatch(Point16 center, Point16 heart) {
			return center.ResolveToTileEntity<TEStorageCenter>()?.GetHeart() is TEStorageHeart heartEntity && heartEntity.Position == heart;
		}

		public override void SaveData(TagCompound tag)
		{
			base.SaveData(tag);

			TagCompound networkStuff = new();
			ComponentManager.Save(networkStuff);
			tag["networkMeta"] = networkStuff;  // IMPORTANT: base uses "network" tag!

			// FIX: v0.7.0.3 - Restore legacy data for backwards compatibility
			var storageUnits = Obsolete_storageUnits();
			if (storageUnits.Count > 0) {
				List<TagCompound> tags = [];

				foreach (Point16 unit in storageUnits) {
					tags.Add(new TagCompound() {
						["X"] = unit.X,
						["Y"] = unit.Y
					});
				}

				tag["StorageUnits"] = tags;
			}
		}

		public override void LoadData(TagCompound tag)
		{
			base.LoadData(tag);

			// NOTE: Load resets the manager's collection
			ConnectedComponentManager manager = ComponentManager;

			if (tag.TryGet("networkMeta", out TagCompound networkStuff))  // IMPORTANT: base uses "network" tag!
				manager.Load(networkStuff);

			// Legacy data
			foreach (TagCompound tagUnit in tag.GetList<TagCompound>("StorageUnits"))
				manager.LinkStorageUnit(new Point16(tagUnit.GetShort("X"), tagUnit.GetShort("Y")));
		}

		public override void NetSend(BinaryWriter writer)
		{
			base.NetSend(writer);

			ComponentManager.Serialize(writer);
		}

		public override void NetReceive(BinaryReader reader)
		{
			base.NetReceive(reader);

			ComponentManager.Deserialize(reader);

			CheckMapSections();
		}
	}
}
