using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
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
				Unknown,
				DeferredLoad  // In cases where attempting to use ByPosition won't work
			}

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
				NetHelper.Report(true, $"Attempting to link {component.FullName} at {component.Position} to Center ({_center.FullName}) at {_center.Position}");

				if (component is TEStorageHeart heart) {
					// Don't link storage hearts to storage storage hearts
					if (_center is TEStorageHeart) {
						NetHelper.Report(false, " -- FAILED: Storage Heart cannot link to another Storage Heart");
					} else if (_foundHeart == Point16.NegativeOne) {
						NetHelper.Report(false, " -- SUCCESS: Found Storage Heart at " + heart.Position);
						_foundHeart = heart.Position;
						_center.Link(heart.Position);
						heart.ComponentManager.LinkIfNotExists(_center);
					} else {
						// Normally, I'd throw an exception here, but I'll just have the logic silently return instead
						NetHelper.Report(false, " -- FAILED: Storage Heart already found at " + _foundHeart);
					}

					return;
				}

				if (component.StorageCenter != Point16.NegativeOne) {
					if (component.StorageCenter != _center.Position) {
						NetHelper.Report(false, $"Component has already been assigned to the Center at {component.StorageCenter}, unlinking...");

						if (component.StorageCenter.ResolveToTileEntity() is TEStorageCenter previousCenter)
							previousCenter.ComponentManager.Unlink(component.Position);
					} else
						NetHelper.Report(false, "Component save data has it linked to the Center, adding proper reference connections...");
				}

				ComponentType type = GetComponentType(component);

				if (!_knownComponentLocationCache.TryGetValue(type, out var set))
					_knownComponentLocationCache[type] = set = new HashSet<Point16>();
				set.Add(component.Position);

				_components.Add(new Component(component.Position, type));
				component.Link(_center.Position);

				NetHelper.Report(false, " -- SUCCESS: Component classification is " + type);

				if (_center is TEStorageHeart && component is TEStorageCenter otherCenter)
					otherCenter.ComponentManager.Link(_center);

				NetHelper.SendTEUpdate(component.ID, component.Position);
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
				return component switch {
					TEAbstractStorageUnit => ComponentType.StorageUnit,
					// TECraftingAccess inherits from TEStorageAccess, so it must be checked first
					TECraftingAccess => ComponentType.CraftingAccess,
					// TEDecraftingAccess inherits from TEStorageAccess, so it must be checked first
					TEDecraftingAccess => ComponentType.DecraftingAccess,
					TEStorageAccess => ComponentType.StorageAccess,
					TEEnvironmentAccess => ComponentType.EnvironmentAccess,
					TERemoteAccess => ComponentType.RemoteAccess,
					_ => ComponentType.Unknown
				};
			}

			public void Unlink(Point16 location) {
				for (int i = _components.Count - 1; i >= 0; i--) {
					var component = _components[i];

					if (component.location == location) {
						NetHelper.Report(true, $"Unlinking component {component.location} from Center {_center.FullName} at {_center.Position}");

						if (component.location.ResolveToTileEntity() is TEStorageComponent storageComponent) {
							storageComponent.Unlink();
							NetHelper.SendTEUpdate(storageComponent.ID, storageComponent.Position);
						}

						_components.RemoveAt(i);

						MarkIndexAsResolved(i);
					}
				}
			}

			private List<Component> ResolveComponents() {
				if (_unresolvedComponents.Count > 0) {
					// There are still some components that need to be resolved
					List<int> toRemove = [];
					foreach (var index in _unresolvedComponents) {
						var component = _components[index];
						if (component.location.ResolveToTileEntity() is TEStorageComponent storageComponent) {
							NetHelper.Report(true, $"Lazily resolved component {storageComponent.FullName} at {storageComponent.Position} for Center {_center.FullName} at {_center.Position}");

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

			public IEnumerable<Point16> GetStorageUnits() => ResolveComponents().Where(static c => c.type == ComponentType.StorageUnit).Select(static c => c.location);

			public IEnumerable<TEAbstractStorageUnit> GetStorageUnitEntities() => GetStorageUnits().ResolveTileEntities<TEAbstractStorageUnit>();

			public IEnumerable<TEStorageUnit> GetRealStorageUnitEntities() => GetStorageUnits().ResolveTileEntities<TEStorageUnit>();

			public IEnumerable<Point16> GetStorageAccesses() => ResolveComponents().Where(static c => c.type == ComponentType.StorageAccess).Select(static c => c.location);

			public IEnumerable<TEStorageAccess> GetStorageAccessEntities() => GetStorageAccesses().ResolveTileEntities<TEStorageAccess>();

			public IEnumerable<Point16> GetCraftingAccesses() => ResolveComponents().Where(static c => c.type == ComponentType.CraftingAccess).Select(static c => c.location);

			public IEnumerable<TECraftingAccess> GetCraftingAccessEntities() => GetCraftingAccesses().ResolveTileEntities<TECraftingAccess>();

			public IEnumerable<Point16> GetEnvironmentAccesses() => ResolveComponents().Where(static c => c.type == ComponentType.EnvironmentAccess).Select(static c => c.location);

			public IEnumerable<TEEnvironmentAccess> GetEnvironmentAccessEntities() => GetEnvironmentAccesses().ResolveTileEntities<TEEnvironmentAccess>();

			public IEnumerable<Point16> GetRemoteAccesses() => ResolveComponents().Where(static c => c.type == ComponentType.RemoteAccess).Select(static c => c.location);

			public IEnumerable<TERemoteAccess> GetRemoteAccessEntities() => GetRemoteAccesses().ResolveTileEntities<TERemoteAccess>();

			public IEnumerable<Point16> GetDecraftingAccesses() => ResolveComponents().Where(static c => c.type == ComponentType.DecraftingAccess).Select(static c => c.location);

			public IEnumerable<TEDecraftingAccess> GetDecraftingAccessEntities() => GetDecraftingAccesses().ResolveTileEntities<TEDecraftingAccess>();

			public IEnumerable<Point16> GetMiscellaneousComponents() => ResolveComponents().Where(static c => c.type == ComponentType.Unknown).Select(static c => c.location);

			public IEnumerable<TEStorageComponent> GetMiscellaneousComponentEntities() => GetMiscellaneousComponents().ResolveTileEntities<TEStorageComponent>();

			public IEnumerable<Point16> GetAllComponents() => ResolveComponents().Select(static c => c.location);

			public IEnumerable<TEStorageComponent> GetAllComponentEntities() => GetAllComponents().ResolveTileEntities<TEStorageComponent>();

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

			public void CheckForRemovedEntities() {
				List<Point16> toRemove = new();
				foreach (Component component in _components) {
					if (component.location.ResolveToTileEntity() is not TEStorageComponent storageComponent || component.type != GetComponentType(storageComponent) || storageComponent.StorageCenter != _center.Position)
						toRemove.Add(component.location);
				}

				foreach (Point16 location in toRemove)
					Unlink(location);
			}

			public void Serialize(BinaryWriter writer) {
				if (_center is not TEStorageHeart)
					writer.Write(_foundHeart);

				writer.Write(_components.Count);
				foreach (Component component in _components)
					writer.Write(component.location);

				NetHelper.Report(true, "ConnectedComponentManager.Serialize invoked.  Component count: " + _components.Count);
			}

			public void Deserialize(BinaryReader reader) {
				Reset();

				// FIX: v0.7.0.5 - Assume that the read coordinate is the heart, and defer linking it
				if (_center is not TEStorageHeart) {
					_foundHeart = reader.ReadPoint16();
					DeferLinking(_foundHeart);
				}

				int count = reader.ReadInt32();

				NetHelper.Report(true, "ConnectedComponentManager.Deserialize invoked.  Component count: " + count);

				for (int k = 0; k < count; k++) {
					Point16 loc = reader.ReadPoint16();
					if (loc.ResolveToTileEntity() is TileEntity te) {
						if (te is TEStorageComponent component)
							Link(component);
						else {
							NetHelper.Report(false, "Tile entity at location " + loc + " is not a TEStorageComponent");

							_components.Add(new Component(loc, ComponentType.Unknown));
						}
					} else {
						NetHelper.Report(false, "Tile entity at location " + loc + " could not be found");

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
						} else {
							_components.Add(new Component(loc, ComponentType.DeferredLoad));
							_unresolvedComponents.Add(_components.Count - 1);
						}
					}

					// FIX: v0.7.0.5 - Assume that the read coordinate is the heart, and defer linking it
					if (_center is not TEStorageHeart && data.TryGet("heart", out Point16 location))
						_foundHeart = location;
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
			set => throw new NotSupportedException();
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
			ConnectedComponentManager manager = ComponentManager;

			List<Point16> oldComponents = manager.GetAllComponents().ToList();
			TEStorageHeart assignedHeart = manager.GetStorageHeart();

			NetHelper.Report(true, $"TEStorageCenter.ResetAndSearch invoked for {FullName}.  Current component count: {manager.Count}");

			CheckMapSections();

			List<Point16> obsolete_storageUnits = Obsolete_storageUnits();

			manager.Reset();

			HashSet<Point16> hashComponents = new();
			HashSet<Point16> explored = new()
			{
				Position
			};
			Queue<Point16> toExplore = new();
			foreach (Point16 point in AdjacentComponents())
				toExplore.Enqueue(point);

			NetHelper.StartUpdateQueue();

			while (toExplore.Count > 0)
			{
				Point16 explore = toExplore.Dequeue();
				if (!explored.Contains(explore) && explore != StorageComponent.killTile)
				{
					explored.Add(explore);
					if (ByPosition.TryGetValue(explore, out TileEntity te) && te is TEStorageComponent component)
					{
						manager.Link(component);

						if (te is TEAbstractStorageUnit)
							obsolete_storageUnits.Add(explore);
						hashComponents.Add(explore);

						OnConnectComponent(component);

						NetHelper.Report(false, $" -- Found component {component.FullName} at {explore}");
					}

					foreach (Point16 point in AdjacentComponents(explore))
						toExplore.Enqueue(point);
				}
			}

			foreach (Point16 oldComponent in oldComponents)
			{
				if (!hashComponents.Contains(oldComponent))
				{
					if (oldComponent.ResolveToTileEntity() is TEStorageComponent storageUnit)
						manager.Unlink(oldComponent);
				}
			}

			// Restore the link to the Heart
			if (assignedHeart is not null)
				manager.Link(assignedHeart);

			NetHelper.Report(true, "TEStorageCenter.ResetAndSearch finished.  New component count: " + manager.Count);

			TEStorageHeart heart = GetHeart();
			heart?.ResetCompactStage();
			NetHelper.SendTEUpdate(ID, Position);

			if (heart is not null)
				NetHelper.SendTEUpdate(heart.ID, heart.Position);

			NetHelper.ProcessUpdateQueue();
		}

		protected virtual void OnConnectComponent(TEStorageComponent component) { }

		public override void OnPlace()
		{
			ResetAndSearch();
		}

		public override void OnKill()
		{
			ConnectedComponentManager manager = ComponentManager;

			foreach (var component in manager.GetAllComponentEntities())
			{
				manager.Unlink(component.Position);
				NetHelper.SendTEUpdate(component.ID, component.Position);
			}

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
