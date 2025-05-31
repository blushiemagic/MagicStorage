using MonoMod.Cil;
using SerousCommonLib.API;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Achievements;

namespace MagicStorage.Edits {
	internal class ItemCraftAchievementHijackILEdit : Edit {
		public override void LoadEdits() {
			IL_AchievementsHelper.NotifyItemCraft += AchievementsHelper_NotifyItemCraft;
		}

		public override void UnloadEdits() {
			IL_AchievementsHelper.NotifyItemCraft -= AchievementsHelper_NotifyItemCraft;
		}

		private static void AchievementsHelper_NotifyItemCraft(MonoMod.Cil.ILContext il) => ILHelper.CommonPatchingWrapper(il, MagicStorageMod.Instance, false, HijackEventArgs);

		private static bool HijackEventArgs(ILCursor c, ref string badReturnReason) {
			FieldInfo Item_netID = typeof(Item).GetField(nameof(Item.netID), BindingFlags.Public | BindingFlags.Instance);
			FieldInfo Item_stack = typeof(Item).GetField(nameof(Item.stack), BindingFlags.Public | BindingFlags.Instance);

			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdfld(Item_netID))) {
				badReturnReason = "Could not find access to Item.netID";
				return false;
			}

			c.EmitDelegate<Func<int, int>>(static netID => CraftingGUI.RecipeEventHijack.Args?.NetID ?? netID);

			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdfld(Item_stack))) {
				badReturnReason = "Could not find access to Item.stack";
				return false;
			}

			c.EmitDelegate<Func<int, int>>(static stack => CraftingGUI.RecipeEventHijack.Args?.Stack ?? stack);

			return true;
		}
	}
}
