using MagicStorage.Common.Systems;
using MagicStorage.Items;
using System;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;

namespace MagicStorage.CrossMod.Calls.Functions {
	internal class PreventShadowDiamonDrop : BaseCallFunction<int, bool> {
		protected override bool Handle(int npcID) {
			if (npcID < 0)
				ThrowWithMessage("NPC ID must be positive", 0);
			else if (npcID < NPCID.Count)
				ThrowWithMessage("NPC ID must refer to a modded NPC ID", 0);

			return StorageWorld.disallowDropModded.Add(npcID);
		}
	}

	internal class SetShadowDiamondDropRule : BaseCallFunctionNoReturn<int, IItemDropRule> {
		protected override void Handle(int npcID, IItemDropRule rule) {
			if (npcID < 0)
				ThrowWithMessage("NPC ID must be positive", 0);
			else if (npcID < NPCID.Count)
				ThrowWithMessage("NPC ID must refer to a modded NPC ID", 0);

			if (rule is null)
				ThrowWithMessage("Item drop rule cannot be null", 1);
			
			StorageWorld.moddedDiamondDropRulesByType[npcID] = rule;
		}
	}

	internal class GetShadowDiamondDropRule : BaseCallFunction {
		public override object Call(ReadOnlySpan<object> args) {
			ThrowIfNotEnoughArgs(args, 1);

			return Handle(
				GetOrThrowIfNot<int>(args, 0),
				GetOrThrowIfNotNothing<int>(args, 1)
			);
		}

		private IItemDropRule Handle(int dropNormal, NothingOr<int> dropExpert) {
			if (dropNormal < 0)
				ThrowWithMessage("Normal mode drop stack must be positive", 0);

			return ShadowDiamondDrop.DropDiamond(dropNormal, dropExpert.IsValue ? dropExpert.Value : -1);
		}
	}
}
