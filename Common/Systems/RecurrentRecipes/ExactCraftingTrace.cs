using System.Collections.Generic;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	internal sealed class ExactCraftingTrace {
		private const int MaxEvents = 160;
		private readonly List<string> events = new();
		private int droppedEvents;

		public bool HasEvents => events.Count > 0 || droppedEvents > 0;

		public void Add(int depth, string message) {
			if (events.Count >= MaxEvents) {
				droppedEvents++;
				return;
			}

			events.Add($"{new string(' ', depth * 2)}{message}");
		}

		public IEnumerable<string> EnumerateLines() {
			foreach (string line in events)
				yield return line;

			if (droppedEvents > 0)
				yield return $"... dropped {droppedEvents} exact trace events";
		}
	}
}
