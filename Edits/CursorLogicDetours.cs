using MagicStorage.Common.Systems;
using SerousCommonLib.API;
using OnMain = On.Terraria.Main;

namespace MagicStorage.Edits {
	internal class CursorLogicDetours : Edit {
		public override void LoadEdits()
		{
			OnMain.DrawInterface_36_Cursor += Main_DrawInterface_36_Cursor;
		}

		private void Main_DrawInterface_36_Cursor(OnMain.orig_DrawInterface_36_Cursor orig) {
			if (MagicUI.MouseCache.didBlockActions) {
				//Prevent the cursor from being drawn by the MouseText methods until we're good and ready to do so
				return;
			}

			orig();
		}

		public override void UnloadEdits()
		{
			OnMain.DrawInterface_36_Cursor -= Main_DrawInterface_36_Cursor;
		}
	}
}
