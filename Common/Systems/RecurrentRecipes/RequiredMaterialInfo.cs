using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Terraria;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public readonly struct RequiredMaterialInfo {
		public readonly int itemOrGroupID;
		public readonly int stack;
		public readonly bool recipeGroup;

		private RequiredMaterialInfo(int itemOrGroupID, int stack, bool recipeGroup) {
			this.itemOrGroupID = itemOrGroupID;
			this.stack = stack;
			this.recipeGroup = recipeGroup;
		}

		public static RequiredMaterialInfo FromItem(int type, int stack) => new RequiredMaterialInfo(type, stack, false);

		public static RequiredMaterialInfo FromItem(Item item) => new RequiredMaterialInfo(item.type, item.stack, false);

		public static RequiredMaterialInfo FromGroup(int groupID, int stack) => new RequiredMaterialInfo(groupID, stack, true);

		public static RequiredMaterialInfo FromGroup(RecipeGroup group, int stack) => new RequiredMaterialInfo(group.RegisteredId, stack, true);

		public IEnumerable<int> GetValidItems() {
			if (!recipeGroup) {
				// Only one item
				yield return itemOrGroupID;
			} else {
				foreach (int groupItem in RecipeGroup.recipeGroups[itemOrGroupID].ValidItems)
					yield return groupItem;
			}
		}

		public RequiredMaterialInfo UpdateStack(int add) {
			return new RequiredMaterialInfo(itemOrGroupID, stack + add, recipeGroup);
		}

		public RequiredMaterialInfo SetStack(int newStack) {
			return new RequiredMaterialInfo(itemOrGroupID, newStack, recipeGroup);
		}

		public override bool Equals([NotNullWhen(true)] object obj) {
			return obj is RequiredMaterialInfo other && itemOrGroupID == other.itemOrGroupID && stack == other.stack && recipeGroup == other.recipeGroup;
		}

		public bool EqualsIgnoreStack(RequiredMaterialInfo other) {
			return itemOrGroupID == other.itemOrGroupID && recipeGroup == other.recipeGroup;
		}

		public override int GetHashCode() {
			return HashCode.Combine(itemOrGroupID, stack, recipeGroup);
		}

		public static bool operator ==(RequiredMaterialInfo left, RequiredMaterialInfo right) {
			return left.itemOrGroupID == right.itemOrGroupID && left.stack == right.stack && left.recipeGroup == right.recipeGroup;
		}

		public static bool operator !=(RequiredMaterialInfo left, RequiredMaterialInfo right) {
			return !(left == right);
		}
	}
}
