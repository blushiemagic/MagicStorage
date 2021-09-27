using MagicStorage.Items;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria;

namespace MagicStorage.Edits.MSIL{
	internal static partial class Vanilla{
		internal static void Player_ItemCheck_ApplyHoldStyle_Inner(ILContext il){
			//Jump forward and grab an instruction so we can use it as the branch target
			FieldInfo Player_itemLocation = typeof(Player).GetField("itemLocation", BindingFlags.Public | BindingFlags.Instance);
			FieldInfo Entity_position = typeof(Entity).GetField("position", BindingFlags.Public | BindingFlags.Instance);
			FieldInfo Vector2_Y = typeof(Vector2).GetField("Y", BindingFlags.Public | BindingFlags.Instance);
			FieldInfo Item_type = typeof(Item).GetField("type", BindingFlags.Public | BindingFlags.Instance);

			ILCursor c = new ILCursor(il);

			if(!c.TryGotoNext(MoveType.Before, i => i.MatchLdarg(0),
				i => i.MatchLdflda(Player_itemLocation),
				i => i.MatchLdarg(0),
				i => i.MatchLdflda(Entity_position),
				i => i.MatchLdfld(Vector2_Y),
				i => i.MatchLdcR4(24),
				i => i.MatchAdd(),
				i => i.MatchLdarg(1),
				i => i.MatchAdd()))
				goto bad_il;

			ILLabel jumpLabel = c.MarkLabel();

			//Go back to the beginning and find the code that handles the Marshmallow on a Stick usage
			c.Index = 0;

			if(!c.TryGotoNext(MoveType.After, i => i.MatchLdarg(2),
				i => i.MatchLdfld(Item_type),
				i => i.MatchLdcI4(968),
				i => i.MatchCeq(),
				i => i.MatchStloc(27),
				i => i.MatchLdloc(27),
				i => i.MatchBrfalse(out _)))
				goto bad_il;

			//After the check that the type is valid, but before the actual use code
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<Player, bool>>(player => {
				bool isGlobe = player.GetModPlayer<BiomePlayer>().biomeGlobe;

				//Mimic the code that sets the X position of the item since that's being skipped
				if(isGlobe)
					player.itemLocation.X = player.position.X + player.width * 0.5f + 8 * player.direction;

				return isGlobe;
			});
			c.Emit(OpCodes.Brtrue, jumpLabel);

			ILHelper.UpdateInstructionOffsets(c);

			return;
bad_il:
			MagicStorage.Instance.Logger.Error("Unable to fully patch Terraria.Player.ItemCheck_ApplyHoldStyle_Inner()");
		}
	}
}
