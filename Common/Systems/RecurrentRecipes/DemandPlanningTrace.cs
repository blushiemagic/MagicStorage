using System.Collections.Generic;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	internal sealed class DemandPlanningTrace {
		private const int MaxEvents = 120;
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
				yield return $"... dropped {droppedEvents} planner trace events";
		}

		public static string DescribeRecipe(Recipe recipe) {
			if (recipe is null)
				return "recipe:null";

			Item result = recipe.createItem;
			return $"#{recipe.RecipeIndex} {DescribeItem(result?.type ?? 0)} x{result?.stack ?? 0}";
		}

		public static string DescribeItem(int itemType) {
			if (itemType <= 0)
				return $"item:{itemType}";

			string name = Lang.GetItemNameValue(itemType);
			return string.IsNullOrWhiteSpace(name) ? $"item:{itemType}" : $"{name}({itemType})";
		}
	}
}
