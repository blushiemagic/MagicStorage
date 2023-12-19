using Mono.Cecil.Cil;
using MonoMod.Cil;
using SerousCommonLib.API;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.UI;
using IL_ItemSlot = Terraria.UI.IL_ItemSlot;

namespace MagicStorage.Edits {
	internal class ItemToStorageCursorOverrideILEdit : Edit {
		public override void LoadEdits() {
			IL_ItemSlot.OverrideHover_ItemArray_int_int += ItemSlot_OverrideHover;
		}

		public override void UnloadEdits() {
			IL_ItemSlot.OverrideHover_ItemArray_int_int -= ItemSlot_OverrideHover;
		}

		private static void ItemSlot_OverrideHover(ILContext il) {
			ILHelper.CommonPatchingWrapper(il, MagicStorageMod.Instance, false, Patch_ItemSlot_OverrideHover);
		}

		private static readonly FieldInfo ItemSlot_Options_DisableQuickTrash = typeof(ItemSlot.Options).GetField(nameof(ItemSlot.Options.DisableQuickTrash), BindingFlags.Public | BindingFlags.Static);
		private static readonly FieldInfo Main_cursorOverride = typeof(Main).GetField(nameof(Main.cursorOverride), BindingFlags.Public | BindingFlags.Static);

		private static bool Patch_ItemSlot_OverrideHover(ILCursor c, ref string badReturnReason) {
			ILLabel afterSwitchBlock = null;
			if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdsfld(ItemSlot_Options_DisableQuickTrash),
				i => i.MatchBrtrue(out afterSwitchBlock),
				i => i.MatchLdcI4(CursorOverrideID.TrashCan),
				i => i.MatchStsfld(Main_cursorOverride),
				i => i.MatchBr(out _))) {
				badReturnReason = "Could not find clause for setting trash cursor icon";
				return false;
			}

			// Capture the incoming labels before the edit
			TargetContext ctx = new TargetContext(c);

			// Next currently points to the start of the old block
			ILLabel oldBlock = c.DefineLabel();
			oldBlock.Target = c.Next;

			// Inject the new code
			/*   } else if (ShowItemToChestIcon()) {
			 *       Main.cursorOverride = CursorOverrideID.InventoryToChest;
			 *   }
			 */
			c.EmitDelegate(ShowItemToChestIconForStorage);
			c.Emit(OpCodes.Brfalse, oldBlock);
			c.Emit(OpCodes.Ldc_I4, CursorOverrideID.InventoryToChest);
			c.Emit(OpCodes.Stsfld, Main_cursorOverride);
			c.Emit(OpCodes.Br, afterSwitchBlock);

			// Update the labels
			ctx.UpdateInstructions();

			return true;
		}

		private static bool ShowItemToChestIconForStorage() {
			return StoragePlayer.LocalPlayer.ViewingStorage().X >= 0;
		}
	}
}
