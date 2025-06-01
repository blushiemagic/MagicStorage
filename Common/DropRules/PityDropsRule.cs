using MagicStorage.Common.Players;
using MagicStorage.Common.Systems;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;

namespace MagicStorage.Common.DropRules {
	public class PityDropsRule : IItemDropRule {
		private readonly CommonDrop _wrappedRule;
		public float strength;

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
			// Give a drop rate X and up to N loops, the chance that a drop will succeed is: 1-(1-X)^N
			// Recalculating as a factor multiplied to X, this ends up being: (1-(1-X)^N)/X
			float X = (float)_wrappedRule.chanceNumerator / _wrappedRule.chanceDenominator;
			int N = 1 + (int)(Main.LocalPlayer.GetModPlayer<PityLootDrops>().Attempts.GetFailedAttempts(_wrappedRule.itemId) * strength);
			float pityFactor = (1 - (float)Math.Pow(1 - X, N)) / X;

			// Standard CommonDrop logic reports the rate as "(numerator/denominator) * parentDroprateChance", so
			//   multiplying the pity factor will cancel out the original X and give us the new increased rate.
			ratesInfo.parentDroprateChance *= pityFactor;

			_wrappedRule.ReportDroprates(drops, ratesInfo);
		}
	}

	public static class PityDropsExtensions {
		public static IItemDropRule WithPityDrops(this IItemDropRule rule, float strength = 1f) {
			if (rule is not CommonDrop commonDrop)
				throw new ArgumentException($"{nameof(WithPityDrops)} can only be used with rules that derive from {typeof(CommonDrop).FullName}", nameof(rule));

			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(strength);

			return new PityDropsRule(commonDrop, strength);
		}
	}
}
