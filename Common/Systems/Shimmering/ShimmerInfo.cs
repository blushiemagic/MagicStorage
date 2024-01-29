using System.Collections.Generic;
using System;
using Terraria.GameContent;
using Terraria.ID;
using Terraria;

namespace MagicStorage.Common.Systems.Shimmering {
	public readonly struct ShimmerInfo {
		public enum ShimmerAttemptResult {
			None,
			CoinLuck,
			TransmutedItem,
			NPCSpawn,
			DecraftedItem
		}

		public readonly int iconicItem;
		public readonly int actualItem;
		private readonly bool _isNullItem;

		private ShimmerInfo(int item, int iconicItem, bool isNullItem) {
			actualItem = item;
			this.iconicItem = iconicItem;
			_isNullItem = isNullItem;
		}
		
		public static ShimmerInfo Create(int type) {
			return ContentSamples.ItemsByType[type].IsAir
				? new ShimmerInfo(type, type, isNullItem: true)
				: new ShimmerInfo(type, ShimmerMetrics.GetShimmerEquivalentType(type), isNullItem: false);
		}

		public ShimmerAttemptResult GetAttempt(out int decraftingRecipeIndex) {
			decraftingRecipeIndex = -1;

			if (_isNullItem)
				return ShimmerAttemptResult.None;

			var sample = ContentSamples.ItemsByType[iconicItem];

			if (!sample.CanShimmer())
				return ShimmerAttemptResult.None;

			if (iconicItem is ItemID.CopperCoin or ItemID.SilverCoin or ItemID.GoldCoin or ItemID.PlatinumCoin || ItemID.Sets.CoinLuckValue[iconicItem] > 0)
				return ShimmerAttemptResult.CoinLuck;

			if (iconicItem is ItemID.RodofDiscord or ItemID.Clentaminator or ItemID.BottomlessBucket or ItemID.BottomlessShimmerBucket or ItemID.LunarBrick)
				return ShimmerAttemptResult.TransmutedItem;

			if (iconicItem is ItemID.GelBalloon || sample.makeNPC > NPCID.None)
				return ShimmerAttemptResult.NPCSpawn;

			if (ItemID.Sets.ShimmerTransformToItem[iconicItem] > ItemID.None)
				return ShimmerAttemptResult.TransmutedItem;

			decraftingRecipeIndex = ShimmerTransforms.GetDecraftingRecipeIndex(iconicItem);
			if (decraftingRecipeIndex > -1) {
				// Recipes may have special requirements for decrafting, but the call to CanShimmer already checks for this
				//   since it calls ShimmerTransforms.IsItemTransformationLocked()
				return ShimmerAttemptResult.DecraftedItem;
			}

			return ShimmerAttemptResult.None;
		}

		public IShimmerResult GetResult() {
			var attempt = GetAttempt(out int decraftingRecipeIndex);

			return attempt switch {
				ShimmerAttemptResult.None => null,
				ShimmerAttemptResult.CoinLuck => new CoinLuck(),
				ShimmerAttemptResult.TransmutedItem => new TransformItem(),
				ShimmerAttemptResult.NPCSpawn => new NPCSpawn(),
				ShimmerAttemptResult.DecraftedItem => new Decraft(decraftingRecipeIndex),
				_ => throw new InvalidOperationException($"Unexpected {nameof(ShimmerAttemptResult)} value {attempt}")
			};
		}

		public IEnumerable<IShimmerResultReport> GetShimmerReports() => GetResult()?.GetShimmerReports(ContentSamples.ItemsByType[actualItem], iconicItem) ?? Array.Empty<IShimmerResultReport>();
	}
}
