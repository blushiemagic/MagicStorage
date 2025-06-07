using MagicStorage.Common.DropRules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using SerousCommonLib.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;

namespace MagicStorage.Edits {
	internal class PityDropsDynamicPercentageILEdit : Edit {
		private delegate IItemDropRule PreReportDroprates(IItemDropRule rule, ref PityDropsRule pityRule);
		private delegate void PostReportDroprates(PityDropsRule pityRule, List<DropRateInfo> list, ref List<PityDropsRule> partnerList);
		private delegate ItemDropBestiaryInfoElement LinkElementToRule(ItemDropBestiaryInfoElement element, List<PityDropsRule> partnerList, ref int partnerListIndex);

		public override void LoadEdits() {
			IL_BestiaryDatabase.ExtractDropsForNPC += BestiaryDatabase_ExtractDropsForNPC;
			IL_ItemDropBestiaryInfoElement.ProvideUIElement += ItemDropBestiaryInfoElement_ProvideUIElement;
		}

		public override void UnloadEdits() {
			IL_BestiaryDatabase.ExtractDropsForNPC -= BestiaryDatabase_ExtractDropsForNPC;
			IL_ItemDropBestiaryInfoElement.ProvideUIElement -= ItemDropBestiaryInfoElement_ProvideUIElement;
		}

		private static void BestiaryDatabase_ExtractDropsForNPC(ILContext il) => ILHelper.CommonPatchingWrapper(il, MagicStorageMod.Instance, false, InjectPityDropsContext);

		private static bool InjectPityDropsContext(ILCursor c, ref string badReturnReason) {
			// EDIT: Tracking PityDropsRule with added DropRateInfo lines
			//   By tracking which DropRateInfo was from a PityDropsRule, the corresponding ItemDropBestiaryInfoElement
			//   can be modified dynamically to reflect the PityDropRule's modifications.
			//   This can't just be done in PityDropsRule.ReportDroprates() since that only runs during mod loading
			//   and not during gameplay.

			// Define or find the necessary locals
			int ruleLocal = c.Context.MakeLocalVariable<PityDropsRule>();
			int partnerListLocal = c.Context.MakeLocalVariable<List<PityDropsRule>>();
			int partnerListIndexLocal = c.Context.MakeLocalVariable<int>();
			int infoListLocal = c.Context.Body.Variables.FirstOrDefault(static v => v.VariableType.Is(typeof(List<DropRateInfo>)))?.Index
				?? throw new InvalidOperationException("Failed to find List<IItemDropRule> local variable");

			MethodInfo List_T_IItemDropRule_Enumerator_get_Current = typeof(List<IItemDropRule>.Enumerator).GetProperty(nameof(List<IItemDropRule>.Enumerator.Current)).GetGetMethod()
				?? throw new InvalidOperationException("Failed to resolve property getter for List<T>.Enumerator.Current");

			if (!c.TryGotoNext(MoveType.After, i => i.MatchCall(List_T_IItemDropRule_Enumerator_get_Current))) {
				badReturnReason = "Failed to find access to List<IItemDropRule>.Enumerator.Current";
				return false;
			}

			c.Emit(OpCodes.Ldloca, ruleLocal);

			c.EmitDelegate<PreReportDroprates>(static (IItemDropRule rule, ref PityDropsRule pityRule) => {
				pityRule = rule as PityDropsRule;  // Null if not a PityDropsRule
				return rule;
			});

			MethodInfo IItemDropRule_ReportDroprates = typeof(IItemDropRule).GetMethod(nameof(IItemDropRule.ReportDroprates), BindingFlags.Public | BindingFlags.Instance)
				?? throw new InvalidOperationException("Failed to resolve method IItemDropRule.ReportDroprates()");

			if (!c.TryGotoNext(MoveType.After, i => i.MatchCallvirt(IItemDropRule_ReportDroprates))) {
				badReturnReason = "Failed to find call IItemDropRule.ReportDroprates()";
				return false;
			}

			c.Emit(OpCodes.Ldloc, ruleLocal);
			c.Emit(OpCodes.Ldloc, infoListLocal);
			c.Emit(OpCodes.Ldloca, partnerListLocal);

			c.EmitDelegate<PostReportDroprates>(static (PityDropsRule pityRule, List<DropRateInfo> list, ref List<PityDropsRule> partnerList) => {
				partnerList ??= [];

				if (list.Count > partnerList.Count)
					partnerList.AddRange(Enumerable.Repeat(pityRule, list.Count - partnerList.Count));
			});

			ConstructorInfo ItemDropBestiaryInfoElement_ctor = typeof(ItemDropBestiaryInfoElement).GetConstructor([ typeof(DropRateInfo) ])
				?? throw new InvalidOperationException("Failed to resolve constructor ItemDropBestiaryInfoElement(DropRateInfo)");

			if (!c.TryGotoNext(MoveType.After, i => i.MatchNewobj(ItemDropBestiaryInfoElement_ctor))) {
				badReturnReason = "Failed to find call to ItemDropBestiaryInfoElement constructor";
				return false;
			}

			c.Emit(OpCodes.Ldloc, partnerListLocal);
			c.Emit(OpCodes.Ldloca, partnerListIndexLocal);

			c.EmitDelegate<LinkElementToRule>(static (ItemDropBestiaryInfoElement element, List<PityDropsRule> partnerList, ref int partnerListIndex) => {
				if (partnerList is null)
					return element;  // Somehow, the lists got desynced.  Default to doing nothing.

				if (partnerList[partnerListIndex] is PityDropsRule pityRule)
					PityDropsHandler.MarkElement(element, pityRule);

				partnerListIndex++;
				return element;
			});

			return true;
		}

		private static void ItemDropBestiaryInfoElement_ProvideUIElement(ILContext il) => ILHelper.CommonPatchingWrapper(il, MagicStorageMod.Instance, false, InjectRateAdjustment);

		private static bool InjectRateAdjustment(ILCursor c, ref string badReturnReason) {
			FieldInfo ItemDropBestiaryInfoElement__droprateInfo = typeof(ItemDropBestiaryInfoElement).GetField(nameof(ItemDropBestiaryInfoElement._droprateInfo), BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new InvalidOperationException("Failed to resolve field ItemDropBestiaryInfoElement._droprateInfo");

			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdfld(ItemDropBestiaryInfoElement__droprateInfo))) {
				badReturnReason = "Failed to find access to field ItemDropBestiaryInfoElement._droprateInfo";
				return false;
			}

			c.Emit(OpCodes.Ldarg_0);

			c.EmitDelegate(static (DropRateInfo rateInfo, ItemDropBestiaryInfoElement self) => {
				if (PityDropsHandler.IsAffected(self, out var pityRule))
					PityDropsHandler.ApplyPity(pityRule, ref rateInfo);

				return rateInfo;
			});

			return true;
		}
	}
}
