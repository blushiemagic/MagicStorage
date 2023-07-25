#nullable enable
using MagicStorage.Items;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SerousCommonLib.API;
using System;
using Terraria;
using Terraria.ID;
using ILPlayer = Terraria.IL_Player;

namespace MagicStorage.Edits;

public class MarshmallowStickILEdit : Edit
{
	public override void LoadEdits()
	{
		ILPlayer.ItemCheck_ApplyHoldStyle_Inner += Player_ItemCheck_ApplyHoldStyle_Inner;
	}

	public override void UnloadEdits()
	{
		ILPlayer.ItemCheck_ApplyHoldStyle_Inner -= Player_ItemCheck_ApplyHoldStyle_Inner;
	}

	private static void Player_ItemCheck_ApplyHoldStyle_Inner(ILContext il) {
		ILHelper.CommonPatchingWrapper(il, MagicStorageMod.Instance, throwOnFail: false, PatchMethod);
	}

	private static bool PatchMethod(ILCursor c, ref string badReturnReason) {
		if (!c.TryGotoNext(MoveType.Before,
			i => i.MatchLdarg(0),
			i => i.MatchLdflda<Player>("itemLocation"),
			i => i.MatchLdarg(0),
			i => i.MatchLdflda<Entity>("position"),
			i => i.MatchLdfld<Vector2>("Y"),
			i => i.MatchLdcR4(24),
			i => i.MatchAdd(),
			i => i.MatchLdarg(1),
			i => i.MatchAdd())) {
			badReturnReason = "Could not find instruction sequence for positioning the Marshmallow on a Stick";
			return false;
		}

		ILLabel jumpLabel = c.MarkLabel();

		//Go back to the beginning and find the code that handles the Marshmallow on a Stick usage
		c.Index = 0;

		if (!c.TryGotoNext(MoveType.After,
			i => i.MatchLdarg(2),
			i => i.MatchLdfld<Item>("type"),
			i => i.MatchLdcI4(ItemID.MarshmallowonaStick),
			i => i.MatchBneUn(out _))) {
			badReturnReason = "Could not find instruction sequence for skipping the logic for the Marshmallow on a Stick";
			return false;
		}

		//After the check that the type is valid, but before the actual use code
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate<Func<Player, bool>>(static player => {
			bool isGlobe = player.GetModPlayer<BiomePlayer>().biomeGlobe;

			//Mimic the code that sets the X position of the item since that's being skipped
			if (isGlobe)
				player.itemLocation.X = player.position.X + player.width * 0.5f + 8 * player.direction;

			return isGlobe;
		});
		c.Emit(OpCodes.Brtrue, jumpLabel);

		return true;
	}
}
