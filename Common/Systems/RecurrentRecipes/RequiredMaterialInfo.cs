using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public struct RequiredMaterialInfo {
		public readonly int itemOrGroupID;
		private SharedCounter _stack;
		public int Stack => _stack;
		public readonly bool recipeGroup;

		private RequiredMaterialInfo(int itemOrGroupID, SharedCounter stack, bool recipeGroup) {
			this.itemOrGroupID = itemOrGroupID;
			this._stack = stack;
			this.recipeGroup = recipeGroup;
		}

		public static RequiredMaterialInfo FromItem(int type, SharedCounter stack) => new RequiredMaterialInfo(type, stack, false);

		public static RequiredMaterialInfo FromGroup(int groupID, SharedCounter stack) => new RequiredMaterialInfo(groupID, stack, true);

		public static RequiredMaterialInfo FromGroup(RecipeGroup group, SharedCounter stack) => new RequiredMaterialInfo(group.RegisteredId, stack, true);

		public IEnumerable<int> GetValidItems() {
			if (!recipeGroup) {
				// Only one item
				yield return itemOrGroupID;
			} else {
				foreach (int groupItem in RecipeGroup.recipeGroups[itemOrGroupID].ValidItems)
					yield return groupItem;
			}
		}

		public void UpdateStack(int add) => _stack += add;

		public void ClearStack() => _stack.Reset();

		public override bool Equals([NotNullWhen(true)] object obj) {
			return obj is RequiredMaterialInfo other && object.ReferenceEquals(_stack, other._stack) && itemOrGroupID == other.itemOrGroupID && Stack == other.Stack && recipeGroup == other.recipeGroup;
		}

		public bool EqualsIgnoreStack(RequiredMaterialInfo other) {
			return itemOrGroupID == other.itemOrGroupID && recipeGroup == other.recipeGroup;
		}

		public override int GetHashCode() {
			return HashCode.Combine(itemOrGroupID, Stack, recipeGroup);
		}

		public static bool operator ==(RequiredMaterialInfo left, RequiredMaterialInfo right) {
			return left.itemOrGroupID == right.itemOrGroupID && object.ReferenceEquals(left._stack, right._stack) && left.Stack == right.Stack && left.recipeGroup == right.recipeGroup;
		}

		public static bool operator !=(RequiredMaterialInfo left, RequiredMaterialInfo right) {
			return !(left == right);
		}
	}
}
