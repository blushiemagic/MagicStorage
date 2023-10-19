using MagicStorage.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace MagicStorage.CrossMod {
	public static class ExtraCraftItemsSystem {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				_directRecipeItems.Clear();
				_conditionalRecipeItems.Clear();
			}
		}

		private readonly struct ConditionalItemDrop {
			public readonly Func<Recipe, bool> condition;
			public readonly IItemDropRule drop;

			public ConditionalItemDrop(Func<Recipe, bool> condition, IItemDropRule drop) {
				this.condition = condition;
				this.drop = drop;
			}
		}

		private static readonly ConditionalWeakTable<Recipe, List<IItemDropRule>> _directRecipeItems = new();
		private static readonly List<ConditionalItemDrop> _conditionalRecipeItems = new();

		/// <summary>
		/// Registers an excess item drop that is directly related to crafting a specific recipe
		/// </summary>
		/// <param name="recipe">The recipe that would be crafted</param>
		/// <param name="rule">The drop rule</param>
		public static void RegisterDrop(Recipe recipe, IItemDropRule rule) {
			ArgumentNullException.ThrowIfNull(recipe);

			if (!_directRecipeItems.TryGetValue(recipe, out var items))
				_directRecipeItems.Add(recipe, items = new());

			items.Add(rule);
		}

		/// <summary>
		/// Registers an excess item drop that is indirectly related to crafting any recipe that satisfies the condition
		/// </summary>
		/// <param name="condition">A delegate that returns whether a recipe can be used to drop <paramref name="rule"/></param>
		/// <param name="rule">The drop rule</param>
		public static void RegisterDrop(Func<Recipe, bool> condition, IItemDropRule rule) {
			ArgumentNullException.ThrowIfNull(condition);

			_conditionalRecipeItems.Add(new ConditionalItemDrop(condition, rule));
		}

		public static List<Item> GetSimulatedItemDrops(Recipe recipe) {
			List<Item> droppedItems = new();

			using (FlagSwitch.ToggleTrue(ref CraftingGUI.CatchDroppedItems)) {
				CraftingGUI.DroppedItems ??= new();
				CraftingGUI.DroppedItems.Clear();

				DropAttemptInfo attempt = default;
				attempt.rng = Main.rand;
				attempt.player = Main.LocalPlayer;
				//attempt.IsInSimulation = CraftingGUI.SimulatingCrafts;
				attempt.IsExpertMode = Main.expertMode;
				attempt.IsMasterMode = Main.masterMode;

				if (_directRecipeItems.TryGetValue(recipe, out var rules)) {
					foreach (var rule in rules)
						rule.TryDroppingItem(attempt);
				}

				foreach (var conditionalDrop in _conditionalRecipeItems) {
					if (conditionalDrop.condition(recipe))
						conditionalDrop.drop.TryDroppingItem(attempt);
				}
			}

			droppedItems.AddRange(CraftingGUI.DroppedItems);

			return droppedItems;
		}
	}
}
