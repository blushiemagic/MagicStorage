using System;
using System.Diagnostics;

namespace MagicStorage {
	partial class CraftingGUI {
		public interface ICraftingRefreshTimingProvider {
			CraftingRefreshTiming RefreshTiming { get; }
		}

		public sealed class CraftingRefreshTiming {
			private long graphTicks;
			private long listAvailabilityTicks;
			private long selectedPreviewTicks;
			private long selectedSimulationTicks;
			private long storedItemsTicks;
			private int graphBuilds;
			private int listQueries;
			private int selectedPreviewRestoreHits;
			private int selectedPreviewRestoreMisses;
			private int selectedPreviewStores;
			private int selectedSimulationRuns;

			public T Measure<T>(CraftingRefreshTimingPhase phase, Func<T> action) {
				long start = Stopwatch.GetTimestamp();
				try {
					return action();
				} finally {
					AddTicks(phase, Stopwatch.GetTimestamp() - start);
				}
			}

			public void Measure(CraftingRefreshTimingPhase phase, Action action) {
				long start = Stopwatch.GetTimestamp();
				try {
					action();
				} finally {
					AddTicks(phase, Stopwatch.GetTimestamp() - start);
				}
			}

			public void CountGraphBuild() => graphBuilds++;
			public void CountListQueries(int count) => listQueries += count;
			public void CountSelectedPreviewRestore(bool hit) {
				if (hit)
					selectedPreviewRestoreHits++;
				else
					selectedPreviewRestoreMisses++;
			}
			public void CountSelectedPreviewStore() => selectedPreviewStores++;
			public void CountSelectedSimulationRun() => selectedSimulationRuns++;

			public void Report(string label) {
				if (graphTicks == 0
				&& listAvailabilityTicks == 0
				&& selectedPreviewTicks == 0
				&& selectedSimulationTicks == 0
				&& storedItemsTicks == 0)
					return;

				NetHelper.Report(false,
					$"Crafting refresh timing ({label}): graph={ToMilliseconds(graphTicks)}ms/{graphBuilds}, " +
					$"list={ToMilliseconds(listAvailabilityTicks)}ms/{listQueries}, " +
					$"selectedPreview={ToMilliseconds(selectedPreviewTicks)}ms hit={selectedPreviewRestoreHits} miss={selectedPreviewRestoreMisses} store={selectedPreviewStores}, " +
					$"selectedSimulation={ToMilliseconds(selectedSimulationTicks)}ms/{selectedSimulationRuns}, " +
					$"storedItems={ToMilliseconds(storedItemsTicks)}ms");
			}

			private void AddTicks(CraftingRefreshTimingPhase phase, long ticks) {
				switch (phase) {
					case CraftingRefreshTimingPhase.GraphBuild:
						graphTicks += ticks;
						break;
					case CraftingRefreshTimingPhase.ListAvailability:
						listAvailabilityTicks += ticks;
						break;
					case CraftingRefreshTimingPhase.SelectedPreviewCache:
						selectedPreviewTicks += ticks;
						break;
					case CraftingRefreshTimingPhase.SelectedSimulation:
						selectedSimulationTicks += ticks;
						break;
					case CraftingRefreshTimingPhase.StoredItems:
						storedItemsTicks += ticks;
						break;
				}
			}

			private static long ToMilliseconds(long ticks) => (long)(ticks * 1000.0 / Stopwatch.Frequency);
		}

		public enum CraftingRefreshTimingPhase {
			GraphBuild,
			ListAvailability,
			SelectedPreviewCache,
			SelectedSimulation,
			StoredItems
		}

		private static CraftingRefreshTiming GetRefreshTiming<T>(T thread)
			=> thread is ICraftingRefreshTimingProvider provider ? provider.RefreshTiming : null;
	}
}
