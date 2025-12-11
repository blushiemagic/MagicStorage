using MagicStorage.Common.Players;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;

namespace MagicStorage.Common.DropRules {
	public class PityDropsRule : IItemDropRule {
		private readonly CommonDrop _wrappedRule;
		public float strength;

		public int Item => _wrappedRule.itemId;

		internal PityDropsRule(CommonDrop wrappedRule, float strength) {
			ArgumentNullException.ThrowIfNull(wrappedRule);
			_wrappedRule = wrappedRule;
			this.strength = strength;
		}

		public List<IItemDropRuleChainAttempt> ChainedRules { get; } = [];

		public bool CanDrop(DropAttemptInfo info) => _wrappedRule.CanDrop(info);

		public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info) {
			int loops = 1 + (int)(info.player.GetModPlayer<PityLootDrops>().Attempts.GetFailedAttempts(_wrappedRule.itemId) * strength);

			for (int i = 0; i < loops; i++) {
				var result = _wrappedRule.TryDroppingItem(info);

				switch (result.State) {
					case ItemDropAttemptResultState.DoesntFillConditions:
					case ItemDropAttemptResultState.DidNotRunCode:
						// Nothing would happen no matter how many times we try
						return result;
					case ItemDropAttemptResultState.FailedRandomRoll:
						break;
					case ItemDropAttemptResultState.Success:
						if (!info.IsInSimulation)
							info.player.GetModPlayer<PityLootDrops>().Attempts.OnSuccessfulDrop(_wrappedRule.itemId);
						return result;
					default:
						throw new InvalidOperationException("Unexpected value for ItemDropAttemptResultState: " + result.State);
				}
			}

			// Conditions were met, but no roll ended up succeeding
			if (!info.IsInSimulation)
				info.player.GetModPlayer<PityLootDrops>().Attempts.OnFailedDrop(_wrappedRule.itemId);

			ItemDropAttemptResult fail = default;
			fail.State = ItemDropAttemptResultState.FailedRandomRoll;
			return fail;
		}

		public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo) {
			// Unfortunately, ReportDroprates is called during mod loading and not gameplay
			// Hence, rate adjustment has to be done when the corresponding UI elements are created instead
			_wrappedRule.ReportDroprates(drops, ratesInfo);
		}
	}

	public static class PityDropsHandler {
		public static IItemDropRule WithPityDrops(this IItemDropRule rule, float strength = 1f) {
			if (rule is not CommonDrop commonDrop)
				throw new ArgumentException($"{nameof(WithPityDrops)} can only be used with rules that derive from {typeof(CommonDrop).FullName}", nameof(rule));

			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(strength);

			return new PityDropsRule(commonDrop, strength);
		}

		public static float GetModifiedRate(Player player, int itemID, float baseRate, float ruleStrength) {
			ArgumentNullException.ThrowIfNull(player);

			if (Main.gameMenu)
				return baseRate;

			// Given a drop rate X and up to N loops, the chance that a drop will succeed is: 1-(1-X)^N
			float X = baseRate;
			int N = 1 + (int)(player.GetModPlayer<PityLootDrops>().Attempts.GetFailedAttempts(itemID) * ruleStrength);
			return 1 - (float)Math.Pow(1 - X, N);
		}

		public static float GetRateMultiplier(Player player, int itemID, float baseRate, float ruleStrength) {
			float modifiedRate = GetModifiedRate(player, itemID, baseRate, ruleStrength);

			// Recalculate the rate as a factor multiplied to the original rate
			return modifiedRate / baseRate;
		}

		internal static void ApplyPity(PityDropsRule rule, ref DropRateInfo info) {
			ArgumentNullException.ThrowIfNull(rule);

			if (Main.gameMenu || rule.Item != info.itemId)
				return;

			info.dropRate = Math.Clamp(GetModifiedRate(Main.LocalPlayer, rule.Item, info.dropRate, rule.strength), 0f, 1f);
		}

		private static readonly ConditionalWeakTable<ItemDropBestiaryInfoElement, PityDropsILContext> _affectedElements = [];

		internal static void MarkElement(ItemDropBestiaryInfoElement element, PityDropsRule rule) => _affectedElements.Add(element, new PityDropsILContext(rule));

		internal static bool IsAffected(ItemDropBestiaryInfoElement element, out PityDropsRule rule) {
			if (!_affectedElements.TryGetValue(element, out var context)) {
				rule = null;
				return false;
			}

			rule = context.rule;
			return true;
		}
	}

	internal class PityDropsILContext(PityDropsRule rule) {
		public readonly PityDropsRule rule = rule;
	}
}
