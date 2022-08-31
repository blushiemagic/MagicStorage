using MagicStorage.Common.Systems;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace MagicStorage.Edits {
	internal class OpCommandCodeSendHandlerDetour : Edit {
		public override void LoadEdits() {
			IL.Terraria.Main.DoUpdate_HandleChat += Main_DoUpdate_HandleChat;
		}

		public override void UnloadEdits() {
			IL.Terraria.Main.DoUpdate_HandleChat -= Main_DoUpdate_HandleChat;
		}

		private void Main_DoUpdate_HandleChat(ILContext il) {
			ILHelper.CommonPatchingWrapper(il, DoCommonPatching);
		}

		private static bool DoCommonPatching(ILCursor c, ref string badReturnReason) {
			int patchNum = 1;

			ILLabel oldLabel = null;
			if (!c.TryGotoNext(MoveType.After, i => i.MatchBrtrue(out oldLabel),
				i => i.MatchRet()))
				goto bad_il;

			patchNum++;

			Instruction oldTarget = oldLabel.Target;

			List<ILLabel> labels = c.IncomingLabels.ToList();

			int index = c.Index;

			c.EmitDelegate(() => Main.netMode == NetmodeID.MultiplayerClient && Netcode.RequestingOperatorKey);
			
			Instruction instr = c.Instrs[index];

			foreach (var label in labels)
				label.Target = instr;

			ILLabel postRequest = c.DefineLabel();
			postRequest.Target = oldTarget;

			c.Emit(OpCodes.Brfalse, postRequest);
			c.EmitDelegate(() => {
				NetHelper.ClientSendOperatorKey(Main.chatText);
				Main.chatText = "";
				Main.ClosePlayerChat();
				Main.chatRelease = false;
				SoundEngine.PlaySound(SoundID.MenuClose);
			});
			c.Emit(OpCodes.Ret);

			return true;

			bad_il:
			badReturnReason += "\nReason: Could not find instruction sequence for patch #" + patchNum;
			return false;
		}
	}
}
