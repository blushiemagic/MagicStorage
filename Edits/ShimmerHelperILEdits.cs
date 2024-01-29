using MagicStorage.Common.Systems.Shimmering;
using MonoMod.Cil;
using SerousCommonLib.API;
using System;
using System.Reflection;
using Terraria;
using IL_Item = Terraria.IL_Item;

namespace MagicStorage.Edits {
	internal class ShimmerHelperILEdits : Edit {
		public override void LoadEdits() {
			IL_Item.FindDecraftAmount += Item_FindDecraftAmount;
		//	IL_Item.CanShimmer += Item_CanShimmer;
		}

		public override void UnloadEdits() {
			IL_Item.FindDecraftAmount -= Item_FindDecraftAmount;
		//	IL_Item.CanShimmer -= Item_CanShimmer;
		}

		private static readonly FieldInfo Item_stack = typeof(Item).GetField(nameof(Item.stack), BindingFlags.Public | BindingFlags.Instance);

		private static void Item_FindDecraftAmount(ILContext il) {
			ILHelper.CommonPatchingWrapper(il, MagicStorageMod.Instance, false, ConditionallyOverrideStack);
		}

		private static bool ConditionallyOverrideStack(ILCursor c, ref string badReturnReason) {
			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdfld(Item_stack))) {
				badReturnReason = "Could not find stack field access";
				return false;
			}

			c.EmitDelegate<Func<int, int>>(static stack => ShimmerMetrics.DecraftAmountStackOverride ?? stack);

			return true;
		}

		/*
		private static readonly FieldInfo Item_makeNPC = typeof(Item).GetField(nameof(Item.makeNPC), BindingFlags.Public | BindingFlags.Instance);

		private static void Item_CanShimmer(ILContext il) {
			ILHelper.CommonPatchingWrapper(il, MagicStorageMod.Instance, false, ConditionallyOverrideMakeNPC);
		}

		private static bool ConditionallyOverrideMakeNPC(ILCursor c, ref string badReturnReason) {
			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdfld(Item_makeNPC))) {
				badReturnReason = "Could not find makeNPC field access";
				return false;
			}

			c.EmitDelegate<Func<int, int>>(static makeNPC => ShimmerHelper.IgnoreMakeNPC ? -1 : makeNPC);

			return true;
		}
		*/
	}
}
