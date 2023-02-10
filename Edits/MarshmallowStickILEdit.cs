#nullable enable
using System;
using MagicStorage.Items;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ILPlayer = IL.Terraria.Player;

namespace MagicStorage.Edits;

public class MarshmallowStickILEdit : Edit
{
	public override void LoadEdits()
	{
		ILPlayer.ItemCheck_ApplyHoldStyle_Inner += ILPlayerOnItemCheck_ApplyHoldStyle_Inner;
	}

	public override void UnloadEdits()
	{
		ILPlayer.ItemCheck_ApplyHoldStyle_Inner -= ILPlayerOnItemCheck_ApplyHoldStyle_Inner;
	}

	private void ILPlayerOnItemCheck_ApplyHoldStyle_Inner(ILContext il)
	{
		//Ignore this IL edit if not building on release (Stable/Preview)
		//Can't check if tML was built for Debug or Release, so this is the next best thing
		if (BuildInfo.IsDev)
			return;

		//Jump forward and grab an instruction so we can use it as the branch target
		ILCursor c = new(il);

		int patchNum = 1;

		if (!c.TryGotoNext(MoveType.Before,
				i => i.MatchLdarg(0),
				i => i.MatchLdflda<Player>("itemLocation"),
				i => i.MatchLdarg(0),
				i => i.MatchLdflda<Entity>("position"),
				i => i.MatchLdfld<Vector2>("Y"),
				i => i.MatchLdcR4(24),
				i => i.MatchAdd(),
				i => i.MatchLdarg(1),
				i => i.MatchAdd()))
			goto bad_il;

		patchNum++;

		ILLabel jumpLabel = c.MarkLabel();

		//Go back to the beginning and find the code that handles the Marshmallow on a Stick usage
		c.Index = 0;

		if (!c.TryGotoNext(MoveType.After,
				i => i.MatchLdarg(2),
				i => i.MatchLdfld<Item>("type"),
				i => i.MatchLdcI4(ItemID.MarshmallowonaStick),
				i => i.MatchBneUn(out _)))
			goto bad_il;

		patchNum++;

		//After the check that the type is valid, but before the actual use code
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate<Func<Player, bool>>(player =>
		{
			bool isGlobe = player.GetModPlayer<BiomePlayer>().biomeGlobe;

			//Mimic the code that sets the X position of the item since that's being skipped
			if (isGlobe)
				player.itemLocation.X = player.position.X + player.width * 0.5f + 8 * player.direction;

			return isGlobe;
		});
		c.Emit(OpCodes.Brtrue, jumpLabel);

		return;
		bad_il:
		string msg = $"Unable to fully patch {il.Method.Name}()\n" +
			$"Reason: Could not find instruction sequence for patch #{patchNum}";

		if (!BuildInfo.IsDev)
			throw new Exception(msg);
		else
			Mod.Logger.Error(msg);
	}
}
