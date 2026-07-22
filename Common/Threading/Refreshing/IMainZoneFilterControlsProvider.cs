using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader.Config;

namespace MagicStorage.Common.Threading.Refreshing {
	public interface IMainZoneFilterControlsProvider {
		MainZoneObjectsFilterControls MainZoneObjectsFilterControls { get; }
	}

	public interface IMainZoneFilterControlsProvider<T> : IMainZoneFilterControlsProvider {
		MainZoneObjectsFilterControls IMainZoneFilterControlsProvider.MainZoneObjectsFilterControls => MainZoneObjectsFilterControls;

		MainZoneObjectsFilterControls<T> MainZoneObjectsFilterControls { get; }
	}

	public abstract class MainZoneObjectsFilterControls {
		public bool[] adjTiles;
		public readonly ItemTypeOrderedSet favoritedTypes;
		public readonly ItemTypeOrderedSet hiddenTypes;
		public readonly HashSet<int> globalHiddenTypes;
		public readonly int zoneObjectFilterChoice;
		public int AdjTilesHash { get; private set; }

		public MainZoneObjectsFilterControls(
			int zoneObjectFilterChoice,
			ItemTypeOrderedSet favorited,
			ItemTypeOrderedSet hidden,
			HashSet<ItemDefinition> configBlacklist
		) {
			favoritedTypes = favorited.Clone();
			hiddenTypes = hidden.Clone();
			globalHiddenTypes = [.. configBlacklist.Where(x => !x.IsUnloaded).Select(x => x.Type)];
			this.zoneObjectFilterChoice = zoneObjectFilterChoice;
		}

		public bool IsFavorited(int item) => favoritedTypes.Contains(item);

		public bool HasHiddenObjects() => globalHiddenTypes.Count > 0 || hiddenTypes.Count > 0;

		public bool IsHidden(int item) => globalHiddenTypes.Contains(item) || hiddenTypes.Contains(item);

		public void SetAdjTiles(bool[] value) {
			adjTiles = value;
			AdjTilesHash = GetTrueIndicesHash(value);
		}

		private static int GetTrueIndicesHash(bool[] values) {
			if (values is null)
				return 0;

			var hash = new System.HashCode();
			hash.Add(values.Length);

			for (int i = 0; i < values.Length; i++) {
				if (values[i])
					hash.Add(i);
			}

			return hash.ToHashCode();
		}
	}

	public class MainZoneObjectsFilterControls<T> : MainZoneObjectsFilterControls {
			
		public IFilterProvider<T> filterProvider;

		public MainZoneObjectsFilterControls(
			int zoneObjectFilterChoice,
			ItemTypeOrderedSet favorited,
			ItemTypeOrderedSet hidden,
			HashSet<ItemDefinition> configBlacklist
		) : base(
			zoneObjectFilterChoice,
			favorited,
			hidden,
			configBlacklist
		) {
		}

		public bool IsFavorited(T item) => filterProvider.IsFavorited(item);

		public bool IsHidden(T item) => filterProvider.IsHidden(item);
	}
}
