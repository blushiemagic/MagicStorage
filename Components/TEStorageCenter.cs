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
				Unknown
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
			private TEStorageHeart _foundHeart;

			private readonly Dictionary<ComponentType, HashSet<Point16>> _knownComponentLocationCache = new();

			public int Count => _components.Count;

			public Point16 StorageCenter => _center.Position;

			public ConnectedComponentManager(TEStorageCenter center) {
				_center = center;
				if (center is TEStorageHeart heart)
					_foundHeart = heart;
			}

			internal void Reset() {
				_components.Clear();
				_foundHeart = null;
				_knownComponentLocationCache.Clear();
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
				if (component is TEStorageHeart heart) {
					// Don't link storage hearts to storage storage hearts
					if (_center is TEStorageHeart)
						return;
					else if (_foundHeart is null)
						_foundHeart = heart;
					else {
						// Normally, I'd throw an exception here, but I'll just have the logic silently return instead
						return;
					}
				}

				ComponentType type = GetComponentType(component);

				if (!_knownComponentLocationCache.TryGetValue(type, out var set))
					_knownComponentLocationCache[type] = set = new HashSet<Point16>();
				set.Add(component.Position);

				_components.Add(new Component(component.Position, type));
				component.Link(_center.Position);
				NetHelper.SendTEUpdate(component.ID, component.Position);
			}

			internal void LinkStorageUnit(Point16 location) {
				_components.Add(new Component(location, ComponentType.StorageUnit));
			}

			internal void LinkRemoteAccess(Point16 location) {
				_components.Add(new Component(location, ComponentType.RemoteAccess));
			}

			internal void LinkEnvironmentAccess(Point16 location) {
				_components.Add(new Component(location, ComponentType.EnvironmentAccess));
			}

			private static ComponentType GetComponentType(TEStorageComponent component) {
				return component switch {
					TEAbstractStorageUnit => ComponentType.StorageUnit,
					// TECraftingAccess inherits from TEStorageAccess, so it must be checked first
					TECraftingAccess => ComponentType.CraftingAccess,
					TEStorageAccess => ComponentType.StorageAccess,
					TEEnvironmentAccess => ComponentType.EnvironmentAccess,
					TERemoteAccess => ComponentType.RemoteAccess,
					_ => ComponentType.Unknown
				};
			}

			public void Unlink(Point16 location) {
				List<Component> components = _components.Where(c => c.location == location).ToList();

				foreach (Component component in components) {
					_components.Remove(component);

					if (ByPosition.TryGetValue(component.location, out TileEntity te) && te is TEStorageComponent storageComponent) {
						storageComponent.Unlink();
						NetHelper.SendTEUpdate(storageComponent.ID, storageComponent.Position);
					}
				}
			}

			public IEnumerable<Point16> GetStorageUnits() => _components.Where(static c => c.type == ComponentType.StorageUnit).Select(static c => c.location);

			public IEnumerable<TEAbstractStorageUnit> GetStorageUnitEntities() => GetStorageUnits().Select(static p => ByPosition.TryGetValue(p, out TileEntity te) ? te : null).OfType<TEAbstractStorageUnit>();

			public IEnumerable<TEStorageUnit> GetRealStorageUnitEntities() => GetStorageUnits().Select(static p => ByPosition.TryGetValue(p, out TileEntity te) ? te : null).OfType<TEStorageUnit>();

			public IEnumerable<Point16> GetStorageAccesses() => _components.Where(static c => c.type == ComponentType.StorageAccess).Select(static c => c.location);

			public IEnumerable<TEStorageAccess> GetStorageAccessEntities() => GetStorageAccesses().Select(static p => ByPosition.TryGetValue(p, out TileEntity te) ? te : null).OfType<TEStorageAccess>();

			public IEnumerable<Point16> GetCraftingAccesses() => _components.Where(static c => c.type == ComponentType.CraftingAccess).Select(static c => c.location);

			public IEnumerable<TECraftingAccess> GetCraftingAccessEntities() => GetCraftingAccesses().Select(static p => ByPosition.TryGetValue(p, out TileEntity te) ? te : null).OfType<TECraftingAccess>();

			public IEnumerable<Point16> GetEnvironmentAccesses() => _components.Where(static c => c.type == ComponentType.EnvironmentAccess).Select(static c => c.location);

			public IEnumerable<TEEnvironmentAccess> GetEnvironmentAccessEntities() => GetEnvironmentAccesses().Select(static p => ByPosition.TryGetValue(p, out TileEntity te) ? te : null).OfType<TEEnvironmentAccess>();

			public IEnumerable<Point16> GetRemoteAccesses() => _components.Where(static c => c.type == ComponentType.RemoteAccess).Select(static c => c.location);

			public IEnumerable<TERemoteAccess> GetRemoteAccessEntities() => GetRemoteAccesses().Select(static p => ByPosition.TryGetValue(p, out TileEntity te) ? te : null).OfType<TERemoteAccess>();

			public IEnumerable<Point16> GetDecraftingAccesses() => _components.Where(static c => c.type == ComponentType.DecraftingAccess).Select(static c => c.location);

			public IEnumerable<TEDecraftingAccess> GetDecraftingAccessEntities() => GetDecraftingAccesses().Select(static p => ByPosition.TryGetValue(p, out TileEntity te) ? te : null).OfType<TEDecraftingAccess>();

			public IEnumerable<Point16> GetMiscellaneousComponents() => _components.Where(static c => c.type == ComponentType.Unknown).Select(static c => c.location);

			public IEnumerable<TEStorageComponent> GetMiscellaneousComponentEntities() => GetMiscellaneousComponents().Select(static p => ByPosition.TryGetValue(p, out TileEntity te) ? te : null).OfType<TEStorageComponent>();

			public IEnumerable<Point16> GetAllComponents() => _components.Select(static c => c.location);

			public IEnumerable<TEStorageComponent> GetAllComponentEntities() => GetAllComponents().Select(static p => ByPosition.TryGetValue(p, out TileEntity te) ? te : null).OfType<TEStorageComponent>();

			public TEStorageHeart GetStorageHeart() => _foundHeart;

			public void CheckForRemovedEntities() {
				List<Point16> _toRemove = new();
				foreach (Component component in _components) {
					if (!ByPosition.TryGetValue(component.location, out TileEntity te) || te is not TEStorageComponent storageComponent || component.type != GetComponentType(storageComponent))
						_toRemove.Add(component.location);
				}

				foreach (Point16 point in _toRemove) {
					Point16 p = point;
					_components.RemoveAll(c => c.location == p);
				}
			}

			public void Serialize(BinaryWriter writer) {
				writer.Write(_components.Count);
				foreach (Component component in _components)
					writer.Write(component.location);
			}

			public void Deserialize(BinaryReader reader) {
				Reset();

				int count = reader.ReadInt32();
				for (int k = 0; k < count; k++) {
					Point16 loc = reader.ReadPoint16();
					if (ByPosition.TryGetValue(loc, out TileEntity te) && te is TEStorageComponent component)
						Link(component);
				}
			}

			public void Save(TagCompound tag) {
				TagCompound data = new TagCompound() {
					["locations"] = _components.Select(static c => c.location).ToList()
				};

				if (_center is not TEStorageHeart && _foundHeart is not null)
					data["heart"] = _foundHeart.Position;

				tag["components"] = data;
			}

			public void Load(TagCompound tag) {
				Reset();

				if (tag.TryGet("components", out TagCompound data)) {
					foreach (Point16 loc in data.GetList<Point16>("components")) {
						if (ByPosition.TryGetValue(loc, out TileEntity te) && te is TEStorageComponent component)
							Link(component);
					}

					if (_center is not TEStorageHeart && data.TryGet("heart", out Point16 location) && ByPosition.TryGetValue(location, out TileEntity te2) && te2 is TEStorageHeart heart)
						_foundHeart = heart;
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
			ComponentManager.CheckForRemovedEntities();
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

			NetHelper.Report(true, "TEStorageCenter.ResetAndSearch invoked.  Current component count: " + manager.Count);

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
					}

					foreach (Point16 point in AdjacentComponents(explore))
						toExplore.Enqueue(point);
				}
			}

			foreach (Point16 oldComponent in oldComponents)
				if (!hashComponents.Contains(oldComponent))
				{
					if (ByPosition.TryGetValue(oldComponent, out TileEntity te) && te is TEStorageComponent storageUnit)
					{
						storageUnit.Unlink();
						NetHelper.SendTEUpdate(storageUnit.ID, storageUnit.Position);
					}
				}

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

			foreach (Point16 storageUnit in manager.GetAllComponents())
			{
				if (!ByPosition.TryGetValue(storageUnit, out var te) || te is not TEStorageComponent component)
					continue;
				
				component.Unlink();
				NetHelper.SendTEUpdate(component.ID, component.Position);
			}

			manager.Reset();
		}

		public static bool IsStorageCenter(Point16 point) => ByPosition.TryGetValue(point, out TileEntity te) && te is TEStorageCenter;

		public static bool HeartsMatch(Point16 center, Point16 heart) {
			if (!TileEntity.ByPosition.TryGetValue(center, out TileEntity entity) || entity is not TEStorageCenter centerEntity)
				return false;

			return centerEntity.GetHeart()?.Position == heart;
		}

		public override void SaveData(TagCompound tag)
		{
			ComponentManager.Save(tag);
		}

		public override void LoadData(TagCompound tag)
		{
			// NOTE: Load resets the manager's collection
			ConnectedComponentManager manager = ComponentManager;

			manager.Load(tag);

			// Legacy data
			foreach (TagCompound tagUnit in tag.GetList<TagCompound>("StorageUnits"))
				manager.LinkStorageUnit(new Point16(tagUnit.GetShort("X"), tagUnit.GetShort("Y")));
		}

		public override void NetSend(BinaryWriter writer)
		{
			ComponentManager.Serialize(writer);
		}

		public override void NetReceive(BinaryReader reader)
		{
			ComponentManager.Deserialize(reader);

			CheckMapSections();
		}
	}
}
