using System.Collections.Generic;

namespace MagicStorage.Common.Threading.Refreshing {
	public interface IMainZoneObjectResultsProvider<T> {
		MainZoneObjectResults<T> MainZoneObjectsResults { get; }
	}

	public class MainZoneObjectResults<T> {
		public T[] objectsToRefresh;
		public readonly ListProvider<T> objects;
		public readonly ListProvider<bool> objectIsAvailable;

		public MainZoneObjectResults(IEnumerable<T> objectsToRefresh, List<T> staticObjectList, List<bool> staticAvailableList) {
			this.objectsToRefresh = objectsToRefresh is null ? [] : [.. objectsToRefresh];
			objects = new ListProvider<T>(staticObjectList);
			objectIsAvailable = new ListProvider<bool>(staticAvailableList);
		}

		public void CollectObjects() {
			objects.Clear();
			objectIsAvailable.Clear();

			if (objectsToRefresh is { Length: > 0 }) {
				// The current list will be manipulated, so cache them here
				CopyFromStaticCollections();
			}
		}

		public void CopyFromStaticCollections() {
			objects.CopyFromStatic();
			objectIsAvailable.CopyFromStatic();
		}

		public void CopyToStaticCollections() {
			objects.OverwriteStatic();
			objectIsAvailable.OverwriteStatic();
		}

		public void ClearStaticCollections() {
			objects.ClearStatic();
			objectIsAvailable.ClearStatic();
		}

		public IEnumerable<(T, bool)> Enumerate() {
			for (int i = 0; i < objects.Count; i++)
				yield return (objects[i], objectIsAvailable[i]);
		}
	}
}
